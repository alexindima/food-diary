import { MAX_CYCLIC_DAYS, MAX_FASTING_HOURS, MAX_INTERMITTENT_FAST_HOURS, MIN_FASTING_HOURS } from './fasting.constants';

function normalizeWholeNumber(value: number | null, maximum: number): number {
    const finiteValue = value !== null && Number.isFinite(value) ? value : MIN_FASTING_HOURS;
    return Math.max(MIN_FASTING_HOURS, Math.min(maximum, Math.trunc(finiteValue)));
}

export function normalizeFastingHours(value: number | null): number {
    return normalizeWholeNumber(value, MAX_FASTING_HOURS);
}

export function normalizeIntermittentFastHours(value: number | null): number {
    return normalizeWholeNumber(value, MAX_INTERMITTENT_FAST_HOURS);
}

export function normalizeCyclicDays(value: number | null): number {
    return normalizeWholeNumber(value, MAX_CYCLIC_DAYS);
}
