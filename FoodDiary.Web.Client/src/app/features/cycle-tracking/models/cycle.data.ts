export type DailySymptoms = {
    pain: number;
    mood: number;
    edema: number;
    headache: number;
    energy: number;
    sleepQuality: number;
    libido: number;
};

export type CycleDay = {
    id: string;
    cycleId: string;
    date: string;
    isPeriod: boolean;
    symptoms: DailySymptoms;
    notes?: string | null;
};

export type CyclePredictions = {
    nextPeriodStart?: string | null;
    ovulationDate?: string | null;
    pmsStart?: string | null;
};

export type CycleResponse = {
    id: string;
    userId: string;
    startDate: string;
    averageLength: number;
    lutealLength: number;
    notes?: string | null;
    days: CycleDay[];
    predictions?: CyclePredictions | null;
};

export type CreateCyclePayload = {
    startDate: string;
    averageLength?: number | null;
    lutealLength?: number | null;
    notes?: string | null;
};

export type UpsertCycleDayPayload = {
    date: string;
    isPeriod: boolean;
    symptoms: DailySymptoms;
    notes?: string | null;
};
