import { describe, expect, it } from 'vitest';

import { MeasurementUnit, type Product, ProductVisibility } from '../../../../products/models/product.data';
import { ConsumptionSourceType, type Meal } from '../../../models/meal.data';
import { buildMealDetailViewModel } from './meal-detail.mapper';

const BASE_AMOUNT = 100;
const TOTAL_CALORIES = 420;
const TOTAL_PROTEINS = 20;
const TOTAL_FATS = 10;
const TOTAL_CARBS = 40;
const TOTAL_FIBER = 7;
const TOTAL_ALCOHOL = 2;
const QUALITY_SCORE = 75;
const EXPECTED_PERCENT_TOTAL = 100;
const EXPECTED_ITEM_PREVIEW_COUNT = 4;
const AI_ITEM_AMOUNT = 200;
const UNKNOWN_UNIT_AI_ITEM_AMOUNT = 1;

const translate = (key: string): string => `translated:${key}`;

function createProduct(name: string, baseUnit: MeasurementUnit = MeasurementUnit.G): Product {
    return {
        id: `product-${name}`,
        name,
        baseUnit,
        baseAmount: BASE_AMOUNT,
        defaultPortionAmount: BASE_AMOUNT,
        caloriesPerBase: 0,
        proteinsPerBase: 0,
        fatsPerBase: 0,
        carbsPerBase: 0,
        fiberPerBase: 0,
        alcoholPerBase: 0,
        usageCount: 0,
        visibility: ProductVisibility.Private,
        createdAt: new Date('2026-05-14T00:00:00Z'),
        isOwnedByCurrentUser: true,
        qualityScore: 0,
        qualityGrade: 'green',
    };
}

function createMeal(overrides: Partial<Meal> = {}): Meal {
    return {
        id: 'meal-1',
        date: '2026-05-14T08:00:00Z',
        mealType: 'breakfast',
        comment: null,
        imageUrl: null,
        imageAssetId: null,
        totalCalories: TOTAL_CALORIES,
        totalProteins: TOTAL_PROTEINS,
        totalFats: TOTAL_FATS,
        totalCarbs: TOTAL_CARBS,
        totalFiber: TOTAL_FIBER,
        totalAlcohol: TOTAL_ALCOHOL,
        isNutritionAutoCalculated: true,
        preMealSatietyLevel: null,
        postMealSatietyLevel: null,
        qualityScore: QUALITY_SCORE,
        qualityGrade: 'green',
        items: [],
        aiSessions: [],
        ...overrides,
    };
}

function createMealWithItemPreview(): Meal {
    return createMeal({
        items: [
            {
                id: 'item-product',
                consumptionId: 'meal-1',
                amount: BASE_AMOUNT,
                sourceType: ConsumptionSourceType.Product,
                product: createProduct('Oatmeal'),
            },
            {
                id: 'item-unknown',
                consumptionId: 'meal-1',
                amount: BASE_AMOUNT,
                sourceType: ConsumptionSourceType.Product,
                product: null,
            },
        ],
        aiSessions: [
            {
                id: 'session-1',
                consumptionId: 'meal-1',
                recognizedAtUtc: '2026-05-14T08:05:00Z',
                items: [
                    {
                        id: 'ai-1',
                        sessionId: 'session-1',
                        nameEn: 'Coffee',
                        nameLocal: 'Local coffee',
                        amount: AI_ITEM_AMOUNT,
                        unit: 'ml',
                        calories: 0,
                        proteins: 0,
                        fats: 0,
                        carbs: 0,
                        fiber: 0,
                        alcohol: 0,
                    },
                    {
                        id: 'ai-2',
                        sessionId: 'session-1',
                        nameEn: '',
                        nameLocal: '   ',
                        amount: UNKNOWN_UNIT_AI_ITEM_AMOUNT,
                        unit: 'bowl',
                        calories: 0,
                        proteins: 0,
                        fats: 0,
                        carbs: 0,
                        fiber: 0,
                        alcohol: 0,
                    },
                ],
            },
        ],
    });
}

