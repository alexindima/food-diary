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
    primaryFill: '--fd-color-chart-primary-fill',
    warning: '--fd-color-orange-500',
} as const;

export type ChartColorPalette = Record<keyof typeof CHART_COLOR_VARIABLES, string>;

const CHART_COLOR_FALLBACKS: ChartColorPalette = {
    proteins: '#2d9cdb',
    fats: '#f2c94c',
    carbs: '#27ae60',
    fiber: '#9b51e0',
    alcohol: '#64748b',
    calories: '#e11d48',
    radarBackground: 'rgba(45, 156, 219, 0.2)',
    radarBorder: '#2d9cdb',
    primaryLine: '#2563eb',
    primaryFill: 'rgba(37, 99, 235, 0.15)',
    warning: '#f97316',
};

function readCssColor(ownerDocument: Document | null, variable: string, fallback: string): string {
    const view = ownerDocument?.defaultView;
    if (ownerDocument === null || view === null || view === undefined) {
        return fallback;
    }

    const value = view.getComputedStyle(ownerDocument.documentElement).getPropertyValue(variable).trim();
    return value.length > 0 ? value : fallback;
}

export function createChartColorPalette(ownerDocument: Document | null): ChartColorPalette {
    const readColor = (key: keyof typeof CHART_COLOR_VARIABLES): string =>
        readCssColor(ownerDocument, CHART_COLOR_VARIABLES[key], CHART_COLOR_FALLBACKS[key]);

    return {
        proteins: readColor('proteins'),
        fats: readColor('fats'),
        carbs: readColor('carbs'),
        fiber: readColor('fiber'),
        alcohol: readColor('alcohol'),
        calories: readColor('calories'),
        radarBackground: readColor('radarBackground'),
        radarBorder: readColor('radarBorder'),
        primaryLine: readColor('primaryLine'),
        primaryFill: readColor('primaryFill'),
        warning: readColor('warning'),
    };
}

export const CHART_COLORS: ChartColorPalette = { ...CHART_COLOR_FALLBACKS };
