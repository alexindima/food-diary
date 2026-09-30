import { compareDatesAsc, parseCalendarDateUtc } from '../../../shared/lib/local-date.utils';
import { resolveAppLocale } from '../../../shared/lib/locale.constants';
import type { WaistEntry, WaistEntrySummaryPoint } from '../models/waist-entry.data';
import type { WaistEntryViewModel } from './waist-history.types';

const CALENDAR_DATE_LENGTH = 10;

export type WaistHistoryChartPoint = {
    label: string;
    value: number | null;
};

export function buildWaistHistoryChartPoints(
    points: WaistEntrySummaryPoint[],
    locale: string,
    currentYear = new Date().getFullYear(),
): WaistHistoryChartPoint[] {
    const ordered = [...points].sort((a, b) => compareDatesAsc(a.startDate, b.startDate));

    const firstDate = parseCalendarDateUtc(ordered[0]?.startDate);
    const lastDate = parseCalendarDateUtc(ordered.at(-1)?.startDate);
    const showYear = firstDate?.getUTCFullYear() !== currentYear || lastDate?.getUTCFullYear() !== currentYear;
    return ordered.map(point => ({
        label:
            point.startDate.slice(0, CALENDAR_DATE_LENGTH) === point.endDate.slice(0, CALENDAR_DATE_LENGTH)
                ? formatWaistHistoryDateLabel(point.startDate, locale, showYear)
                : `${formatWaistHistoryDateLabel(point.startDate, locale, showYear)} – ${formatWaistHistoryDateLabel(point.endDate, locale, showYear)}`,
        value: point.averageCircumferenceCm > 0 ? point.averageCircumferenceCm : null,
    }));
}

export function buildWaistEntryViewModels(entries: WaistEntry[], locale: string): WaistEntryViewModel[] {
    return entries.map(entry => ({
        entry,
        dateLabel: formatWaistHistoryNumericDate(entry.date, locale),
    }));
}

export function formatWaistHistoryNumericDate(value: string, language: string): string {
    const date = parseCalendarDateUtc(value);
    if (date === null) {
        return value;
    }

    return new Intl.DateTimeFormat(resolveAppLocale(language), {
        day: '2-digit',
        month: '2-digit',
        year: 'numeric',
        timeZone: 'UTC',
    }).format(date);
}

function formatWaistHistoryDateLabel(dateString: string, locale: string, showYear: boolean): string {
    const date = parseCalendarDateUtc(dateString);
    if (date === null) {
        return dateString;
    }

    const day = new Intl.DateTimeFormat(resolveAppLocale(locale), { day: '2-digit', timeZone: 'UTC' }).format(date);
    const month = abbreviateMonth(new Intl.DateTimeFormat(resolveAppLocale(locale), { month: 'short', timeZone: 'UTC' }).format(date));
    return showYear ? `${day}\n${month}\n${date.getUTCFullYear()}` : `${day}\n${month}`;
}

function abbreviateMonth(month: string): string {
    const shortMonthLength = 3;
    return month.endsWith('.') || month.length <= shortMonthLength ? month : `${month.slice(0, shortMonthLength)}.`;
}
