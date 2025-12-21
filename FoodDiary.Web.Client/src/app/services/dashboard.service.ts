import { Injectable } from '@angular/core';
import { Observable, catchError, of } from 'rxjs';
import { ApiService } from './api.service';
import { DashboardSnapshot } from '../types/dashboard.data';
import { environment } from '../../environments/environment';

@Injectable({
    providedIn: 'root',
})
export class DashboardService extends ApiService {
    protected readonly baseUrl = environment.apiUrls.dashboard;

    public getSnapshot(
        date: Date,
        page = 1,
        pageSize = 10,
        locale?: string,
        trendDays?: number,
    ): Observable<DashboardSnapshot | null> {
        const params: Record<string, string | number> = {
            date: date.toISOString(),
            page,
            pageSize,
        };

        if (locale) {
            params['locale'] = locale;
        }

        if (trendDays) {
            params['trendDays'] = trendDays;
        }

        return this.get<DashboardSnapshot>('', params).pipe(
            catchError(error => {
                console.error('Dashboard snapshot fetch error', error);
                return of(null);
            }),
        );
    }
}
