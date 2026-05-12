import type { WeightEntry } from '../models/weight-entry.data';

export type BmiSegmentViewModel = {
    labelKey: string;
    from: number;
    to: number;
    class: string;
    width: string;
};

export type WeightEntryViewModel = {
    entry: WeightEntry;
    dateLabel: string;
};
