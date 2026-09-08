export type AdminAuditEntry = {
    id: string;
    actorUserId: string;
    subjectClientUserId: string | null;
    action: string;
    targetType: string;
    targetId: string | null;
    metadata: string | null;
    createdAtUtc: string;
};

export type AdminAuditPageResult = {
    items: AdminAuditEntry[];
    totalItems: number;
};
