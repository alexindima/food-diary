import { HttpClient } from '@angular/common/http';
import { inject, Service } from '@angular/core';
import { map, type Observable } from 'rxjs';

import { environment } from '../../../../environments/environment';
import { StatisticsSdk } from '../../../shared/api/sdk/generated/api/statistics.service';
import { waistSummaryFromSdk, weightSummaryFromSdk } from '../../../shared/api/sdk/measurement-sdk.mapper';
import { createSdkConnection } from '../../../shared/api/sdk/sdk-connection';
import { requireSdkFields } from '../../../shared/api/sdk/sdk-response';
import type { AggregatedStatistics, GetStatisticsDto, GetStatisticsSummaryDto, StatisticsSummary } from '../models/statistics.data';
import { aggregatedStatisticsFromSdk } from './statistics-sdk.mapper';

@Service()
export class StatisticsService {
    protected readonly baseUrl = environment.apiUrls.statistics;
    private readonly sdk = createSdkConnection(StatisticsSdk, this.baseUrl, inject(HttpClient));

    public getAggregatedStatistics(params: GetStatisticsDto): Observable<AggregatedStatistics[]> {
        const queryParams = {
            ...params,
            version: this.sdk.version,
            dateFrom: this.toIsoString(params.dateFrom),
            dateTo: this.toIsoString(params.dateTo),
        };

        return this.sdk.client.getStatistics(queryParams).pipe(map(values => values.map(aggregatedStatisticsFromSdk)));
    }

    public getSummary(params: GetStatisticsSummaryDto): Observable<StatisticsSummary> {
        const queryParams = {
            ...params,
            version: this.sdk.version,
            dateFrom: this.toIsoString(params.dateFrom),
            dateTo: this.toIsoString(params.dateTo),
        };

        return this.sdk.client.getStatisticsSummary(queryParams).pipe(
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

    private toIsoString(value: Date | string): string {
        if (typeof value === 'string') {
            const parsed = new Date(value);
            return parsed.toISOString();
        }
        return value.toISOString();
    }
}
