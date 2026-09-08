export type OutgoingEmail = {
    id: string;
    status: string;
    purpose: string;
    fromAddress: string;
    to: string[];
    subject: string;
    createdAtUtc: string;
    sentAtUtc: string | null;
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
