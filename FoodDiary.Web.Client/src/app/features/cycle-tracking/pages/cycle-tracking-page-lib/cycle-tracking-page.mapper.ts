import { formatDateValue } from '../../../../shared/lib/local-date.utils';
import {
    BLEEDING_TYPE_BLEEDING,
    type BleedingEntry,
    type CyclePredictions,
    type CycleResponse,
    type CycleSymptomEntry,
} from '../../models/cycle.data';
import { DEFAULT_DAY_ACCENT_COLOR, PERIOD_DAY_ACCENT_COLOR } from './cycle-tracking-page.config';
import type { CycleDayViewModel, CyclePredictionViewModel, CycleViewModel } from './cycle-tracking-page.types';

const FULL_DATE_OPTIONS: Intl.DateTimeFormatOptions = { day: 'numeric', month: 'short', year: 'numeric' };
const SHORT_DATE_OPTIONS: Intl.DateTimeFormatOptions = { day: 'numeric', month: 'short' };
const UTC_TIME_ZONE: Intl.DateTimeFormatOptions['timeZone'] = 'UTC';

export function buildCycleCurrentView(cycle: CycleResponse | null, locale: string): CycleViewModel | null {
    if (cycle === null) {
        return null;
    }

    return {
        cycle,
        trackingStartDateLabel: formatCycleDate(cycle.trackingStartDate, locale, FULL_DATE_OPTIONS),
    };
}

export function buildCyclePredictionView(prediction: CyclePredictions | null, locale: string): CyclePredictionViewModel | null {
    if (prediction === null) {
        return null;
    }

    return {
        prediction,
        nextPeriodRangeLabel: formatRange(prediction.nextPeriodStartFrom, prediction.nextPeriodStartTo, locale),
        ovulationRangeLabel: formatRange(prediction.ovulationFrom, prediction.ovulationTo, locale),
        pmsRangeLabel: formatRange(prediction.pmsWindowStart, prediction.pmsWindowEnd, locale),
        confidenceLabel: prediction.confidence,
    };
}

export function buildCycleDayItems(bleedingEntries: BleedingEntry[], symptoms: CycleSymptomEntry[], locale: string): CycleDayViewModel[] {
    const dates = new Set([...bleedingEntries.map(entry => entry.date), ...symptoms.map(symptom => symptom.date)]);
    return [...dates]
        .sort((a, b) => b.localeCompare(a))
        .map(date => {
            const dayBleeding = bleedingEntries.filter(entry => entry.date === date);
            const hasBleeding = dayBleeding.some(entry => entry.type === BLEEDING_TYPE_BLEEDING);
            return {
                date,
                dateLabel: formatCycleDate(date, locale, FULL_DATE_OPTIONS),
                bleedingEntries: dayBleeding,
                symptoms: symptoms.filter(symptom => symptom.date === date),
                accentColor: hasBleeding ? PERIOD_DAY_ACCENT_COLOR : DEFAULT_DAY_ACCENT_COLOR,
                badgeLabelKey: hasBleeding ? 'CYCLE_TRACKING.BADGE_PERIOD' : 'CYCLE_TRACKING.BADGE_TRACKED',
            };
        });
}

function formatRange(from: string | null | undefined, to: string | null | undefined, locale: string): string {
    const fromLabel = formatCycleDate(from, locale, SHORT_DATE_OPTIONS, UTC_TIME_ZONE);
    const toLabel = formatCycleDate(to, locale, SHORT_DATE_OPTIONS, UTC_TIME_ZONE);
    if (fromLabel.length === 0) {
        return toLabel;
    }

    if (toLabel.length === 0 || fromLabel === toLabel) {
        return fromLabel;
    }

    return `${fromLabel} - ${toLabel}`;
}

function formatCycleDate(
    value: string | null | undefined,
    locale: string,
    options: Intl.DateTimeFormatOptions,
    timeZone?: Intl.DateTimeFormatOptions['timeZone'],
): string {
    return (
        formatDateValue(value, locale, {
            ...options,
            timeZone,
        }) ??
        value ??
        ''
    );
}
