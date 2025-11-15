export interface WeightEntry {
    id: string;
    userId: string;
    date: string;
    weight: number;
}

export interface CreateWeightEntryPayload {
    date: string;
    weight: number;
}

export type UpdateWeightEntryPayload = CreateWeightEntryPayload;

export interface WeightEntryFilters {
    dateFrom?: string;
    dateTo?: string;
    limit?: number;
    sort?: 'asc' | 'desc';
}
