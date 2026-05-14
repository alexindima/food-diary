import { TestBed } from '@angular/core/testing';
import { FormArray, FormControl, FormGroup } from '@angular/forms';
import { FdUiDialogService } from 'fd-ui-kit/dialog/fd-ui-dialog.service';
import { of } from 'rxjs';
import { beforeEach, describe, expect, it, vi } from 'vitest';

import { AuthService } from '../../../../services/auth.service';
import { NavigationService } from '../../../../services/navigation.service';
import type { ImageSelection } from '../../../../shared/models/image-upload.data';
import { MealService } from '../../api/meal.service';
import type { ConsumptionFormData, ConsumptionItemFormData } from '../../components/manage/meal-manage-lib/meal-manage.types';
import {
    type Consumption,
    type ConsumptionAiSessionManageDto,
    type ConsumptionManageDto,
    ConsumptionSourceType,
    createEmptyProductSnapshot,
    createEmptyRecipeSnapshot,
} from '../../models/meal.data';
import { RecipeServingWeightService } from '../recipe-serving/recipe-serving-weight.service';
import { MealManageFacade } from './meal-manage.facade';

const RECIPE_SERVING_WEIGHT = 50;
const RECIPE_SERVING_AMOUNT = 2;
const EXPECTED_RECIPE_AMOUNT = 100;
const PRODUCT_PORTION_AMOUNT = 180;
const MANUAL_RECIPE_WEIGHT = 75;
const PRODUCT_AMOUNT = 200;
const RECIPE_AMOUNT = 100;
const PRODUCT_CALORIES_PER_BASE = 250;
const PRODUCT_PROTEINS_PER_BASE = 10;
const PRODUCT_FATS_PER_BASE = 5;
const PRODUCT_CARBS_PER_BASE = 20;
const PRODUCT_FIBER_PER_BASE = 3;
const PRODUCT_ALCOHOL_PER_BASE = 1;
const RECIPE_SERVINGS = 2;
const RECIPE_TOTAL_CALORIES = 600;
const RECIPE_TOTAL_PROTEINS = 30;
const RECIPE_TOTAL_FATS = 20;
const RECIPE_TOTAL_CARBS = 50;
const RECIPE_TOTAL_FIBER = 8;
const AI_APPLE_AMOUNT = 100;
const AI_APPLE_CALORIES = 40;
const AI_APPLE_PROTEINS = 2;
const AI_APPLE_FATS = 1;
const AI_APPLE_CARBS = 5;
const AI_BERRY_AMOUNT = 50;
const AI_BERRY_CALORIES = 20;
const AI_BERRY_PROTEINS = 1;
const AI_BERRY_CARBS = 2;
const AI_BERRY_FIBER = 1;
const CALORIE_MISMATCH_THRESHOLD = 0.2;
const EXPECTED_AUTO_TOTALS = {
    calories: 1160,
    proteins: 53,
    fats: 31,
    carbs: 97,
    fiber: 15,
    alcohol: 2,
} as const;
const AI_RECOGNITION_SESSIONS: ConsumptionAiSessionManageDto[] = [
    {
        items: [
            {
                nameEn: 'Apple',
                amount: AI_APPLE_AMOUNT,
                unit: 'g',
                calories: AI_APPLE_CALORIES,
                proteins: AI_APPLE_PROTEINS,
                fats: AI_APPLE_FATS,
                carbs: AI_APPLE_CARBS,
                fiber: 0,
                alcohol: 0,
            },
            {
                nameEn: 'Berry',
                amount: AI_BERRY_AMOUNT,
                unit: 'g',
                calories: AI_BERRY_CALORIES,
                proteins: AI_BERRY_PROTEINS,
                fats: 0,
                carbs: AI_BERRY_CARBS,
                fiber: AI_BERRY_FIBER,
                alcohol: 0,
            },
        ],
    },
];
const MANUAL_CALORIES = 100;
const MANUAL_PROTEINS = 20;
const MANUAL_FATS = 10;
const MANUAL_CARBS = 15;
const EXPECTED_MISMATCH_CALORIES = 230;

