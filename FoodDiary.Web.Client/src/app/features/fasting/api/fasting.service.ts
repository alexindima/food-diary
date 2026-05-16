import { inject, Injectable } from '@angular/core';
import { catchError, type Observable } from 'rxjs';

import { environment } from '../../../../environments/environment';
import { ApiService } from '../../../services/api.service';
import { fallbackApiError, rethrowApiError } from '../../../shared/lib/api-error.utils';
import type { PageOf } from '../../../shared/models/page-of.data';
import type {
    ExtendFastingPayload,
    FastingHistoryQuery,
    FastingOverview,
    FastingSession,
    ReduceFastingTargetPayload,
    StartFastingPayload,
    UpdateFastingCheckInPayload,
} from '../models/fasting.data';
import { FASTING_API_LIMITS } from './fasting-api.tokens';

@Injectable({ providedIn: 'root' })
export class FastingService extends ApiService {
    private readonly defaultLimits = inject(FASTING_API_LIMITS);

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
                        limit: this.defaultLimits.historyPageSize,
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
            limit: query.limit ?? this.defaultLimits.historyPageSize,
        }).pipe(
            catchError((error: unknown) =>
                fallbackApiError('Get fasting history error', error, {
                    data: [],
                    page: query.page ?? 1,
                    limit: query.limit ?? this.defaultLimits.historyPageSize,
                    totalPages: 0,
                    totalItems: 0,
                }),
            ),
        );
    }
}
