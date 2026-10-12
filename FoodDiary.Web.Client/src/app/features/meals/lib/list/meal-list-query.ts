import type { ParamMap } from '@angular/router';

import { formatDateInputValue, parseLocalDateInputValue } from '../../../../shared/lib/local-date.utils';
import { normalizeMealType } from '../../../../shared/lib/meal-type.util';
import { readPaginationPage } from '../../../../shared/navigation/pagination-query.utils';
import type { MealListStructuredFilters } from './meal-list.facade';

export type MealListQuery = MealListStructuredFilters & { page: number };

export function readMealListQuery(params: ParamMap): MealListQuery {
    const start = readDay(params.get('dateFrom'));
    const end = readDay(params.get('dateTo'));
    const caloriesFrom = readCalories(params.get('caloriesFrom'));
    const caloriesTo = readCalories(params.get('caloriesTo'));
    return {
        page: readPaginationPage(params.get('page')),
        dateRange: start === null && end === null ? null : { start: start !== null && end !== null && start > end ? null : start, end },
        mealTypes: [
            ...new Set(
                params
                    .getAll('mealTypes')
                    .flatMap(value => value.split(','))
                    .map(normalizeMealType),
            ),
        ]
            .filter(value => value !== null)
            .sort(),
        caloriesFrom: caloriesFrom !== null && caloriesTo !== null && caloriesFrom > caloriesTo ? null : caloriesFrom,
        caloriesTo,
        hasImage: readBoolean(params.get('hasImage')),
        hasAiSession: readBoolean(params.get('hasAiSession')),
    };
}

export function writeMealListQuery(query: MealListQuery): Record<string, string | null> {
    return {
        page: query.page === 1 ? null : String(query.page),
        dateFrom: writeDay(query.dateRange?.start),
        dateTo: writeDay(query.dateRange?.end),
        mealTypes: query.mealTypes.length > 0 ? [...new Set(query.mealTypes)].sort().join(',') : null,
        caloriesFrom: query.caloriesFrom === null ? null : String(query.caloriesFrom),
        caloriesTo: query.caloriesTo === null ? null : String(query.caloriesTo),
        hasImage: query.hasImage === null ? null : String(query.hasImage),
        hasAiSession: query.hasAiSession === null ? null : String(query.hasAiSession),
    };
}

export function mealListQueryKey(query: MealListQuery): string {
    return JSON.stringify(writeMealListQuery(query));
}

function readDay(value: string | null): Date | null {
    if (value === null || !/^\d{4}-\d{2}-\d{2}$/.test(value)) {
        return null;
    }
    const day = parseLocalDateInputValue(value);
    return day !== null && formatDateInputValue(day) === value ? day : null;
}

function writeDay(value: Date | null | undefined): string | null {
    return value === null || value === undefined ? null : formatDateInputValue(value);
}

function readCalories(value: string | null): number | null {
    if (value === null || value.trim().length === 0) {
        return null;
    }
    const number = Number(value);
    return Number.isFinite(number) && number >= 0 ? number : null;
}

function readBoolean(value: string | null): boolean | null {
    return value === 'true' || value === 'false' ? value === 'true' : null;
}
