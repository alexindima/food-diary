import type { AdminRetentionCohortHttpResponse } from '../../../shared/api/sdk/generated/model/admin-retention-cohort-http-response';
import type { AdminRetentionDayHttpResponse } from '../../../shared/api/sdk/generated/model/admin-retention-day-http-response';
import type { AdminRetentionReportHttpResponse } from '../../../shared/api/sdk/generated/model/admin-retention-report-http-response';
import { requireSdkFields } from '../../../shared/api/sdk/sdk-response';
import type { AdminRetentionCohort, AdminRetentionReport } from '../models/admin-retention';

export function adminRetentionReportFromSdk(response: AdminRetentionReportHttpResponse): AdminRetentionReport {
    const value = requireSdkFields(response, [
        'fromUtc',
        'toUtc',
        'asOfUtc',
        'cohortFromUtc',
        'cohortToUtc',
        'mealEntriesInPeriod',
        'activeUsersInPeriod',
        'cohorts',
        'activityByDay',
    ]);
    return {
        ...value,
        cohorts: value.cohorts.map(item => adminRetentionCohortFromSdk(item)),
        activityByDay: value.activityByDay.map(item => adminRetentionDayFromSdk(item)),
    };
}

export function adminRetentionCohortFromSdk(response: AdminRetentionCohortHttpResponse): AdminRetentionCohort {
    const value = requireSdkFields(response, ['date', 'registered']);
    return {
        ...value,
        activatedWithinSevenDays: value.activatedWithinSevenDays ?? null,
        day1: value.day1 ?? null,
        day7: value.day7 ?? null,
        day30: value.day30 ?? null,
    };
}

export function adminRetentionDayFromSdk(response: AdminRetentionDayHttpResponse): {
    date: string;
    activeUsers: number;
    mealEntries: number;
} {
    const value = requireSdkFields(response, ['date', 'activeUsers', 'mealEntries']);
    return { ...value };
}
