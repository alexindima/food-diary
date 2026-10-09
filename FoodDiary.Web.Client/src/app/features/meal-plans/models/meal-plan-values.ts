/* eslint-disable @typescript-eslint/no-unsafe-type-assertion -- stored projections attach meaning while retaining historical scalar values */
import type { SemanticQuantity, UnbrandedQuantity } from '../../../shared/models/semantics/quantity-meaning';
import type { SemanticString, UnbrandedString } from '../../../shared/models/semantics/string-meaning';

export type PlanDurationDays = SemanticQuantity<'plan-duration-days'>;
export type PlanDayNumber = SemanticQuantity<'plan-day-number'>;
export type PlannedServings = SemanticQuantity<'planned-servings'>;
export type PlannedMealType = SemanticString<'planned-meal-type'>;

const MAXIMUM_PLAN_DAYS = 31;

function validateDayCount(value: number): void {
    if (!Number.isInteger(value) || value < 1 || value > MAXIMUM_PLAN_DAYS) {
        throw new RangeError('Plan days must be integers in [1, 31].');
    }
}

export function planDurationDays(value: UnbrandedQuantity | PlanDurationDays): PlanDurationDays {
    validateDayCount(value);
    return planDurationDaysFromStored(value);
}

export function planDayNumber(value: UnbrandedQuantity | PlanDayNumber): PlanDayNumber {
    validateDayCount(value);
    return planDayNumberFromStored(value);
}

export function plannedServings(value: UnbrandedQuantity | PlannedServings): PlannedServings {
    if (!Number.isInteger(value) || value <= 0) {
        throw new RangeError('Planned servings must be positive integers.');
    }
    return plannedServingsFromStored(value);
}

/** Read-only stored projections preserve legacy values and display fallbacks. */
export function planDurationDaysFromStored(value: UnbrandedQuantity | PlanDurationDays): PlanDurationDays {
    return value as PlanDurationDays;
}

export function planDayNumberFromStored(value: UnbrandedQuantity | PlanDayNumber): PlanDayNumber {
    return value as PlanDayNumber;
}

export function plannedServingsFromStored(value: UnbrandedQuantity | PlannedServings): PlannedServings {
    return value as PlannedServings;
}

/** Keep casing, unknown codes and the existing OTHER fallback at diary selection. */
export function plannedMealTypeFromStored(value: UnbrandedString | PlannedMealType): PlannedMealType {
    return value as PlannedMealType;
}
