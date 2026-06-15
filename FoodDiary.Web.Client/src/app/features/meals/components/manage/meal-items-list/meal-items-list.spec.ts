import { type ComponentFixture, TestBed } from '@angular/core/testing';
import { TranslateService } from '@ngx-translate/core';
import { describe, expect, it, vi } from 'vitest';

import { provideTranslateTesting } from '../../../../../../testing/translate-testing.module';
import { MeasurementUnit, type Product, ProductType, ProductVisibility } from '../../../../products/models/product.data';
import { type Recipe, RecipeVisibility } from '../../../../recipes/models/recipe.data';
import { RecipeServingWeightService } from '../../../lib/recipe-serving/recipe-serving-weight.service';
import { ConsumptionSourceType } from '../../../models/meal.data';
import { MealItemsListComponent, type MealItemsListItemState } from './meal-items-list';

const PRODUCT_AMOUNT = 150;
const PRODUCT_BASE_AMOUNT = 100;
const RECIPE_AMOUNT_GRAMS = 120;
const RECIPE_SERVING_WEIGHT = 60;
const EDIT_ITEM_INDEX = 1;
const REMOVE_ITEM_INDEX = 2;
const OPEN_ITEM_INDEX = 3;

describe('MealItemsListComponent rows', () => {
    it('should build product row with calculated nutrition totals', async () => {
        const product = createProduct();
        const { component } = await setupComponentAsync({
            items: [createItemState({ sourceType: ConsumptionSourceType.Product, product, amount: PRODUCT_AMOUNT })],
        });

        expect(component['manualItemRows']()).toEqual([
            expect.objectContaining({
                index: 0,
                imageUrl: product.imageUrl,
                icon: 'restaurant',
                sourceName: product.name,
                amountLabel: '150 PRODUCT_AMOUNT_UNITS.G',
                caloriesLabel: '180 GENERAL.UNITS.KCAL',
                proteinsLabel: '15 GENERAL.UNITS.G',
                fatsLabel: '7.5 GENERAL.UNITS.G',
                carbsLabel: '45 GENERAL.UNITS.G',
            }),
        ]);
    });

    it('should build recipe row with serving weight conversion', async () => {
        const recipe = createRecipe();
        const recipeWeight = { convertGramsToServings: vi.fn().mockReturnValue(2) };
        const { component } = await setupComponentAsync({
            items: [createItemState({ sourceType: ConsumptionSourceType.Recipe, recipe, amount: RECIPE_AMOUNT_GRAMS })],
            recipeWeight,
        });

        expect(recipeWeight.convertGramsToServings).toHaveBeenCalledWith(recipe, RECIPE_AMOUNT_GRAMS);
        expect(component['manualItemRows']()).toEqual([
            expect.objectContaining({
                index: 0,
                imageUrl: recipe.imageUrl,
                icon: 'menu_book',
                sourceName: recipe.name,
                amountLabel: '120 PRODUCT_AMOUNT_UNITS.G',
                caloriesLabel: '300 GENERAL.UNITS.KCAL',
                proteinsLabel: '20 GENERAL.UNITS.G',
                fatsLabel: '10 GENERAL.UNITS.G',
                carbsLabel: '40 GENERAL.UNITS.G',
            }),
        ]);
    });

    it('should hide empty manual item rows when only external items exist', async () => {
        const { component } = await setupComponentAsync({
            hasExternalItems: true,
            items: [createItemState({ sourceType: ConsumptionSourceType.Product, product: null, amount: null })],
        });

        expect(component['manualItemRows']()).toEqual([]);
        expect(component['hasManualItem'](0)).toBe(false);
    });
});

describe('MealItemsListComponent validation', () => {
    it('should expose array and amount errors after controls are touched', async () => {
        const { component } = await setupComponentAsync({
            arrayError: 'FORM_ERRORS.NON_EMPTY_ARRAY',
            items: [
                createItemState({
                    amount: null,
                    amountError: 'FORM_ERRORS.REQUIRED',
                    product: createProduct(),
                    sourceType: ConsumptionSourceType.Product,
                }),
            ],
        });

        expect(component['arrayError']()).toBe('FORM_ERRORS.NON_EMPTY_ARRAY');
        expect(component['getAmountControlError'](0)).toBe('FORM_ERRORS.REQUIRED');
    });

    it('should expose missing source errors for selected item type', async () => {
        const { component } = await setupComponentAsync({
            items: [
                createItemState({
                    sourceType: ConsumptionSourceType.Product,
                    product: null,
                    amount: PRODUCT_AMOUNT,
                    productInvalid: true,
                    sourceError: 'CONSUMPTION_MANAGE.ITEM_SOURCE_ERROR',
                }),
            ],
        });

        expect(component['isProductInvalid'](0)).toBe(true);
        expect(component['isRecipeInvalid'](0)).toBe(false);
        expect(component['isItemSourceInvalid'](0)).toBe(true);
        expect(component['getItemSourceError'](0)).toBe('CONSUMPTION_MANAGE.ITEM_SOURCE_ERROR');
    });
});

