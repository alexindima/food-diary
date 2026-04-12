export interface FastingTelemetryPresetSummary {
    presetId: string;
    selectionCount: number;
    timingSaveCount: number;
    firstReminderHours: number | null;
    followUpReminderHours: number | null;
    startedSessions: number;
    completedSessions: number;
    savedCheckIns: number;
    completionRatePercent: number;
    checkInRatePercent: number;
}

export interface FastingTelemetrySummary {
    windowHours: number;
    generatedAtUtc: string;
    startedSessions: number;
    completedSessions: number;
    savedCheckIns: number;
    reminderPresetSelections: number;
    reminderTimingSaves: number;
    presetReminderTimingSaves: number;
    manualReminderTimingSaves: number;
    completionRatePercent: number;
    checkInRatePercent: number;
    averageCompletedDurationHours: number | null;
    lastCheckInAtUtc: string | null;
    lastEventAtUtc: string | null;
    topPresets: FastingTelemetryPresetSummary[];
}
