export type WeightEntry = {
    id: string;
    userId: string;
    date: string;
    weight: number;
};

export type CreateWeightEntryPayload = {
    date: string;
    weight: number;
};

export type UpdateWeightEntryPayload = CreateWeightEntryPayload;

export type WeightEntryFilters = {
    dateFrom?: string;
    dateTo?: string;
    limit?: number;
    sort?: 'asc' | 'desc';
};

export type WeightEntrySummaryPoint = {
    startDate: string;
    endDate: string;
    averageWeight: number;
};

export type WeightEntrySummaryFilters = {
    dateFrom: string;
    dateTo: string;
    quantizationDays: number;
};
