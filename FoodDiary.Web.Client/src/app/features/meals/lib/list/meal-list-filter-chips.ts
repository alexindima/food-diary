import { formatDateInputValue } from '../../../../shared/lib/local-date.utils';
import type { MealListStructuredFilters } from './meal-list.facade';

export type MealFilterChip = { id: string; label: string };
export function mealFilterChips(model: MealListStructuredFilters, translate: (key: string) => string, locale: string): MealFilterChip[] {
    const chips: MealFilterChip[] = [];
    const add = (id: string, label: string): void => {
        chips.push({ id, label });
    };
    chips.push(...dateChips(model, translate));
    for (const type of model.mealTypes) {
        add(`mealType:${type}`, translate(`MEAL_TYPES.${type}`));
    }
    const number = new Intl.NumberFormat(locale);
    if (model.caloriesFrom !== null) {
        add('caloriesFrom', `${translate('MEAL_LIST.FILTER_CALORIES_FROM')}: ${number.format(model.caloriesFrom)}`);
    }
    if (model.caloriesTo !== null) {
        add('caloriesTo', `${translate('MEAL_LIST.FILTER_CALORIES_TO')}: ${number.format(model.caloriesTo)}`);
    }
    if (model.hasImage !== null) {
        add('hasImage', translate(model.hasImage ? 'MEAL_LIST.FILTER_IMAGE_WITH' : 'MEAL_LIST.FILTER_IMAGE_WITHOUT'));
    }
    if (model.hasAiSession !== null) {
        add('hasAiSession', translate(model.hasAiSession ? 'MEAL_LIST.FILTER_AI_WITH' : 'MEAL_LIST.FILTER_AI_WITHOUT'));
    }
    return chips;
}

export function removeMealFilter(model: MealListStructuredFilters, id: string): MealListStructuredFilters {
    if (id.startsWith('mealType:')) {
        return { ...model, mealTypes: model.mealTypes.filter(type => `mealType:${type}` !== id) };
    }
    if (id === 'start' || id === 'end') {
        return { ...model, dateRange: { start: model.dateRange?.start ?? null, end: model.dateRange?.end ?? null, [id]: null } };
    }
    if (['caloriesFrom', 'caloriesTo', 'hasImage', 'hasAiSession'].includes(id)) {
        return { ...model, [id]: null };
    }
    return model;
}

export function emptyMealFilters(): MealListStructuredFilters {
    return { dateRange: null, mealTypes: [], caloriesFrom: null, caloriesTo: null, hasImage: null, hasAiSession: null };
}

function dateChips(model: MealListStructuredFilters, translate: (key: string) => string): MealFilterChip[] {
    const chips: MealFilterChip[] = [];
    for (const boundary of ['start', 'end'] as const) {
        const date = model.dateRange?.[boundary];
        if (date !== null && date !== undefined) {
            const labelKey = boundary === 'start' ? 'MEAL_LIST.DATE_FILTER_FROM_LABEL' : 'MEAL_LIST.DATE_FILTER_TO_LABEL';
            chips.push({ id: boundary, label: `${translate(labelKey)}: ${formatDateInputValue(date)}` });
        }
    }
    return chips;
}
