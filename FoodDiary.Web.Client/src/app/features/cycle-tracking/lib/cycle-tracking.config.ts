import type { CycleSymptomCategory } from '../models/cycle.data';

export type CycleSymptomField = {
    key: 'pain' | 'mood' | 'energy' | 'sleepQuality' | 'bloating' | 'headache' | 'libido';
    category: CycleSymptomCategory;
    labelKey: string;
};

export const CYCLE_SYMPTOM_FIELDS: readonly CycleSymptomField[] = [
    { key: 'pain', category: 0, labelKey: 'CYCLE_TRACKING.SYMPTOM_PAIN' },
    { key: 'mood', category: 1, labelKey: 'CYCLE_TRACKING.SYMPTOM_MOOD' },
    { key: 'energy', category: 2, labelKey: 'CYCLE_TRACKING.SYMPTOM_ENERGY' },
    { key: 'sleepQuality', category: 3, labelKey: 'CYCLE_TRACKING.SYMPTOM_SLEEP' },
    { key: 'bloating', category: 6, labelKey: 'CYCLE_TRACKING.SYMPTOM_BLOATING' },
    { key: 'headache', category: 7, labelKey: 'CYCLE_TRACKING.SYMPTOM_HEADACHE' },
    { key: 'libido', category: 11, labelKey: 'CYCLE_TRACKING.SYMPTOM_LIBIDO' },
];

export const DEFAULT_AVERAGE_CYCLE_LENGTH = 28;
export const DEFAULT_AVERAGE_PERIOD_LENGTH = 5;
export const MIN_AVERAGE_CYCLE_LENGTH = 18;
export const MAX_AVERAGE_CYCLE_LENGTH = 60;
export const MIN_AVERAGE_PERIOD_LENGTH = 1;
export const MAX_AVERAGE_PERIOD_LENGTH = 14;
export const DEFAULT_LUTEAL_LENGTH = 14;
export const MIN_LUTEAL_LENGTH = 8;
export const MAX_LUTEAL_LENGTH = 18;
export const MIN_SYMPTOM_VALUE = 0;
export const MAX_SYMPTOM_VALUE = 10;

export const DATE_INPUT_MONTH_OFFSET = 1;
export const DATE_INPUT_PART_LENGTH = 2;
