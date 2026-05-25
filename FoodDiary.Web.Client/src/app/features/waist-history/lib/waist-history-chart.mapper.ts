import { compareDatesAsc, parseDateValue } from '../../../shared/lib/local-date.utils';
import { resolveAppLocale } from '../../../shared/lib/locale.constants';
import type { WaistEntry, WaistEntrySummaryPoint } from '../models/waist-entry.data';
import type { WaistEntryViewModel } from './waist-history.types';

export type WaistHistoryChartPoint = {
    label: string;
    value: number | null;
};

export function buildWaistHistoryChartPoints(points: WaistEntrySummaryPoint[], locale: string): WaistHistoryChartPoint[] {
    const ordered = [...points].sort((a, b) => compareDatesAsc(a.startDate, b.startDate));

    return ordered.map(point => ({
        label: formatWaistHistoryDateLabel(point.startDate, locale),
        value: point.averageCircumference > 0 ? point.averageCircumference : null,
    }));
}

export function buildWaistEntryViewModels(entries: WaistEntry[], locale: string): WaistEntryViewModel[] {
    return entries.map(entry => ({
        entry,
        dateLabel: formatWaistHistoryNumericDate(entry.date, locale),
    }));
}

export function formatWaistHistoryNumericDate(value: string, language: string): string {
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

function formatWaistHistoryDateLabel(dateString: string, locale: string): string {
    const date = parseDateValue(dateString);
    return date !== null ? new Intl.DateTimeFormat(resolveAppLocale(locale)).format(date) : dateString;
}
