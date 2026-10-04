import { describe, expect, it } from 'vitest';

import type { RecipeImportResult } from '../../../../../shared/models/recipe-import.data';
import { mapRecognizedRecipe } from './recipe-recognition.mapper';

const salad: RecipeImportResult = {
    name: 'Салат с огурцом',
    description: null,
    ingredients: [
        { name: 'Греческий йогурт', amount: '180 г' },
        { name: 'Чеснок', amount: '1½ зубчика' },
        { name: 'Укроп', amount: 'щепотка' },
    ],
    steps: ['Смешать соус.', 'Добавить огурец.', 'Подать сразу.'],
    servings: null,
    prepMinutes: null,
    cookMinutes: null,
    authorNutrition: '100 ккал; 20 г белка (порция не указана)',
    sourceUrl: 'https://www.instagram.com/reel/example/',
};

describe('recognized recipe mapping', () => {
    it('keeps quantities as text and attaches ingredients once', () => {
        const draft = mapRecognizedRecipe(salad, 'Источник', 'Со слов автора');
        expect(draft.steps?.[0]?.ingredients.map(item => item.amountText)).toEqual(['180 г', '1½ зубчика', 'щепотка']);
        expect(draft.steps?.[0]?.ingredients.every(item => item.productId === null && item.amount === null)).toBe(true);
        expect(draft.steps?.slice(1).every(step => step.ingredients.length === 0)).toBe(true);
        expect(draft.prepTime).toBeNull();
        expect(draft.cookTime).toBeNull();
        expect(draft.servings).toBe(1);
    });
    it('keeps author nutrition as a note and clears previous manual totals', () => {
        const draft = mapRecognizedRecipe(salad, 'Источник', 'Со слов автора');
        expect(draft.comment).toContain(salad.sourceUrl);
        expect(draft.comment).toContain(salad.authorNutrition);
        expect(draft.manualCalories).toBeNull();
        expect(draft.manualProteins).toBeNull();
    });
});
