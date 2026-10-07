import type { AdminAiUsageBreakdownHttpResponse } from '../../../shared/api/sdk/generated/model/admin-ai-usage-breakdown-http-response';
import type { AdminAiUsageDailyHttpResponse } from '../../../shared/api/sdk/generated/model/admin-ai-usage-daily-http-response';
import type { AdminAiUsageSummaryHttpResponse } from '../../../shared/api/sdk/generated/model/admin-ai-usage-summary-http-response';
import type { AdminAiUsageUserHttpResponse } from '../../../shared/api/sdk/generated/model/admin-ai-usage-user-http-response';
import { requireSdkFields } from '../../../shared/api/sdk/sdk-response';
import type { AdminAiUsageBreakdown, AdminAiUsageDaily, AdminAiUsageSummary, AdminAiUsageUser } from '../models/admin-ai-usage.data';

export function adminAiUsageSummaryFromSdk(response: AdminAiUsageSummaryHttpResponse): AdminAiUsageSummary {
    const value = requireSdkFields(response, ['totalTokens', 'inputTokens', 'outputTokens', 'byDay', 'byOperation', 'byModel', 'byUser']);
    return {
        ...value,
        byDay: value.byDay.map(item => adminAiUsageDailyFromSdk(item)),
        byOperation: value.byOperation.map(item => adminAiUsageBreakdownFromSdk(item)),
        byModel: value.byModel.map(item => adminAiUsageBreakdownFromSdk(item)),
        byUser: value.byUser.map(item => adminAiUsageUserFromSdk(item)),
    };
}

export function adminAiUsageDailyFromSdk(response: AdminAiUsageDailyHttpResponse): AdminAiUsageDaily {
    const value = requireSdkFields(response, ['date', 'totalTokens', 'inputTokens', 'outputTokens']);
    return { ...value };
}

export function adminAiUsageBreakdownFromSdk(response: AdminAiUsageBreakdownHttpResponse): AdminAiUsageBreakdown {
    const value = requireSdkFields(response, ['key', 'totalTokens', 'inputTokens', 'outputTokens']);
    return { ...value };
}

export function adminAiUsageUserFromSdk(response: AdminAiUsageUserHttpResponse): AdminAiUsageUser {
    const value = requireSdkFields(response, ['id', 'totalTokens', 'inputTokens', 'outputTokens']);
    return { ...value, email: value.email ?? null };
}
