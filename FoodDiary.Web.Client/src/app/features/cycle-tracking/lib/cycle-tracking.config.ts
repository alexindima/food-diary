import type { CycleSymptomCategory } from '../../../shared/models/cycle.data';

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

export const CYCLE_SYMPTOM_FIELDS_BY_KEY: {
    readonly [Key in CycleSymptomField['key']]: CycleSymptomField & { key: Key };
} = {
    pain: { key: 'pain', category: 0, labelKey: 'CYCLE_TRACKING.SYMPTOM_PAIN' },
    mood: { key: 'mood', category: 1, labelKey: 'CYCLE_TRACKING.SYMPTOM_MOOD' },
    energy: { key: 'energy', category: 2, labelKey: 'CYCLE_TRACKING.SYMPTOM_ENERGY' },
    sleepQuality: { key: 'sleepQuality', category: 3, labelKey: 'CYCLE_TRACKING.SYMPTOM_SLEEP' },
    appetite: { key: 'appetite', category: 4, labelKey: 'CYCLE_TRACKING.SYMPTOM_APPETITE' },
    craving: { key: 'craving', category: 5, labelKey: 'CYCLE_TRACKING.SYMPTOM_CRAVING' },
    bloating: { key: 'bloating', category: 6, labelKey: 'CYCLE_TRACKING.SYMPTOM_BLOATING' },
    headache: { key: 'headache', category: 7, labelKey: 'CYCLE_TRACKING.SYMPTOM_HEADACHE' },
    skin: { key: 'skin', category: 8, labelKey: 'CYCLE_TRACKING.SYMPTOM_SKIN' },
    stool: { key: 'stool', category: 9, labelKey: 'CYCLE_TRACKING.SYMPTOM_STOOL' },
    nausea: { key: 'nausea', category: 10, labelKey: 'CYCLE_TRACKING.SYMPTOM_NAUSEA' },
    libido: { key: 'libido', category: 11, labelKey: 'CYCLE_TRACKING.SYMPTOM_LIBIDO' },
};

export const CYCLE_SYMPTOM_FIELDS: readonly CycleSymptomField[] = Object.values(CYCLE_SYMPTOM_FIELDS_BY_KEY);

export const DEFAULT_AVERAGE_CYCLE_LENGTH = 28;
export const MAX_CYCLE_NOTES_LENGTH = 1024;
export const MAX_CERVICAL_FLUID_LENGTH = 128;
export const MIN_BASAL_BODY_TEMPERATURE = 34;
export const MAX_BASAL_BODY_TEMPERATURE = 42;
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
