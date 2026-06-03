import { HttpContext } from '@angular/common/http';
import { inject, Service } from '@angular/core';
import { catchError, type Observable } from 'rxjs';

import { environment } from '../../../../environments/environment';
import { SKIP_GLOBAL_LOADING } from '../../../constants/global-loading-context.tokens';
import { ApiService } from '../../../services/api.service';
import { fallbackApiError, rethrowApiError } from '../../../shared/lib/api-error.utils';
import type { DashboardSnapshot } from '../models/dashboard.data';
import { DASHBOARD_SNAPSHOT_QUERY_DEFAULTS } from './dashboard-api.tokens';

export type DashboardSnapshotQuery = {
    date: Date;
    page?: number;
    pageSize?: number;
    locale?: string;
    trendDays?: number;
};

@Service()
export class DashboardService extends ApiService {
    protected readonly baseUrl = environment.apiUrls.dashboard;
    private readonly snapshotQueryDefaults = inject(DASHBOARD_SNAPSHOT_QUERY_DEFAULTS);
    private readonly silentLoadingContext = new HttpContext().set(SKIP_GLOBAL_LOADING, true);

    public getSnapshot(query: DashboardSnapshotQuery): Observable<DashboardSnapshot | null> {
        return this.requestSnapshot(query).pipe(
            catchError((error: unknown) => fallbackApiError('Dashboard snapshot fetch error', error, null)),
        );
    }

    public getSnapshotStrict(query: DashboardSnapshotQuery): Observable<DashboardSnapshot> {
        return this.requestSnapshot(query).pipe(catchError((error: unknown) => rethrowApiError('Dashboard snapshot fetch error', error)));
    }

    public getSnapshotSilently(query: DashboardSnapshotQuery): Observable<DashboardSnapshot | null> {
        return this.requestSnapshot(query, this.silentLoadingContext).pipe(
            catchError((error: unknown) => fallbackApiError('Dashboard snapshot fetch error', error, null)),
        );
    }

    public getSnapshotSilentlyStrict(query: DashboardSnapshotQuery): Observable<DashboardSnapshot> {
        return this.requestSnapshot(query, this.silentLoadingContext).pipe(
            catchError((error: unknown) => rethrowApiError('Dashboard snapshot fetch error', error)),
        );
    }

    private requestSnapshot(query: DashboardSnapshotQuery, context?: HttpContext): Observable<DashboardSnapshot> {
        const params = this.createSnapshotParams(query);
        return this.get<DashboardSnapshot>('', params, undefined, context);
    }

    private createSnapshotParams(query: DashboardSnapshotQuery): Record<string, string | number> {
        const { date, page = this.snapshotQueryDefaults.page, pageSize = this.snapshotQueryDefaults.pageSize, locale, trendDays } = query;
        const params: Record<string, string | number> = {
            date: date.toISOString(),
            page,
            pageSize,
        };

        if (locale !== undefined && locale.trim().length > 0) {
            params['locale'] = locale;
        }

        if (trendDays !== undefined) {
            params['trendDays'] = trendDays;
        }

        return params;
    }
}
