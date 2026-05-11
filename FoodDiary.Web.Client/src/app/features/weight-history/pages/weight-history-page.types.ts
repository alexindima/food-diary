import type { WeightEntry } from '../models/weight-entry.data';

export interface BmiSegmentViewModel {
    labelKey: string;
    from: number;
    to: number;
    class: string;
    width: string;
}

export interface WeightEntryViewModel {
    entry: WeightEntry;
    dateLabel: string;
}
