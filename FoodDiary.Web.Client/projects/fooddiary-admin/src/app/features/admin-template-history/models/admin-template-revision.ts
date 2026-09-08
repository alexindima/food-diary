export type AdminTemplateRevision = {
    id: string;
    subject: string | null;
    htmlBody: string | null;
    textBody: string;
    isActive: boolean;
    version: number | null;
    savedOnUtc: string;
    archivedOnUtc: string;
};
