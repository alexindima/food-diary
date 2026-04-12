export type FastingProtocol = 'F16_8' | 'F18_6' | 'F20_4' | 'F24_0' | 'F36_0' | 'F72_0' | 'Custom' | 'CustomIntermittent';
export type FastingSessionStatus = 'Active' | 'Completed' | 'Interrupted' | 'Skipped' | 'Postponed';
export type FastingPlanType = 'Intermittent' | 'Extended' | 'Cyclic';
export type FastingOccurrenceKind = 'FastingWindow' | 'EatingWindow' | 'FastDay' | 'EatDay';
export type FastingMode = 'intermittent' | 'extended' | 'cyclic';
export type FastingMessageTone = 'warning' | 'positive' | 'neutral';

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
    cyclicPhaseDayNumber: number | null;
    cyclicPhaseDayTotal: number | null;
    isCompleted: boolean;
    status: FastingSessionStatus;
    notes: string | null;
    checkInAtUtc: string | null;
    hungerLevel: number | null;
    energyLevel: number | null;
    moodLevel: number | null;
    symptoms: string[];
    checkInNotes: string | null;
    checkIns: FastingCheckIn[];
}

export interface FastingCheckIn {
    id: string;
    checkedInAtUtc: string;
    hungerLevel: number;
    energyLevel: number;
    moodLevel: number;
    symptoms: string[];
    notes: string | null;
}

export interface FastingStats {
    totalCompleted: number;
    currentStreak: number;
    averageDurationHours: number;
}

export interface FastingMessage {
    id: string;
    titleKey: string;
    bodyKey: string;
    tone: FastingMessageTone;
    bodyParams: Record<string, string> | null;
}

export interface FastingInsights {
    insights: FastingMessage[];
    currentPrompt: FastingMessage | null;
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

export interface UpdateFastingCheckInPayload {
    hungerLevel: number;
    energyLevel: number;
    moodLevel: number;
    symptoms?: string[] | null;
    checkInNotes?: string | null;
}

export interface FastingHistoryQuery {
    from: string;
    to: string;
    page?: number;
    limit?: number;
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
    { fastDays: 2, eatDays: 1, label: '2:1' },
    { fastDays: 3, eatDays: 1, label: '3:1' },
];

export const FASTING_CHECK_IN_SCALE = [1, 2, 3, 4, 5] as const;

export const FASTING_SYMPTOM_OPTIONS = [
    { value: 'headache', labelKey: 'FASTING.CHECK_IN.SYMPTOMS.HEADACHE' },
    { value: 'weakness', labelKey: 'FASTING.CHECK_IN.SYMPTOMS.WEAKNESS' },
    { value: 'irritability', labelKey: 'FASTING.CHECK_IN.SYMPTOMS.IRRITABILITY' },
    { value: 'dizziness', labelKey: 'FASTING.CHECK_IN.SYMPTOMS.DIZZINESS' },
    { value: 'cravings', labelKey: 'FASTING.CHECK_IN.SYMPTOMS.CRAVINGS' },
    { value: 'good', labelKey: 'FASTING.CHECK_IN.SYMPTOMS.GOOD' },
] as const;
