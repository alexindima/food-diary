import type { BleedingEntry, CyclePredictions, CycleResponse, CycleSymptomEntry } from '../../models/cycle.data';

export type CycleViewModel = {
    cycle: CycleResponse;
    trackingStartDateLabel: string;
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
    accentColor: string;
    badgeLabelKey: string;
};
