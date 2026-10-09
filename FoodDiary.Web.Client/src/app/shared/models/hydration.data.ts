import type { CalendarDate, UtcInstant } from './semantics/date-value';
import type { HydrationEntryId } from './semantics/entity-id';
export type HydrationEntry = {
    id: HydrationEntryId;
    timestampUtc: UtcInstant;
    amountMl: number;
};

export type HydrationDaily = {
    dateUtc: CalendarDate;
    totalMl: number;
    goalMl: number | null;
};

export type CreateHydrationEntryPayload = {
    timestampUtc: string;
    amountMl: number;
};
