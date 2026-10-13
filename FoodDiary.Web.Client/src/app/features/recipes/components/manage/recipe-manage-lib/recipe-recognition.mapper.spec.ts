import { describe, expect, it } from 'vitest';

import type { RecipeImportResult } from '../../../../../shared/models/recipe-import.data';
import { mapRecognizedRecipe } from './recipe-recognition.mapper';

const OUT_OF_RANGE_STEP_INDEX = 4;
const FRACTIONAL_STEP_INDEX = 0.5;
const FRACTIONAL_SERVINGS = 1.5;

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

    it('assigns source rows to their cooking steps without merging the same ingredient used separately', () => {
        const draft = mapRecognizedRecipe(
            {
                ...salad,
                ingredients: [
                    { name: 'Chicken', amount: '1.25 kg', stepIndex: 2 },
                    { name: 'Black pepper', amount: '1/2 tsp', stepIndex: 1 },
                    { name: 'Black pepper', amount: '1/2 tsp', stepIndex: 3 },
                ],
                steps: ['Preheat oven.', 'Mix seasoning.', 'Season chicken.', 'Mix gravy.'],
            },
            'Source',
            'Author',
        );
        expect(draft.steps?.map(step => step.ingredients.map(item => [item.textName, item.amountText]))).toEqual([
            [],
            [['Black pepper', '1/2 tsp']],
            [['Chicken', '1.25 kg']],
            [['Black pepper', '1/2 tsp']],
        ]);
    });

    it.each([null, undefined, -1, OUT_OF_RANGE_STEP_INDEX, FRACTIONAL_STEP_INDEX])(
        'keeps ingredients with unsupported step index %s in the fallback step',
        stepIndex => {
            const draft = mapRecognizedRecipe(
                { ...salad, ingredients: [{ name: 'Salt', amount: 'to taste', stepIndex }] },
                'Source',
                'Author',
            );
            expect(draft.steps?.[0]?.ingredients[0]?.amountText).toBe('to taste');
            expect(draft.steps?.flatMap(step => step.ingredients)).toHaveLength(1);
        },
    );

    it('keeps ingredients when the source has no instructions', () => {
        const draft = mapRecognizedRecipe({ ...salad, steps: [] }, 'Source', 'Author');
        expect(draft.steps).toHaveLength(1);
        expect(draft.steps?.[0]?.description).toBe('');
        expect(draft.steps?.[0]?.ingredients).toHaveLength(salad.ingredients.length);
    });
});

describe('stated recipe nutrition mapping', () => {
    it('imports the reported five-serving nutrition as manual recipe totals without product creation', () => {
        const draft = mapRecognizedRecipe({ ...salad, servings: 5, nutrition: statedNutrition }, 'Source', 'Author');
        expect(draft).toMatchObject({
            calculateNutritionAutomatically: false,
            manualCalories: 2525,
            manualProteins: 180,
            manualFats: 185,
            manualCarbs: 25,
            manualFiber: 5,
            manualAlcohol: null,
        });
        expect(draft.comment).toContain(salad.authorNutrition);
        expect(draft.steps?.flatMap(step => step.ingredients).every(item => item.productId === null)).toBe(true);
    });

    it('does not multiply whole-recipe nutrition and preserves explicit zeroes and missing optional values', () => {
        const draft = mapRecognizedRecipe(
            {
                ...salad,
                servings: null,
                nutrition: { ...statedNutrition, basis: 'recipe', calories: 333.33, proteins: 0, fats: 0, carbs: 0, fiber: null },
            },
            'Source',
            'Author',
        );
        expect(draft).toMatchObject({
            calculateNutritionAutomatically: false,
            manualCalories: 333.33,
            manualProteins: 0,
            manualFiber: null,
        });
    });

    it.each([
        { ...statedNutrition, basis: 'unknown' as const },
        { ...statedNutrition, basis: 'per100g' as const },
        { ...statedNutrition, proteins: null },
        { ...statedNutrition, calories: -1 },
        { ...statedNutrition, fats: Number.NaN },
        { ...statedNutrition, calories: Number.POSITIVE_INFINITY },
        { ...statedNutrition, calories: 100001 },
        { ...statedNutrition, proteins: 10001 },
    ])('leaves unusable nutrition $basis/$calories/$proteins as a note', nutrition => {
        const draft = mapRecognizedRecipe({ ...salad, servings: 1, nutrition }, 'Source', 'Author');
        expect(draft.calculateNutritionAutomatically).toBe(true);
        expect(draft.manualCalories).toBeNull();
        expect(draft.comment).toContain(salad.authorNutrition);
    });

    it.each([null, 0, FRACTIONAL_SERVINGS])('does not treat missing or invalid servings %s as a known nutrition basis', servings => {
        const draft = mapRecognizedRecipe({ ...salad, servings, nutrition: statedNutrition }, 'Source', 'Author');
        expect(draft.calculateNutritionAutomatically).toBe(true);
        expect(draft.manualCalories).toBeNull();
    });

    it('checks the manual limits after converting per-serving nutrition to recipe totals', () => {
        const draft = mapRecognizedRecipe({ ...salad, servings: 1000, nutrition: statedNutrition }, 'Source', 'Author');
        expect(draft.manualCalories).toBeNull();
        expect(draft.calculateNutritionAutomatically).toBe(true);
    });
});

const statedNutrition: NonNullable<RecipeImportResult['nutrition']> = {
    basis: 'serving',
    calories: 505,
    proteins: 36,
    fats: 37,
    carbs: 5,
    fiber: 1,
    alcohol: null,
};
