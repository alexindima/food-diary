import type { WaistEntry } from '../models/waist-entry.data';

export type WaistEntryViewModel = {
    entry: WaistEntry;
    dateLabel: string;
};

export type WhtSegment = {
    labelKey: string;
    width: string;
    class: string;
};
