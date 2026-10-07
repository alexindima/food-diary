import type { AdminDailyAdviceGroupHttpResponse } from '../../../shared/api/sdk/generated/model/admin-daily-advice-group-http-response';
import type { AdminDailyAdvicesImportHttpResponse } from '../../../shared/api/sdk/generated/model/admin-daily-advices-import-http-response';
import { requireSdkFields } from '../../../shared/api/sdk/sdk-response';
import type { AdminDailyAdvice, AdminDailyAdvicesImportResponse } from '../models/admin-daily-advice.models';

export function adminDailyAdviceGroupFromSdk(response: AdminDailyAdviceGroupHttpResponse): AdminDailyAdvice {
    const value = requireSdkFields(response, ['id', 'weight']);
    return { ...value, ru: value.ru ?? null, en: value.en ?? null, tag: value.tag ?? null };
}

export function adminDailyAdvicesImportFromSdk(response: AdminDailyAdvicesImportHttpResponse): AdminDailyAdvicesImportResponse {
    const value = requireSdkFields(response, ['importedCount', 'skippedCount']);
    return { ...value };
}
