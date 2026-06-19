import { TestBed } from '@angular/core/testing';
import { FdUiDialogService } from 'fd-ui-kit/dialog/fd-ui-dialog.service';
import { FdUiToastService } from 'fd-ui-kit/toast/fd-ui-toast.service';
import { of } from 'rxjs';
import { beforeEach, describe, expect, it, vi } from 'vitest';

import { provideTranslateTesting } from '../../../../../testing/translate-testing.module';
import { AuthService } from '../../../../services/auth.service';
import { NavigationService } from '../../../../services/navigation.service';
import { MealService } from '../../api/meal.service';
import type { ConsumptionFormValues } from '../../components/manage/meal-manage-lib/meal-manage.types';
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
let toastService: { success: ReturnType<typeof vi.fn> };
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
        toastService = {
            success: vi.fn(),
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
                provideTranslateTesting(),
                MealManageFacade,
                { provide: MealService, useValue: mealService },
                { provide: AuthService, useValue: authService },
                { provide: NavigationService, useValue: navigationService },
                { provide: FdUiDialogService, useValue: dialogService },
                { provide: FdUiToastService, useValue: toastService },
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

        it('should toast and redirect to list after create', async () => {
            await facade.showSuccessToastAndRedirectAsync(false);

            expect(toastService.success).toHaveBeenCalledWith('CONSUMPTION_MANAGE.CREATE_SUCCESS');
            expect(navigationService.navigateToConsumptionListAsync).toHaveBeenCalled();
            expect(dialogService.open).not.toHaveBeenCalled();
        });

        it('should toast and redirect to list after update', async () => {
            await facade.showSuccessToastAndRedirectAsync(true);

            expect(toastService.success).toHaveBeenCalledWith('CONSUMPTION_MANAGE.EDIT_SUCCESS');
            expect(navigationService.navigateToConsumptionListAsync).toHaveBeenCalled();
            expect(dialogService.open).not.toHaveBeenCalled();
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
        it('should create product-based consumption item value', () => {
            const item = facade.createConsumptionItem(null, null, null, ConsumptionSourceType.Product);

            expect(item).toEqual({
                sourceType: ConsumptionSourceType.Product,
                product: null,
                recipe: null,
                amount: null,
            });
        });

        it('should resolve recipe servings to grams', async () => {
            const recipe = { ...createEmptyRecipeSnapshot(), id: 'r1' };

            const amount = await facade.resolveRecipeServingsToGramsAsync(recipe, RECIPE_SERVING_AMOUNT);

            expect(recipeWeightService.loadServingWeight).toHaveBeenCalled();
            expect(amount).toBe(EXPECTED_RECIPE_AMOUNT);
        });

        it('should use product default portion amount after manual selection', async () => {
            const product = {
                ...createEmptyProductSnapshot(),
                id: 'p1',
                defaultPortionAmount: PRODUCT_PORTION_AMOUNT,
                baseAmount: RECIPE_AMOUNT,
            };
            dialogService.open.mockReturnValue({ afterClosed: () => of({ type: 'Product', product }) });

            const item = await facade.openItemSelectionDialogAsync('Product');

            expect(item?.product).toBe(product);
            expect(item?.amount).toBe(PRODUCT_PORTION_AMOUNT);
        });

        it('should convert one recipe serving to grams after manual selection', async () => {
            const recipe = { ...createEmptyRecipeSnapshot(), id: 'r1' };
            dialogService.open.mockReturnValue({ afterClosed: () => of({ type: 'Recipe', recipe }) });
            recipeWeightService.loadServingWeight.mockReturnValue(of(MANUAL_RECIPE_WEIGHT));

            const item = await facade.openItemSelectionDialogAsync('Recipe');

            expect(item?.recipe).toBe(recipe);
            expect(item?.amount).toBe(MANUAL_RECIPE_WEIGHT);
        });
    });
}

function registerNutritionSummaryTests(): void {
    describe('nutrition summary', () => {
        it('should calculate nutrition summary from signal form values', () => {
            const state = facade.buildNutritionSummaryStateFromValues(
                createNutritionFormValue(true),
                AI_RECOGNITION_SESSIONS,
                CALORIE_MISMATCH_THRESHOLD,
            );

            expect(state.autoTotals).toEqual(EXPECTED_AUTO_TOTALS);
            expect(state.summaryTotals).toEqual(state.autoTotals);
            expect(state.warning).toBeNull();
        });

        it('should build calorie mismatch warning from signal form values in manual mode', () => {
            const state = facade.buildNutritionSummaryStateFromValues(createNutritionFormValue(false), [], CALORIE_MISMATCH_THRESHOLD);

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

function createNutritionFormValue(isAuto: boolean): ConsumptionFormValues {
    return {
        date: '2026-04-02',
        time: '12:00',
        mealType: null,
        items: [
            {
                sourceType: ConsumptionSourceType.Product,
                product: createNutritionProduct(),
                recipe: null,
                amount: PRODUCT_AMOUNT,
            },
            {
                sourceType: ConsumptionSourceType.Recipe,
                product: null,
                recipe: createNutritionRecipe(),
                amount: RECIPE_AMOUNT,
            },
        ],
        comment: null,
        imageUrl: null,
        isNutritionAutoCalculated: isAuto,
        manualCalories: isAuto ? null : MANUAL_CALORIES,
        manualProteins: isAuto ? null : MANUAL_PROTEINS,
        manualFats: isAuto ? null : MANUAL_FATS,
        manualCarbs: isAuto ? null : MANUAL_CARBS,
        manualFiber: 0,
        manualAlcohol: 0,
        preMealSatietyLevel: null,
        postMealSatietyLevel: null,
    };
}