describe('MealItemsListComponent actions', () => {
    it('should emit item actions with item index', async () => {
        const { component } = await setupComponentAsync();
        const editHandler = vi.fn();
        const removeHandler = vi.fn();
        const openHandler = vi.fn();
        component['editItem'].subscribe(editHandler);
        component['removeItemEvent'].subscribe(removeHandler);
        component['openItemSelect'].subscribe(openHandler);

        component['onEditItem'](EDIT_ITEM_INDEX);
        component['onRemoveItem'](REMOVE_ITEM_INDEX);
        component['onItemSourceClick'](OPEN_ITEM_INDEX);

        expect(editHandler).toHaveBeenCalledWith(EDIT_ITEM_INDEX);
        expect(removeHandler).toHaveBeenCalledWith(REMOVE_ITEM_INDEX);
        expect(openHandler).toHaveBeenCalledWith(OPEN_ITEM_INDEX);
    });
});

type MealItemsListSetupOptions = {
    arrayError?: string | null;
    hasExternalItems?: boolean;
    items?: MealItemsListItemState[];
    recipeWeight?: { convertGramsToServings: ReturnType<typeof vi.fn> };
};

async function setupComponentAsync(
    options: MealItemsListSetupOptions = {},
): Promise<{ component: MealItemsListComponent; fixture: ComponentFixture<MealItemsListComponent> }> {
    const recipeWeight = options.recipeWeight ?? {
        convertGramsToServings: vi.fn().mockReturnValue(RECIPE_AMOUNT_GRAMS / RECIPE_SERVING_WEIGHT),
    };

    await TestBed.configureTestingModule({
        imports: [MealItemsListComponent],
        providers: [provideTranslateTesting(), { provide: RecipeServingWeightService, useValue: recipeWeight }],
    }).compileComponents();

    TestBed.inject(TranslateService).use('en');

    const fixture = TestBed.createComponent(MealItemsListComponent);
    fixture.componentRef.setInput('items', options.items ?? [createItemState()]);
    fixture.componentRef.setInput('hasExternalItems', options.hasExternalItems ?? false);
    fixture.componentRef.setInput('arrayError', options.arrayError ?? null);
    fixture.componentRef.setInput('renderVersion', 0);
    fixture.detectChanges();

    return {
        component: fixture.componentInstance,
        fixture,
    };
}

function createItemState(values: Partial<MealItemsListItemState> = {}): MealItemsListItemState {
    return {
        sourceType: values.sourceType ?? ConsumptionSourceType.Product,
        product: values.product ?? null,
        recipe: values.recipe ?? null,
        amount: values.amount ?? null,
        amountError: values.amountError ?? null,
        productInvalid: values.productInvalid ?? false,
        recipeInvalid: values.recipeInvalid ?? false,
        sourceError: values.sourceError ?? null,
    };
}

function createProduct(): Product {
    return {
        id: 'product-1',
        name: 'Apple',
        imageUrl: 'https://example.test/apple.jpg',
        productType: ProductType.Unknown,
        baseUnit: MeasurementUnit.G,
        baseAmount: PRODUCT_BASE_AMOUNT,
        defaultPortionAmount: PRODUCT_AMOUNT,
        caloriesPerBase: 120,
        proteinsPerBase: 10,
        fatsPerBase: 5,
        carbsPerBase: 30,
        fiberPerBase: 4,
        alcoholPerBase: 0,
        visibility: ProductVisibility.Private,
        usageCount: 0,
        createdAt: new Date('2026-04-05T10:30:00Z'),
        isOwnedByCurrentUser: true,
        qualityScore: 90,
        qualityGrade: 'green',
    };
}

function createRecipe(): Recipe {
    return {
        id: 'recipe-1',
        name: 'Soup',
        imageUrl: 'https://example.test/soup.jpg',
        comment: null,
        servings: 4,
        visibility: RecipeVisibility.Private,
        usageCount: 0,
        createdAt: '2026-04-05T10:30:00Z',
        isOwnedByCurrentUser: true,
        isNutritionAutoCalculated: true,
        totalCalories: 600,
        totalProteins: 40,
        totalFats: 20,
        totalCarbs: 80,
        totalFiber: 12,
        totalAlcohol: 0,
        steps: [],
    };
}
