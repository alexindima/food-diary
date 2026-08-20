export type AdminMailInboxMessageSummary = {
    id: string;
    fromAddress?: string | null;
    envelopeFromAddress?: string | null;
    isTrustedRelay: boolean;
    fromAddressIsVerified: boolean;
    toRecipients: string[];
    subject?: string | null;
    category: string;
    status: string;
    readAtUtc?: string | null;
    receivedAtUtc: string;
};

export type AdminMailInboxMessageDetails = AdminMailInboxMessageSummary & {
    messageId?: string | null;
    textBody?: string | null;
    htmlBody?: string | null;
    rawMime?: string | null;
    contentPurgedAtUtc?: string | null;
    dmarcReport?: AdminMailInboxDmarcReport | null;
    dmarcReportIsVerified: boolean;
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
