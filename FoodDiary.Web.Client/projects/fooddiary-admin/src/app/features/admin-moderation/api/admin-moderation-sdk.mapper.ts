import type { AdminContentReportHttpResponse } from '../../../shared/api/sdk/generated/model/admin-content-report-http-response';
import { requireSdkFields } from '../../../shared/api/sdk/sdk-response';
import type { AdminContentReport } from '../models/admin-moderation.data';

export function adminContentReportFromSdk(response: AdminContentReportHttpResponse): AdminContentReport {
    const value = requireSdkFields(response, ['id', 'reporterId', 'targetType', 'targetId', 'reason', 'status', 'createdAtUtc']);
    return { ...value };
}
