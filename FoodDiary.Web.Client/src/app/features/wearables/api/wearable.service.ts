import { HttpClient } from '@angular/common/http';
import { inject, Service } from '@angular/core';
import { catchError, map, type Observable } from 'rxjs';

import { environment } from '../../../../environments/environment';
import { WearablesSdk } from '../../../shared/api/sdk/generated/api/wearables.service';
import type { WearableConnectionHttpResponse } from '../../../shared/api/sdk/generated/model/wearable-connection-http-response';
import type { WearableDailySummaryHttpResponse } from '../../../shared/api/sdk/generated/model/wearable-daily-summary-http-response';
import { createSdkConnection } from '../../../shared/api/sdk/sdk-connection';
import { requireSdkFields } from '../../../shared/api/sdk/sdk-response';
import { fallbackApiError, rethrowApiError } from '../../../shared/lib/api-error.utils';
import type { WearableAuthUrl, WearableConnection, WearableDailySummary } from '../models/wearable.data';

@Service()
export class WearableService {
    protected readonly baseUrl = environment.apiUrls.wearables;
    private readonly sdk = createSdkConnection(WearablesSdk, this.baseUrl, inject(HttpClient));

    public getConnections(): Observable<WearableConnection[]> {
        return this.sdk.client.getWearablesConnections({ version: this.sdk.version }).pipe(
            map(values => values.map(wearableConnectionFromSdk)),
            catchError((error: unknown) => fallbackApiError('Get wearable connections error', error, [])),
        );
    }

    public getAuthUrl(provider: string, state: string): Observable<WearableAuthUrl> {
        return this.sdk.client.getWearablesByProviderAuthUrl({ version: this.sdk.version, provider, state }).pipe(
            map(value => requireSdkFields(value, ['authorizationUrl'])),
            catchError((error: unknown) => rethrowApiError('Get wearable auth URL error', error)),
        );
    }

    public connect(provider: string, code: string, state: string): Observable<WearableConnection> {
        return this.sdk.client
            .postWearablesByProviderConnect({
                version: this.sdk.version,
                provider,
                idempotencyKey: crypto.randomUUID(),
                connectWearableHttpRequest: { code, state },
            })
            .pipe(
                map(wearableConnectionFromSdk),
                catchError((error: unknown) => rethrowApiError('Connect wearable error', error)),
            );
    }

    public disconnect(provider: string): Observable<void> {
        return this.sdk.client.deleteWearablesByProviderDisconnect({ version: this.sdk.version, provider }).pipe(
            map(() => {}),
            catchError((error: unknown) => rethrowApiError('Disconnect wearable error', error)),
        );
    }

    public sync(provider: string, date: string): Observable<WearableDailySummary> {
        return this.sdk.client
            .postWearablesByProviderSync({ version: this.sdk.version, provider, date, idempotencyKey: crypto.randomUUID() })
            .pipe(
                map(wearableDailyFromSdk),
                catchError((error: unknown) => rethrowApiError('Sync wearable data error', error)),
            );
    }

    public getDailySummary(date: string): Observable<WearableDailySummary> {
        return this.sdk.client.getWearablesDailySummary({ version: this.sdk.version, date }).pipe(
            map(wearableDailyFromSdk),
            catchError((error: unknown) =>
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

function wearableConnectionFromSdk(response: WearableConnectionHttpResponse): WearableConnection {
    return {
        ...requireSdkFields(response, ['provider', 'externalUserId', 'isActive', 'connectedAtUtc']),
        lastSyncedAtUtc: response.lastSyncedAtUtc ?? null,
    };
}

function wearableDailyFromSdk(response: WearableDailySummaryHttpResponse): WearableDailySummary {
    return {
        ...requireSdkFields(response, ['date']),
        steps: response.steps ?? null,
        heartRate: response.heartRate ?? null,
        caloriesBurned: response.caloriesBurned ?? null,
        activeMinutes: response.activeMinutes ?? null,
        sleepMinutes: response.sleepMinutes ?? null,
    };
}
