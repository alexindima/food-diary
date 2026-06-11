export const CYCLE_TRACKING_MODE_PERIOD_TRACKING = 0;
export const CYCLE_TRACKING_MODE_TRYING_TO_CONCEIVE = 1;
export const CYCLE_TRACKING_MODE_PREGNANCY = 2;
export const CYCLE_TRACKING_MODE_POSTPARTUM_LACTATION = 3;
export const CYCLE_TRACKING_MODE_PERIMENOPAUSE = 4;
export const CYCLE_TRACKING_MODE_NO_PERIOD = 5;
export const CYCLE_CONFIDENCE_LEARNING = 0;
export const CYCLE_CONFIDENCE_LOW = 1;
export const CYCLE_CONFIDENCE_MEDIUM = 2;
export const CYCLE_CONFIDENCE_HIGH = 3;
export const BLEEDING_TYPE_BLEEDING = 0;
export const BLEEDING_TYPE_SPOTTING = 1;
export const CYCLE_FLOW_NONE = 0;
export const CYCLE_FLOW_LIGHT = 1;
export const CYCLE_FLOW_MEDIUM = 2;
export const CYCLE_FLOW_HEAVY = 3;
export const CYCLE_SYMPTOM_CATEGORY_PAIN = 0;
export const CYCLE_SYMPTOM_CATEGORY_MOOD = 1;
export const CYCLE_SYMPTOM_CATEGORY_ENERGY = 2;
export const CYCLE_SYMPTOM_CATEGORY_SLEEP = 3;
export const CYCLE_SYMPTOM_CATEGORY_APPETITE = 4;
export const CYCLE_SYMPTOM_CATEGORY_CRAVING = 5;
export const CYCLE_SYMPTOM_CATEGORY_BLOATING = 6;
export const CYCLE_SYMPTOM_CATEGORY_HEADACHE = 7;
export const CYCLE_SYMPTOM_CATEGORY_SKIN = 8;
export const CYCLE_SYMPTOM_CATEGORY_STOOL = 9;
export const CYCLE_SYMPTOM_CATEGORY_NAUSEA = 10;
export const CYCLE_SYMPTOM_CATEGORY_LIBIDO = 11;
export const CYCLE_SYMPTOM_CATEGORY_OTHER = 99;
export const OVULATION_TEST_RESULT_NEGATIVE = 0;
export const OVULATION_TEST_RESULT_POSITIVE = 1;
export const OVULATION_TEST_RESULT_UNKNOWN = 2;
export const CYCLE_FACTOR_TYPE_PREGNANCY = 0;
export const CYCLE_FACTOR_TYPE_LACTATION = 1;
export const CYCLE_FACTOR_TYPE_HORMONAL_CONTRACEPTION = 2;
export const CYCLE_FACTOR_TYPE_NON_HORMONAL_CONTRACEPTION = 3;
export const CYCLE_FACTOR_TYPE_POSTPARTUM = 4;
export const CYCLE_FACTOR_TYPE_PERIMENOPAUSE = 5;
export const CYCLE_FACTOR_TYPE_NO_PERIOD = 6;

export type CycleTrackingMode =
    | typeof CYCLE_TRACKING_MODE_PERIOD_TRACKING
    | typeof CYCLE_TRACKING_MODE_TRYING_TO_CONCEIVE
    | typeof CYCLE_TRACKING_MODE_PREGNANCY
    | typeof CYCLE_TRACKING_MODE_POSTPARTUM_LACTATION
    | typeof CYCLE_TRACKING_MODE_PERIMENOPAUSE
    | typeof CYCLE_TRACKING_MODE_NO_PERIOD;
export type CycleConfidence =
    | typeof CYCLE_CONFIDENCE_LEARNING
    | typeof CYCLE_CONFIDENCE_LOW
    | typeof CYCLE_CONFIDENCE_MEDIUM
    | typeof CYCLE_CONFIDENCE_HIGH;
export type BleedingType = typeof BLEEDING_TYPE_BLEEDING | typeof BLEEDING_TYPE_SPOTTING;
export type CycleFlowLevel = typeof CYCLE_FLOW_NONE | typeof CYCLE_FLOW_LIGHT | typeof CYCLE_FLOW_MEDIUM | typeof CYCLE_FLOW_HEAVY;
export type CycleSymptomCategory =
    | typeof CYCLE_SYMPTOM_CATEGORY_PAIN
    | typeof CYCLE_SYMPTOM_CATEGORY_MOOD
    | typeof CYCLE_SYMPTOM_CATEGORY_ENERGY
    | typeof CYCLE_SYMPTOM_CATEGORY_SLEEP
    | typeof CYCLE_SYMPTOM_CATEGORY_APPETITE
    | typeof CYCLE_SYMPTOM_CATEGORY_CRAVING
    | typeof CYCLE_SYMPTOM_CATEGORY_BLOATING
    | typeof CYCLE_SYMPTOM_CATEGORY_HEADACHE
    | typeof CYCLE_SYMPTOM_CATEGORY_SKIN
    | typeof CYCLE_SYMPTOM_CATEGORY_STOOL
    | typeof CYCLE_SYMPTOM_CATEGORY_NAUSEA
    | typeof CYCLE_SYMPTOM_CATEGORY_LIBIDO
    | typeof CYCLE_SYMPTOM_CATEGORY_OTHER;
