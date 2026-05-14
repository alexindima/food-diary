import type { WeightEntry } from '../models/weight-entry.data';

export type WeightHistoryRange = 'week' | 'month' | 'year' | 'custom';

export type WeightHistoryDateRange = {
    start: Date;
    end: Date;
};

export type WeightHistoryCustomRange = {
    start: Date | null;
    end: Date | null;
};

export type BmiStatusInfo = {
    labelKey: string;
    descriptionKey: string;
    class: string;
};

export type BmiSegmentViewModel = {
    labelKey: string;
    from: number;
    to: number;
    class: string;
    width: string;
};

export type BmiViewModel = {
    value: number;
    status: BmiStatusInfo;
    segments: BmiSegmentViewModel[];
    pointerPosition: string;
};

export type WeightEntryViewModel = {
    entry: WeightEntry;
    dateLabel: string;
};
