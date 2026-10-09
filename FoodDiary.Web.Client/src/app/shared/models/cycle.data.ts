import type { CalendarDate, UtcInstant } from './semantics/date-value';
import type {
    BleedingEntryId,
    CycleConsentId,
    CycleFactorId,
    CyclePredictionRevisionId,
    CycleProfileId,
    CycleSymptomEntryId,
    FertilitySignalId,
    MenstrualEpisodeId,
    UserId,
} from './semantics/entity-id';
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
export const MENSTRUAL_EPISODE_STATUS_INFERRED = 0;
export const MENSTRUAL_EPISODE_STATUS_CONFIRMED = 1;
export const CYCLE_TRACKING_GOAL_PERIOD_AWARENESS = 0;
export const CYCLE_TRACKING_GOAL_SYMPTOM_AWARENESS = 1;
export const CYCLE_TRACKING_GOAL_TRYING_TO_CONCEIVE = 2;
export const CYCLE_REPRODUCTIVE_STATE_CYCLING = 0;
export const CYCLE_REPRODUCTIVE_STATE_PREGNANCY = 1;
export const CYCLE_REPRODUCTIVE_STATE_POSTPARTUM = 2;
export const CYCLE_REPRODUCTIVE_STATE_LACTATION = 3;
export const CYCLE_REPRODUCTIVE_STATE_PERIMENOPAUSE = 4;
export const CYCLE_REPRODUCTIVE_STATE_NO_PERIOD = 5;
export const CYCLE_CONSENT_PURPOSE_CYCLE_TRACKING = 0;
export const CYCLE_CONSENT_PURPOSE_FERTILITY_SIGNALS = 1;
export const CYCLE_CONSENT_PURPOSE_NUTRITION_INSIGHTS = 2;

export type CycleTrackingMode =
    | typeof CYCLE_TRACKING_MODE_PERIOD_TRACKING
    | typeof CYCLE_TRACKING_MODE_TRYING_TO_CONCEIVE
    | typeof CYCLE_TRACKING_MODE_PREGNANCY
    | typeof CYCLE_TRACKING_MODE_POSTPARTUM_LACTATION
    | typeof CYCLE_TRACKING_MODE_PERIMENOPAUSE
    | typeof CYCLE_TRACKING_MODE_NO_PERIOD;
export type CycleTrackingGoal =
    | typeof CYCLE_TRACKING_GOAL_PERIOD_AWARENESS
    | typeof CYCLE_TRACKING_GOAL_SYMPTOM_AWARENESS
    | typeof CYCLE_TRACKING_GOAL_TRYING_TO_CONCEIVE;
export type CycleReproductiveState =
    | typeof CYCLE_REPRODUCTIVE_STATE_CYCLING
    | typeof CYCLE_REPRODUCTIVE_STATE_PREGNANCY
    | typeof CYCLE_REPRODUCTIVE_STATE_POSTPARTUM
    | typeof CYCLE_REPRODUCTIVE_STATE_LACTATION
    | typeof CYCLE_REPRODUCTIVE_STATE_PERIMENOPAUSE
    | typeof CYCLE_REPRODUCTIVE_STATE_NO_PERIOD;
export type CycleConsentPurpose = 0 | 1 | 2;
export type CycleConfidence =
    typeof CYCLE_CONFIDENCE_LEARNING | typeof CYCLE_CONFIDENCE_LOW | typeof CYCLE_CONFIDENCE_MEDIUM | typeof CYCLE_CONFIDENCE_HIGH;
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
    typeof OVULATION_TEST_RESULT_NEGATIVE | typeof OVULATION_TEST_RESULT_POSITIVE | typeof OVULATION_TEST_RESULT_UNKNOWN;
export type CycleFactorType =
    | typeof CYCLE_FACTOR_TYPE_PREGNANCY
    | typeof CYCLE_FACTOR_TYPE_LACTATION
    | typeof CYCLE_FACTOR_TYPE_HORMONAL_CONTRACEPTION
    | typeof CYCLE_FACTOR_TYPE_NON_HORMONAL_CONTRACEPTION
    | typeof CYCLE_FACTOR_TYPE_POSTPARTUM
    | typeof CYCLE_FACTOR_TYPE_PERIMENOPAUSE
    | typeof CYCLE_FACTOR_TYPE_NO_PERIOD;

export type BleedingEntry = {
    id: BleedingEntryId;
    cycleProfileId: CycleProfileId;
    date: CalendarDate;
    type: BleedingType;
    flow: CycleFlowLevel;
    painImpact?: number | null;
    notes?: string | null;
};

export type CycleSymptomEntry = {
    id: CycleSymptomEntryId;
    cycleProfileId: CycleProfileId;
    date: CalendarDate;
    category: CycleSymptomCategory;
    intensity: number;
    tags: string[];
    note?: string | null;
};

export type CycleFactor = {
    id: CycleFactorId;
    cycleProfileId: CycleProfileId;
    type: CycleFactorType;
    startDate: CalendarDate;
    endDate?: CalendarDate | null;
    notes?: string | null;
};