let facade: MealManageFacade;
let mealService: { create: ReturnType<typeof vi.fn>; update: ReturnType<typeof vi.fn> };
let authService: { isPremium: ReturnType<typeof vi.fn> };
let navigationService: {
    navigateToHomeAsync: ReturnType<typeof vi.fn>;
    navigateToConsumptionListAsync: ReturnType<typeof vi.fn>;
    navigateToPremiumAccessAsync: ReturnType<typeof vi.fn>;
};
let dialogService: { open: ReturnType<typeof vi.fn> };
let recipeWeightService: { loadServingWeight: ReturnType<typeof vi.fn>; convertGramsToServings: ReturnType<typeof vi.fn> };

const consumption: Consumption = {
    id: 'c1',
    date: '2026-04-02T12:00:00Z',
    totalCalories: 0,
    totalProteins: 0,
    totalFats: 0,
    totalCarbs: 0,
    totalFiber: 0,
    totalAlcohol: 0,
    isNutritionAutoCalculated: true,
    items: [],
};
const consumptionData: ConsumptionManageDto = {
    date: new Date('2026-04-02T12:00:00Z'),
    items: [],
    isNutritionAutoCalculated: true,
};
describe('MealManageFacade', () => {
    beforeEach(() => {
        mealService = {
            create: vi.fn(),
            update: vi.fn(),
        };
        authService = {
            isPremium: vi.fn(),
        };
        navigationService = {
            navigateToHomeAsync: vi.fn(),
            navigateToConsumptionListAsync: vi.fn(),
            navigateToPremiumAccessAsync: vi.fn(),
        };
        dialogService = {
            open: vi.fn(),
        };
        recipeWeightService = {
            loadServingWeight: vi.fn(),
            convertGramsToServings: vi.fn(),
        };

        mealService.create.mockReturnValue(of(consumption));
        mealService.update.mockReturnValue(of(consumption));
        authService.isPremium.mockReturnValue(true);
        dialogService.open.mockReturnValue({ afterClosed: () => of('ConsumptionList') });
        recipeWeightService.loadServingWeight.mockReturnValue(of(RECIPE_SERVING_WEIGHT));
        recipeWeightService.convertGramsToServings.mockImplementation((_recipe: unknown, amount: number) => amount / RECIPE_SERVING_WEIGHT);
        navigationService.navigateToHomeAsync.mockResolvedValue(true);
        navigationService.navigateToConsumptionListAsync.mockResolvedValue(true);
        navigationService.navigateToPremiumAccessAsync.mockResolvedValue(true);

        TestBed.configureTestingModule({
            providers: [
                MealManageFacade,
                { provide: MealService, useValue: mealService },
                { provide: AuthService, useValue: authService },
                { provide: NavigationService, useValue: navigationService },
                { provide: FdUiDialogService, useValue: dialogService },
                { provide: RecipeServingWeightService, useValue: recipeWeightService },
            ],
        });

        facade = TestBed.inject(MealManageFacade);
    });

    registerSubmitAndNavigationTests();
    registerAiSessionTests();
    registerItemSelectionTests();
    registerNutritionSummaryTests();
});

function registerSubmitAndNavigationTests(): void {
    describe('submit and navigation', () => {
        it('should create consumption when original consumption is null', async () => {
            const result = await facade.submitConsumptionAsync(null, consumptionData);

            expect(mealService.create).toHaveBeenCalled();
            expect(result).toEqual(consumption);
        });

        it('should update consumption when editing existing consumption', async () => {
            const result = await facade.submitConsumptionAsync(consumption, consumptionData);

            expect(mealService.update).toHaveBeenCalledWith('c1', consumptionData);
            expect(result).toEqual(consumption);
        });

        it('should redirect after success dialog choice', async () => {
            await facade.showSuccessRedirectAsync(false);

            expect(navigationService.navigateToConsumptionListAsync).toHaveBeenCalled();
        });
    });
}

