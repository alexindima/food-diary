import { type HttpErrorResponse } from '@angular/common/http';
import { Injectable } from '@angular/core';
import { catchError, type Observable } from 'rxjs';

import { environment } from '../../../../environments/environment';
import { ApiService } from '../../../services/api.service';
import { fallbackApiError, rethrowApiError } from '../../../shared/lib/api-error.utils';
import {
    type CreateWaistEntryPayload,
    type UpdateWaistEntryPayload,
    type WaistEntry,
    type WaistEntryFilters,
    type WaistEntrySummaryFilters,
    type WaistEntrySummaryPoint,
} from '../models/waist-entry.data';

@Injectable({
    providedIn: 'root',
})
export class WaistEntriesService extends ApiService {
    protected readonly baseUrl = environment.apiUrls.waists;

    public getEntries(filters?: WaistEntryFilters): Observable<WaistEntry[]> {
        const params: Record<string, string | number> = {};

        if (filters?.dateFrom) {
            params['dateFrom'] = filters.dateFrom;
        }
        if (filters?.dateTo) {
            params['dateTo'] = filters.dateTo;
        }
        if (filters?.limit) {
            params['limit'] = filters.limit;
        }
        if (filters?.sort) {
            params['sort'] = filters.sort;
        }

        return this.get<WaistEntry[]>('', params).pipe(
            catchError((error: HttpErrorResponse) => fallbackApiError('Waist entries fetch error', error, [])),
        );
    }

    public getLatest(): Observable<WaistEntry | null> {
        return this.get<WaistEntry | null>('latest').pipe(
            catchError((error: HttpErrorResponse) => fallbackApiError('Waist latest fetch error', error, null)),
        );
    }

    public create(payload: CreateWaistEntryPayload): Observable<WaistEntry> {
        return this.post<WaistEntry>('', payload).pipe(
            catchError((error: HttpErrorResponse) => rethrowApiError('Create waist entry error', error)),
        );
    }

    public update(id: string, payload: UpdateWaistEntryPayload): Observable<WaistEntry> {
        return this.put<WaistEntry>(`${id}`, payload).pipe(
            catchError((error: HttpErrorResponse) => rethrowApiError('Update waist entry error', error)),
        );
    }

    public remove(id: string): Observable<void> {
        return super
            .delete<void>(`${id}`)
            .pipe(catchError((error: HttpErrorResponse) => rethrowApiError('Delete waist entry error', error)));
    }

    public getSummary(filters: WaistEntrySummaryFilters): Observable<WaistEntrySummaryPoint[]> {
        const params: Record<string, string | number> = {
            dateFrom: filters.dateFrom,
            dateTo: filters.dateTo,
            quantizationDays: filters.quantizationDays,
        };

        return this.get<WaistEntrySummaryPoint[]>('summary', params).pipe(
            catchError((error: HttpErrorResponse) => fallbackApiError('Waist summary fetch error', error, [])),
        );
    }
}