export type FertilitySignal = {
    id: FertilitySignalId;
    cycleProfileId: CycleProfileId;
    date: CalendarDate;
    basalBodyTemperatureCelsius?: number | null;
    ovulationTestResult?: OvulationTestResult | null;
    cervicalFluid?: string | null;
    hadSex?: boolean | null;
    notes?: string | null;
};

export type MenstrualEpisode = {
    id: MenstrualEpisodeId;
    cycleProfileId: CycleProfileId;
    startDate: CalendarDate;
    endDate?: CalendarDate | null;
    status: number;
    excludedFromPredictions: boolean;
};

export type CyclePredictions = {
    nextPeriodStartFrom?: CalendarDate | null;
    nextPeriodStartTo?: CalendarDate | null;
    ovulationFrom?: CalendarDate | null;
    ovulationTo?: CalendarDate | null;
    pmsWindowStart?: CalendarDate | null;
    pmsWindowEnd?: CalendarDate | null;
    confidence: string;
    rationale: string;
    dataSufficiency?: string;
    patternConsistency?: string;
    completedCycleCount?: number;
    usedEpisodeCount?: number;
    excludedEpisodeCount?: number;
    reasonCodes?: string[];
    algorithmVersion?: string;
    calibrationSampleCount?: number;
    historicalCoveragePercent?: number | null;
    meanAbsoluteErrorDays?: number | null;
};

export type CycleConsent = {
    id: CycleConsentId;
    purpose: CycleConsentPurpose;
    grantedAtUtc: UtcInstant;
    revokedAtUtc?: UtcInstant | null;
    isActive: boolean;
};

export type CyclePredictionRevision = {
    id: CyclePredictionRevisionId;
    generatedAtUtc: UtcInstant;
    nextPeriodStartFrom?: CalendarDate | null;
    nextPeriodStartTo?: CalendarDate | null;
    confidence: string;
    dataSufficiency: string;
    patternConsistency: string;
    completedCycleCount: number;
    calibrationSampleCount: number;
    historicalCoveragePercent?: number | null;
    meanAbsoluteErrorDays?: number | null;
    reasonCodes: string[];
    algorithmVersion: string;
};

export type CycleNutritionSummary = {
    dateFrom: CalendarDate;
    dateTo: CalendarDate;
    loggedCycleDays: number;
    daysWithMeals: number;
    bleedingDays: number;
    averageCaloriesOnBleedingDays: number;
    averageCaloriesOnNonBleedingCycleDays: number;
    averageFiberOnBleedingDays: number;
    averageFiberOnNonBleedingCycleDays: number;
    averagePainImpactOnDaysWithMeals: number;
    hasEnoughNutritionData: boolean;
    consentRequired?: boolean;
    completedCyclesAnalyzed?: number;
    comparableCycles?: number;
    dataSufficiency?: string;
    reasonCodes?: string[];
    algorithmVersion?: string;
};

export type CycleDayNote = {
    date: CalendarDate;
    notes: string;
};

export type CycleResponse = {
    id: CycleProfileId;
    userId: UserId;
    mode: CycleTrackingMode;
    confidence: CycleConfidence;
    trackingStartDate: CalendarDate;
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
    menstrualEpisodes?: MenstrualEpisode[];
    predictions?: CyclePredictions | null;
    goal: CycleTrackingGoal;
    reproductiveState: CycleReproductiveState;
    hideFromDashboard: boolean;
    consents?: CycleConsent[];
    predictionRevisions?: CyclePredictionRevision[];
    dayNotes?: CycleDayNote[];
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
    goal?: CycleTrackingGoal;
    reproductiveState?: CycleReproductiveState;
    hideFromDashboard?: boolean;
    cycleTrackingConsentGranted?: boolean;
    nutritionInsightsConsentGranted?: boolean;
    fertilitySignalsConsentGranted?: boolean;
};

export type UpdateCycleSettingsPayload = {
    mode: CycleTrackingMode;
    averageCycleLength: number;
    averagePeriodLength: number;
    lutealLength: number;
    isRegular: boolean;
    showFertilityEstimates: boolean;
    discreetNotifications: boolean;
    goal?: CycleTrackingGoal;
    reproductiveState?: CycleReproductiveState;
    hideFromDashboard?: boolean;
};

export type UpdateCycleConsentPayload = { granted: boolean };

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
    cycleProfileId: CycleProfileId;
    date: CalendarDate;
    bleedingEntries: BleedingEntry[];
    symptoms: CycleSymptomEntry[];
    fertilitySignal?: FertilitySignal | null;
    notes?: string | null;
};

export type UpsertCycleDayPayload = {
    date: string;
    bleeding?: BleedingLogPayload | null;
    clearBleeding?: boolean;
    symptoms: SymptomLogPayload[];
    clearSymptomCategories?: CycleSymptomCategory[];
    fertilitySignal?: FertilitySignalPayload | null;
    clearFertilitySignal?: boolean;
    notes?: string | null;
    clearNotes?: boolean;
};

export type UpsertCycleFactorPayload = {
    factorId?: string;
    type: CycleFactorType;
    startDate: string;
    endDate?: string | null;
    notes?: string | null;
    clearNotes: boolean;
};

export type UpdateMenstrualEpisodePayload = {
    startDate: string;
    endDate?: string | null;
    excludedFromPredictions?: boolean;
};
