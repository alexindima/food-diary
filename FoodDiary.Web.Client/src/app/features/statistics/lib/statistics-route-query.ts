import { formatDateInputValue } from '../../../shared/lib/local-date.utils';
import { type DateRange, isNutritionTab, isStatisticsRange, type NutritionChartTab, type StatisticsRange } from './statistics-data-mapper';

export type StatisticsRouteQuery = {
    range: StatisticsRange;
    nutritionTab: NutritionChartTab;
    customRange: DateRange | null;
};

export type StatisticsRouteParams = Readonly<Record<string, string | null | undefined>>;

export function firstStatisticsQueryValue(value: string | string[] | undefined): string | null {
    return (Array.isArray(value) ? value[0] : value) ?? null;
}

function readCalendarDate(value: string | null | undefined): Date | null {
    if (value === null || value === undefined || !/^\d{4}-\d{2}-\d{2}$/.test(value)) {
        return null;
    }
    const date = new Date(`${value}T00:00:00`);
    return !Number.isNaN(date.getTime()) && formatDateInputValue(date) === value ? date : null;
}

export function readStatisticsRouteQuery(params: StatisticsRouteParams): StatisticsRouteQuery {
    const range = params['range'];
    const tab = params['nutritionTab'];
    const start = readCalendarDate(params['dateFrom']);
    const end = readCalendarDate(params['dateTo']);
    const customRange = start !== null && end !== null ? (start <= end ? { start, end } : { start: end, end: start }) : null;
    return {
        range: isStatisticsRange(range) && (range !== 'custom' || customRange !== null) ? range : 'week',
        nutritionTab: isNutritionTab(tab) ? tab : 'calories',
        customRange: range === 'custom' ? customRange : null,
    };
}

export function writeStatisticsRouteQuery(query: StatisticsRouteQuery): Record<string, string | null> {
    return {
        range: query.range === 'week' ? null : query.range,
        nutritionTab: query.nutritionTab === 'calories' ? null : query.nutritionTab,
        dateFrom: query.range === 'custom' && query.customRange !== null ? formatDateInputValue(query.customRange.start) : null,
        dateTo: query.range === 'custom' && query.customRange !== null ? formatDateInputValue(query.customRange.end) : null,
    };
}
