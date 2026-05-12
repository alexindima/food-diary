import { TestBed } from '@angular/core/testing';
import { FormArray, FormControl, FormGroup } from '@angular/forms';
import { TranslateService } from '@ngx-translate/core';
import { FdUiDialogService } from 'fd-ui-kit/dialog/fd-ui-dialog.service';
import { firstValueFrom, of, throwError } from 'rxjs';
import { beforeEach, describe, expect, it, vi } from 'vitest';

import { NavigationService } from '../../../services/navigation.service';
import { DEFAULT_NUTRITION_BASE_AMOUNT } from '../../../shared/lib/nutrition.constants';
import { MeasurementUnit, ProductType, ProductVisibility } from '../../products/models/product.data';
import { RecipeService } from '../api/recipe.service';
import type { IngredientFormData, StepFormData } from '../components/manage/recipe-manage.types';
import { RecipeVisibility } from '../models/recipe.data';
import { RecipeManageFacade } from './recipe-manage.facade';

const PRODUCT_DEFAULT_PORTION_AMOUNT = 150;
const APPLE_CALORIES = 52;
const APPLE_PROTEINS = 0.3;
const APPLE_FATS = 0.2;
const APPLE_CARBS = 14;
const APPLE_FIBER = 2.4;
const APPLE_QUALITY_SCORE = 65;
const INGREDIENT_AMOUNT = 50;
const SUMMARY_CALORIES = 100;
const SUMMARY_PROTEINS = 5;
const SUMMARY_FATS = 2.5;
const SUMMARY_CARBS = 10;
const SUMMARY_FIBER = 1.5;

let facade: RecipeManageFacade;
let recipeService: {
    create: ReturnType<typeof vi.fn>;
    update: ReturnType<typeof vi.fn>;
};
let navigationService: {
    navigateToRecipeListAsync: ReturnType<typeof vi.fn>;
};
let dialogService: {
    open: ReturnType<typeof vi.fn>;
};

beforeEach(() => {
    recipeService = {
        create: vi.fn().mockReturnValue(of({ id: 'recipe-1', name: 'Recipe' })),
        update: vi.fn().mockReturnValue(of({ id: 'recipe-1', name: 'Recipe' })),
    };
    navigationService = {
        navigateToRecipeListAsync: vi.fn().mockResolvedValue(true),
    };
    dialogService = {
        open: vi.fn().mockReturnValue({
            afterClosed: () => of(null),
        }),
    };

    TestBed.configureTestingModule({
        providers: [
            RecipeManageFacade,
            { provide: RecipeService, useValue: recipeService },
            { provide: NavigationService, useValue: navigationService },
            { provide: FdUiDialogService, useValue: dialogService },
            {
                provide: TranslateService,
                useValue: {
                    instant: vi.fn((key: string) => key),
                },
            },
        ],
    });

    facade = TestBed.inject(RecipeManageFacade);
});

describe('RecipeManageFacade submit', () => {
    it('creates recipe and navigates on success', async () => {
        facade.addRecipe({
            name: 'Recipe',
            steps: [],
            servings: 1,
            cookTime: 1,
            prepTime: 0,
            visibility: RecipeVisibility.Public,
            calculateNutritionAutomatically: true,
        });

        await Promise.resolve();

        expect(recipeService.create).toHaveBeenCalled();
        expect(navigationService.navigateToRecipeListAsync).toHaveBeenCalledTimes(1);
        expect(facade.globalError()).toBeNull();
    });

    it('updates recipe and navigates on success', async () => {
        facade.updateRecipe('recipe-1', {
            name: 'Recipe',
            steps: [],
            servings: 1,
            cookTime: 1,
            prepTime: 0,
            visibility: RecipeVisibility.Public,
            calculateNutritionAutomatically: true,
        });

        await Promise.resolve();

        expect(recipeService.update).toHaveBeenCalledWith(
            'recipe-1',
            expect.objectContaining({
                name: 'Recipe',
            }),
        );
        expect(navigationService.navigateToRecipeListAsync).toHaveBeenCalledTimes(1);
    });

    it('sets non-translated backend error on submit failure', () => {
        recipeService.create.mockReturnValue(
            throwError(() => ({
                error: {
                    message: 'Backend failed',
                },
            })),
        );

        facade.addRecipe({
            name: 'Recipe',
            steps: [],
            servings: 1,
            cookTime: 1,
            prepTime: 0,
            visibility: RecipeVisibility.Public,
            calculateNutritionAutomatically: true,
        });

        expect(facade.globalError()).toBe('Backend failed');
    });
});

