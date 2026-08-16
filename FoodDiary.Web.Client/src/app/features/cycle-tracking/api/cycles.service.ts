import { Service } from '@angular/core';
import { catchError, map, type Observable } from 'rxjs';

import { environment } from '../../../../environments/environment';
import { ApiService } from '../../../services/api.service';
import { fallbackApiError, rethrowApiError } from '../../../shared/lib/api-error.utils';
import type {
    CreateCyclePayload,
    CycleLogDay,
    CycleNutritionSummary,
    CycleResponse,
    UpdateMenstrualEpisodePayload,
    UpsertCycleDayPayload,
    UpsertCycleFactorPayload,
} from '../models/cycle.data';

@Service()
export class CyclesService extends ApiService {
    protected readonly baseUrl = environment.apiUrls.cycles;

    public getCurrent(): Observable<CycleResponse | null> {
        return this.get<CycleResponse | null>('current').pipe(
            catchError((error: unknown) => fallbackApiError('Cycle fetch error', error, null)),
        );
    }

    public getNutritionSummary(dateFrom: string, dateTo: string): Observable<CycleNutritionSummary | null> {
        return this.get<CycleNutritionSummary | null>('current/nutrition-summary', { dateFrom, dateTo }).pipe(
            catchError((error: unknown) => fallbackApiError('Cycle nutrition summary fetch error', error, null)),
        );
    }

    public create(payload: CreateCyclePayload): Observable<CycleResponse> {
        return this.post<CycleResponse>('', payload).pipe(catchError((error: unknown) => rethrowApiError('Cycle create error', error)));
    }

    public upsertDay(cycleProfileId: string, payload: UpsertCycleDayPayload): Observable<CycleLogDay> {
        return this.put<CycleLogDay>(`${cycleProfileId}/days`, payload).pipe(
            map(day => day),
            catchError((error: unknown) => rethrowApiError('Cycle day upsert error', error)),
        );
    }

    public clearDay(cycleProfileId: string, date: string): Observable<void> {
        return this.delete<void>(`${cycleProfileId}/days`, undefined, { date }).pipe(
            catchError((error: unknown) => rethrowApiError('Cycle day clear error', error)),
        );
    }

    public confirmPeriodStart(cycleProfileId: string, date: string): Observable<CycleResponse> {
        return this.put<CycleResponse>(`${cycleProfileId}/period-start`, { date }).pipe(
            catchError((error: unknown) => rethrowApiError('Period start confirmation error', error)),
        );
    }

    public updateMenstrualEpisode(
        cycleProfileId: string,
        menstrualEpisodeId: string,
        payload: UpdateMenstrualEpisodePayload,
    ): Observable<CycleResponse> {
        return this.put<CycleResponse>(`${cycleProfileId}/menstrual-episodes/${menstrualEpisodeId}`, payload).pipe(
            catchError((error: unknown) => rethrowApiError('Menstrual episode update error', error)),
        );
    }

    public deleteMenstrualEpisode(cycleProfileId: string, menstrualEpisodeId: string): Observable<CycleResponse> {
        return this.delete<CycleResponse>(`${cycleProfileId}/menstrual-episodes/${menstrualEpisodeId}`).pipe(
            catchError((error: unknown) => rethrowApiError('Menstrual episode delete error', error)),
        );
    }

    public upsertFactor(cycleProfileId: string, payload: UpsertCycleFactorPayload): Observable<CycleResponse> {
        return this.put<CycleResponse>(`${cycleProfileId}/factors`, payload).pipe(
            map(cycle => cycle),
            catchError((error: unknown) => rethrowApiError('Cycle factor upsert error', error)),
        );
    }
}
