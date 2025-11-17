export interface WaistEntry {
    id: string;
    userId: string;
    date: string;
    circumference: number;
}

export interface CreateWaistEntryPayload {
    date: string;
    circumference: number;
}

export type UpdateWaistEntryPayload = CreateWaistEntryPayload;

export interface WaistEntryFilters {
    dateFrom?: string;
    dateTo?: string;
    limit?: number;
    sort?: 'asc' | 'desc';
}

export interface WaistEntrySummaryPoint {
    dateFrom: string;
    dateTo: string;
    averageCircumference: number;
}

export interface WaistEntrySummaryFilters {
    dateFrom: string;
    dateTo: string;
    quantizationDays: number;
}
