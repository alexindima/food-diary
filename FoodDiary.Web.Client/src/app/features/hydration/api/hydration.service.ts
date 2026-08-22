import { HttpHeaders } from '@angular/common/http';
import { Service } from '@angular/core';
import { catchError, type Observable } from 'rxjs';

import { environment } from '../../../../environments/environment';
import { ApiService } from '../../../services/api.service';
import { fallbackApiError, rethrowApiError } from '../../../shared/lib/api-error.utils';
import type { CreateHydrationEntryPayload, HydrationDaily, HydrationEntry } from '../models/hydration.data';

@Service()
export class HydrationService extends ApiService {
    protected readonly baseUrl = environment.apiUrls.hydration;

    public getDaily(dateUtc: Date): Observable<HydrationDaily> {
        const date = this.toCalendarDate(dateUtc);
        const params = { dateUtc: date };
        return this.get<HydrationDaily>('daily', params).pipe(
            catchError((error: unknown) =>
                fallbackApiError('Hydration daily fetch error', error, {
                    dateUtc: date,
                    totalMl: 0,
                    goalMl: null,
                }),
            ),
        );
    }

    public getEntries(dateUtc: Date): Observable<HydrationEntry[]> {
        const params = { dateUtc: this.toCalendarDate(dateUtc) };
        return this.get<HydrationEntry[]>('', params).pipe(
            catchError((error: unknown) => fallbackApiError('Hydration entries fetch error', error, [])),
        );
    }

    public addEntry(amountMl: number, timestampUtc: Date = new Date()): Observable<HydrationEntry> {
        const payload: CreateHydrationEntryPayload = {
            amountMl,
            timestampUtc: timestampUtc.toISOString(),
        };

        const headers = new HttpHeaders({ 'Idempotency-Key': crypto.randomUUID() });
        return this.post<HydrationEntry>('', payload, headers).pipe(
            catchError((error: unknown) => rethrowApiError('Create hydration entry error', error)),
        );
    }

    private toCalendarDate(date: Date): string {
        const year = date.getFullYear();
        const month = String(date.getMonth() + 1).padStart(2, '0');
        const day = String(date.getDate()).padStart(2, '0');
        return `${year}-${month}-${day}`;
    }
}
