import type { WaistEntry } from '../models/waist-entry.data';

export type WaistHistoryRange = 'week' | 'month' | 'year' | 'custom';

export type WaistHistoryDateRange = {
    start: Date;
    end: Date;
};

export type WaistHistoryCustomRange = {
    start: Date | null;
    end: Date | null;
};

export type WhtStatusInfo = {
    labelKey: string;
    descriptionKey: string;
    class: string;
};

export type WhtSegmentViewModel = {
    labelKey: string;
    from: number;
    to: number;
    class: string;
    width: string;
};

export type WhtViewModel = {
    value: number;
    status: WhtStatusInfo;
    segments: WhtSegmentViewModel[];
    pointerPosition: string;
};

export type WaistEntryViewModel = {
    entry: WaistEntry;
    dateLabel: string;
};
