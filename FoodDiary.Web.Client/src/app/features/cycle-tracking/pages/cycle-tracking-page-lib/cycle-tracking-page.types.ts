import type { BleedingEntry, CyclePredictions, CycleResponse, CycleSymptomEntry, FertilitySignal } from '../../models/cycle.data';

export type CycleViewModel = {
    cycle: CycleResponse;
    trackingStartDateLabel: string;
    summaryItems: CycleSummaryItemViewModel[];
    activeFactorItems: CycleActiveFactorViewModel[];
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
};

export type CycleDayViewModel = {
    date: string;
    dateLabel: string;
    bleedingEntries: BleedingEntry[];
    symptoms: CycleSymptomEntry[];
    fertilitySignal: FertilitySignal | null;
    fertilitySignalItems: CycleDaySignalItemViewModel[];
    notes: string | null;
    accentColor: string;
    badgeLabelKey: string;
};

export type CycleDaySignalItemViewModel = {
    textKey: string;
    params?: Record<string, string | number>;
};
