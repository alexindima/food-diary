import { Injectable } from '@angular/core';
import { catchError, type Observable } from 'rxjs';

import { environment } from '../../../../environments/environment';
import { ApiService } from '../../../services/api.service';
import { fallbackApiError, rethrowApiError } from '../../../shared/lib/api-error.utils';
import { addOptionalNumberParam, addOptionalStringParam, type ApiQueryParams } from '../../../shared/lib/api-query-params.utils';
import type {
    CreateWeightEntryPayload,
    UpdateWeightEntryPayload,
    WeightEntry,
    WeightEntryFilters,
    WeightEntrySummaryFilters,
    WeightEntrySummaryPoint,
} from '../models/weight-entry.data';

@Injectable({ providedIn: 'root' })
export class WeightEntriesService extends ApiService {
    protected readonly baseUrl = environment.apiUrls.weights;

    public getEntries(filters?: WeightEntryFilters): Observable<WeightEntry[]> {
        const params: ApiQueryParams = {};

        addOptionalStringParam(params, 'dateFrom', filters?.dateFrom);
        addOptionalStringParam(params, 'dateTo', filters?.dateTo);
        addOptionalNumberParam(params, 'limit', filters?.limit);
        addOptionalStringParam(params, 'sort', filters?.sort);

        return this.get<WeightEntry[]>('', params).pipe(
            catchError((error: unknown) => fallbackApiError('Weight entries fetch error', error, [])),
        );
    }

    public getLatest(): Observable<WeightEntry | null> {
        return this.get<WeightEntry | null>('latest').pipe(
            catchError((error: unknown) => fallbackApiError('Weight latest fetch error', error, null)),
        );
    }

    public create(payload: CreateWeightEntryPayload): Observable<WeightEntry> {
        return this.post<WeightEntry>('', payload).pipe(
            catchError((error: unknown) => rethrowApiError('Create weight entry error', error)),
        );
    }

    public update(id: string, payload: UpdateWeightEntryPayload): Observable<WeightEntry> {
        return this.put<WeightEntry>(id, payload).pipe(catchError((error: unknown) => rethrowApiError('Update weight entry error', error)));
    }

    public remove(id: string): Observable<void> {
        return super.delete<void>(id).pipe(catchError((error: unknown) => rethrowApiError('Delete weight entry error', error)));
    }

    public getSummary(filters: WeightEntrySummaryFilters): Observable<WeightEntrySummaryPoint[]> {
        const params: ApiQueryParams = {
            dateFrom: filters.dateFrom,
            dateTo: filters.dateTo,
            quantizationDays: filters.quantizationDays,
        };

        return this.get<WeightEntrySummaryPoint[]>('summary', params).pipe(
            catchError((error: unknown) => fallbackApiError('Weight summary fetch error', error, [])),
        );
    }
}
