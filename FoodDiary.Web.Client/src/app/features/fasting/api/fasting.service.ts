import { inject, Service } from '@angular/core';
import { catchError, map, type Observable } from 'rxjs';

import { environment } from '../../../../environments/environment';
import { fastingOverviewFromSdk, fastingSessionFromSdk } from '../../../shared/api/sdk/fasting-sdk.mapper';
import { FastingSdk } from '../../../shared/api/sdk/generated/api/fasting.service';
import { createSdkConnection } from '../../../shared/api/sdk/sdk-connection';
import { sdkPage } from '../../../shared/api/sdk/sdk-response';
import { fallbackApiError, rethrowApiError } from '../../../shared/lib/api-error.utils';
import type {
    ExtendFastingPayload,
    FastingHistoryQuery,
    FastingOverview,
    FastingSession,
    ReduceFastingTargetPayload,
    StartFastingPayload,
    UpdateFastingCheckInPayload,
} from '../../../shared/models/fasting.data';
import type { PageOf } from '../../../shared/models/page-of.data';
import { FASTING_API_LIMITS } from './fasting-api.tokens';

@Service()
export class FastingService {
    private readonly defaultLimits = inject(FASTING_API_LIMITS);

    protected readonly baseUrl = environment.apiUrls.fasting;
    private readonly sdk = createSdkConnection(FastingSdk, this.baseUrl, inject(HttpClient));

    public start(payload: StartFastingPayload): Observable<FastingSession> {
        return this.sdk.client.postFastingStart({ version: this.sdk.version, startFastingHttpRequest: payload }).pipe(
            map(fastingSessionFromSdk),
            catchError((error: unknown) => rethrowApiError('Start fasting error', error)),
        );
    }

    public end(): Observable<FastingSession> {
        return this.sdk.client.putFastingEnd({ version: this.sdk.version }).pipe(
            map(fastingSessionFromSdk),
            catchError((error: unknown) => rethrowApiError('End fasting error', error)),
        );
    }

    public extend(payload: ExtendFastingPayload): Observable<FastingSession> {
        return this.sdk.client.putFastingCurrentDuration({ version: this.sdk.version, extendActiveFastingHttpRequest: payload }).pipe(
            map(fastingSessionFromSdk),
            catchError((error: unknown) => rethrowApiError('Extend fasting error', error)),
        );
    }

    public reduceTarget(payload: ReduceFastingTargetPayload): Observable<FastingSession> {
        return this.sdk.client
            .putFastingCurrentDurationReduce({ version: this.sdk.version, reduceActiveFastingTargetHttpRequest: payload })
            .pipe(
                map(fastingSessionFromSdk),
                catchError((error: unknown) => rethrowApiError('Reduce fasting target error', error)),
            );
    }

    public updateCheckIn(payload: UpdateFastingCheckInPayload): Observable<FastingSession> {
        return this.sdk.client.putFastingCurrentCheckIn({ version: this.sdk.version, updateFastingCheckInHttpRequest: payload }).pipe(
            map(fastingSessionFromSdk),
            catchError((error: unknown) => rethrowApiError('Update fasting check-in error', error)),
        );
    }

    public skipCyclicDay(): Observable<FastingSession> {
        return this.sdk.client.putFastingCurrentSkipDay({ version: this.sdk.version }).pipe(
            map(fastingSessionFromSdk),
            catchError((error: unknown) => rethrowApiError('Skip cyclic day error', error)),
        );
    }

    public postponeCyclicDay(): Observable<FastingSession> {
        return this.sdk.client.putFastingCurrentPostponeDay({ version: this.sdk.version }).pipe(
            map(fastingSessionFromSdk),
            catchError((error: unknown) => rethrowApiError('Postpone cyclic day error', error)),
        );
    }

    public getOverview(): Observable<FastingOverview> {
        return this.requestOverview().pipe(
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

    public getOverviewStrict(): Observable<FastingOverview> {
        return this.requestOverview().pipe(catchError((error: unknown) => rethrowApiError('Get fasting overview error', error)));
    }

    public getHistory(query: FastingHistoryQuery): Observable<PageOf<FastingSession>> {
        return this.sdk.client
            .getFastingHistory({
                version: this.sdk.version,
                from: query.from,
                to: query.to,
                page: query.page ?? 1,
                limit: query.limit ?? this.defaultLimits.historyPageSize,
            })
            .pipe(
                map(value => sdkPage(value, fastingSessionFromSdk)),
                catchError((error: unknown) => rethrowApiError('Get fasting history error', error)),
            );
    }

    private requestOverview(): Observable<FastingOverview> {
        return this.sdk.client.getFastingOverview({ version: this.sdk.version }).pipe(map(fastingOverviewFromSdk));
    }
}
import { HttpClient } from '@angular/common/http';
