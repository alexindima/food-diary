import type { CalendarDate } from './semantics/date-value';
import type { UserId, WaistEntryId } from './semantics/entity-id';
export type WaistEntry = {
    id: WaistEntryId;
    userId: UserId;
    date: CalendarDate;
    circumferenceCm: number;
};

export type CreateWaistEntryPayload = {
    date: string;
    circumferenceCm: number;
};

export type UpdateWaistEntryPayload = CreateWaistEntryPayload;

export type WaistEntryFilters = {
    dateFrom?: string;
    dateTo?: string;
    limit?: number;
    sort?: 'asc' | 'desc';
};

export type WaistEntrySummaryPoint = {
    startDate: CalendarDate;
    endDate: CalendarDate;
    averageCircumferenceCm: number;
};

export type WaistEntrySummaryFilters = {
    dateFrom: string;
    dateTo: string;
    quantizationDays: number;
};

export type WaistHistoryPageSummary = {
    entries: WaistEntry[];
    summary: WaistEntrySummaryPoint[];
    heightCm: number | null;
    goal: DesiredWaistResponse;
    goalHistory: WaistGoalHistoryItem[];
};

export type WaistHistoryPageSummaryFilters = WaistEntrySummaryFilters & {
    entriesLimit: number;
};
import type { DesiredWaistResponse, WaistGoalHistoryItem } from './user.data';
