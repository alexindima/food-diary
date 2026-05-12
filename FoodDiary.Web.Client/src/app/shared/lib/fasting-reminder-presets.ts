export type FastingReminderPreset = {
    id: string;
    firstReminderHours: number;
    followUpReminderHours: number;
    labelKey: string;
};

export const FASTING_REMINDER_PRESETS: readonly FastingReminderPreset[] = [
    {
        id: 'starter',
        firstReminderHours: 12,
        followUpReminderHours: 20,
        labelKey: 'USER_MANAGE.NOTIFICATIONS_FASTING_REMINDER_PRESET_STARTER',
    },
    {
        id: 'steady',
        firstReminderHours: 16,
        followUpReminderHours: 24,
        labelKey: 'USER_MANAGE.NOTIFICATIONS_FASTING_REMINDER_PRESET_STEADY',
    },
    { id: 'late', firstReminderHours: 20, followUpReminderHours: 28, labelKey: 'USER_MANAGE.NOTIFICATIONS_FASTING_REMINDER_PRESET_LATE' },
] as const;

export const resolveFastingReminderPresetId = (firstReminderHours: number, followUpReminderHours: number): string => {
    return (
        FASTING_REMINDER_PRESETS.find(
            preset => preset.firstReminderHours === firstReminderHours && preset.followUpReminderHours === followUpReminderHours,
        )?.id ?? 'custom'
    );
};
