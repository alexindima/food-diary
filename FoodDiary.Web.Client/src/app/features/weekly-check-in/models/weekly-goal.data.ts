export type WeeklyGoal = {
    id: string;
    weekStart: string;
    type: 'DiaryLogging';
    targetDays: number;
    progressDays: number;
    isCompleted: boolean;
    reminderEnabled: boolean;
    reminderTime: string | null;
    timeZoneOffsetMinutes: number | null;
};

export type UpsertWeeklyGoalPayload = {
    weekStart: string;
    targetDays: number;
    reminderEnabled: boolean;
    reminderTime: string | null;
    timeZoneOffsetMinutes: number | null;
};
