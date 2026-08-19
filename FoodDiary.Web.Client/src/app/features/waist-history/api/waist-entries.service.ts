import { HttpHeaders } from '@angular/common/http';
import { Service } from '@angular/core';
import { catchError, type Observable } from 'rxjs';

import { environment } from '../../../../environments/environment';
import { ApiService } from '../../../services/api.service';
import { fallbackApiError, rethrowApiError } from '../../../shared/lib/api-error.utils';
import { addOptionalNumberParam, addOptionalStringParam, type ApiQueryParams } from '../../../shared/lib/api-query-params.utils';
import type {
    CreateWaistEntryPayload,
    UpdateWaistEntryPayload,
    WaistEntry,
    WaistEntryFilters,
    WaistEntrySummaryFilters,
    WaistEntrySummaryPoint,
    WaistHistoryPageSummary,
    WaistHistoryPageSummaryFilters,
} from '../models/waist-entry.data';

@Service()
export class WaistEntriesService extends ApiService {
    protected readonly baseUrl = environment.apiUrls.waists;

    public getEntries(filters?: WaistEntryFilters): Observable<WaistEntry[]> {
        const params: ApiQueryParams = {};

        addOptionalStringParam(params, 'dateFrom', filters?.dateFrom);
        addOptionalStringParam(params, 'dateTo', filters?.dateTo);
        addOptionalNumberParam(params, 'limit', filters?.limit);
        addOptionalStringParam(params, 'sort', filters?.sort);

        return this.get<WaistEntry[]>('', params).pipe(
            catchError((error: unknown) => fallbackApiError('Waist entries fetch error', error, [])),
        );
    }

    public getLatest(): Observable<WaistEntry | null> {
        return this.get<WaistEntry | null>('latest').pipe(
            catchError((error: unknown) => fallbackApiError('Waist latest fetch error', error, null)),
        );
    }

    public create(payload: CreateWaistEntryPayload): Observable<WaistEntry> {
        const headers = new HttpHeaders({ 'Idempotency-Key': crypto.randomUUID() });
        return this.post<WaistEntry>('', payload, headers).pipe(
            catchError((error: unknown) => rethrowApiError('Create waist entry error', error)),
        );
    }

    public update(id: string, payload: UpdateWaistEntryPayload): Observable<WaistEntry> {
        return this.put<WaistEntry>(id, payload).pipe(catchError((error: unknown) => rethrowApiError('Update waist entry error', error)));
    }

    public remove(id: string): Observable<void> {
        return super.delete<void>(id).pipe(catchError((error: unknown) => rethrowApiError('Delete waist entry error', error)));
    }

    public getSummary(filters: WaistEntrySummaryFilters): Observable<WaistEntrySummaryPoint[]> {
        const params: ApiQueryParams = {
            dateFrom: filters.dateFrom,
            dateTo: filters.dateTo,
            quantizationDays: filters.quantizationDays,
        };

        return this.get<WaistEntrySummaryPoint[]>('summary', params).pipe(
            catchError((error: unknown) => fallbackApiError('Waist summary fetch error', error, [])),
        );
    }

    public getPageSummary(filters: WaistHistoryPageSummaryFilters): Observable<WaistHistoryPageSummary> {
        const params: ApiQueryParams = {
            dateFrom: filters.dateFrom,
            dateTo: filters.dateTo,
            quantizationDays: filters.quantizationDays,
            entriesLimit: filters.entriesLimit,
        };

        return this.get<WaistHistoryPageSummary>('page-summary', params).pipe(
            catchError((error: unknown) => rethrowApiError('Waist history page summary fetch error', error)),
        );
    }
}
