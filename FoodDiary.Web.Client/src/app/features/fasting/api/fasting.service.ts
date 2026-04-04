import { HttpErrorResponse } from '@angular/common/http';
import { Injectable } from '@angular/core';
import { Observable, catchError } from 'rxjs';
import { environment } from '../../../../environments/environment';
import { ApiService } from '../../../services/api.service';
import { fallbackApiError, rethrowApiError } from '../../../shared/lib/api-error.utils';
import { FastingHistoryQuery, FastingSession, FastingStats, StartFastingPayload } from '../models/fasting.data';

@Injectable({
    providedIn: 'root',
})
export class FastingService extends ApiService {
    protected readonly baseUrl = environment.apiUrls.fasting;

    public start(payload: StartFastingPayload): Observable<FastingSession> {
        return this.post<FastingSession>('start', payload).pipe(
            catchError((error: HttpErrorResponse) => rethrowApiError('Start fasting error', error)),
        );
    }

    public end(): Observable<FastingSession> {
        return this.put<FastingSession>('end', {}).pipe(
            catchError((error: HttpErrorResponse) => rethrowApiError('End fasting error', error)),
        );
    }

    public getCurrent(): Observable<FastingSession | null> {
        return this.get<FastingSession | null>('current').pipe(
            catchError((error: HttpErrorResponse) => fallbackApiError('Get current fasting error', error, null)),
        );
    }

    public getHistory(query: FastingHistoryQuery): Observable<FastingSession[]> {
        return this.get<FastingSession[]>('history', { from: query.from, to: query.to }).pipe(
            catchError((error: HttpErrorResponse) => fallbackApiError('Get fasting history error', error, [])),
        );
    }

    public getStats(): Observable<FastingStats> {
        return this.get<FastingStats>('stats').pipe(
            catchError((error: HttpErrorResponse) =>
                fallbackApiError('Get fasting stats error', error, {
                    totalCompleted: 0,
                    currentStreak: 0,
                    averageDurationHours: 0,
                }),
            ),
        );
    }
}
