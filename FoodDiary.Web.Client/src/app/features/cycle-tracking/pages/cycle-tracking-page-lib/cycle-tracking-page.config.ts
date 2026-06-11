import type { CycleSymptomCategory } from '../../models/cycle.data';

export type CycleSymptomField = {
    key: 'pain' | 'mood' | 'energy' | 'sleepQuality' | 'bloating' | 'headache' | 'libido';
    category: CycleSymptomCategory;
    labelKey: string;
};

export const CYCLE_SYMPTOM_FIELDS: CycleSymptomField[] = [
    { key: 'pain', category: 0, labelKey: 'CYCLE_TRACKING.SYMPTOM_PAIN' },
    { key: 'mood', category: 1, labelKey: 'CYCLE_TRACKING.SYMPTOM_MOOD' },
    { key: 'energy', category: 2, labelKey: 'CYCLE_TRACKING.SYMPTOM_ENERGY' },
    { key: 'sleepQuality', category: 3, labelKey: 'CYCLE_TRACKING.SYMPTOM_SLEEP' },
    { key: 'bloating', category: 6, labelKey: 'CYCLE_TRACKING.SYMPTOM_BLOATING' },
    { key: 'headache', category: 7, labelKey: 'CYCLE_TRACKING.SYMPTOM_HEADACHE' },
    { key: 'libido', category: 11, labelKey: 'CYCLE_TRACKING.SYMPTOM_LIBIDO' },
];

export const PERIOD_DAY_ACCENT_COLOR = 'linear-gradient(135deg, var(--fd-color-red-600), var(--fd-color-orange-500))';
export const DEFAULT_DAY_ACCENT_COLOR = 'var(--fd-color-sky-500)';
