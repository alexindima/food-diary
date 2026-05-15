import { Injectable } from '@angular/core';
import { catchError, type Observable } from 'rxjs';

import { environment } from '../../../../environments/environment';
import { ApiService } from '../../../services/api.service';
import { fallbackApiError, rethrowApiError } from '../../../shared/lib/api-error.utils';
import type { CreateHydrationEntryPayload, HydrationDaily, HydrationEntry } from '../models/hydration.data';

@Injectable({ providedIn: 'root' })
export class HydrationService extends ApiService {
    protected readonly baseUrl = environment.apiUrls.hydration;

    public getDaily(dateUtc: Date): Observable<HydrationDaily> {
        const params = { dateUtc: dateUtc.toISOString() };
        return this.get<HydrationDaily>('daily', params).pipe(
            catchError((error: unknown) =>
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
            catchError((error: unknown) => fallbackApiError('Hydration entries fetch error', error, [])),
        );
    }

    public addEntry(amountMl: number, timestampUtc: Date = new Date()): Observable<HydrationEntry> {
        const payload: CreateHydrationEntryPayload = {
            amountMl,
            timestampUtc: timestampUtc.toISOString(),
        };

        return this.post<HydrationEntry>('', payload).pipe(
            catchError((error: unknown) => rethrowApiError('Create hydration entry error', error)),
        );
    }
}