describe('RecipeManageFacade selection', () => {
    it('opens item selection dialog and normalizes undefined dialog result to null', async () => {
        const afterClosed = of(undefined);
        dialogService.open.mockReturnValue({
            afterClosed: () => afterClosed,
        });

        const result$ = facade.openItemSelectionDialog();
        const result = await firstValueFrom(result$);

        expect(dialogService.open).toHaveBeenCalledTimes(1);
        expect(result).toBeNull();
    });

    it('applies product selection defaults to ingredient form group', () => {
        const ingredientGroup = new FormGroup({
            food: new FormControl(null),
            amount: new FormControl<number | null>(null),
            foodName: new FormControl<string | null>(null),
            nestedRecipeId: new FormControl<string | null>(null),
            nestedRecipeName: new FormControl<string | null>(null),
        }) as unknown as FormGroup<IngredientFormData>;

        facade.applyItemSelection(ingredientGroup, {
            type: 'Product',
            product: {
                id: 'product-1',
                name: 'Apple',
                baseUnit: MeasurementUnit.G,
                baseAmount: DEFAULT_NUTRITION_BASE_AMOUNT,
                defaultPortionAmount: PRODUCT_DEFAULT_PORTION_AMOUNT,
                productType: ProductType.Fruit,
                barcode: null,
                brand: null,
                category: null,
                description: null,
                imageUrl: null,
                caloriesPerBase: APPLE_CALORIES,
                proteinsPerBase: APPLE_PROTEINS,
                fatsPerBase: APPLE_FATS,
                carbsPerBase: APPLE_CARBS,
                fiberPerBase: APPLE_FIBER,
                alcoholPerBase: 0,
                usageCount: 0,
                visibility: ProductVisibility.Private,
                createdAt: new Date(),
                isOwnedByCurrentUser: true,
                qualityScore: APPLE_QUALITY_SCORE,
                qualityGrade: 'yellow',
            },
        });

        expect(ingredientGroup.value.foodName).toBe('Apple');
        expect(ingredientGroup.value.amount).toBe(PRODUCT_DEFAULT_PORTION_AMOUNT);
        expect(ingredientGroup.value.nestedRecipeId).toBeNull();
    });
});

describe('RecipeManageFacade nutrition summary', () => {
    it('calculates nutrient summary from recipe steps', () => {
        const stepsArray = new FormArray([
            new FormGroup({
                title: new FormControl<string | null>(null),
                imageUrl: new FormControl(null),
                description: new FormControl('Step'),
                ingredients: new FormArray([
                    new FormGroup({
                        food: new FormControl({
                            baseAmount: DEFAULT_NUTRITION_BASE_AMOUNT,
                            caloriesPerBase: 200,
                            proteinsPerBase: 10,
                            fatsPerBase: 5,
                            carbsPerBase: 20,
                            fiberPerBase: 3,
                            alcoholPerBase: 0,
                        }),
                        amount: new FormControl(INGREDIENT_AMOUNT),
                        foodName: new FormControl('Ingredient'),
                        nestedRecipeId: new FormControl<string | null>(null),
                        nestedRecipeName: new FormControl<string | null>(null),
                    }),
                ]),
            }),
        ]) as unknown as FormArray<FormGroup<StepFormData>>;

        const summary = facade.calculateAutoSummary(stepsArray);

        expect(summary).toEqual({
            calories: SUMMARY_CALORIES,
            proteins: SUMMARY_PROTEINS,
            fats: SUMMARY_FATS,
            carbs: SUMMARY_CARBS,
            fiber: SUMMARY_FIBER,
            alcohol: 0,
        });
    });
});
