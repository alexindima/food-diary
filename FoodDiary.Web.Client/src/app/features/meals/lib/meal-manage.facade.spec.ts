import { TestBed } from '@angular/core/testing';
import { FormArray, FormControl, FormGroup } from '@angular/forms';
import { FdUiDialogService } from 'fd-ui-kit/dialog/fd-ui-dialog.service';
import { of } from 'rxjs';
import { beforeEach, describe, expect, it, vi } from 'vitest';

import { AuthService } from '../../../services/auth.service';
import { NavigationService } from '../../../services/navigation.service';
import { AiFoodService } from '../../../shared/api/ai-food.service';
import { type UserAiUsageResponse } from '../../../shared/models/ai.data';
import { type ImageSelection } from '../../../shared/models/image-upload.data';
import { MealService } from '../api/meal.service';
import { type ConsumptionFormData, type ConsumptionItemFormData } from '../components/manage/base-meal-manage.types';
import {
    type Consumption,
    type ConsumptionAiSessionManageDto,
    type ConsumptionManageDto,
    ConsumptionSourceType,
    createEmptyProductSnapshot,
    createEmptyRecipeSnapshot,
} from '../models/meal.data';
import { MealManageFacade } from './meal-manage.facade';
import { RecipeServingWeightService } from './recipe-serving-weight.service';

