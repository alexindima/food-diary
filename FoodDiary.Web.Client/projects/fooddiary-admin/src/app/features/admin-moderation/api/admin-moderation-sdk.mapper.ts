import type { AdminContentReportHttpResponse } from '../../../shared/api/sdk/generated/model/admin-content-report-http-response';
import { requireSdkFields } from '../../../shared/api/sdk/sdk-response';
import { adminId, adminUtcInstant, optionalAdminId, optionalAdminUtcInstant } from '../../../shared/models/semantics/admin-meaning';
import type { AdminContentReport } from '../models/admin-moderation.data';

export function adminContentReportFromSdk(response: AdminContentReportHttpResponse): AdminContentReport {
    const value = requireSdkFields(response, ['id', 'reporterId', 'targetType', 'targetId', 'reason', 'status', 'createdAtUtc']);
    return {
        ...value,
        id: adminId<'content-report'>(value.id),
        reporterId: adminId<'user'>(value.reporterId),
        createdAtUtc: adminUtcInstant(value.createdAtUtc),
        reviewedAtUtc: optionalAdminUtcInstant(value.reviewedAtUtc),
        reviewedByUserId: optionalAdminId<'user'>(value.reviewedByUserId),
    };
}
