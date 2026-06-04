import { describe, expect, it, vi } from 'vitest';

import type { RecipeNutritionSummary } from '../../../lib/recipe-manage.facade';
import { type Recipe, RecipeVisibility } from '../../../models/recipe.data';
import type { RecipeFormValues } from './recipe-manage.types';
import { createRecipeFormValue } from './recipe-manage-form.mapper';
import { RecipeNutritionFormManager, type RecipeNutritionFormOperations } from './recipe-nutrition-form.manager';

const SUMMARY: RecipeNutritionSummary = {
    calories: 300,
    proteins: 20,
    fats: 10,
    carbs: 30,
    fiber: 5,
    alcohol: 1,
};
const SERVINGS_COUNT = 2;
const MANUAL_CALORIES = 100;
const PORTION_CALORIES = 50;
const MANUAL_PROTEINS = 10;
const PORTION_PROTEINS = 5;
const MANUAL_FATS = 4;
const MANUAL_CARBS = 20;
const MANUAL_FIBER = 2;
const MANUAL_ALCOHOL = 0;
const UPDATED_CALORIES = 450;
const ROUNDING_FACTOR = 10;

describe('RecipeNutritionFormManager initialization', () => {
    it('initializes auto mode from form and syncs manual controls with auto summary', () => {
        const form = createRecipeFormState();
        const operations = createOperations({ calculateAutoSummary: vi.fn().mockReturnValue(SUMMARY) });
        const manager = new RecipeNutritionFormManager(form, operations);

        manager.initialize();

        expect(manager.nutritionMode()).toBe('auto');
        expect(manager.totalCalories()).toBe(SUMMARY.calories);
        expect(manager.nutrientChartData()).toEqual({
            proteins: SUMMARY.proteins,
            fats: SUMMARY.fats,
            carbs: SUMMARY.carbs,
        });
        expect(form.controls.manualCalories.value).toBe(SUMMARY.calories);
        expect(form.controls.manualProteins.value).toBe(SUMMARY.proteins);
    });

    it('copies current summary into manual controls when auto calculation is disabled', () => {
        const form = createRecipeFormState();
        const manager = new RecipeNutritionFormManager(form, createOperations());
        manager.recalculateNutrientsFromForm();

        manager.handleAutoCalculationChange(false);

        expect(manager.nutritionMode()).toBe('manual');
        expect(form.controls.manualCalories.value).toBe(SUMMARY.calories);
        expect(form.controls.manualFiber.value).toBe(SUMMARY.fiber);
    });
});

describe('RecipeNutritionFormManager manual mode', () => {
    it('converts manual nutrition between recipe and portion scale', () => {
        const form = createRecipeFormState();
        form.patchValue({
            calculateNutritionAutomatically: false,
            servings: SERVINGS_COUNT,
            manualCalories: MANUAL_CALORIES,
            manualProteins: MANUAL_PROTEINS,
            manualFats: MANUAL_FATS,
            manualCarbs: MANUAL_CARBS,
            manualFiber: MANUAL_FIBER,
            manualAlcohol: MANUAL_ALCOHOL,
        });
        const operations = createOperations();
        const manager = new RecipeNutritionFormManager(form, operations);

        manager.onNutritionScaleModeChange('portion');

        expect(manager.nutritionScaleMode).toBe('portion');
        expect(form.controls.manualCalories.value).toBe(PORTION_CALORIES);
        expect(form.controls.manualProteins.value).toBe(PORTION_PROTEINS);
        expect(manager.totalCalories()).toBe(MANUAL_CALORIES);

        manager.onNutritionScaleModeChange('recipe');

        expect(manager.nutritionScaleMode).toBe('recipe');
        expect(form.controls.manualCalories.value).toBe(MANUAL_CALORIES);
        expect(operations.roundNutritionValue).toHaveBeenCalled();
    });

    it('detects empty manual macro values as a macro error', () => {
        const form = createRecipeFormState();
        form.patchValue({
            calculateNutritionAutomatically: false,
            manualCalories: MANUAL_CALORIES,
            manualProteins: 0,
            manualFats: 0,
            manualCarbs: 0,
            manualAlcohol: 0,
        });
        form.controls.manualProteins.markAsDirty();
        const manager = new RecipeNutritionFormManager(form, createOperations());

        expect(manager.hasMacrosError()).toBe(true);

        form.controls.manualProteins.setValue(1);

        expect(manager.hasMacrosError()).toBe(false);
    });

    it('normalizes invalid servings to one', () => {
        const form = createRecipeFormState();
        form.controls.servings.setValue(0);
        const manager = new RecipeNutritionFormManager(form, createOperations());

        expect(manager.getServingsValue()).toBe(1);
    });
});

