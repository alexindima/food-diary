import type { ChartConfiguration } from 'chart.js';

import { resolveAppLocale } from '../../../shared/lib/locale.constants';
import type { WaistEntry, WaistEntrySummaryPoint } from '../models/waist-entry.data';
import type { WaistEntryViewModel } from './waist-history.types';

export const WAIST_HISTORY_CHART_OPTIONS: ChartConfiguration<'line'>['options'] = {
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

export function buildWaistHistoryChartData(
    points: WaistEntrySummaryPoint[],
    label: string,
    locale: string,
): ChartConfiguration<'line'>['data'] {
    const ordered = [...points].sort((a, b) => new Date(a.startDate).getTime() - new Date(b.startDate).getTime());

    return {
        labels: ordered.map(point => formatWaistHistoryDateLabel(point.startDate, locale)),
        datasets: [
            {
                data: ordered.map(point => (point.averageCircumference > 0 ? point.averageCircumference : null)),
                label,
                borderColor: 'var(--fd-color-sky-500)',
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

export function buildWaistEntryViewModels(entries: WaistEntry[], locale: string): WaistEntryViewModel[] {
    return entries.map(entry => ({
        entry,
        dateLabel: formatWaistHistoryNumericDate(entry.date, locale),
    }));
}

export function formatWaistHistoryNumericDate(value: string, language: string): string {
    const date = new Date(value);
    if (Number.isNaN(date.getTime())) {
        return value;
    }

    return new Intl.DateTimeFormat(resolveAppLocale(language), {
        day: '2-digit',
        month: '2-digit',
        year: 'numeric',
    }).format(date);
}

function formatWaistHistoryDateLabel(dateString: string, locale: string): string {
    return new Date(dateString).toLocaleDateString(resolveAppLocale(locale));
}
