import type { AdminId, AdminUtcInstant } from '../../../shared/models/semantics/admin-meaning';
export type OutgoingEmail = {
    id: AdminId<'outgoing-email'>;
    status: string;
    purpose: string;
    fromAddress: string;
    to: string[];
    subject: string;
    createdAtUtc: AdminUtcInstant;
    sentAtUtc: AdminUtcInstant | null;
    attemptCount: number;
    maxAttempts: number;
    correlationId: string | null;
    textBody: string | null;
    contentHidden: boolean;
    replyTo: string | null;
    inReplyTo: string | null;
};

export type OutgoingEmailPage = {
    items: OutgoingEmail[];
    totalItems: number;
    statusCounts?: Record<string, number> | null;
};
