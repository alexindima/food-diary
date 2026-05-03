import { Injectable } from '@angular/core';
import { catchError, map, type Observable } from 'rxjs';

import { environment } from '../../../../environments/environment';
import { ApiService } from '../../../services/api.service';
import { fallbackApiError, rethrowApiError } from '../../../shared/lib/api-error.utils';
import { type CreateCyclePayload, type CycleDay, type CycleResponse, type UpsertCycleDayPayload } from '../models/cycle.data';

@Injectable({
    providedIn: 'root',
})
export class CyclesService extends ApiService {
    protected readonly baseUrl = environment.apiUrls.cycles;

    public getCurrent(): Observable<CycleResponse | null> {
        return this.get<CycleResponse | null>('current').pipe(catchError(error => fallbackApiError('Cycle fetch error', error, null)));
    }

    public create(payload: CreateCyclePayload): Observable<CycleResponse> {
        return this.post<CycleResponse>('', payload).pipe(catchError(error => rethrowApiError('Cycle create error', error)));
    }

    public upsertDay(cycleId: string, payload: UpsertCycleDayPayload): Observable<CycleDay> {
        return this.put<CycleDay>(`${cycleId}/days`, payload).pipe(
            map(day => ({
                ...day,
                date: day.date,
            })),
            catchError(error => rethrowApiError('Cycle day upsert error', error)),
        );
    }
}
