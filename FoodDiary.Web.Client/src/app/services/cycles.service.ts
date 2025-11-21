import { Injectable } from '@angular/core';
import { Observable, map, catchError, of } from 'rxjs';
import { ApiService } from './api.service';
import {
    CycleResponse,
    CreateCyclePayload,
    UpsertCycleDayPayload,
    CycleDay,
} from '../types/cycle.data';
import { environment } from '../../environments/environment';

@Injectable({
    providedIn: 'root',
})
export class CyclesService extends ApiService {
    protected readonly baseUrl = environment.apiUrls.cycles;

    public getCurrent(): Observable<CycleResponse | null> {
        return this.get<CycleResponse | null>('current').pipe(
            catchError(error => {
                console.error('Cycle fetch error', error);
                return of(null);
            }),
        );
    }

    public create(payload: CreateCyclePayload): Observable<CycleResponse> {
        return this.post<CycleResponse>('', payload);
    }

    public upsertDay(cycleId: string, payload: UpsertCycleDayPayload): Observable<CycleDay> {
        return this.put<CycleDay>(`${cycleId}/days`, payload).pipe(
            map(day => ({
                ...day,
                date: day.date,
            })),
        );
    }
}