describe('MealManageFacade', () => {
    let facade: MealManageFacade;
    let mealService: { create: ReturnType<typeof vi.fn>; update: ReturnType<typeof vi.fn> };
    let aiFoodService: { getUsageSummary: ReturnType<typeof vi.fn> };
    let authService: { isPremium: ReturnType<typeof vi.fn> };
    let navigationService: {
        navigateToHome: ReturnType<typeof vi.fn>;
        navigateToConsumptionList: ReturnType<typeof vi.fn>;
        navigateToPremiumAccess: ReturnType<typeof vi.fn>;
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
    const usage: UserAiUsageResponse = {
        inputLimit: 1000,
        outputLimit: 1000,
        inputUsed: 100,
        outputUsed: 200,
        resetAtUtc: '2026-04-03T00:00:00Z',
    };

    beforeEach(() => {
        mealService = {
            create: vi.fn(),
            update: vi.fn(),
        };
        aiFoodService = {
            getUsageSummary: vi.fn(),
        };
        authService = {
            isPremium: vi.fn(),
        };
        navigationService = {
            navigateToHome: vi.fn(),
            navigateToConsumptionList: vi.fn(),
            navigateToPremiumAccess: vi.fn(),
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
        aiFoodService.getUsageSummary.mockReturnValue(of(usage));
        authService.isPremium.mockReturnValue(true);
        dialogService.open.mockReturnValue({ afterClosed: () => of('ConsumptionList') });
        recipeWeightService.loadServingWeight.mockReturnValue(of(50));
        recipeWeightService.convertGramsToServings.mockImplementation((_recipe: unknown, amount: number) => amount / 50);
        navigationService.navigateToHome.mockResolvedValue(true);
        navigationService.navigateToConsumptionList.mockResolvedValue(true);
        navigationService.navigateToPremiumAccess.mockResolvedValue(true);

        TestBed.configureTestingModule({
            providers: [
                MealManageFacade,
                { provide: MealService, useValue: mealService },
                { provide: AiFoodService, useValue: aiFoodService },
                { provide: AuthService, useValue: authService },
                { provide: NavigationService, useValue: navigationService },
                { provide: FdUiDialogService, useValue: dialogService },
                { provide: RecipeServingWeightService, useValue: recipeWeightService },
            ],
        });

        facade = TestBed.inject(MealManageFacade);
    });

    it('should load ai usage summary', async () => {
        await expect(facade.loadAiUsage()).resolves.toEqual(usage);
    });

    it('should create consumption when original consumption is null', async () => {
        const result = await facade.submitConsumption(null, consumptionData);

        expect(mealService.create).toHaveBeenCalled();
        expect(result).toEqual(consumption);
    });

    it('should update consumption when editing existing consumption', async () => {
        const result = await facade.submitConsumption(consumption, consumptionData);

        expect(mealService.update).toHaveBeenCalledWith('c1', consumptionData);
        expect(result).toEqual(consumption);
    });

    it('should redirect after success dialog choice', async () => {
        await facade.showSuccessRedirect(false);

        expect(navigationService.navigateToConsumptionList).toHaveBeenCalled();
    });

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

    it('should create product-based consumption item form group', () => {
        const group = facade.createConsumptionItem(null, null, null, ConsumptionSourceType.Product);

        expect(group.controls.sourceType.value).toBe(ConsumptionSourceType.Product);
        expect(group.controls.amount.disabled).toBe(true);
    });

    it('should enable amount and set recipe weight for recipe item', () => {
        const recipe = { ...createEmptyRecipeSnapshot(), id: 'r1' };
        const group = facade.createConsumptionItem(null, recipe, 2, ConsumptionSourceType.Recipe);

        facade.ensureRecipeWeightForExistingItem(group, 2, recipe);

        expect(recipeWeightService.loadServingWeight).toHaveBeenCalled();
        expect(group.controls.amount.value).toBe(100);
    });

    it('should use product default portion amount after manual selection', async () => {
        const product = {
            ...createEmptyProductSnapshot(),
            id: 'p1',
            defaultPortionAmount: 180,
            baseAmount: 100,
        };
        const group = facade.createConsumptionItem();
        dialogService.open.mockReturnValue({ afterClosed: () => of({ type: 'Product', product }) });

        await facade.openItemSelectionDialog(group, 'Product');

        expect(group.controls.product.value).toBe(product);
        expect(group.controls.amount.value).toBe(180);
    });

    it('should convert one recipe serving to grams after manual selection', async () => {
        const recipe = { ...createEmptyRecipeSnapshot(), id: 'r1' };
        const group = facade.createConsumptionItem();
        dialogService.open.mockReturnValue({ afterClosed: () => of({ type: 'Recipe', recipe }) });
        recipeWeightService.loadServingWeight.mockReturnValue(of(75));

        await facade.openItemSelectionDialog(group, 'Recipe');

        expect(group.controls.recipe.value).toBe(recipe);
        expect(group.controls.amount.value).toBe(75);
    });

    it('should validate items array as non-empty when ai sessions are absent', () => {
        const validator = facade.createItemsValidator(() => []);

        expect(validator(new FormArray([]))).toEqual({ nonEmptyArray: true });
    });

    it('should calculate nutrition summary from manual items and ai sessions', () => {
        recipeWeightService.loadServingWeight.mockReturnValue(of(50));
        const product = {
            ...createEmptyProductSnapshot(),
            id: 'p1',
            baseAmount: 100,
            caloriesPerBase: 250,
            proteinsPerBase: 10,
            fatsPerBase: 5,
            carbsPerBase: 20,
            fiberPerBase: 3,
            alcoholPerBase: 1,
        };
        const recipe = {
            ...createEmptyRecipeSnapshot(),
            id: 'r1',
            servings: 2,
            totalCalories: 600,
            totalProteins: 30,
            totalFats: 20,
            totalCarbs: 50,
            totalFiber: 8,
            totalAlcohol: 0,
        };

        const items = new FormArray<FormGroup<ConsumptionItemFormData>>([
            facade.createConsumptionItem(product, null, 200, ConsumptionSourceType.Product),
            facade.createConsumptionItem(null, recipe, 100, ConsumptionSourceType.Recipe),
        ]);

        const form = new FormGroup<ConsumptionFormData>({
            date: new FormControl('2026-04-02', { nonNullable: true }),
            time: new FormControl('12:00', { nonNullable: true }),
            mealType: new FormControl<string | null>(null),
            items,
            comment: new FormControl<string | null>(null),
            imageUrl: new FormControl<ImageSelection | null>(null),
            isNutritionAutoCalculated: new FormControl(true, { nonNullable: true }),
            manualCalories: new FormControl<number | null>(null),
            manualProteins: new FormControl<number | null>(null),
            manualFats: new FormControl<number | null>(null),
            manualCarbs: new FormControl<number | null>(null),
            manualFiber: new FormControl<number | null>(null),
            manualAlcohol: new FormControl<number | null>(null),
            preMealSatietyLevel: new FormControl<number | null>(null),
            postMealSatietyLevel: new FormControl<number | null>(null),
        });

        const state = facade.buildNutritionSummaryState(
            form,
            items,
            [
                {
                    items: [
                        {
                            nameEn: 'Apple',
                            amount: 100,
                            unit: 'g',
                            calories: 40,
                            proteins: 2,
                            fats: 1,
                            carbs: 5,
                            fiber: 0,
                            alcohol: 0,
                        },
                        {
                            nameEn: 'Berry',
                            amount: 50,
                            unit: 'g',
                            calories: 20,
                            proteins: 1,
                            fats: 0,
                            carbs: 2,
                            fiber: 1,
                            alcohol: 0,
                        },
                    ],
                },
            ],
            0.2,
        );

        expect(state.autoTotals).toEqual({
            calories: 1160,
            proteins: 53,
            fats: 31,
            carbs: 97,
            fiber: 15,
            alcohol: 2,
        });
        expect(state.summaryTotals).toEqual(state.autoTotals);
        expect(state.warning).toBeNull();
    });

    it('should build calorie mismatch warning in manual mode', () => {
        const items = new FormArray<FormGroup<ConsumptionItemFormData>>([facade.createConsumptionItem()]);
        const form = new FormGroup<ConsumptionFormData>({
            date: new FormControl('2026-04-02', { nonNullable: true }),
            time: new FormControl('12:00', { nonNullable: true }),
            mealType: new FormControl<string | null>(null),
            items,
            comment: new FormControl<string | null>(null),
            imageUrl: new FormControl<ImageSelection | null>(null),
            isNutritionAutoCalculated: new FormControl(false, { nonNullable: true }),
            manualCalories: new FormControl<number | null>(100),
            manualProteins: new FormControl<number | null>(20),
            manualFats: new FormControl<number | null>(10),
            manualCarbs: new FormControl<number | null>(15),
            manualFiber: new FormControl<number | null>(0),
            manualAlcohol: new FormControl<number | null>(0),
            preMealSatietyLevel: new FormControl<number | null>(null),
            postMealSatietyLevel: new FormControl<number | null>(null),
        });

        const state = facade.buildNutritionSummaryState(form, items, [], 0.2);

        expect(state.summaryTotals.calories).toBe(100);
        expect(state.warning).toEqual({
            expectedCalories: 230,
            actualCalories: 100,
        });
    });
});
