import { Injectable } from '@angular/core';
import { HttpContext } from '@angular/common/http';
import { Observable, catchError } from 'rxjs';
import { environment } from '../../../../environments/environment';
import { ApiService } from '../../../services/api.service';
import { DashboardSnapshot } from '../models/dashboard.data';
import { fallbackApiError } from '../../../shared/lib/api-error.utils';
import { SKIP_GLOBAL_LOADING } from '../../../constants/global-loading-context.tokens';

@Injectable({
    providedIn: 'root',
})
export class DashboardService extends ApiService {
    protected readonly baseUrl = environment.apiUrls.dashboard;
    private readonly silentLoadingContext = new HttpContext().set(SKIP_GLOBAL_LOADING, true);

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
            catchError(error => fallbackApiError('Dashboard snapshot fetch error', error, null)),
        );
    }

    public getSnapshotSilently(
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

        return this.get<DashboardSnapshot>('', params, undefined, this.silentLoadingContext).pipe(
            catchError(error => fallbackApiError('Dashboard snapshot fetch error', error, null)),
        );
    }

    public sendTestEmail(): Observable<void> {
        return this.post<void>('test-email', {});
    }
}
