import { HttpClient } from '@angular/common/http';
import { inject, Service } from '@angular/core';
import { catchError, map, type Observable } from 'rxjs';

import { environment } from '../../../../environments/environment';
import { cycleDayFromSdk, cycleFromSdk, cycleNutritionFromSdk } from '../../../shared/api/sdk/cycle-sdk.mapper';
import { CyclesSdk } from '../../../shared/api/sdk/generated/api/cycles.service';
import { createSdkConnection } from '../../../shared/api/sdk/sdk-connection';
import { sdkOptional } from '../../../shared/api/sdk/sdk-response';
import { fallbackApiError, rethrowApiError } from '../../../shared/lib/api-error.utils';
import type {
    CreateCyclePayload,
    CycleLogDay,
    CycleNutritionSummary,
    CycleResponse,
    UpdateCycleConsentPayload,
    UpdateCycleSettingsPayload,
    UpdateMenstrualEpisodePayload,
    UpsertCycleDayPayload,
    UpsertCycleFactorPayload,
} from '../../../shared/models/cycle.data';

@Service()
export class CyclesService {
    protected readonly baseUrl = environment.apiUrls.cycles;
    private readonly sdk = createSdkConnection(CyclesSdk, this.baseUrl, inject(HttpClient));

    public getCurrent(): Observable<CycleResponse | null> {
        return this.sdk.client.getCyclesCurrent({ version: this.sdk.version }).pipe(
            map(value => sdkOptional(value, cycleFromSdk)),
            catchError((error: unknown) => rethrowApiError('Cycle fetch error', error)),
        );
    }

    public getNutritionSummary(dateFrom: string, dateTo: string): Observable<CycleNutritionSummary | null> {
        return this.sdk.client.getCyclesCurrentNutritionSummary({ version: this.sdk.version, dateFrom, dateTo }).pipe(
            map(value => sdkOptional(value, cycleNutritionFromSdk)),
            catchError((error: unknown) => fallbackApiError('Cycle nutrition summary fetch error', error, null)),
        );
    }

    public create(payload: CreateCyclePayload): Observable<CycleResponse> {
        return this.sdk.client.postCycles({ version: this.sdk.version, createCycleHttpRequest: payload }).pipe(
            map(cycleFromSdk),
            catchError((error: unknown) => rethrowApiError('Cycle create error', error)),
        );
    }

    public updateSettings(cycleProfileId: string, payload: UpdateCycleSettingsPayload): Observable<CycleResponse> {
        return this.sdk.client
            .putCyclesByCycleProfileIdSettings({ version: this.sdk.version, cycleProfileId, updateCycleSettingsHttpRequest: payload })
            .pipe(
                map(cycleFromSdk),
                catchError((error: unknown) => rethrowApiError('Cycle settings update error', error)),
            );
    }

    public updateConsent(cycleProfileId: string, purpose: number, payload: UpdateCycleConsentPayload): Observable<CycleResponse> {
        return this.sdk.client
            .putCyclesByCycleProfileIdConsentsByPurpose({
                version: this.sdk.version,
                cycleProfileId,
                purpose,
                updateCycleConsentHttpRequest: payload,
            })
            .pipe(
                map(cycleFromSdk),
                catchError((error: unknown) => rethrowApiError('Cycle consent update error', error)),
            );
    }

    public deleteCycle(cycleProfileId: string): Observable<void> {
        return this.sdk.client
            .deleteCyclesByCycleProfileId({ version: this.sdk.version, cycleProfileId })
            .pipe(catchError((error: unknown) => rethrowApiError('Cycle delete error', error)));
    }

    public upsertDay(cycleProfileId: string, payload: UpsertCycleDayPayload): Observable<CycleLogDay> {
        return this.sdk.client
            .putCyclesByCycleProfileIdDays({
                version: this.sdk.version,
                cycleProfileId,
                upsertCycleDayHttpRequest: payload,
            })
            .pipe(
                map(cycleDayFromSdk),
                catchError((error: unknown) => rethrowApiError('Cycle day upsert error', error)),
            );
    }

    public clearDay(cycleProfileId: string, date: string): Observable<void> {
        return this.sdk.client
            .deleteCyclesByCycleProfileIdDays({ version: this.sdk.version, cycleProfileId, date })
            .pipe(catchError((error: unknown) => rethrowApiError('Cycle day clear error', error)));
    }

    public confirmPeriodStart(cycleProfileId: string, date: string): Observable<CycleResponse> {
        return this.sdk.client
            .putCyclesByCycleProfileIdPeriodStart({ version: this.sdk.version, cycleProfileId, confirmPeriodStartHttpRequest: { date } })
            .pipe(
                map(cycleFromSdk),
                catchError((error: unknown) => rethrowApiError('Period start confirmation error', error)),
            );
    }

    public updateMenstrualEpisode(
        cycleProfileId: string,
        menstrualEpisodeId: string,
        payload: UpdateMenstrualEpisodePayload,
    ): Observable<CycleResponse> {
        return this.sdk.client
            .putCyclesByCycleProfileIdMenstrualEpisodesByMenstrualEpisodeId({
                version: this.sdk.version,
                cycleProfileId,
                menstrualEpisodeId,
                updateMenstrualEpisodeHttpRequest: payload,
            })
            .pipe(
                map(cycleFromSdk),
                catchError((error: unknown) => rethrowApiError('Menstrual episode update error', error)),
            );
    }

    public deleteMenstrualEpisode(cycleProfileId: string, menstrualEpisodeId: string): Observable<CycleResponse> {
        return this.sdk.client
            .deleteCyclesByCycleProfileIdMenstrualEpisodesByMenstrualEpisodeId({
                version: this.sdk.version,
                cycleProfileId,
                menstrualEpisodeId,
            })
            .pipe(
                map(cycleFromSdk),
                catchError((error: unknown) => rethrowApiError('Menstrual episode delete error', error)),
            );
    }

    public upsertFactor(cycleProfileId: string, payload: UpsertCycleFactorPayload): Observable<CycleResponse> {
        return this.sdk.client
            .putCyclesByCycleProfileIdFactors({ version: this.sdk.version, cycleProfileId, upsertCycleFactorHttpRequest: payload })
            .pipe(
                map(cycleFromSdk),
                catchError((error: unknown) => rethrowApiError('Cycle factor upsert error', error)),
            );
    }
}
