import type { CycleDay, CyclePredictions, CycleResponse } from '../../models/cycle.data';
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
        startDateLabel: formatCycleDate(cycle.startDate, locale, FULL_DATE_OPTIONS),
    };
}

export function buildCyclePredictionView(prediction: CyclePredictions | null, locale: string): CyclePredictionViewModel | null {
    if (prediction === null) {
        return null;
    }

    return {
        prediction,
        nextPeriodStartLabel: formatCycleDate(prediction.nextPeriodStart, locale, SHORT_DATE_OPTIONS, UTC_TIME_ZONE),
        ovulationDateLabel: formatCycleDate(prediction.ovulationDate, locale, SHORT_DATE_OPTIONS, UTC_TIME_ZONE),
        pmsStartLabel: formatCycleDate(prediction.pmsStart, locale, SHORT_DATE_OPTIONS, UTC_TIME_ZONE),
    };
}

export function buildCycleDayItems(days: CycleDay[], locale: string): CycleDayViewModel[] {
    return days.map(day => ({
        day,
        dateLabel: formatCycleDate(day.date, locale, FULL_DATE_OPTIONS),
        accentColor: day.isPeriod ? PERIOD_DAY_ACCENT_COLOR : DEFAULT_DAY_ACCENT_COLOR,
        badgeLabelKey: day.isPeriod ? 'CYCLE_TRACKING.BADGE_PERIOD' : 'CYCLE_TRACKING.BADGE_FOLLICULAR',
    }));
}

function formatCycleDate(
    value: string | null | undefined,
    locale: string,
    options: Intl.DateTimeFormatOptions,
    timeZone?: Intl.DateTimeFormatOptions['timeZone'],
): string {
    if (value === null || value === undefined || value.length === 0) {
        return '';
    }

    const date = new Date(value);
    if (Number.isNaN(date.getTime())) {
        return value;
    }

    return new Intl.DateTimeFormat(locale, {
        ...options,
        timeZone,
    }).format(date);
}
