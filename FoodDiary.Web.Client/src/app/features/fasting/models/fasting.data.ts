export type FastingProtocol = 'F16_8' | 'F18_6' | 'F20_4' | 'Custom';

export interface FastingSession {
    id: string;
    startedAtUtc: string;
    endedAtUtc: string | null;
    plannedDurationHours: number;
    protocol: string;
    isCompleted: boolean;
    notes: string | null;
}

export interface FastingStats {
    totalCompleted: number;
    currentStreak: number;
    averageDurationHours: number;
}

export interface StartFastingPayload {
    protocol: string;
    plannedDurationHours?: number;
    notes?: string;
}

export interface FastingHistoryQuery {
    from: string;
    to: string;
}

export const FASTING_PROTOCOLS: { value: FastingProtocol; labelKey: string; hours: number }[] = [
    { value: 'F16_8', labelKey: 'FASTING.PROTOCOL_16_8', hours: 16 },
    { value: 'F18_6', labelKey: 'FASTING.PROTOCOL_18_6', hours: 18 },
    { value: 'F20_4', labelKey: 'FASTING.PROTOCOL_20_4', hours: 20 },
    { value: 'Custom', labelKey: 'FASTING.PROTOCOL_CUSTOM', hours: 16 },
];
