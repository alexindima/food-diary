import { Injectable } from '@angular/core';
import { Observable, catchError, of } from 'rxjs';
import { environment } from '../../../../environments/environment';
import { ApiService } from '../../../services/api.service';
import { DashboardSnapshot } from '../models/dashboard.data';

@Injectable({
    providedIn: 'root',
})
export class DashboardService extends ApiService {
    protected readonly baseUrl = environment.apiUrls.dashboard;

    public getSnapshot(date: Date, page = 1, pageSize = 10, locale?: string, trendDays?: number): Observable<DashboardSnapshot | null> {
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
