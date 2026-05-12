export type WaistEntry = {
    id: string;
    userId: string;
    date: string;
    circumference: number;
};

export type CreateWaistEntryPayload = {
    date: string;
    circumference: number;
};

export type UpdateWaistEntryPayload = CreateWaistEntryPayload;

export type WaistEntryFilters = {
    dateFrom?: string;
    dateTo?: string;
    limit?: number;
    sort?: 'asc' | 'desc';
};

export type WaistEntrySummaryPoint = {
    startDate: string;
    endDate: string;
    averageCircumference: number;
};

export type WaistEntrySummaryFilters = {
    dateFrom: string;
    dateTo: string;
    quantizationDays: number;
};