describe('RecipeNutritionFormManager summary updates', () => {
    it('uses recipe summary with current state as fallback', () => {
        const form = createRecipeFormState();
        const getSummaryFromRecipe = vi.fn((_recipe: Recipe | null, fallback: RecipeNutritionSummary) => ({
            ...fallback,
            calories: UPDATED_CALORIES,
        }));
        const manager = new RecipeNutritionFormManager(form, createOperations({ getSummaryFromRecipe }));

        manager.recalculateNutrientsFromForm();
        manager.updateNutrientSummary(createRecipe());

        expect(getSummaryFromRecipe).toHaveBeenCalledWith(
            expect.anything(),
            expect.objectContaining({
                calories: SUMMARY.calories,
                proteins: SUMMARY.proteins,
            }),
        );
        expect(manager.totalCalories()).toBe(UPDATED_CALORIES);
        expect(manager.nutrientChartData().proteins).toBe(SUMMARY.proteins);
    });
});

function createOperations(overrides: Partial<RecipeNutritionFormOperations> = {}): RecipeNutritionFormOperations {
    return {
        calculateAutoSummary: vi.fn().mockReturnValue(SUMMARY),
        fromRecipeTotal: vi.fn((value: number | null | undefined, scaleMode, servings) => {
            const normalized = Number(value ?? 0);
            return scaleMode === 'portion' ? normalized / servings : normalized;
        }),
        getSummaryFromRecipe: vi.fn((_recipe: Recipe | null, fallback: RecipeNutritionSummary) => fallback),
        roundNutritionValue: vi.fn((value: number) => Math.round(value * ROUNDING_FACTOR) / ROUNDING_FACTOR),
        toRecipeTotal: vi.fn((value: number | null | undefined, scaleMode, servings) => {
            const normalized = Number(value ?? 0);
            return scaleMode === 'portion' ? normalized * servings : normalized;
        }),
        ...overrides,
    };
}

function createRecipeFormState(): TestRecipeNutritionFormState {
    let value = createRecipeFormValue();
    const createControl = <Field extends keyof RecipeFormValues>(field: Field): TestRecipeNutritionControl<RecipeFormValues[Field]> => {
        let dirty = false;
        return {
            get dirty(): boolean {
                return dirty;
            },
            get touched(): boolean {
                return dirty;
            },
            get value(): RecipeFormValues[Field] {
                return value[field];
            },
            markAsDirty(): void {
                dirty = true;
            },
            setValue(nextValue: RecipeFormValues[Field]): void {
                value = {
                    ...value,
                    [field]: nextValue,
                };
                dirty = true;
            },
        };
    };

    return {
        controls: {
            calculateNutritionAutomatically: createControl('calculateNutritionAutomatically'),
            manualAlcohol: createControl('manualAlcohol'),
            manualCalories: createControl('manualCalories'),
            manualCarbs: createControl('manualCarbs'),
            manualFats: createControl('manualFats'),
            manualFiber: createControl('manualFiber'),
            manualProteins: createControl('manualProteins'),
            servings: createControl('servings'),
            get steps(): RecipeFormValues['steps'] {
                return value.steps;
            },
        },
        patchValue(patch: Partial<RecipeFormValues>): void {
            value = {
                ...value,
                ...patch,
            };
        },
    };
}

type TestRecipeNutritionControl<T> = {
    readonly dirty: boolean;
    readonly touched: boolean;
    readonly value: T;
    markAsDirty: () => void;
    setValue: (value: T) => void;
};

type TestRecipeNutritionFormState = {
    controls: {
        calculateNutritionAutomatically: TestRecipeNutritionControl<boolean>;
        manualAlcohol: TestRecipeNutritionControl<number | null>;
        manualCalories: TestRecipeNutritionControl<number | null>;
        manualCarbs: TestRecipeNutritionControl<number | null>;
        manualFats: TestRecipeNutritionControl<number | null>;
        manualFiber: TestRecipeNutritionControl<number | null>;
        manualProteins: TestRecipeNutritionControl<number | null>;
        servings: TestRecipeNutritionControl<number>;
        readonly steps: RecipeFormValues['steps'];
    };
    patchValue: (value: Partial<RecipeFormValues>) => void;
};

function createRecipe(): Recipe {
    return {
        id: 'recipe-1',
        name: 'Recipe',
        servings: 1,
        visibility: RecipeVisibility.Public,
        usageCount: 0,
        createdAt: '2026-01-01T00:00:00Z',
        isOwnedByCurrentUser: true,
        isNutritionAutoCalculated: true,
        steps: [],
    };
}
