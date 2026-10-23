import type { ParamMap } from '@angular/router';

import { formatDateInputValue } from '../../../shared/lib/local-date.utils';
import { type DateRange, isNutritionTab, isStatisticsRange, type NutritionChartTab, type StatisticsRange } from './statistics-data-mapper';

export type StatisticsRouteQuery = {
    range: StatisticsRange;
    nutritionTab: NutritionChartTab;
    customRange: DateRange | null;
};

function readCalendarDate(value: string | null): Date | null {
    if (value === null || !/^\d{4}-\d{2}-\d{2}$/.test(value)) {
        return null;
    }
    const date = new Date(`${value}T00:00:00`);
    return !Number.isNaN(date.getTime()) && formatDateInputValue(date) === value ? date : null;
}

export function readStatisticsRouteQuery(params: ParamMap): StatisticsRouteQuery {
    const range = params.get('range');
    const tab = params.get('nutritionTab');
    const start = readCalendarDate(params.get('dateFrom'));
    const end = readCalendarDate(params.get('dateTo'));
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
