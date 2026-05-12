import { Injectable } from '@angular/core';
import { catchError, type Observable } from 'rxjs';

import { environment } from '../../../../environments/environment';
import { ApiService } from '../../../services/api.service';
import { fallbackApiError, rethrowApiError } from '../../../shared/lib/api-error.utils';
import type {
    CreateWaistEntryPayload,
    UpdateWaistEntryPayload,
    WaistEntry,
    WaistEntryFilters,
    WaistEntrySummaryFilters,
    WaistEntrySummaryPoint,
} from '../models/waist-entry.data';

function addOptionalStringParam(params: Record<string, string | number>, key: string, value: string | undefined): void {
    if (value !== undefined && value.length > 0) {
        params[key] = value;
    }
}

function addOptionalNumberParam(params: Record<string, string | number>, key: string, value: number | undefined): void {
    if (value !== undefined) {
        params[key] = value;
    }
}

@Injectable({
    providedIn: 'root',
})
export class WaistEntriesService extends ApiService {
    protected readonly baseUrl = environment.apiUrls.waists;

    public getEntries(filters?: WaistEntryFilters): Observable<WaistEntry[]> {
        const params: Record<string, string | number> = {};

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
        return this.post<WaistEntry>('', payload).pipe(catchError((error: unknown) => rethrowApiError('Create waist entry error', error)));
    }

    public update(id: string, payload: UpdateWaistEntryPayload): Observable<WaistEntry> {
        return this.put<WaistEntry>(id, payload).pipe(catchError((error: unknown) => rethrowApiError('Update waist entry error', error)));
    }

    public remove(id: string): Observable<void> {
        return super.delete<void>(id).pipe(catchError((error: unknown) => rethrowApiError('Delete waist entry error', error)));
    }

    public getSummary(filters: WaistEntrySummaryFilters): Observable<WaistEntrySummaryPoint[]> {
        const params: Record<string, string | number> = {
            dateFrom: filters.dateFrom,
            dateTo: filters.dateTo,
            quantizationDays: filters.quantizationDays,
        };

        return this.get<WaistEntrySummaryPoint[]>('summary', params).pipe(
            catchError((error: unknown) => fallbackApiError('Waist summary fetch error', error, [])),
        );
    }
}
