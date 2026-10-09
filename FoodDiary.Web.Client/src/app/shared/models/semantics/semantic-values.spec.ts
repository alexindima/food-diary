import { describe, expect, it } from 'vitest';

import { mealItemFromStored, MealSourceType } from '../meal.data';
import { calendarDate, optionalCalendarDate, utcInstant } from './date-value';
import { entityId, optionalEntityId } from './entity-id';
import { productQuantity, productQuantityFromStored, recipeServings } from './meal-quantity';

const MAXIMUM_QUANTITY = 1_000_000;
const OVER_MAXIMUM_QUANTITY = MAXIMUM_QUANTITY + 1;

describe('semantic scalar boundaries', () => {
    it('retains precision, existing encodings, placeholders and absent values', () => {
        expect(utcInstant('2026-10-08T12:00:00.1234567Z')).toBe('2026-10-08T12:00:00.1234567Z');
        expect(calendarDate('2026-10-08T00:00:00Z')).toBe('2026-10-08T00:00:00Z');
        expect(entityId<'meal'>('draft-id')).toBe('draft-id');
        expect(optionalEntityId<'meal'>(undefined)).toBeUndefined();
        expect(optionalEntityId<'meal'>(null)).toBeNull();
        expect(optionalCalendarDate(null)).toBeNull();
    });

    it('validates mutation quantities while preserving historical observations', () => {
        for (const value of [0, -1, Number.NaN, Number.POSITIVE_INFINITY, OVER_MAXIMUM_QUANTITY]) {
            expect(() => productQuantity(value)).toThrow(RangeError);
            expect(() => recipeServings(value)).toThrow(RangeError);
        }
        expect(productQuantity(MAXIMUM_QUANTITY)).toBe(MAXIMUM_QUANTITY);
        expect(productQuantityFromStored(0)).toBe(0);
    });

    it('retains source meaning and amount when the stored source was deleted', () => {
        const item = mealItemFromStored({ id: entityId<'meal-item'>('item'), mealId: entityId<'meal'>('meal') }, 0, {
            product: null,
            recipe: null,
            sourceType: MealSourceType.Product,
        });
        expect(item).toMatchObject({ sourceType: MealSourceType.Product, amount: 0, product: null, recipe: null });
    });
});
