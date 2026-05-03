import { type HttpErrorResponse } from '@angular/common/http';
import { Injectable } from '@angular/core';
import { catchError, type Observable, tap } from 'rxjs';

import { environment } from '../../../../environments/environment';
import { ApiService } from '../../../services/api.service';
import { fallbackApiError, rethrowApiError } from '../../../shared/lib/api-error.utils';
import { type CreateHydrationEntryPayload, type HydrationDaily, type HydrationEntry } from '../models/hydration.data';

@Injectable({
    providedIn: 'root',
})
export class HydrationService extends ApiService {
    protected readonly baseUrl = environment.apiUrls.hydration;

    public getDaily(dateUtc: Date): Observable<HydrationDaily> {
        const params = { dateUtc: dateUtc.toISOString() };
        return this.get<HydrationDaily>('daily', params).pipe(
            catchError((error: HttpErrorResponse) =>
                fallbackApiError('Hydration daily fetch error', error, {
                    dateUtc: dateUtc.toISOString(),
                    totalMl: 0,
                    goalMl: null,
                }),
            ),
        );
    }

    public getEntries(dateUtc: Date): Observable<HydrationEntry[]> {
        const params = { dateUtc: dateUtc.toISOString() };
        return this.get<HydrationEntry[]>('', params).pipe(
            catchError((error: HttpErrorResponse) => fallbackApiError('Hydration entries fetch error', error, [])),
        );
    }

    public addEntry(amountMl: number, timestampUtc: Date = new Date()): Observable<HydrationEntry> {
        const payload: CreateHydrationEntryPayload = {
            amountMl,
            timestampUtc: timestampUtc.toISOString(),
        };

        return this.post<HydrationEntry>('', payload).pipe(
            tap(() => {
                // no-op side effects; refresh handled by caller
            }),
            catchError((error: HttpErrorResponse) => rethrowApiError('Create hydration entry error', error)),
        );
    }
}
