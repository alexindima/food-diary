import { Injectable } from '@angular/core';
import { Observable, catchError, of } from 'rxjs';
import { HttpErrorResponse } from '@angular/common/http';
import { ApiService } from './api.service';
import { environment } from '../../environments/environment';
import {
    CreateWaistEntryPayload,
    UpdateWaistEntryPayload,
    WaistEntry,
    WaistEntryFilters,
    WaistEntrySummaryFilters,
    WaistEntrySummaryPoint,
} from '../types/waist-entry.data';

@Injectable({
    providedIn: 'root',
})
export class WaistEntriesService extends ApiService {
    protected readonly baseUrl = environment.apiUrls.waists;

    public getEntries(filters?: WaistEntryFilters): Observable<WaistEntry[]> {
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

        return this.get<WaistEntry[]>('', params).pipe(
            catchError((error: HttpErrorResponse) => {
                console.error('Waist entries fetch error', error);
                return of([]);
            }),
        );
    }

    public getLatest(): Observable<WaistEntry | null> {
        return this.get<WaistEntry | null>('latest').pipe(
            catchError((error: HttpErrorResponse) => {
                console.error('Waist latest fetch error', error);
                return of(null);
            }),
        );
    }

    public create(payload: CreateWaistEntryPayload): Observable<WaistEntry> {
        return this.post<WaistEntry>('', payload).pipe(
            catchError((error: HttpErrorResponse) => {
                console.error('Create waist entry error', error);
                throw error;
            }),
        );
    }

    public update(id: string, payload: UpdateWaistEntryPayload): Observable<WaistEntry> {
        return this.put<WaistEntry>(`${id}`, payload).pipe(
            catchError((error: HttpErrorResponse) => {
                console.error('Update waist entry error', error);
                throw error;
            }),
        );
    }

    public remove(id: string): Observable<void> {
        return super.delete<void>(`${id}`).pipe(
            catchError((error: HttpErrorResponse) => {
                console.error('Delete waist entry error', error);
                throw error;
            }),
        );
    }

    public getSummary(filters: WaistEntrySummaryFilters): Observable<WaistEntrySummaryPoint[]> {
        const params: Record<string, string | number> = {
            dateFrom: filters.dateFrom,
            dateTo: filters.dateTo,
            quantizationDays: filters.quantizationDays,
        };

        return this.get<WaistEntrySummaryPoint[]>('summary', params).pipe(
            catchError((error: HttpErrorResponse) => {
                console.error('Waist summary fetch error', error);
                return of([]);
            }),
        );
    }
}
