export type HydrationEntry = {
    id: string;
    timestampUtc: string;
    amountMl: number;
};

export type HydrationDaily = {
    dateUtc: string;
    totalMl: number;
    goalMl: number | null;
};

export type CreateHydrationEntryPayload = {
    timestampUtc: string;
    amountMl: number;
};
