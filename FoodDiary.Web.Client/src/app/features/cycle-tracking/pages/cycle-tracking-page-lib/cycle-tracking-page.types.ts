import type {
    BleedingEntry,
    CycleNutritionSummary,
    CyclePredictions,
    CycleResponse,
    CycleSymptomEntry,
    FertilitySignal,
} from '../../models/cycle.data';

export type CycleViewModel = {
    cycle: CycleResponse;
    trackingStartDateLabel: string;
    summaryItems: CycleSummaryItemViewModel[];
    activeFactorItems: CycleActiveFactorViewModel[];
};

export type CycleOverviewViewModel = {
    todayDateKey: string;
    todayDateLabel: string;
    monthLabel: string;
    cycleDayNumber: number | null;
    hasTodayEntry: boolean;
    days: CycleOverviewDayViewModel[];
};

export type CycleOverviewDayViewModel = {
    dateKey: string;
    weekdayLabel: string;
    dayLabel: string;
    cycleDayNumber: number | null;
    isToday: boolean;
    isFuture: boolean;
    isBleeding: boolean;
    isPredictedPeriod: boolean;
    isTracked: boolean;
};

export type CycleSummaryItemViewModel = {
    labelKey: string;
    valueKey: string;
    params?: Record<string, string | number>;
    accentColor: string;
};

export type CycleActiveFactorViewModel = {
    id: string;
    labelKey: string;
    startDateLabel: string;
};

export type CycleFactorListItemViewModel = {
    id: string;
    labelKey: string;
    dateRangeLabel: string;
    statusLabelKey: string;
    isActive: boolean;
};

export type CyclePredictionViewModel = {
    prediction: CyclePredictions;
    nextPeriodRangeLabel: string;
    ovulationRangeLabel: string;
    pmsRangeLabel: string;
    confidenceLabel: string;
    dataSufficiencyKey: string;
    completedCycleCount: number;
    usedEpisodeCount: number;
    excludedEpisodeCount: number;
    explanationKey: string;
    hasPredictionRanges: boolean;
    limitedReasonKey: string | null;
    calibrationSampleCount: number;
    historicalCoveragePercent: number | null;
    meanAbsoluteErrorDays: number | null;
};

export type CycleNutritionSummaryViewModel = {
    summary: CycleNutritionSummary;
    hasEnoughData: boolean;
    consentRequired: boolean;
    completedCyclesAnalyzed: number;
    comparableCycles: number;
    bleedingCaloriesLabel: string;
    nonBleedingCaloriesLabel: string;
    bleedingFiberLabel: string;
    nonBleedingFiberLabel: string;
    painImpactLabel: string;
};

export type CycleDayViewModel = {
    date: string;
    dateLabel: string;
    bleedingEntries: BleedingEntry[];
    symptoms: CycleSymptomEntry[];
    bleedingSummaryItems: CycleDayBleedingSummaryViewModel[];
    symptomSummaryItems: CycleDaySymptomSummaryViewModel[];
    additionalSymptomCount: number;
    fertilitySignal: FertilitySignal | null;
    fertilitySignalItems: CycleDaySignalItemViewModel[];
    carePromptItems: CycleDayCarePromptViewModel[];
    notes: string | null;
    accentColor: string;
    badgeLabelKey: string;
    isPeriodStart?: boolean;
    isPeriodStartConfirmed?: boolean;
};

export type CycleDayBleedingSummaryViewModel = {
    id: string;
    typeLabelKey: string;
    flowLabelKey: string | null;
    painSeverityKey: string | null;
};

export type CycleDaySymptomSummaryViewModel = {
    id: string;
    labelKey: string;
    severityKey: string;
};

export type CycleObservationsViewModel = {
    hasEnoughData: boolean;
    trackedDayCount: number;
    bleedingDayCount: number;
    activeSymptomRecordCount: number;
    topSymptom: CycleTopSymptomViewModel | null;
};

export type CycleTopSymptomViewModel = {
    labelKey: string;
    loggedDayCount: number;
    severityKey: string;
};

export type CycleDaySignalItemViewModel = {
    textKey: string;
    params?: Record<string, string | number>;
};

export type CycleDayCarePromptViewModel = {
    id: string;
    textKey: string;
};
