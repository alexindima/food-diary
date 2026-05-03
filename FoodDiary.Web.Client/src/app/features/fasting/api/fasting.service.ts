import { type HttpErrorResponse } from '@angular/common/http';
import { Injectable } from '@angular/core';
import { catchError, type Observable } from 'rxjs';

import { environment } from '../../../../environments/environment';
import { ApiService } from '../../../services/api.service';
import { fallbackApiError, rethrowApiError } from '../../../shared/lib/api-error.utils';
import { type PageOf } from '../../../shared/models/page-of.data';
import {
    type ExtendFastingPayload,
    type FastingHistoryQuery,
    type FastingInsights,
    type FastingOverview,
    type FastingSession,
    type FastingStats,
    type ReduceFastingTargetPayload,
    type StartFastingPayload,
    type UpdateFastingCheckInPayload,
} from '../models/fasting.data';

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

    public extend(payload: ExtendFastingPayload): Observable<FastingSession> {
        return this.put<FastingSession>('current/duration', payload).pipe(
            catchError((error: HttpErrorResponse) => rethrowApiError('Extend fasting error', error)),
        );
    }

    public reduceTarget(payload: ReduceFastingTargetPayload): Observable<FastingSession> {
        return this.put<FastingSession>('current/duration/reduce', payload).pipe(
            catchError((error: HttpErrorResponse) => rethrowApiError('Reduce fasting target error', error)),
        );
    }

    public updateCheckIn(payload: UpdateFastingCheckInPayload): Observable<FastingSession> {
        return this.put<FastingSession>('current/check-in', payload).pipe(
            catchError((error: HttpErrorResponse) => rethrowApiError('Update fasting check-in error', error)),
        );
    }

    public skipCyclicDay(): Observable<FastingSession> {
        return this.put<FastingSession>('current/skip-day', {}).pipe(
            catchError((error: HttpErrorResponse) => rethrowApiError('Skip cyclic day error', error)),
        );
    }

    public postponeCyclicDay(): Observable<FastingSession> {
        return this.put<FastingSession>('current/postpone-day', {}).pipe(
            catchError((error: HttpErrorResponse) => rethrowApiError('Postpone cyclic day error', error)),
        );
    }

    public getCurrent(): Observable<FastingSession | null> {
        return this.get<FastingSession | null>('current').pipe(
            catchError((error: HttpErrorResponse) => fallbackApiError('Get current fasting error', error, null)),
        );
    }

    public getOverview(): Observable<FastingOverview> {
        return this.get<FastingOverview>('overview').pipe(
            catchError((error: HttpErrorResponse) =>
                fallbackApiError('Get fasting overview error', error, {
                    currentSession: null,
                    stats: {
                        totalCompleted: 0,
                        currentStreak: 0,
                        averageDurationHours: 0,
                        completionRateLast30Days: 0,
                        checkInRateLast30Days: 0,
                        lastCheckInAtUtc: null,
                        topSymptom: null,
                    },
                    insights: {
                        alerts: [],
                        insights: [],
                    },
                    history: {
                        data: [],
                        page: 1,
                        limit: 10,
                        totalPages: 0,
                        totalItems: 0,
                    },
                }),
            ),
        );
    }

    public getHistory(query: FastingHistoryQuery): Observable<PageOf<FastingSession>> {
        return this.get<PageOf<FastingSession>>('history', {
            from: query.from,
            to: query.to,
            page: query.page ?? 1,
            limit: query.limit ?? 10,
        }).pipe(
            catchError((error: HttpErrorResponse) =>
                fallbackApiError('Get fasting history error', error, {
                    data: [],
                    page: query.page ?? 1,
                    limit: query.limit ?? 10,
                    totalPages: 0,
                    totalItems: 0,
                }),
            ),
        );
    }

    public getStats(): Observable<FastingStats> {
        return this.get<FastingStats>('stats').pipe(
            catchError((error: HttpErrorResponse) =>
                fallbackApiError('Get fasting stats error', error, {
                    totalCompleted: 0,
                    currentStreak: 0,
                    averageDurationHours: 0,
                    completionRateLast30Days: 0,
                    checkInRateLast30Days: 0,
                    lastCheckInAtUtc: null,
                    topSymptom: null,
                }),
            ),
        );
    }

    public getInsights(): Observable<FastingInsights> {
        return this.get<FastingInsights>('insights').pipe(
            catchError((error: HttpErrorResponse) =>
                fallbackApiError('Get fasting insights error', error, {
                    alerts: [],
                    insights: [],
                }),
            ),
        );
    }
}
