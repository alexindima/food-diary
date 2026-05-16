import type { DailySymptoms } from '../../models/cycle.data';

export type CycleSymptomField = {
    key: keyof DailySymptoms;
    labelKey: string;
};

export const CYCLE_SYMPTOM_FIELDS: CycleSymptomField[] = [
    { key: 'pain', labelKey: 'CYCLE_TRACKING.SYMPTOM_PAIN' },
    { key: 'mood', labelKey: 'CYCLE_TRACKING.SYMPTOM_MOOD' },
    { key: 'edema', labelKey: 'CYCLE_TRACKING.SYMPTOM_EDEMA' },
    { key: 'headache', labelKey: 'CYCLE_TRACKING.SYMPTOM_HEADACHE' },
    { key: 'energy', labelKey: 'CYCLE_TRACKING.SYMPTOM_ENERGY' },
    { key: 'sleepQuality', labelKey: 'CYCLE_TRACKING.SYMPTOM_SLEEP' },
    { key: 'libido', labelKey: 'CYCLE_TRACKING.SYMPTOM_LIBIDO' },
];

export const PERIOD_DAY_ACCENT_COLOR = 'linear-gradient(135deg, var(--fd-color-red-600), var(--fd-color-orange-500))';
export const DEFAULT_DAY_ACCENT_COLOR = 'var(--fd-color-sky-500)';