export type OvulationTestResult =
    | typeof OVULATION_TEST_RESULT_NEGATIVE
    | typeof OVULATION_TEST_RESULT_POSITIVE
    | typeof OVULATION_TEST_RESULT_UNKNOWN;
export type CycleFactorType =
    | typeof CYCLE_FACTOR_TYPE_PREGNANCY
    | typeof CYCLE_FACTOR_TYPE_LACTATION
    | typeof CYCLE_FACTOR_TYPE_HORMONAL_CONTRACEPTION
    | typeof CYCLE_FACTOR_TYPE_NON_HORMONAL_CONTRACEPTION
    | typeof CYCLE_FACTOR_TYPE_POSTPARTUM
    | typeof CYCLE_FACTOR_TYPE_PERIMENOPAUSE
    | typeof CYCLE_FACTOR_TYPE_NO_PERIOD;

export type BleedingEntry = {
    id: string;
    cycleProfileId: string;
    date: string;
    type: BleedingType;
    flow: CycleFlowLevel;
    painImpact?: number | null;
    notes?: string | null;
};

export type CycleSymptomEntry = {
    id: string;
    cycleProfileId: string;
    date: string;
    category: CycleSymptomCategory;
    intensity: number;
    tags: string[];
    note?: string | null;
};

export type CycleFactor = {
    id: string;
    cycleProfileId: string;
    type: CycleFactorType;
    startDate: string;
    endDate?: string | null;
    notes?: string | null;
};

export type FertilitySignal = {
    id: string;
    cycleProfileId: string;
    date: string;
    basalBodyTemperatureCelsius?: number | null;
    ovulationTestResult?: OvulationTestResult | null;
    cervicalFluid?: string | null;
    hadSex?: boolean | null;
    notes?: string | null;
};

export type CyclePredictions = {
    nextPeriodStartFrom?: string | null;
    nextPeriodStartTo?: string | null;
    ovulationFrom?: string | null;
    ovulationTo?: string | null;
    pmsWindowStart?: string | null;
    pmsWindowEnd?: string | null;
    confidence: string;
    rationale: string;
};

export type CycleNutritionSummary = {
    dateFrom: string;
    dateTo: string;
    loggedCycleDays: number;
    daysWithMeals: number;
    bleedingDays: number;
    averageCaloriesOnBleedingDays: number;
    averageCaloriesOnNonBleedingCycleDays: number;
    averageFiberOnBleedingDays: number;
    averageFiberOnNonBleedingCycleDays: number;
    averagePainImpactOnDaysWithMeals: number;
    hasEnoughNutritionData: boolean;
};

export type CycleResponse = {
    id: string;
    userId: string;
    mode: CycleTrackingMode;
    confidence: CycleConfidence;
    trackingStartDate: string;
    averageCycleLength: number;
    averagePeriodLength: number;
    lutealLength: number;
    isRegular: boolean;
    isOnboardingComplete: boolean;
    showFertilityEstimates: boolean;
    discreetNotifications: boolean;
    notes?: string | null;
    bleedingEntries: BleedingEntry[];
    symptoms: CycleSymptomEntry[];
    factors: CycleFactor[];
    fertilitySignals: FertilitySignal[];
    predictions?: CyclePredictions | null;
};

export type CreateCyclePayload = {
    trackingStartDate: string;
    mode: CycleTrackingMode;
    averageCycleLength?: number | null;
    averagePeriodLength?: number | null;
    lutealLength?: number | null;
    isRegular: boolean;
    isOnboardingComplete: boolean;
    showFertilityEstimates: boolean;
    discreetNotifications: boolean;
    notes?: string | null;
};

export type BleedingLogPayload = {
    type: BleedingType;
    flow: CycleFlowLevel;
    painImpact?: number | null;
    notes?: string | null;
    clearNotes: boolean;
};

export type SymptomLogPayload = {
    category: CycleSymptomCategory;
    intensity: number;
    tags: string[];
    note?: string | null;
    clearNote: boolean;
};

export type FertilitySignalPayload = {
    basalBodyTemperatureCelsius?: number | null;
    ovulationTestResult?: OvulationTestResult | null;
    cervicalFluid?: string | null;
    hadSex?: boolean | null;
    notes?: string | null;
    clearNotes: boolean;
};

export type CycleLogDay = {
    cycleProfileId: string;
    date: string;
    bleedingEntries: BleedingEntry[];
    symptoms: CycleSymptomEntry[];
    fertilitySignal?: FertilitySignal | null;
};

export type UpsertCycleDayPayload = {
    date: string;
    bleeding?: BleedingLogPayload | null;
    symptoms: SymptomLogPayload[];
    fertilitySignal?: FertilitySignalPayload | null;
};

export type UpsertCycleFactorPayload = {
    type: CycleFactorType;
    startDate: string;
    endDate?: string | null;
    notes?: string | null;
    clearNotes: boolean;
};