describe('buildMealDetailViewModel nutrition state', () => {
    it('maps nutrition, quality, meal type and form values', () => {
        const viewModel = buildMealDetailViewModel(createMeal(), translate);

        expect(viewModel.calories).toBe(TOTAL_CALORIES);
        expect(viewModel.proteins).toBe(TOTAL_PROTEINS);
        expect(viewModel.fats).toBe(TOTAL_FATS);
        expect(viewModel.carbs).toBe(TOTAL_CARBS);
        expect(viewModel.fiber).toBe(TOTAL_FIBER);
        expect(viewModel.alcohol).toBe(TOTAL_ALCOHOL);
        expect(viewModel.qualityScore).toBe(QUALITY_SCORE);
        expect(viewModel.qualityGrade).toBe('green');
        expect(viewModel.qualityHintKey).toBe('QUALITY.GREEN');
        expect(viewModel.mealTypeLabel).toBe('translated:MEAL_TYPES.breakfast');
        expect(viewModel.nutritionForm.controls.calories.value).toBe(TOTAL_CALORIES);
    });

    it('builds macro blocks and macro bar state from core macros', () => {
        const viewModel = buildMealDetailViewModel(createMeal(), translate);

        expect(viewModel.macroBlocks.map(block => block.value)).toEqual([
            TOTAL_PROTEINS,
            TOTAL_FATS,
            TOTAL_CARBS,
            TOTAL_FIBER,
            TOTAL_ALCOHOL,
        ]);
        expect(viewModel.macroBarState.isEmpty).toBe(false);
        expect(viewModel.macroBarState.segments.map(segment => segment.key)).toEqual(['proteins', 'fats', 'carbs']);
        expect(viewModel.macroBarState.segments.reduce((sum, segment) => sum + segment.percent, 0)).toBeCloseTo(EXPECTED_PERCENT_TOTAL);
    });

    it('uses empty macro state when proteins, fats and carbs are zero', () => {
        const viewModel = buildMealDetailViewModel(
            createMeal({
                totalProteins: 0,
                totalFats: 0,
                totalCarbs: 0,
            }),
            translate,
        );

        expect(viewModel.macroBarState.isEmpty).toBe(true);
        expect(viewModel.macroBarState.segments.every(segment => segment.percent === 0)).toBe(true);
    });
});

describe('buildMealDetailViewModel item preview state', () => {
    it('combines manual and AI item previews with translated fallback names and known units', () => {
        const viewModel = buildMealDetailViewModel(createMealWithItemPreview(), translate);

        expect(viewModel.itemsCount).toBe(EXPECTED_ITEM_PREVIEW_COUNT);
        expect(viewModel.itemPreview).toEqual([
            { name: 'Oatmeal', amount: BASE_AMOUNT, unitKey: 'GENERAL.UNITS.G', unitText: null },
            {
                name: 'translated:CONSUMPTION_DETAIL.SUMMARY.UNKNOWN_ITEM',
                amount: BASE_AMOUNT,
                unitKey: 'CONSUMPTION_DETAIL.SERVINGS',
                unitText: null,
            },
            { name: 'Local coffee', amount: AI_ITEM_AMOUNT, unitKey: 'GENERAL.UNITS.ML', unitText: null },
            {
                name: 'translated:CONSUMPTION_DETAIL.SUMMARY.UNKNOWN_ITEM',
                amount: UNKNOWN_UNIT_AI_ITEM_AMOUNT,
                unitKey: null,
                unitText: 'bowl',
            },
        ]);
    });

    it('builds fallback satiety metadata when levels are absent', () => {
        const viewModel = buildMealDetailViewModel(createMeal(), translate);

        expect(viewModel.preMealSatietyMeta.title).toBe('translated:HUNGER_SCALE.LEVEL_0.TITLE');
        expect(viewModel.preMealSatietyMeta.description).toBe('translated:HUNGER_SCALE.LEVEL_0.DESCRIPTION');
        expect(viewModel.postMealSatietyMeta.title).toBe('translated:HUNGER_SCALE.LEVEL_0.TITLE');
    });
});
