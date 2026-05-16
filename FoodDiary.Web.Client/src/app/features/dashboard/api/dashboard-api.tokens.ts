import { InjectionToken } from '@angular/core';

const DEFAULT_DASHBOARD_SNAPSHOT_PAGE = 1;
const DEFAULT_DASHBOARD_SNAPSHOT_PAGE_SIZE = 10;

export type DashboardSnapshotQueryDefaults = {
    page: number;
    pageSize: number;
};

export const DASHBOARD_SNAPSHOT_QUERY_DEFAULTS = new InjectionToken<DashboardSnapshotQueryDefaults>('DASHBOARD_SNAPSHOT_QUERY_DEFAULTS', {
    providedIn: 'root',
    factory: (): DashboardSnapshotQueryDefaults => ({
        page: DEFAULT_DASHBOARD_SNAPSHOT_PAGE,
        pageSize: DEFAULT_DASHBOARD_SNAPSHOT_PAGE_SIZE,
    }),
});
