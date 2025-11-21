export interface DailySymptoms {
    pain: number;
    mood: number;
    edema: number;
    headache: number;
    energy: number;
    sleepQuality: number;
    libido: number;
}

export interface CycleDay {
    id: string;
    cycleId: string;
    date: string;
    isPeriod: boolean;
    symptoms: DailySymptoms;
    notes?: string | null;
}

export interface CyclePredictions {
    nextPeriodStart?: string | null;
    ovulationDate?: string | null;
    pmsStart?: string | null;
}

export interface CycleResponse {
    id: string;
    userId: string;
    startDate: string;
    averageLength: number;
    lutealLength: number;
    notes?: string | null;
    days: CycleDay[];
    predictions?: CyclePredictions | null;
}

export interface CreateCyclePayload {
    startDate: string;
    averageLength?: number | null;
    lutealLength?: number | null;
    notes?: string | null;
}

export interface UpsertCycleDayPayload {
    date: string;
    isPeriod: boolean;
    symptoms: DailySymptoms;
    notes?: string | null;
}
