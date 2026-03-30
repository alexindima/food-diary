export type AdminEmailTemplate = {
    id: string;
    key: string;
    locale: string;
    subject: string;
    htmlBody: string;
    textBody: string;
    isActive: boolean;
    createdOnUtc: string;
    updatedOnUtc?: string | null;
};

export type AdminEmailTemplateUpsertRequest = {
    subject: string;
    htmlBody: string;
    textBody: string;
    isActive: boolean;
};
