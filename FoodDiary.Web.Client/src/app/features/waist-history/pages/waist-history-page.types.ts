import type { WaistEntry } from '../models/waist-entry.data';

export interface WaistEntryViewModel {
    entry: WaistEntry;
    dateLabel: string;
}

export interface WhtSegment {
    labelKey: string;
    width: string;
    class: string;
}
