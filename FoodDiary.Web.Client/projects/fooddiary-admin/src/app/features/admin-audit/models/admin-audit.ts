import type { AdminId, AdminUtcInstant } from '../../../shared/models/semantics/admin-meaning';
export type AdminAuditEntry = {
    id: AdminId<'audit-event'>;
    actorUserId: AdminId<'user'>;
    subjectClientUserId: AdminId<'user'> | null;
    action: string;
    targetType: string;
    targetId: string | null;
    metadata: string | null;
    createdAtUtc: AdminUtcInstant;
};

export type AdminAuditPageResult = {
    items: AdminAuditEntry[];
    totalItems: number;
};
