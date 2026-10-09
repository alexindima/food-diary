import type { AdminId, AdminUtcInstant } from '../../../shared/models/semantics/admin-meaning';
export type AdminEmailTemplate = {
    id: AdminId<'email-template'>;
    key: string;
    locale: string;
    subject: string;
    htmlBody: string;
    textBody: string;
    isActive: boolean;
    createdOnUtc: AdminUtcInstant;
    updatedOnUtc?: AdminUtcInstant | null;
};

export type AdminEmailTemplateUpsertRequest = {
    subject: string;
    htmlBody: string;
    textBody: string;
    isActive: boolean;
};

export type AdminEmailTemplateTestRequest = {
    toEmail: string;
    key: string;
    subject: string;
    htmlBody: string;
    textBody: string;
};
