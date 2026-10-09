import type { CalendarDate } from './semantics/date-value';
import type { UserId, WeightEntryId } from './semantics/entity-id';
export type WeightEntry = {
    id: WeightEntryId;
    userId: UserId;
    date: CalendarDate;
    weightKg: number;
};

export type CreateWeightEntryPayload = {
    date: string;
    weightKg: number;
};

export type UpdateWeightEntryPayload = CreateWeightEntryPayload;

export type WeightEntryFilters = {
    dateFrom?: string;
    dateTo?: string;
    limit?: number;
    sort?: 'asc' | 'desc';
};

export type WeightEntrySummaryPoint = {
    startDate: CalendarDate;
    endDate: CalendarDate;
    averageWeightKg: number;
};

export type WeightEntrySummaryFilters = {
    dateFrom: string;
    dateTo: string;
    quantizationDays: number;
};

export type WeightHistoryPageSummary = {
    entries: WeightEntry[];
    summary: WeightEntrySummaryPoint[];
    heightCm: number | null;
    goal: DesiredWeightResponse;
    goalHistory: WeightGoalHistoryItem[];
};

export type WeightHistoryPageSummaryFilters = WeightEntrySummaryFilters & {
    entriesLimit: number;
};
import type { DesiredWeightResponse, WeightGoalHistoryItem } from './user.data';
