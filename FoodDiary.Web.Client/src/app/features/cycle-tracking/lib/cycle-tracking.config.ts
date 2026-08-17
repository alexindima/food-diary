import type { CycleSymptomCategory } from '../models/cycle.data';

export type CycleSymptomField = {
    key:
        | 'pain'
        | 'mood'
        | 'energy'
        | 'sleepQuality'
        | 'appetite'
        | 'craving'
        | 'bloating'
        | 'headache'
        | 'skin'
        | 'stool'
        | 'nausea'
        | 'libido';
    category: CycleSymptomCategory;
    labelKey: string;
};

export const CYCLE_SYMPTOM_FIELDS: readonly CycleSymptomField[] = [
    { key: 'pain', category: 0, labelKey: 'CYCLE_TRACKING.SYMPTOM_PAIN' },
    { key: 'mood', category: 1, labelKey: 'CYCLE_TRACKING.SYMPTOM_MOOD' },
    { key: 'energy', category: 2, labelKey: 'CYCLE_TRACKING.SYMPTOM_ENERGY' },
    { key: 'sleepQuality', category: 3, labelKey: 'CYCLE_TRACKING.SYMPTOM_SLEEP' },
    { key: 'appetite', category: 4, labelKey: 'CYCLE_TRACKING.SYMPTOM_APPETITE' },
    { key: 'craving', category: 5, labelKey: 'CYCLE_TRACKING.SYMPTOM_CRAVING' },
    { key: 'bloating', category: 6, labelKey: 'CYCLE_TRACKING.SYMPTOM_BLOATING' },
    { key: 'headache', category: 7, labelKey: 'CYCLE_TRACKING.SYMPTOM_HEADACHE' },
    { key: 'skin', category: 8, labelKey: 'CYCLE_TRACKING.SYMPTOM_SKIN' },
    { key: 'stool', category: 9, labelKey: 'CYCLE_TRACKING.SYMPTOM_STOOL' },
    { key: 'nausea', category: 10, labelKey: 'CYCLE_TRACKING.SYMPTOM_NAUSEA' },
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
