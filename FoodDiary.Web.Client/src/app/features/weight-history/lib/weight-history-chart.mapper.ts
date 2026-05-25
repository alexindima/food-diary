import { compareDatesAsc, parseDateValue } from '../../../shared/lib/local-date.utils';
import { resolveAppLocale } from '../../../shared/lib/locale.constants';
import type { WeightEntry, WeightEntrySummaryPoint } from '../models/weight-entry.data';
import type { WeightEntryViewModel } from './weight-history.types';

export type WeightHistoryChartPoint = {
    label: string;
    value: number | null;
};

export function buildWeightHistoryChartPoints(points: WeightEntrySummaryPoint[], locale: string): WeightHistoryChartPoint[] {
    const ordered = [...points].sort((a, b) => compareDatesAsc(a.startDate, b.startDate));

    return ordered.map(point => ({
        label: formatWeightHistoryDateLabel(point.startDate, locale),
        value: point.averageWeight > 0 ? point.averageWeight : null,
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

function formatWeightHistoryDateLabel(dateString: string, locale: string): string {
    const date = parseDateValue(dateString);
    return date !== null ? new Intl.DateTimeFormat(resolveAppLocale(locale)).format(date) : dateString;
}
