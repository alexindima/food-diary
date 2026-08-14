import { compareDatesAsc, parseDateValue } from '../../../shared/lib/local-date.utils';
import { resolveAppLocale } from '../../../shared/lib/locale.constants';
import type { WeightEntry, WeightEntrySummaryPoint } from '../models/weight-entry.data';
import type { WeightEntryViewModel } from './weight-history.types';

export type WeightHistoryChartPoint = {
    label: string;
    value: number | null;
};

export function buildWeightHistoryChartPoints(
    points: WeightEntrySummaryPoint[],
    locale: string,
    currentYear = new Date().getUTCFullYear(),
): WeightHistoryChartPoint[] {
    const ordered = [...points].sort((a, b) => compareDatesAsc(a.startDate, b.startDate));
    const firstDate = parseDateValue(ordered[0]?.startDate);
    const lastDate = parseDateValue(ordered.at(-1)?.startDate);
    const showYear = firstDate?.getUTCFullYear() !== currentYear || lastDate?.getUTCFullYear() !== currentYear;

    return ordered.map(point => ({
        label: formatWeightHistoryDateLabel(point.startDate, locale, showYear),
        value: point.averageWeightKg > 0 ? point.averageWeightKg : null,
    }));
}

export function buildWeightEntryViewModels(entries: WeightEntry[], locale: string): WeightEntryViewModel[] {
    return entries.map(entry => ({
        entry,
        dateLabel: formatWeightHistoryNumericDate(entry.date, locale),
    }));
}

export function formatWeightHistoryNumericDate(value: string, language: string): string {
    const date = parseDateValue(value);
    if (date === null) {
        return value;
    }

    return new Intl.DateTimeFormat(resolveAppLocale(language), {
        day: '2-digit',
        month: '2-digit',
        year: 'numeric',
    }).format(date);
}

function formatWeightHistoryDateLabel(dateString: string, locale: string, showYear: boolean): string {
    const date = parseDateValue(dateString);
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
