import type { CycleDay, CyclePredictions, CycleResponse } from '../models/cycle.data';

export interface CycleViewModel {
    cycle: CycleResponse;
    startDateLabel: string;
}

export interface CyclePredictionViewModel {
    prediction: CyclePredictions;
    nextPeriodStartLabel: string;
    ovulationDateLabel: string;
    pmsStartLabel: string;
}

export interface CycleDayViewModel {
    day: CycleDay;
    dateLabel: string;
    accentColor: string;
    badgeLabelKey: string;
}
