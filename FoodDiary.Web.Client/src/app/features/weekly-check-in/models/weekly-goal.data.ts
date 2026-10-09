import type { CalendarDate } from '../../../shared/models/semantics/date-value';

export type WeeklyGoal = {
    id: string;
    weekStart: CalendarDate;
    type: 'DiaryLogging';
    targetDays: number;
    progressDays: number;
    isCompleted: boolean;
    reminderEnabled: boolean;
    reminderTime: string | null;
    timeZoneOffsetMinutes: number | null;
};

export type UpsertWeeklyGoalPayload = {
    weekStart: CalendarDate;
    targetDays: number;
    reminderEnabled: boolean;
    reminderTime: string | null;
    timeZoneOffsetMinutes: number | null;
};
