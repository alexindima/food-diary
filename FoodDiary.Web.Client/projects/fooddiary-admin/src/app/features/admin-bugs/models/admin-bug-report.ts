export type AdminBugReport = {
    id: string;
    sourceMessageId: string;
    receivedAtUtc: string;
    subject: string;
    status: string;
    attempt: number;
    summary: string | null;
    mergeRequestUrl: string | null;
    contentExpired: boolean;
};

export type AdminBugReportPage = {
    items: AdminBugReport[];
    totalItems: number;
    isConfigured: boolean;
};
