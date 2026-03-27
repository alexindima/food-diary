export interface HydrationEntry {
    id: string;
    timestampUtc: string;
    amountMl: number;
}

export interface HydrationDaily {
    dateUtc: string;
    totalMl: number;
    goalMl: number | null;
}

export interface CreateHydrationEntryPayload {
    timestampUtc: string;
    amountMl: number;
}
