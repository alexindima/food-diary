import { TestBed } from '@angular/core/testing';
import { FormArray, FormControl, FormGroup } from '@angular/forms';
import { FdUiDialogService } from 'fd-ui-kit/dialog/fd-ui-dialog.service';
import { of } from 'rxjs';
import { beforeEach, describe, expect, it, vi } from 'vitest';

import { AuthService } from '../../../services/auth.service';
import { NavigationService } from '../../../services/navigation.service';
import { AiFoodService } from '../../../shared/api/ai-food.service';
import { ImageSelection } from '../../../shared/models/image-upload.data';
import { MealService } from '../api/meal.service';
import { ConsumptionFormData, ConsumptionItemFormData } from '../components/manage/base-meal-manage.types';
import { ConsumptionSourceType } from '../models/meal.data';
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

    const consumption = { id: 'c1' };
    const usage = {
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

        mealService.create.mockReturnValue(of(consumption as any));
        mealService.update.mockReturnValue(of(consumption as any));
        aiFoodService.getUsageSummary.mockReturnValue(of(usage as any));
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
        await expect(facade.loadAiUsage()).resolves.toEqual(usage as any);
    });

    it('should create consumption when original consumption is null', async () => {
        const result = await facade.submitConsumption(null, { items: [] } as any);

        expect(mealService.create).toHaveBeenCalled();
        expect(result).toEqual(consumption as any);
    });

    it('should update consumption when editing existing consumption', async () => {
        const result = await facade.submitConsumption(consumption as any, { items: [] } as any);

        expect(mealService.update).toHaveBeenCalledWith('c1', { items: [] });
        expect(result).toEqual(consumption as any);
    });

    it('should redirect after success dialog choice', async () => {
        await facade.showSuccessRedirect(false);

        expect(navigationService.navigateToConsumptionList).toHaveBeenCalled();
    });

    it('should append ai session', () => {
        const sessions = [{ id: 's1' }] as any;
        const next = facade.addAiSession(sessions, { id: 's2' } as any);

        expect(next).toEqual([{ id: 's1' }, { id: 's2' }] as any);
    });

    it('should remove ai session by index', () => {
        const sessions = [{ id: 's1' }, { id: 's2' }] as any;
        const next = facade.removeAiSession(sessions, 0);

        expect(next).toEqual([{ id: 's2' }] as any);
    });

    it('should replace ai session by index', () => {
        const sessions = [{ id: 's1' }, { id: 's2' }] as any;
        const next = facade.replaceAiSession(sessions, 1, { id: 's3' } as any);

        expect(next).toEqual([{ id: 's1' }, { id: 's3' }] as any);
    });

    it('should create product-based consumption item form group', () => {
        const group = facade.createConsumptionItem(null, null, null, ConsumptionSourceType.Product);

        expect(group.controls.sourceType.value).toBe(ConsumptionSourceType.Product);
        expect(group.controls.amount.disabled).toBe(true);
    });

    it('should enable amount and set recipe weight for recipe item', () => {
        const group = facade.createConsumptionItem(null, { id: 'r1' } as any, 2, ConsumptionSourceType.Recipe);

        facade.ensureRecipeWeightForExistingItem(group, 2, { id: 'r1' } as any);

        expect(recipeWeightService.loadServingWeight).toHaveBeenCalled();
        expect(group.controls.amount.value).toBe(100);
    });

    it('should validate items array as non-empty when ai sessions are absent', () => {
        const validator = facade.createItemsValidator(() => []);

        expect(validator(new FormArray([]))).toEqual({ nonEmptyArray: true });
    });

    it('should calculate nutrition summary from manual items and ai sessions', () => {
        recipeWeightService.loadServingWeight.mockReturnValue(of(50));

        const items = new FormArray<FormGroup<ConsumptionItemFormData>>([
            facade.createConsumptionItem(
                {
                    id: 'p1',
                    baseAmount: 100,
                    caloriesPerBase: 250,
                    proteinsPerBase: 10,
                    fatsPerBase: 5,
                    carbsPerBase: 20,
                    fiberPerBase: 3,
                    alcoholPerBase: 1,
                } as any,
                null,
                200,
                ConsumptionSourceType.Product,
            ),
            facade.createConsumptionItem(
                null,
                {
                    id: 'r1',
                    servings: 2,
                    totalCalories: 600,
                    totalProteins: 30,
                    totalFats: 20,
                    totalCarbs: 50,
                    totalFiber: 8,
                    totalAlcohol: 0,
                } as any,
                100,
                ConsumptionSourceType.Recipe,
            ),
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
                        { calories: 40, proteins: 2, fats: 1, carbs: 5, fiber: 0, alcohol: 0 },
                        { calories: 20, proteins: 1, fats: 0, carbs: 2, fiber: 1, alcohol: 0 },
                    ],
                } as any,
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
