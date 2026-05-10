import { HttpContext } from '@angular/common/http';
import { Injectable } from '@angular/core';
import { catchError, type Observable } from 'rxjs';

import { environment } from '../../../../environments/environment';
import { SKIP_GLOBAL_LOADING } from '../../../constants/global-loading-context.tokens';
import { ApiService } from '../../../services/api.service';
import { fallbackApiError } from '../../../shared/lib/api-error.utils';
import type { DashboardSnapshot } from '../models/dashboard.data';

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
}
