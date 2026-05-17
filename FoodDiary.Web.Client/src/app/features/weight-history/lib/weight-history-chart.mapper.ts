import type { ChartConfiguration } from 'chart.js';

import { compareDatesAsc, parseDateValue } from '../../../shared/lib/local-date.utils';
import { resolveAppLocale } from '../../../shared/lib/locale.constants';
import type { WeightEntry, WeightEntrySummaryPoint } from '../models/weight-entry.data';
import type { WeightEntryViewModel } from './weight-history.types';

export const WEIGHT_HISTORY_CHART_OPTIONS: ChartConfiguration<'line'>['options'] = {
    responsive: true,
    scales: {
        x: {
            ticks: {
                maxRotation: 0,
                autoSkip: true,
                maxTicksLimit: 6,
            },
        },
        y: {
            beginAtZero: true,
        },
    },
    plugins: {
        legend: {
            display: false,
        },
    },
};

export function buildWeightHistoryChartData(
    points: WeightEntrySummaryPoint[],
    label: string,
    locale: string,
): ChartConfiguration<'line'>['data'] {
    const ordered = [...points].sort((a, b) => compareDatesAsc(a.startDate, b.startDate));

    return {
        labels: ordered.map(point => formatWeightHistoryDateLabel(point.startDate, locale)),
        datasets: [
            {
                data: ordered.map(point => (point.averageWeight > 0 ? point.averageWeight : null)),
                label,
                borderColor: 'var(--fd-color-primary-600)',
                backgroundColor: 'transparent',
                fill: false,
                tension: 0.35,
                pointRadius: 4,
                pointBackgroundColor: 'var(--fd-color-white)',
                borderWidth: 2,
                spanGaps: true,
            },
        ],
    };
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
