import type { HttpErrorResponse } from '@angular/common/http';
import { Injectable } from '@angular/core';
import { catchError, type Observable } from 'rxjs';

import { environment } from '../../../../environments/environment';
import { ApiService } from '../../../services/api.service';
import { fallbackApiError, rethrowApiError } from '../../../shared/lib/api-error.utils';
import type {
    CreateWeightEntryPayload,
    UpdateWeightEntryPayload,
    WeightEntry,
    WeightEntryFilters,
    WeightEntrySummaryFilters,
    WeightEntrySummaryPoint,
} from '../models/weight-entry.data';

@Injectable({
    providedIn: 'root',
})
export class WeightEntriesService extends ApiService {
    protected readonly baseUrl = environment.apiUrls.weights;

    public getEntries(filters?: WeightEntryFilters): Observable<WeightEntry[]> {
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

        return this.get<WeightEntry[]>('', params).pipe(
            catchError((error: HttpErrorResponse) => fallbackApiError('Weight entries fetch error', error, [])),
        );
    }

    public getLatest(): Observable<WeightEntry | null> {
        return this.get<WeightEntry | null>('latest').pipe(
            catchError((error: HttpErrorResponse) => fallbackApiError('Weight latest fetch error', error, null)),
        );
    }

    public create(payload: CreateWeightEntryPayload): Observable<WeightEntry> {
        return this.post<WeightEntry>('', payload).pipe(
            catchError((error: HttpErrorResponse) => rethrowApiError('Create weight entry error', error)),
        );
    }

    public update(id: string, payload: UpdateWeightEntryPayload): Observable<WeightEntry> {
        return this.put<WeightEntry>(id, payload).pipe(
            catchError((error: HttpErrorResponse) => rethrowApiError('Update weight entry error', error)),
        );
    }

    public remove(id: string): Observable<void> {
        return super.delete<void>(id).pipe(catchError((error: HttpErrorResponse) => rethrowApiError('Delete weight entry error', error)));
    }

    public getSummary(filters: WeightEntrySummaryFilters): Observable<WeightEntrySummaryPoint[]> {
        const params: Record<string, string | number> = {
            dateFrom: filters.dateFrom,
            dateTo: filters.dateTo,
            quantizationDays: filters.quantizationDays,
        };

        return this.get<WeightEntrySummaryPoint[]>('summary', params).pipe(
            catchError((error: HttpErrorResponse) => fallbackApiError('Weight summary fetch error', error, [])),
        );
    }
}
