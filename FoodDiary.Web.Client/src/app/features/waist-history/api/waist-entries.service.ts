import { HttpClient, HttpContext } from '@angular/common/http';
import { inject, Service } from '@angular/core';
import { catchError, map, type Observable } from 'rxjs';

import { environment } from '../../../../environments/environment';
import { SKIP_GLOBAL_LOADING } from '../../../constants/global-loading-context.tokens';
import { WaistEntriesSdk } from '../../../shared/api/sdk/generated/api/waist-entries.service';
import { waistEntryFromSdk, waistPageSummaryFromSdk, waistSummaryFromSdk } from '../../../shared/api/sdk/measurement-sdk.mapper';
import { createSdkConnection, sdkRequestOptions } from '../../../shared/api/sdk/sdk-connection';
import { sdkOptional } from '../../../shared/api/sdk/sdk-response';
import { fallbackApiError, rethrowApiError } from '../../../shared/lib/api-error.utils';
import { MEASUREMENT_HISTORY_FETCH_LIMIT } from '../../../shared/measurements/measurement-history.constants';
import type { CalendarDate } from '../../../shared/models/semantics/date-value';
import type { WaistEntryId } from '../../../shared/models/semantics/entity-id';
import type {
    CreateWaistEntryPayload,
    UpdateWaistEntryPayload,
    WaistEntry,
    WaistEntryFilters,
    WaistEntrySummaryFilters,
    WaistEntrySummaryPoint,
    WaistHistoryPageSummary,
    WaistHistoryPageSummaryFilters,
} from '../../../shared/models/waist-entry.data';

@Service()
export class WaistEntriesService {
    protected readonly baseUrl = environment.apiUrls.waists;
    private readonly sdk = createSdkConnection(WaistEntriesSdk, this.baseUrl, inject(HttpClient));

    public getEntries(filters?: WaistEntryFilters): Observable<WaistEntry[]> {
        return this.sdk.client
            .getWaistEntries({
                version: this.sdk.version,
                ...filters,
                dateFrom: filters?.dateFrom === '' ? undefined : filters?.dateFrom,
                dateTo: filters?.dateTo === '' ? undefined : filters?.dateTo,
            })
            .pipe(
                map(entries => entries.map(waistEntryFromSdk)),
                catchError((error: unknown) => fallbackApiError('Waist entries fetch error', error, [])),
            );
    }

    public getHistoryPage(dateTo?: CalendarDate): Observable<WaistEntry[]> {
        return this.sdk.client
            .getWaistEntries(
                { version: this.sdk.version, limit: MEASUREMENT_HISTORY_FETCH_LIMIT, sort: 'desc', dateTo },
                'body',
                false,
                sdkRequestOptions(undefined, new HttpContext().set(SKIP_GLOBAL_LOADING, true)),
            )
            .pipe(map(entries => entries.map(waistEntryFromSdk)));
    }

    public getLatest(): Observable<WaistEntry | null> {
        return this.sdk.client.getWaistEntriesLatest({ version: this.sdk.version }).pipe(
            map(value => sdkOptional(value, waistEntryFromSdk)),
            catchError((error: unknown) => fallbackApiError('Waist latest fetch error', error, null)),
        );
    }

    public create(payload: CreateWaistEntryPayload): Observable<WaistEntry> {
        return this.sdk.client
            .postWaistEntries({ version: this.sdk.version, idempotencyKey: crypto.randomUUID(), createWaistEntryHttpRequest: payload })
            .pipe(
                map(waistEntryFromSdk),
                catchError((error: unknown) => rethrowApiError('Create waist entry error', error)),
            );
    }

    public update(id: WaistEntryId, payload: UpdateWaistEntryPayload): Observable<WaistEntry> {
        return this.sdk.client.putWaistEntriesById({ version: this.sdk.version, id, updateWaistEntryHttpRequest: payload }).pipe(
            map(waistEntryFromSdk),
            catchError((error: unknown) => rethrowApiError('Update waist entry error', error)),
        );
    }

    public remove(id: WaistEntryId): Observable<void> {
        return this.sdk.client.deleteWaistEntriesById({ version: this.sdk.version, id }).pipe(
            map(() => {}),
            catchError((error: unknown) => rethrowApiError('Delete waist entry error', error)),
        );
    }

    public getSummary(filters: WaistEntrySummaryFilters): Observable<WaistEntrySummaryPoint[]> {
        return this.sdk.client.getWaistEntriesSummary({ version: this.sdk.version, ...filters }).pipe(
            map(points => points.map(waistSummaryFromSdk)),
            catchError((error: unknown) => rethrowApiError('Waist summary fetch error', error)),
        );
    }

    public getPageSummary(filters: WaistHistoryPageSummaryFilters): Observable<WaistHistoryPageSummary> {
        return this.sdk.client.getWaistEntriesPageSummary({ version: this.sdk.version, ...filters }).pipe(
            map(waistPageSummaryFromSdk),
            catchError((error: unknown) => rethrowApiError('Waist history page summary fetch error', error)),
        );
    }
}