function registerAiSessionTests(): void {
    describe('ai sessions', () => {
        it('should append ai session', () => {
            const sessions: ConsumptionAiSessionManageDto[] = [{ notes: 's1', items: [] }];
            const next = facade.addAiSession(sessions, { notes: 's2', items: [] });

            expect(next).toEqual([
                { notes: 's1', items: [] },
                { notes: 's2', items: [] },
            ]);
        });

        it('should remove ai session by index', () => {
            const sessions: ConsumptionAiSessionManageDto[] = [
                { notes: 's1', items: [] },
                { notes: 's2', items: [] },
            ];
            const next = facade.removeAiSession(sessions, 0);

            expect(next).toEqual([{ notes: 's2', items: [] }]);
        });

        it('should replace ai session by index', () => {
            const sessions: ConsumptionAiSessionManageDto[] = [
                { notes: 's1', items: [] },
                { notes: 's2', items: [] },
            ];
            const next = facade.replaceAiSession(sessions, 1, { notes: 's3', items: [] });

            expect(next).toEqual([
                { notes: 's1', items: [] },
                { notes: 's3', items: [] },
            ]);
        });
    });
}

function registerItemSelectionTests(): void {
    describe('item selection', () => {
        it('should create product-based consumption item form group', () => {
            const group = facade.createConsumptionItem(null, null, null, ConsumptionSourceType.Product);

            expect(group.controls.sourceType.value).toBe(ConsumptionSourceType.Product);
            expect(group.controls.amount.disabled).toBe(true);
        });

        it('should enable amount and set recipe weight for recipe item', () => {
            const recipe = { ...createEmptyRecipeSnapshot(), id: 'r1' };
            const group = facade.createConsumptionItem(null, recipe, RECIPE_SERVING_AMOUNT, ConsumptionSourceType.Recipe);

            facade.ensureRecipeWeightForExistingItem(group, RECIPE_SERVING_AMOUNT, recipe);

            expect(recipeWeightService.loadServingWeight).toHaveBeenCalled();
            expect(group.controls.amount.value).toBe(EXPECTED_RECIPE_AMOUNT);
        });

        it('should use product default portion amount after manual selection', async () => {
            const product = {
                ...createEmptyProductSnapshot(),
                id: 'p1',
                defaultPortionAmount: PRODUCT_PORTION_AMOUNT,
                baseAmount: RECIPE_AMOUNT,
            };
            const group = facade.createConsumptionItem();
            dialogService.open.mockReturnValue({ afterClosed: () => of({ type: 'Product', product }) });

            await facade.openItemSelectionDialogAsync(group, 'Product');

            expect(group.controls.product.value).toBe(product);
            expect(group.controls.amount.value).toBe(PRODUCT_PORTION_AMOUNT);
        });

        it('should convert one recipe serving to grams after manual selection', async () => {
            const recipe = { ...createEmptyRecipeSnapshot(), id: 'r1' };
            const group = facade.createConsumptionItem();
            dialogService.open.mockReturnValue({ afterClosed: () => of({ type: 'Recipe', recipe }) });
            recipeWeightService.loadServingWeight.mockReturnValue(of(MANUAL_RECIPE_WEIGHT));

            await facade.openItemSelectionDialogAsync(group, 'Recipe');

            expect(group.controls.recipe.value).toBe(recipe);
            expect(group.controls.amount.value).toBe(MANUAL_RECIPE_WEIGHT);
        });

        it('should validate items array as non-empty when ai sessions are absent', () => {
            const validator = facade.createItemsValidator(() => []);

            expect(validator(new FormArray([]))).toEqual({ nonEmptyArray: true });
        });
    });
}

