import type { HttpErrorResponse } from '@angular/common/http';
import { Injectable } from '@angular/core';
import type { Observable } from 'rxjs';
import { catchError } from 'rxjs/operators';

import { environment } from '../../../../environments/environment';
import { ApiService } from '../../../services/api.service';
import { fallbackApiError, rethrowApiError } from '../../../shared/lib/api-error.utils';
import type { WearableAuthUrl, WearableConnection, WearableDailySummary } from '../models/wearable.data';

@Injectable({ providedIn: 'root' })
export class WearableService extends ApiService {
    protected readonly baseUrl = environment.apiUrls.wearables;

    public getConnections(): Observable<WearableConnection[]> {
        return this.get<WearableConnection[]>('connections').pipe(
            catchError((error: HttpErrorResponse) => fallbackApiError('Get wearable connections error', error, [])),
        );
    }

    public getAuthUrl(provider: string, state: string): Observable<WearableAuthUrl> {
        return this.get<WearableAuthUrl>(`${provider}/auth-url`, { state }).pipe(
            catchError((error: HttpErrorResponse) => rethrowApiError('Get wearable auth URL error', error)),
        );
    }

    public connect(provider: string, code: string): Observable<WearableConnection> {
        return this.post<WearableConnection>(`${provider}/connect`, { code }).pipe(
            catchError((error: HttpErrorResponse) => rethrowApiError('Connect wearable error', error)),
        );
    }

    public disconnect(provider: string): Observable<void> {
        return this.delete<void>(`${provider}/disconnect`).pipe(
            catchError((error: HttpErrorResponse) => rethrowApiError('Disconnect wearable error', error)),
        );
    }

    public sync(provider: string, date: string): Observable<WearableDailySummary> {
        return this.post<WearableDailySummary>(`${provider}/sync`, null, undefined, { date }).pipe(
            catchError((error: HttpErrorResponse) => rethrowApiError('Sync wearable data error', error)),
        );
    }

    public getDailySummary(date: string): Observable<WearableDailySummary> {
        return this.get<WearableDailySummary>('daily-summary', { date }).pipe(
            catchError((error: HttpErrorResponse) =>
                fallbackApiError('Get wearable daily summary error', error, {
                    date,
                    steps: null,
                    heartRate: null,
                    caloriesBurned: null,
                    activeMinutes: null,
                    sleepMinutes: null,
                }),
            ),
        );
    }
}
