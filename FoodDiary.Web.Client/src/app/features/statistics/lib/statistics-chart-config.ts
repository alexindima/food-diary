import { ChartConfiguration, ChartOptions } from 'chart.js';

/**
 * Converts a hex color string to an rgba string with the given alpha.
 */
export function applyAlpha(hexColor: string, alpha: number): string {
    const match = /^#?([a-f\d]{2})([a-f\d]{2})([a-f\d]{2})$/i.exec(hexColor);
    if (!match) {
        return hexColor;
    }

    const r = parseInt(match[1], 16);
    const g = parseInt(match[2], 16);
    const b = parseInt(match[3], 16);
    return `rgba(${r}, ${g}, ${b}, ${alpha})`;
}

export const TICK_COLOR = '#475569';

export function createCaloriesLineChartOptions(formatTooltip: (label: string, value: number) => string): ChartConfiguration['options'] {
    return {
        responsive: true,
        maintainAspectRatio: false,
        plugins: {
            legend: { display: false },
            tooltip: {
                callbacks: {
                    label: (context): string => {
                        const label = context.label || '';
                        const value = Number(context.raw) || 0;
                        return formatTooltip(label, value);
                    },
                },
            },
        },
        scales: {
            y: {
                beginAtZero: true,
                ticks: { color: TICK_COLOR },
            },
            x: {
                ticks: { color: TICK_COLOR, maxRotation: 0 },
            },
        },
    };
}

export const nutrientsLineChartOptions: ChartConfiguration['options'] = {
    responsive: true,
    maintainAspectRatio: false,
    plugins: {
        legend: {
            position: 'bottom',
        },
    },
    scales: {
        y: {
            beginAtZero: true,
            ticks: { color: TICK_COLOR },
        },
        x: {
            ticks: { color: TICK_COLOR, maxRotation: 0 },
        },
    },
};

export function createPieChartOptions(formatTooltip: (label: string, value: number) => string): ChartOptions<'pie'> {
    return {
        plugins: {
            tooltip: {
                callbacks: {
                    label: (context): string => {
                        const label = context.label || '';
                        const value = Number(context.raw) || 0;
                        return formatTooltip(label, value);
                    },
                },
            },
        },
    };
}

export const radarChartOptions: ChartOptions<'radar'> = {
    scales: {
        r: {
            beginAtZero: true,
            angleLines: { color: '#cbd5f5' },
            grid: { color: '#e2e8f0' },
            ticks: { showLabelBackdrop: false },
        },
    },
};

export const barChartOptions: ChartOptions<'bar'> = {
    responsive: true,
    scales: {
        y: {
            beginAtZero: true,
        },
    },
    plugins: {
        legend: { display: false },
    },
};

export const bodyChartOptions: ChartConfiguration['options'] = {
    responsive: true,
    maintainAspectRatio: false,
    plugins: {
        legend: { display: false },
    },
    scales: {
        y: { beginAtZero: false, ticks: { color: TICK_COLOR } },
        x: { ticks: { color: TICK_COLOR, maxRotation: 0 } },
    },
};

export const summarySparklineOptions: ChartConfiguration['options'] = {
    responsive: true,
    maintainAspectRatio: false,
    plugins: {
        legend: { display: false },
        tooltip: {
            enabled: false,
        },
    },
    elements: {
        line: { borderJoinStyle: 'round' },
    },
    scales: {
        x: { display: false },
        y: { display: false },
    },
};
