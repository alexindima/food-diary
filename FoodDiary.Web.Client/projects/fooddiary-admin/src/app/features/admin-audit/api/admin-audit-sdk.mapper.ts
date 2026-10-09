import type { AdminAuditEntryHttpResponse } from '../../../shared/api/sdk/generated/model/admin-audit-entry-http-response';
import type { AdminAuditPageHttpResponse } from '../../../shared/api/sdk/generated/model/admin-audit-page-http-response';
import { requireSdkFields } from '../../../shared/api/sdk/sdk-response';
import { adminId, adminUtcInstant, optionalAdminId } from '../../../shared/models/semantics/admin-meaning';
import type { AdminAuditEntry, AdminAuditPageResult } from '../models/admin-audit';

export function adminAuditPageFromSdk(response: AdminAuditPageHttpResponse): AdminAuditPageResult {
    const value = requireSdkFields(response, ['items', 'totalItems']);
    return { ...value, items: value.items.map(item => adminAuditEntryFromSdk(item)) };
}

export function adminAuditEntryFromSdk(response: AdminAuditEntryHttpResponse): AdminAuditEntry {
    const value = requireSdkFields(response, ['id', 'actorUserId', 'action', 'targetType', 'createdAtUtc']);
    return {
        ...value,
        subjectClientUserId: optionalAdminId<'user'>(value.subjectClientUserId ?? null),
        targetId: value.targetId ?? null,
        metadata: value.metadata ?? null,

        actorUserId: adminId<'user'>(value.actorUserId),
        createdAtUtc: adminUtcInstant(value.createdAtUtc),

        id: adminId<'audit-event'>(value.id),
    };
}
