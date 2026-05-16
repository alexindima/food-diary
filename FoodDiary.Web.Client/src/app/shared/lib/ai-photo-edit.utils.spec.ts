import { describe, expect, it } from 'vitest';

import type { FoodNutritionResponse, FoodVisionItem } from '../models/ai.data';
import {
    type AiEditableFoodItem,
    buildAiEditableItems,
    createEmptyAiEditableItem,
    normalizeAiEditableItems,
    requiresAiNutritionRecalculation,
    resolveAiPhotoUnitKey,
    updateAiEditableItem,
} from './ai-photo-edit.utils';

const SOURCE_AMOUNT = 100;
const UPDATED_AMOUNT = '125';
const UPDATED_AMOUNT_VALUE = 125;

const resultItem: FoodVisionItem = {
    nameEn: ' Apple ',
    nameLocal: ' Яблоко ',
    amount: SOURCE_AMOUNT,
    unit: 'g',
    confidence: 1,
};

const nutrition: FoodNutritionResponse = {
    calories: 100,
    protein: 1,
    fat: 0,
    carbs: 20,
    fiber: 2,
    alcohol: 0,
    items: [
        {
            name: 'Banana',
            amount: SOURCE_AMOUNT,
            unit: 'g',
            calories: 100,
            protein: 1,
            fat: 0,
            carbs: 20,
            fiber: 2,
            alcohol: 0,
        },
    ],
};

function createEditableItem(overrides: Partial<AiEditableFoodItem> = {}): AiEditableFoodItem {
    return {
        id: 'item-1',
        name: 'Apple',
        nameEn: 'Apple',
        nameLocal: null,
        amount: SOURCE_AMOUNT,
        unit: 'g',
        ...overrides,
    };
}

describe('ai photo edit utils', () => {
    it('builds trimmed editable items from vision results', () => {
        const items = buildAiEditableItems([resultItem], null, () => 'item-1');

        expect(items).toEqual([
            {
                id: 'item-1',
                name: 'Яблоко',
                nameEn: 'Apple',
                nameLocal: 'Яблоко',
                amount: SOURCE_AMOUNT,
                unit: 'g',
            },
        ]);
    });

    it('falls back to nutrition items when vision results are empty', () => {
        const items = buildAiEditableItems([], nutrition, () => 'item-1');

        expect(items[0]).toMatchObject({ name: 'Banana', nameEn: 'Banana', amount: SOURCE_AMOUNT });
    });

    it('normalizes editable items to food vision items', () => {
        const items = normalizeAiEditableItems([createEditableItem({ name: 'Pear', nameEn: '', nameLocal: ' Pear ' })]);

        expect(items).toEqual([{ nameEn: 'Pear', nameLocal: 'Pear', amount: SOURCE_AMOUNT, unit: 'g', confidence: 1 }]);
    });

    it('requires AI recalculation for added, renamed, or re-unitized items only', () => {
        const source = [createEditableItem()];

        expect(requiresAiNutritionRecalculation(source, [createEditableItem({ amount: 150 })])).toBe(false);
        expect(requiresAiNutritionRecalculation(source, [createEditableItem({ name: 'Pear' })])).toBe(true);
        expect(requiresAiNutritionRecalculation(source, [createEditableItem({ unit: 'ml' })])).toBe(true);
        expect(requiresAiNutritionRecalculation(source, [createEditableItem(), createEditableItem({ id: 'new' })])).toBe(true);
    });

    it('updates editable item fields immutably', () => {
        const source = [createEditableItem()];

        expect(updateAiEditableItem(source, 0, 'amount', UPDATED_AMOUNT)[0]?.amount).toBe(UPDATED_AMOUNT_VALUE);
        expect(updateAiEditableItem(source, 0, 'unit', 'ml')[0]?.unit).toBe('ml');
        expect(updateAiEditableItem(source, 0, 'name', 'Pear')[0]).toMatchObject({ name: 'Pear', nameEn: 'Pear', nameLocal: 'Pear' });
    });

    it('creates empty editable items with the requested unit', () => {
        expect(createEmptyAiEditableItem(() => 'new-id', 'pcs')).toEqual({
            id: 'new-id',
            name: '',
            nameEn: '',
            nameLocal: '',
            amount: 0,
            unit: 'pcs',
        });
    });

    it('resolves supported unit translation keys', () => {
        expect(resolveAiPhotoUnitKey(null)).toBeNull();
        expect(resolveAiPhotoUnitKey(' grams ')).toBe('GENERAL.UNITS.G');
        expect(resolveAiPhotoUnitKey('ML')).toBe('GENERAL.UNITS.ML');
        expect(resolveAiPhotoUnitKey('piece')).toBe('GENERAL.UNITS.PCS');
        expect(resolveAiPhotoUnitKey('unknown')).toBeNull();
    });
});
