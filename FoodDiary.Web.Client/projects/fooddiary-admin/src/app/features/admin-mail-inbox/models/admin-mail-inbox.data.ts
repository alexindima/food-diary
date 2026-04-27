export type AdminMailInboxMessageSummary = {
    id: string;
    fromAddress?: string | null;
    toRecipients: string[];
    subject?: string | null;
    category: 'general' | 'dmarc-report' | string;
    status: string;
    receivedAtUtc: string;
};

export type AdminMailInboxMessageDetails = AdminMailInboxMessageSummary & {
    messageId?: string | null;
    textBody?: string | null;
    htmlBody?: string | null;
    rawMime: string;
    dmarcReport?: AdminMailInboxDmarcReport | null;
};

export type AdminMailInboxDmarcReport = {
    organizationName?: string | null;
    reportId?: string | null;
    domain?: string | null;
    dateRangeStartUtc?: string | null;
    dateRangeEndUtc?: string | null;
    records: AdminMailInboxDmarcRecord[];
};

export type AdminMailInboxDmarcRecord = {
    sourceIp?: string | null;
    count: number;
    disposition?: string | null;
    dkim?: string | null;
    spf?: string | null;
    headerFrom?: string | null;
    envelopeFrom?: string | null;
    dkimDomain?: string | null;
    dkimResult?: string | null;
    spfDomain?: string | null;
    spfResult?: string | null;
};
