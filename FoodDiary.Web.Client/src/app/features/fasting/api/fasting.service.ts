import { Injectable } from '@angular/core';
import { catchError, type Observable } from 'rxjs';

import { environment } from '../../../../environments/environment';
import { ApiService } from '../../../services/api.service';
import { fallbackApiError, rethrowApiError } from '../../../shared/lib/api-error.utils';
import type { PageOf } from '../../../shared/models/page-of.data';
import type {
    ExtendFastingPayload,
    FastingHistoryQuery,
    FastingInsights,
    FastingOverview,
    FastingSession,
    FastingStats,
    ReduceFastingTargetPayload,
    StartFastingPayload,
    UpdateFastingCheckInPayload,
} from '../models/fasting.data';

const DEFAULT_HISTORY_PAGE_SIZE = 10;

@Injectable({ providedIn: 'root' })
export class FastingService extends ApiService {
    protected readonly baseUrl = environment.apiUrls.fasting;

    public start(payload: StartFastingPayload): Observable<FastingSession> {
        return this.post<FastingSession>('start', payload).pipe(
            catchError((error: unknown) => rethrowApiError('Start fasting error', error)),
        );
    }

    public end(): Observable<FastingSession> {
        return this.put<FastingSession>('end', {}).pipe(catchError((error: unknown) => rethrowApiError('End fasting error', error)));
    }

    public extend(payload: ExtendFastingPayload): Observable<FastingSession> {
        return this.put<FastingSession>('current/duration', payload).pipe(
            catchError((error: unknown) => rethrowApiError('Extend fasting error', error)),
        );
    }

    public reduceTarget(payload: ReduceFastingTargetPayload): Observable<FastingSession> {
        return this.put<FastingSession>('current/duration/reduce', payload).pipe(
            catchError((error: unknown) => rethrowApiError('Reduce fasting target error', error)),
        );
    }

    public updateCheckIn(payload: UpdateFastingCheckInPayload): Observable<FastingSession> {
        return this.put<FastingSession>('current/check-in', payload).pipe(
            catchError((error: unknown) => rethrowApiError('Update fasting check-in error', error)),
        );
    }

    public skipCyclicDay(): Observable<FastingSession> {
        return this.put<FastingSession>('current/skip-day', {}).pipe(
            catchError((error: unknown) => rethrowApiError('Skip cyclic day error', error)),
        );
    }

    public postponeCyclicDay(): Observable<FastingSession> {
        return this.put<FastingSession>('current/postpone-day', {}).pipe(
            catchError((error: unknown) => rethrowApiError('Postpone cyclic day error', error)),
        );
    }

    public getCurrent(): Observable<FastingSession | null> {
        return this.get<FastingSession | null>('current').pipe(
            catchError((error: unknown) => fallbackApiError('Get current fasting error', error, null)),
        );
    }

    public getOverview(): Observable<FastingOverview> {
        return this.get<FastingOverview>('overview').pipe(
            catchError((error: unknown) =>
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
                        limit: DEFAULT_HISTORY_PAGE_SIZE,
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
            limit: query.limit ?? DEFAULT_HISTORY_PAGE_SIZE,
        }).pipe(
            catchError((error: unknown) =>
                fallbackApiError('Get fasting history error', error, {
                    data: [],
                    page: query.page ?? 1,
                    limit: query.limit ?? DEFAULT_HISTORY_PAGE_SIZE,
                    totalPages: 0,
                    totalItems: 0,
                }),
            ),
        );
    }

    public getStats(): Observable<FastingStats> {
        return this.get<FastingStats>('stats').pipe(
            catchError((error: unknown) =>
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
            catchError((error: unknown) =>
                fallbackApiError('Get fasting insights error', error, {
                    alerts: [],
                    insights: [],
                }),
            ),
        );
    }
}
