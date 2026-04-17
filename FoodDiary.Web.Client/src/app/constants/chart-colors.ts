const CHART_COLOR_VARIABLES = {
    proteins: '--fd-color-chart-proteins',
    fats: '--fd-color-chart-fats',
    carbs: '--fd-color-chart-carbs',
    fiber: '--fd-color-chart-fiber',
    alcohol: '--fd-color-nutrition-alcohol',
    calories: '--fd-color-nutrition-calories',
    radarBackground: '--fd-color-chart-radar-background',
    radarBorder: '--fd-color-chart-radar-border',
    primaryLine: '--fd-color-primary-600',
    warning: '--fd-color-orange-500',
} as const;

const CHART_COLOR_FALLBACKS: Record<keyof typeof CHART_COLOR_VARIABLES, string> = {
    proteins: '#2d9cdb',
    fats: '#f2c94c',
    carbs: '#27ae60',
    fiber: '#9b51e0',
    alcohol: '#64748b',
    calories: '#e11d48',
    radarBackground: 'rgba(45, 156, 219, 0.2)',
    radarBorder: '#2d9cdb',
    primaryLine: '#2563eb',
    warning: '#f97316',
};

function readCssColor(variable: string, fallback: string): string {
    if (typeof window === 'undefined' || typeof document === 'undefined') {
        return fallback;
    }

    const value = window.getComputedStyle(document.documentElement).getPropertyValue(variable).trim();
    return value || fallback;
}

function getChartColor(key: keyof typeof CHART_COLOR_VARIABLES): string {
    return readCssColor(CHART_COLOR_VARIABLES[key], CHART_COLOR_FALLBACKS[key]);
}

export const CHART_COLORS = {
    get proteins(): string {
        return getChartColor('proteins');
    },
    get fats(): string {
        return getChartColor('fats');
    },
    get carbs(): string {
        return getChartColor('carbs');
    },
    get fiber(): string {
        return getChartColor('fiber');
    },
    get alcohol(): string {
        return getChartColor('alcohol');
    },
    get calories(): string {
        return getChartColor('calories');
    },
    get radarBackground(): string {
        return getChartColor('radarBackground');
    },
    get radarBorder(): string {
        return getChartColor('radarBorder');
    },
    get primaryLine(): string {
        return getChartColor('primaryLine');
    },
    get warning(): string {
        return getChartColor('warning');
    },
};