function registerNutritionSummaryTests(): void {
    describe('nutrition summary', () => {
        it('should calculate nutrition summary from manual items and ai sessions', () => {
            recipeWeightService.loadServingWeight.mockReturnValue(of(RECIPE_SERVING_WEIGHT));
            const items = new FormArray<FormGroup<ConsumptionItemFormData>>([
                facade.createConsumptionItem(createNutritionProduct(), null, PRODUCT_AMOUNT, ConsumptionSourceType.Product),
                facade.createConsumptionItem(null, createNutritionRecipe(), RECIPE_AMOUNT, ConsumptionSourceType.Recipe),
            ]);

            const state = facade.buildNutritionSummaryState(
                createNutritionForm(items, true),
                items,
                AI_RECOGNITION_SESSIONS,
                CALORIE_MISMATCH_THRESHOLD,
            );

            expect(state.autoTotals).toEqual(EXPECTED_AUTO_TOTALS);
            expect(state.summaryTotals).toEqual(state.autoTotals);
            expect(state.warning).toBeNull();
        });

        it('should build calorie mismatch warning in manual mode', () => {
            const items = new FormArray<FormGroup<ConsumptionItemFormData>>([facade.createConsumptionItem()]);
            const form = createNutritionForm(items, false);

            const state = facade.buildNutritionSummaryState(form, items, [], CALORIE_MISMATCH_THRESHOLD);

            expect(state.summaryTotals.calories).toBe(MANUAL_CALORIES);
            expect(state.warning).toEqual({
                expectedCalories: EXPECTED_MISMATCH_CALORIES,
                actualCalories: MANUAL_CALORIES,
            });
        });
    });
}

function createNutritionProduct(): ReturnType<typeof createEmptyProductSnapshot> {
    return {
        ...createEmptyProductSnapshot(),
        id: 'p1',
        baseAmount: RECIPE_AMOUNT,
        caloriesPerBase: PRODUCT_CALORIES_PER_BASE,
        proteinsPerBase: PRODUCT_PROTEINS_PER_BASE,
        fatsPerBase: PRODUCT_FATS_PER_BASE,
        carbsPerBase: PRODUCT_CARBS_PER_BASE,
        fiberPerBase: PRODUCT_FIBER_PER_BASE,
        alcoholPerBase: PRODUCT_ALCOHOL_PER_BASE,
    };
}

function createNutritionRecipe(): ReturnType<typeof createEmptyRecipeSnapshot> {
    return {
        ...createEmptyRecipeSnapshot(),
        id: 'r1',
        servings: RECIPE_SERVINGS,
        totalCalories: RECIPE_TOTAL_CALORIES,
        totalProteins: RECIPE_TOTAL_PROTEINS,
        totalFats: RECIPE_TOTAL_FATS,
        totalCarbs: RECIPE_TOTAL_CARBS,
        totalFiber: RECIPE_TOTAL_FIBER,
        totalAlcohol: 0,
    };
}

function createNutritionForm(items: FormArray<FormGroup<ConsumptionItemFormData>>, isAuto: boolean): FormGroup<ConsumptionFormData> {
    return new FormGroup<ConsumptionFormData>({
        date: new FormControl('2026-04-02', { nonNullable: true }),
        time: new FormControl('12:00', { nonNullable: true }),
        mealType: new FormControl<string | null>(null),
        items,
        comment: new FormControl<string | null>(null),
        imageUrl: new FormControl<ImageSelection | null>(null),
        isNutritionAutoCalculated: new FormControl(isAuto, { nonNullable: true }),
        manualCalories: new FormControl<number | null>(isAuto ? null : MANUAL_CALORIES),
        manualProteins: new FormControl<number | null>(isAuto ? null : MANUAL_PROTEINS),
        manualFats: new FormControl<number | null>(isAuto ? null : MANUAL_FATS),
        manualCarbs: new FormControl<number | null>(isAuto ? null : MANUAL_CARBS),
        manualFiber: new FormControl<number | null>(0),
        manualAlcohol: new FormControl<number | null>(0),
        preMealSatietyLevel: new FormControl<number | null>(null),
        postMealSatietyLevel: new FormControl<number | null>(null),
    });
}
