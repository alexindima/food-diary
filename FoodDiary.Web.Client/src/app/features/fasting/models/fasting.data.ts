export type FastingProtocol = 'F16_8' | 'F18_6' | 'F20_4' | 'F24_0' | 'F36_0' | 'F72_0' | 'Custom' | 'CustomIntermittent';
export type FastingSessionStatus = 'Active' | 'Completed' | 'Interrupted' | 'Skipped' | 'Postponed';
export type FastingPlanType = 'Intermittent' | 'Extended' | 'Cyclic';
export type FastingOccurrenceKind = 'FastingWindow' | 'EatingWindow' | 'FastDay' | 'EatDay';
export type FastingMode = 'intermittent' | 'extended' | 'cyclic';

export interface FastingProtocolOption {
    value: FastingProtocol;
    labelKey: string;
    hours: number;
    category: 'intermittent' | 'extended';
}

export interface FastingSession {
    id: string;
    startedAtUtc: string;
    endedAtUtc: string | null;
    initialPlannedDurationHours: number;
    addedDurationHours: number;
    plannedDurationHours: number;
    protocol: string;
    planType: FastingPlanType;
    occurrenceKind: FastingOccurrenceKind;
    cyclicFastDays: number | null;
    cyclicEatDays: number | null;
    cyclicEatDayFastHours: number | null;
    cyclicEatDayEatingWindowHours: number | null;
    isCompleted: boolean;
    status: FastingSessionStatus;
    notes: string | null;
}

export interface FastingStats {
    totalCompleted: number;
    currentStreak: number;
    averageDurationHours: number;
}

export interface StartFastingPayload {
    protocol?: string;
    planType?: FastingPlanType;
    plannedDurationHours?: number;
    cyclicFastDays?: number;
    cyclicEatDays?: number;
    cyclicEatDayFastHours?: number;
    cyclicEatDayEatingWindowHours?: number;
    notes?: string;
}

export interface ExtendFastingPayload {
    additionalHours: number;
}

export interface FastingHistoryQuery {
    from: string;
    to: string;
}

export const FASTING_PROTOCOLS: FastingProtocolOption[] = [
    { value: 'F16_8', labelKey: 'FASTING.PROTOCOL_16_8', hours: 16, category: 'intermittent' },
    { value: 'F18_6', labelKey: 'FASTING.PROTOCOL_18_6', hours: 18, category: 'intermittent' },
    { value: 'F20_4', labelKey: 'FASTING.PROTOCOL_20_4', hours: 20, category: 'intermittent' },
    { value: 'CustomIntermittent', labelKey: 'FASTING.PROTOCOL_CUSTOM_INTERMITTENT', hours: 16, category: 'intermittent' },
    { value: 'F24_0', labelKey: 'FASTING.PROTOCOL_24_0', hours: 24, category: 'extended' },
    { value: 'F36_0', labelKey: 'FASTING.PROTOCOL_36_0', hours: 36, category: 'extended' },
    { value: 'F72_0', labelKey: 'FASTING.PROTOCOL_72_0', hours: 72, category: 'extended' },
    { value: 'Custom', labelKey: 'FASTING.PROTOCOL_CUSTOM', hours: 16, category: 'extended' },
];

export interface CyclicPresetOption {
    fastDays: number;
    eatDays: number;
    label: string;
}

export const CYCLIC_PRESETS: CyclicPresetOption[] = [
    { fastDays: 1, eatDays: 1, label: '1:1' },
    { fastDays: 1, eatDays: 2, label: '1:2' },
    { fastDays: 1, eatDays: 3, label: '1:3' },
];
