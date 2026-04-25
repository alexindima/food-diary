export type AdminMailInboxMessageSummary = {
    id: string;
    fromAddress?: string | null;
    toRecipients: string[];
    subject?: string | null;
    status: string;
    receivedAtUtc: string;
};

export type AdminMailInboxMessageDetails = AdminMailInboxMessageSummary & {
    messageId?: string | null;
    textBody?: string | null;
    htmlBody?: string | null;
    rawMime: string;
};
