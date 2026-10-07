import { HttpClient, HttpContext } from '@angular/common/http';
import { inject, Service } from '@angular/core';
import { catchError, map, type Observable } from 'rxjs';

import { environment } from '../../../../environments/environment';
import { SKIP_GLOBAL_LOADING } from '../../../constants/global-loading-context.tokens';
import { dashboardSnapshotFromSdk } from '../../../shared/api/sdk/dashboard-sdk.mapper';
import { DashboardSdk, type GetDashboardRequestParams } from '../../../shared/api/sdk/generated/api/dashboard.service';
import { createSdkConnection, sdkRequestOptions } from '../../../shared/api/sdk/sdk-connection';
import { fallbackApiError, rethrowApiError } from '../../../shared/lib/api-error.utils';
import type { DashboardSnapshot } from '../../../shared/models/dashboard.data';
import { DASHBOARD_SNAPSHOT_QUERY_DEFAULTS } from './dashboard-api.tokens';

export type DashboardSnapshotQuery = {
    date: Date;
    timeZoneOffsetMinutes: number;
    timeZoneId?: string;
    page?: number;
    pageSize?: number;
    locale?: string;
    trendDays?: number;
};

@Service()
export class DashboardService {
    protected readonly baseUrl = environment.apiUrls.dashboard;
    private readonly sdk = createSdkConnection(DashboardSdk, this.baseUrl, inject(HttpClient));
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
        return this.sdk.client
            .getDashboard(params, 'body', false, sdkRequestOptions(undefined, context))
            .pipe(map(dashboardSnapshotFromSdk));
    }

    private createSnapshotParams(query: DashboardSnapshotQuery): GetDashboardRequestParams {
        const { date, page = this.snapshotQueryDefaults.page, pageSize = this.snapshotQueryDefaults.pageSize, locale, trendDays } = query;
        const params: GetDashboardRequestParams = {
            version: this.sdk.version,
            date: date.toISOString(),
            timeZoneOffsetMinutes: query.timeZoneOffsetMinutes,
            page,
            limit: pageSize,
        };

        if (query.timeZoneId !== undefined) {
            params['timeZoneId'] = query.timeZoneId;
        }

        if (locale !== undefined && locale.trim().length > 0) {
            params['locale'] = locale;
        }

        if (trendDays !== undefined) {
            params['trendDays'] = trendDays;
        }

        return params;
    }
}
