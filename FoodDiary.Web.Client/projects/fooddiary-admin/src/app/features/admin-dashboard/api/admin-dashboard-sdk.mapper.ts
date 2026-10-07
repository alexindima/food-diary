import type { AdminBillingRevenueCurrencyHttpResponse } from '../../../shared/api/sdk/generated/model/admin-billing-revenue-currency-http-response';
import type { AdminDashboardMetricsHttpResponse } from '../../../shared/api/sdk/generated/model/admin-dashboard-metrics-http-response';
import type { AdminDashboardOverviewHttpResponse } from '../../../shared/api/sdk/generated/model/admin-dashboard-overview-http-response';
import type { AdminDashboardPeriodHttpResponse } from '../../../shared/api/sdk/generated/model/admin-dashboard-period-http-response';
import type { AdminDashboardRevenuePointHttpResponse } from '../../../shared/api/sdk/generated/model/admin-dashboard-revenue-point-http-response';
import type { AdminDashboardSummaryHttpResponse } from '../../../shared/api/sdk/generated/model/admin-dashboard-summary-http-response';
import type { AdminDashboardTrendHttpResponse } from '../../../shared/api/sdk/generated/model/admin-dashboard-trend-http-response';
import type { AdminUserHttpResponse } from '../../../shared/api/sdk/generated/model/admin-user-http-response';
import type { FastingTelemetryPresetHttpResponse } from '../../../shared/api/sdk/generated/model/fasting-telemetry-preset-http-response';
import type { FastingTelemetrySummaryHttpResponse } from '../../../shared/api/sdk/generated/model/fasting-telemetry-summary-http-response';
import { requireSdkFields, sdkEnum, sdkOptional } from '../../../shared/api/sdk/sdk-response';
import type { AdminDashboardSummary, AdminDashboardUser } from '../models/admin-dashboard.data';
import type { AdminDashboardOverview, DashboardCurrency, DashboardPeriod, DashboardTrend } from '../models/admin-dashboard-overview.data';
import type { FastingTelemetryPresetSummary, FastingTelemetrySummary } from '../models/admin-telemetry.data';

export function adminDashboardSummaryFromSdk(response: AdminDashboardSummaryHttpResponse): AdminDashboardSummary {
    const value = requireSdkFields(response, [
        'totalUsers',
        'activeUsers',
        'premiumUsers',
        'deletedUsers',
        'pendingReportsCount',
        'recentUsers',
    ]);
    return { ...value, recentUsers: value.recentUsers.map(item => adminUserFromSdk(item)) };
}

export function adminUserFromSdk(response: AdminUserHttpResponse): AdminDashboardUser {
    const value = requireSdkFields(response, ['id', 'isActive', 'createdOnUtc', 'roles']);
    return { ...value, email: value.email ?? null };
}

export function adminDashboardOverviewFromSdk(response: AdminDashboardOverviewHttpResponse): AdminDashboardOverview {
    const value = requireSdkFields(response, [
        'fromUtc',
        'toUtc',
        'interval',
        'period',
        'totalUsersNow',
        'premiumUsersNow',
        'pendingReportsNow',
    ]);
    return {
        ...value,
        interval: sdkEnum(value.interval, ['day', 'month'] as const),
        period: adminDashboardPeriodFromSdk(value.period),
        previous: sdkOptional(value.previous, adminDashboardPeriodFromSdk),
    };
}

export function adminDashboardPeriodFromSdk(response: AdminDashboardPeriodHttpResponse): DashboardPeriod {
    const value = requireSdkFields(response, ['fromUtc', 'toUtc', 'metrics', 'currencies']);
    return {
        ...value,
        metrics: adminDashboardMetricsFromSdk(value.metrics),
        currencies: value.currencies.map(item => adminBillingRevenueCurrencyFromSdk(item)),
    };
}

export function adminDashboardMetricsFromSdk(response: AdminDashboardMetricsHttpResponse): {
    registrations: number;
    payingUsers: number;
    aiTokens: number;
    trend: DashboardTrend[];
} {
    const value = requireSdkFields(response, ['registrations', 'payingUsers', 'aiTokens', 'trend']);
    return { ...value, trend: value.trend.map(item => adminDashboardTrendFromSdk(item)) };
}

export function adminDashboardTrendFromSdk(response: AdminDashboardTrendHttpResponse): DashboardTrend {
    const value = requireSdkFields(response, ['date', 'registrations', 'aiTokens', 'revenue']);
    return { ...value, revenue: value.revenue.map(item => adminDashboardRevenuePointFromSdk(item)) };
}

export function adminDashboardRevenuePointFromSdk(response: AdminDashboardRevenuePointHttpResponse): { currency: string; gross: number } {
    const value = requireSdkFields(response, ['currency', 'gross']);
    return { ...value };
}

export function adminBillingRevenueCurrencyFromSdk(response: AdminBillingRevenueCurrencyHttpResponse): DashboardCurrency {
    const value = requireSdkFields(response, ['currency', 'gross', 'net', 'refunds', 'chargebacks', 'successfulPayments']);
    return { ...value };
}

export function fastingTelemetrySummaryFromSdk(response: FastingTelemetrySummaryHttpResponse): FastingTelemetrySummary {
    const value = requireSdkFields(response, [
        'windowHours',
        'generatedAtUtc',
        'startedSessions',
        'completedSessions',
        'savedCheckIns',
        'reminderPresetSelections',
        'reminderTimingSaves',
        'presetReminderTimingSaves',
        'manualReminderTimingSaves',
        'completionRatePercent',
        'checkInRatePercent',
        'topPresets',
    ]);
    return {
        ...value,
        averageCompletedDurationHours: value.averageCompletedDurationHours ?? null,
        lastCheckInAtUtc: value.lastCheckInAtUtc ?? null,
        lastEventAtUtc: value.lastEventAtUtc ?? null,
        topPresets: value.topPresets.map(item => fastingTelemetryPresetFromSdk(item)),
    };
}

export function fastingTelemetryPresetFromSdk(response: FastingTelemetryPresetHttpResponse): FastingTelemetryPresetSummary {
    const value = requireSdkFields(response, [
        'presetId',
        'selectionCount',
        'timingSaveCount',
        'startedSessions',
        'completedSessions',
        'savedCheckIns',
        'completionRatePercent',
        'checkInRatePercent',
    ]);
    return { ...value, firstReminderHours: value.firstReminderHours ?? null, followUpReminderHours: value.followUpReminderHours ?? null };
}
