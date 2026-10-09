import type { AdminId, AdminUtcInstant } from '../../../shared/models/semantics/admin-meaning';
export type AdminTemplateRevision = {
    id: AdminId<'template-revision'>;
    subject: string | null;
    htmlBody: string | null;
    textBody: string;
    isActive: boolean;
    version: number | null;
    savedOnUtc: AdminUtcInstant;
    archivedOnUtc: AdminUtcInstant;
};
