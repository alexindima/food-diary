import { Injectable } from '@angular/core';
import { Observable, catchError, of, tap } from 'rxjs';
import { HttpErrorResponse } from '@angular/common/http';
import { ApiService } from './api.service';
import { environment } from '../../environments/environment';
import { CreateHydrationEntryPayload, HydrationDaily, HydrationEntry } from '../types/hydration.data';

@Injectable({
    providedIn: 'root',
})
export class HydrationService extends ApiService {
    protected readonly baseUrl = environment.apiUrls.hydration;

    public getDaily(dateUtc: Date): Observable<HydrationDaily> {
        const params = { dateUtc: dateUtc.toISOString() };
        return this.get<HydrationDaily>('daily', params).pipe(
            catchError((error: HttpErrorResponse) => {
                console.error('Hydration daily fetch error', error);
                return of({
                    dateUtc: dateUtc.toISOString(),
                    totalMl: 0,
                    goalMl: null,
                });
            })
        );
    }

    public getEntries(dateUtc: Date): Observable<HydrationEntry[]> {
        const params = { dateUtc: dateUtc.toISOString() };
        return this.get<HydrationEntry[]>('', params).pipe(
            catchError((error: HttpErrorResponse) => {
                console.error('Hydration entries fetch error', error);
                return of([]);
            })
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
            catchError((error: HttpErrorResponse) => {
                console.error('Create hydration entry error', error);
                throw error;
            })
        );
    }
}
