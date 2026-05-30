export type ExportFormat = 'csv' | 'pdf';

export type ExportDiaryRequest = {
    dateFrom: string;
    dateTo: string;
    format?: ExportFormat;
    locale?: string;
    timeZoneOffsetMinutes?: number;
};
