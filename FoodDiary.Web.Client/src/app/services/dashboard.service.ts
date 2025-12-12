import { Injectable } from '@angular/core';
import { Observable, catchError, of } from 'rxjs';
import { ApiService } from './api.service';
import { DashboardSnapshot } from '../types/dashboard.data';
import { environment } from '../../environments/environment';
import { DailyAdvice } from '../types/daily-advice.data';

@Injectable({
    providedIn: 'root',
})
export class DashboardService extends ApiService {
    protected readonly baseUrl = environment.apiUrls.dashboard;

    public getSnapshot(date: Date, page = 1, pageSize = 10): Observable<DashboardSnapshot | null> {
        const params = {
            date: date.toISOString(),
            page,
            pageSize,
        };

        return this.get<DashboardSnapshot>('', params).pipe(
            catchError(error => {
                console.error('Dashboard snapshot fetch error', error);
                return of(null);
            }),
        );
    }

    public getDailyAdvice(date: Date, locale: string): Observable<DailyAdvice | null> {
        const params = {
            date: date.toISOString(),
            locale,
        };

        return this.get<DailyAdvice>('advice', params).pipe(
            catchError(error => {
                console.error('Daily advice fetch error', error);
                return of(null);
            }),
        );
    }
}
