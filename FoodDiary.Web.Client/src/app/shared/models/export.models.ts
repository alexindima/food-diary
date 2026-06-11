export type ExportFormat = 'csv' | 'pdf';

export type ExportDiaryRequest = {
    dateFrom: string;
    dateTo: string;
    format?: ExportFormat;
    locale?: string;
    timeZoneOffsetMinutes?: number;
};

export type ExportCycleRequest = {
    dateFrom: string;
    dateTo: string;
    timeZoneOffsetMinutes?: number;
};
