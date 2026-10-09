import { HttpClient, HttpContext } from '@angular/common/http';
import { inject, Service } from '@angular/core';
import { catchError, map, type Observable } from 'rxjs';

import { environment } from '../../../../environments/environment';
import { SKIP_GLOBAL_LOADING } from '../../../constants/global-loading-context.tokens';
import { WeightEntriesSdk } from '../../../shared/api/sdk/generated/api/weight-entries.service';
import { weightEntryFromSdk, weightPageSummaryFromSdk, weightSummaryFromSdk } from '../../../shared/api/sdk/measurement-sdk.mapper';
import { createSdkConnection, sdkRequestOptions } from '../../../shared/api/sdk/sdk-connection';
import { sdkOptional } from '../../../shared/api/sdk/sdk-response';
import { fallbackApiError, rethrowApiError } from '../../../shared/lib/api-error.utils';
import { MEASUREMENT_HISTORY_FETCH_LIMIT } from '../../../shared/measurements/measurement-history.constants';
import type { CalendarDate } from '../../../shared/models/semantics/date-value';
import type { WeightEntryId } from '../../../shared/models/semantics/entity-id';
import type {
    CreateWeightEntryPayload,
    UpdateWeightEntryPayload,
    WeightEntry,
    WeightEntryFilters,
    WeightEntrySummaryFilters,
    WeightEntrySummaryPoint,
    WeightHistoryPageSummary,
    WeightHistoryPageSummaryFilters,
} from '../../../shared/models/weight-entry.data';

@Service()
export class WeightEntriesService {
    protected readonly baseUrl = environment.apiUrls.weights;
    private readonly sdk = createSdkConnection(WeightEntriesSdk, this.baseUrl, inject(HttpClient));

    public getEntries(filters?: WeightEntryFilters): Observable<WeightEntry[]> {
        return this.sdk.client
            .getWeightEntries({
                version: this.sdk.version,
                ...filters,
                dateFrom: filters?.dateFrom === '' ? undefined : filters?.dateFrom,
                dateTo: filters?.dateTo === '' ? undefined : filters?.dateTo,
            })
            .pipe(
                map(entries => entries.map(weightEntryFromSdk)),
                catchError((error: unknown) => fallbackApiError('Weight entries fetch error', error, [])),
            );
    }

    public getHistoryPage(dateTo?: CalendarDate): Observable<WeightEntry[]> {
        return this.sdk.client
            .getWeightEntries(
                { version: this.sdk.version, limit: MEASUREMENT_HISTORY_FETCH_LIMIT, sort: 'desc', dateTo },
                'body',
                false,
                sdkRequestOptions(undefined, new HttpContext().set(SKIP_GLOBAL_LOADING, true)),
            )
            .pipe(map(entries => entries.map(weightEntryFromSdk)));
    }

    public getLatest(): Observable<WeightEntry | null> {
        return this.sdk.client.getWeightEntriesLatest({ version: this.sdk.version }).pipe(
            map(value => sdkOptional(value, weightEntryFromSdk)),
            catchError((error: unknown) => fallbackApiError('Weight latest fetch error', error, null)),
        );
    }

    public create(payload: CreateWeightEntryPayload): Observable<WeightEntry> {
        return this.sdk.client.postWeightEntries({ version: this.sdk.version, createWeightEntryHttpRequest: payload }).pipe(
            map(weightEntryFromSdk),
            catchError((error: unknown) => rethrowApiError('Create weight entry error', error)),
        );
    }

    public update(id: WeightEntryId, payload: UpdateWeightEntryPayload): Observable<WeightEntry> {
        return this.sdk.client.putWeightEntriesById({ version: this.sdk.version, id, updateWeightEntryHttpRequest: payload }).pipe(
            map(weightEntryFromSdk),
            catchError((error: unknown) => rethrowApiError('Update weight entry error', error)),
        );
    }

    public remove(id: WeightEntryId): Observable<void> {
        return this.sdk.client.deleteWeightEntriesById({ version: this.sdk.version, id }).pipe(
            map(() => {}),
            catchError((error: unknown) => rethrowApiError('Delete weight entry error', error)),
        );
    }

    public getSummary(filters: WeightEntrySummaryFilters): Observable<WeightEntrySummaryPoint[]> {
        return this.sdk.client.getWeightEntriesSummary({ version: this.sdk.version, ...filters }).pipe(
            map(points => points.map(weightSummaryFromSdk)),
            catchError((error: unknown) => rethrowApiError('Weight summary fetch error', error)),
        );
    }

    public getPageSummary(filters: WeightHistoryPageSummaryFilters): Observable<WeightHistoryPageSummary> {
        return this.sdk.client.getWeightEntriesPageSummary({ version: this.sdk.version, ...filters }).pipe(
            map(weightPageSummaryFromSdk),
            catchError((error: unknown) => rethrowApiError('Weight history page summary fetch error', error)),
        );
    }
}
