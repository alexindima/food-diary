import type { CycleDay, CyclePredictions, CycleResponse } from '../models/cycle.data';

export type CycleViewModel = {
    cycle: CycleResponse;
    startDateLabel: string;
};

export type CyclePredictionViewModel = {
    prediction: CyclePredictions;
    nextPeriodStartLabel: string;
    ovulationDateLabel: string;
    pmsStartLabel: string;
};

export type CycleDayViewModel = {
    day: CycleDay;
    dateLabel: string;
    accentColor: string;
    badgeLabelKey: string;
};
