import { HttpClient, type HttpContext } from '@angular/common/http';
import { inject, Service } from '@angular/core';
import { forkJoin, map, type Observable } from 'rxjs';

import { environment } from '../../../../environments/environment';
import { StatisticsSdk } from '../../../shared/api/sdk/generated/api/statistics.service';
import { WaistEntriesSdk } from '../../../shared/api/sdk/generated/api/waist-entries.service';
import { WeightEntriesSdk } from '../../../shared/api/sdk/generated/api/weight-entries.service';
import { waistSummaryFromSdk, weightSummaryFromSdk } from '../../../shared/api/sdk/measurement-sdk.mapper';
import { createSdkConnection } from '../../../shared/api/sdk/sdk-connection';
import { requireSdkFields } from '../../../shared/api/sdk/sdk-response';
import type {
    AggregatedStatistics,
    GetStatisticsDto,
    GetStatisticsSummaryDto,
    StatisticsBodyMeasurements,
    StatisticsMeasurements,
    StatisticsSummary,
} from '../models/statistics.data';
import { aggregatedStatisticsFromSdk } from './statistics-sdk.mapper';

const MEASUREMENT_PERIOD_FETCH_LIMIT = 500;

@Service()
export class StatisticsService {
    protected readonly baseUrl = environment.apiUrls.statistics;
    private readonly sdk = createSdkConnection(StatisticsSdk, this.baseUrl, inject(HttpClient));
    private readonly weights = createSdkConnection(WeightEntriesSdk, environment.apiUrls.weights, inject(HttpClient));
    private readonly waists = createSdkConnection(WaistEntriesSdk, environment.apiUrls.waists, inject(HttpClient));

    public getAggregatedStatistics(params: GetStatisticsDto): Observable<AggregatedStatistics[]> {
        const queryParams = {
            ...params,
            version: this.sdk.version,
            dateFrom: this.toIsoString(params.dateFrom),
            dateTo: this.toIsoString(params.dateTo),
        };

        return this.sdk.client.getStatistics(queryParams).pipe(map(values => values.map(aggregatedStatisticsFromSdk)));
    }

    public getSummary(params: GetStatisticsSummaryDto, context?: HttpContext): Observable<StatisticsSummary> {
        const queryParams = {
            ...params,
            version: this.sdk.version,
            dateFrom: this.toIsoString(params.dateFrom),
            dateTo: this.toIsoString(params.dateTo),
        };
        return this.sdk.client.getStatisticsSummary(queryParams, 'body', false, { context }).pipe(
            map(response => {
                const value = requireSdkFields(response, ['nutrition', 'weight', 'waist']);
                return {
                    nutrition: value.nutrition.map(aggregatedStatisticsFromSdk),
                    weight: value.weight.map(weightSummaryFromSdk),
                    waist: value.waist.map(waistSummaryFromSdk),
                };
            }),
        );
    }

    public getBodyMeasurements(params: GetStatisticsSummaryDto): Observable<StatisticsMeasurements> {
        // A period is at most 366 days and each metric has at most one entry per calendar day.
        // Read exact measurements separately from chart buckets so counts and latest values stay precise.
        const entryParams = {
            version: this.sdk.version,
            dateFrom: params.bodyDateFrom ?? this.toIsoString(params.dateFrom),
            dateTo: params.bodyDateTo ?? this.toIsoString(params.dateTo),
            limit: MEASUREMENT_PERIOD_FETCH_LIMIT,
            sort: 'asc' as const,
        };
        return forkJoin({
            weight: this.weights.client.getWeightEntries(entryParams),
            waist: this.waists.client.getWaistEntries(entryParams),
        }).pipe(
            map(({ weight, waist }) => ({
                weightMeasurements: summarizeMeasurements(weight.map(entry => requireSdkFields(entry, ['weightKg']).weightKg)),
                waistMeasurements: summarizeMeasurements(waist.map(entry => requireSdkFields(entry, ['circumferenceCm']).circumferenceCm)),
            })),
        );
    }

    private toIsoString(value: Date | string): string {
        if (typeof value === 'string') {
            const parsed = new Date(value);
            return parsed.toISOString();
        }
        return value.toISOString();
    }
}

function summarizeMeasurements(values: readonly number[]): StatisticsBodyMeasurements {
    return { first: values[0] ?? null, last: values.at(-1) ?? null, count: values.length };
}
