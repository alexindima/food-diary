import { Injectable } from '@angular/core';
import { Observable, catchError, of } from 'rxjs';
import { HttpErrorResponse } from '@angular/common/http';
import { ApiService } from './api.service';
import { environment } from '../../environments/environment';
import {
    CreateWeightEntryPayload,
    UpdateWeightEntryPayload,
    WeightEntry,
    WeightEntryFilters,
} from '../types/weight-entry.data';

@Injectable({
    providedIn: 'root',
})
export class WeightEntriesService extends ApiService {
    protected readonly baseUrl = environment.apiUrls.weights;

    public getEntries(filters?: WeightEntryFilters): Observable<WeightEntry[]> {
        const params: Record<string, string | number> = {};

        if (filters?.dateFrom) {
            params['dateFrom'] = filters.dateFrom;
        }
        if (filters?.dateTo) {
            params['dateTo'] = filters.dateTo;
        }
        if (filters?.limit) {
            params['limit'] = filters.limit;
        }
        if (filters?.sort) {
            params['sort'] = filters.sort;
        }

        return this.get<WeightEntry[]>('', params).pipe(
            catchError((error: HttpErrorResponse) => {
                console.error('Weight entries fetch error', error);
                return of([]);
            }),
        );
    }

    public getLatest(): Observable<WeightEntry | null> {
        return this.get<WeightEntry | null>('latest').pipe(
            catchError((error: HttpErrorResponse) => {
                console.error('Weight latest fetch error', error);
                return of(null);
            }),
        );
    }

    public create(payload: CreateWeightEntryPayload): Observable<WeightEntry> {
        return this.post<WeightEntry>('', payload).pipe(
            catchError((error: HttpErrorResponse) => {
                console.error('Create weight entry error', error);
                throw error;
            }),
        );
    }

    public update(id: string, payload: UpdateWeightEntryPayload): Observable<WeightEntry> {
        return this.put<WeightEntry>(`${id}`, payload).pipe(
            catchError((error: HttpErrorResponse) => {
                console.error('Update weight entry error', error);
                throw error;
            }),
        );
    }

    public remove(id: string): Observable<void> {
        return super.delete<void>(`${id}`).pipe(
            catchError((error: HttpErrorResponse) => {
                console.error('Delete weight entry error', error);
                throw error;
            }),
        );
    }
}
