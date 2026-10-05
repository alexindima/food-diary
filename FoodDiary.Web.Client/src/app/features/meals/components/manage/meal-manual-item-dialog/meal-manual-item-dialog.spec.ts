import { Component, input, output } from '@angular/core';
import { type ComponentFixture, TestBed } from '@angular/core/testing';
import { FdUiDialogService } from 'fd-ui-kit/dialog/fd-ui-dialog.service';
import { FD_UI_DIALOG_DATA } from 'fd-ui-kit/dialog/fd-ui-dialog-data';
import { FdUiDialogRef } from 'fd-ui-kit/dialog/fd-ui-dialog-ref';
import { of, Subject } from 'rxjs';
import { describe, expect, it, vi } from 'vitest';

import { provideTranslateTesting } from '../../../../../../testing/translate-testing.module';
import { ItemSelectDialogComponent } from '../../../../../shared/dialogs/item-select-dialog/item-select-dialog';
import { MealSourceType } from '../../../../../shared/models/meal.data';
import { MeasurementUnit, type Product, ProductType, ProductVisibility } from '../../../../../shared/models/product.data';
import { type Recipe, RecipeVisibility } from '../../../../../shared/models/recipe.data';
import { RecipeServingWeightService } from '../../../lib/recipe-serving/recipe-serving-weight.service';
import type { MealItemFormValues } from '../meal-manage-lib/meal-manage.types';
import { MealManualItemDialogComponent, type MealManualItemDialogData } from './meal-manual-item-dialog';

@Component({ selector: 'fd-item-select-dialog', template: '' })
class ItemSelectStub {
    public readonly allowRecipeCreation = input(true);
    public readonly embedded = input(false);
    public readonly productSelected = output<Product>();
    public readonly recipeSelected = output<Recipe>();
}

const PRODUCT_DEFAULT_PORTION_AMOUNT = 125;
const RECIPE_SERVING_WEIGHT = 80;
const RECIPE_FALLBACK_SERVINGS = 1.5;

type DialogSetup = {
    component: MealManualItemDialogComponent;
    dialogRef: { close: ReturnType<typeof vi.fn> };
    fdDialogService: { open: ReturnType<typeof vi.fn> };
    fixture: ComponentFixture<MealManualItemDialogComponent>;
};

describe('MealManualItemDialogComponent selection', () => {
    it('should apply selected product default portion amount', async () => {
        const product = createProduct();
        const { component, fdDialogService } = await setupComponentAsync();
        component['onProductSelected'](product);
        expect(fdDialogService.open).not.toHaveBeenCalled();

        expect(component['product']()).toBe(product);
        expect(component['recipe']()).toBeNull();
        expect(component['amountModel']()).toBe(PRODUCT_DEFAULT_PORTION_AMOUNT);
        expect(component['sourceType']()).toBe(MealSourceType.Product);
    });

    it('should apply selected recipe serving weight', async () => {
        const recipe = createRecipe();
        const { component, fdDialogService } = await setupComponentAsync();
        component['onRecipeSelected'](recipe);
        expect(fdDialogService.open).not.toHaveBeenCalled();

        expect(component['recipe']()).toBe(recipe);
        expect(component['product']()).toBeNull();
        expect(component['amountModel']()).toBe(RECIPE_SERVING_WEIGHT);
        expect(component['sourceType']()).toBe(MealSourceType.Recipe);
    });
});

describe('MealManualItemDialogComponent save', () => {
    it('should not show source error before submit attempt', async () => {
        const { component } = await setupComponentAsync();

        expect(component['sourceError']()).toBeNull();
    });

    it('should close with item value on valid save', async () => {
        const product = createProduct();
        const { component, dialogRef } = await setupComponentAsync({
            product,
            amount: PRODUCT_DEFAULT_PORTION_AMOUNT,
        });

        component['save']();

        expect(dialogRef.close).toHaveBeenCalledWith({
            sourceType: MealSourceType.Product,
            product,
            recipe: null,
            amount: PRODUCT_DEFAULT_PORTION_AMOUNT,
        });
    });

    it('should keep dialog open when source is missing', async () => {
        const { component, dialogRef } = await setupComponentAsync();

        component['save']();

        expect(component['sourceError']()).toBe('MEAL_MANAGE.ITEM_SOURCE_ERROR');
        expect(dialogRef.close).not.toHaveBeenCalled();
    });

    it('should close with false on cancel', async () => {
        const { component, dialogRef } = await setupComponentAsync();

        component['cancel']();

        expect(dialogRef.close).toHaveBeenCalledWith(null);
    });
});

async function setupComponentAsync(
    values: Partial<{ product: Product; recipe: Recipe; amount: number; servingWeightAvailable: boolean }> = {},
): Promise<DialogSetup> {
    const item = createItemValue(values);
    const dialogRef = { close: vi.fn() };
    const fdDialogService = {
        open: vi.fn().mockReturnValue({ afterClosed: () => of(null) }),
    };

    await TestBed.configureTestingModule({
        imports: [MealManualItemDialogComponent],
        providers: [
            provideTranslateTesting(),
            { provide: FD_UI_DIALOG_DATA, useValue: { item } satisfies MealManualItemDialogData },
            { provide: FdUiDialogRef, useValue: dialogRef },
            { provide: FdUiDialogService, useValue: fdDialogService },
            {
                provide: RecipeServingWeightService,
                useValue: {
                    loadServingWeight: vi.fn().mockReturnValue(of(RECIPE_SERVING_WEIGHT)),
                    hasServingWeight: vi.fn().mockReturnValue(values.servingWeightAvailable ?? true),
                },
            },
        ],
    })
        .overrideComponent(MealManualItemDialogComponent, {
            remove: { imports: [ItemSelectDialogComponent] },
            add: { imports: [ItemSelectStub] },
        })
        .compileComponents();

    const fixture = TestBed.createComponent(MealManualItemDialogComponent);
    fixture.detectChanges();

    return {
        component: fixture.componentInstance,
        dialogRef,
        fdDialogService,
        fixture,
    };
}

function createItemValue(values: Partial<{ product: Product; recipe: Recipe; amount: number }>): MealItemFormValues {
    return {
        sourceType: values.recipe === undefined ? MealSourceType.Product : MealSourceType.Recipe,
        product: values.product ?? null,
        recipe: values.recipe ?? null,
        amount: values.amount ?? null,
    };
}

function createProduct(): Product {
    return {
        id: 'product-1',
        name: 'Apple',
        productType: ProductType.Unknown,
        baseUnit: MeasurementUnit.G,
        baseAmount: 100,
        defaultPortionAmount: PRODUCT_DEFAULT_PORTION_AMOUNT,
        caloriesPerBase: 50,
        proteinsPerBase: 1,
        fatsPerBase: 0,
        carbsPerBase: 12,
        fiberPerBase: 2,
        alcoholPerBase: 0,
        visibility: ProductVisibility.Private,
        usageCount: 0,
        createdAt: new Date('2026-04-05T10:30:00Z'),
        isOwnedByCurrentUser: true,
        qualityScore: 80,
        qualityGrade: 'green',
    };
}

function createRecipe(): Recipe {
    return {
        id: 'recipe-1',
        name: 'Soup',
        comment: null,
        servings: 2,
        visibility: RecipeVisibility.Private,
        usageCount: 0,
        createdAt: '2026-04-05T10:30:00Z',
        isOwnedByCurrentUser: true,
        isNutritionAutoCalculated: true,
        steps: [],
    };
}

describe('MealManualItemDialogComponent asynchronous serving weight', () => {
    it.each([RECIPE_SERVING_WEIGHT, null])('blocks amount editing and save until weight resolves to %s', async servingWeight => {
        const { component, fixture, dialogRef } = await setupComponentAsync();
        const weight = new Subject<number | null>();
        vi.spyOn(TestBed.inject(RecipeServingWeightService), 'loadServingWeight').mockReturnValue(weight);
        component['onRecipeSelected'](createRecipe());
        fixture.detectChanges();
        const amountInput = (fixture.nativeElement as HTMLElement).querySelector<HTMLInputElement>('input[type="number"]');
        expect(component['canSave']()).toBe(false);
        expect(amountInput?.disabled).toBe(true);
        component['save']();
        expect(dialogRef.close).not.toHaveBeenCalled();

        weight.next(servingWeight);
        fixture.detectChanges();
        expect(component['canSave']()).toBe(true);
        expect(amountInput?.disabled).toBe(false);
        expect(component['amountModel']()).toBe(servingWeight ?? 1);
        expect(component['amountPlaceholderKey']()).toBe(
            servingWeight === null ? 'MEAL_MANAGE.AMOUNT_PLACEHOLDER_RECIPE_SERVINGS' : 'MEAL_MANAGE.AMOUNT_PLACEHOLDER_RECIPE',
        );
        if (servingWeight === null) {
            expect(component['amountLabel']()).toBe('MEAL_MANAGE.AMOUNT_LABEL_RECIPE_SERVINGS');
        }
    });
    it('labels an existing recipe in servings when no weight is available', async () => {
        const { component } = await setupComponentAsync({
            recipe: createRecipe(),
            amount: RECIPE_FALLBACK_SERVINGS,
            servingWeightAvailable: false,
        });
        expect(component['amountLabel']()).toBe('MEAL_MANAGE.AMOUNT_LABEL_RECIPE_SERVINGS');
        expect(component['amountModel']()).toBe(RECIPE_FALLBACK_SERVINGS);
    });
    it.each(['product', 'amount', 'destroy'] as const)('does not overwrite newer state after %s', async action => {
        const { component, fixture } = await setupComponentAsync();
        const weight = new Subject<number | null>();
        vi.spyOn(TestBed.inject(RecipeServingWeightService), 'loadServingWeight').mockReturnValue(weight);
        component['onRecipeSelected'](createRecipe());
        if (action === 'product') {
            component['onSourceTypeChange']('Product');
            component['onProductSelected'](createProduct());
        } else if (action === 'amount') {
            component['amount']().value.set(PRODUCT_DEFAULT_PORTION_AMOUNT);
        } else {
            fixture.destroy();
        }
        const expected = component['amountModel']();
        weight.next(RECIPE_SERVING_WEIGHT);
        expect(component['amountModel']()).toBe(expected);
        if (action !== 'amount') {
            expect(weight.observed).toBe(false);
        }
    });
});

describe('MealManualItemDialogComponent validation and selection boundaries', () => {
    it.each([null, 0, -1])('rejects invalid amount %s and exposes an error', async amount => {
        const { component, dialogRef } = await setupComponentAsync({ product: createProduct() });
        component['amount']().value.set(amount);
        component['save']();
        expect(component['canSave']()).toBe(false);
        expect(component['amountError']()).not.toBeNull();
        expect(dialogRef.close).not.toHaveBeenCalled();
    });
    it('preserves the selection when choosing the same type and clears it on a type change', async () => {
        const product = createProduct();
        const { component } = await setupComponentAsync({ product, amount: PRODUCT_DEFAULT_PORTION_AMOUNT });
        component['onSourceTypeChange']('Product');
        expect(component['product']()).toBe(product);
        component['onSourceTypeChange']('Recipe');
        expect(component['product']()).toBeNull();
        expect(component['amountModel']()).toBeNull();
        expect(component['sourceError']()).toBeNull();
        expect(component['sourceActionLabelKey']()).toBe('MEAL_MANAGE.MANUAL_ITEM_CHOOSE_RECIPE');
        expect(component['sourceTypeLabelKey']()).toBe('MEAL_MANAGE.ITEM_TYPE_OPTIONS.Recipe');
    });
    it('keeps the current item when selection is cancelled', async () => {
        const product = createProduct();
        const { component } = await setupComponentAsync({ product, amount: PRODUCT_DEFAULT_PORTION_AMOUNT });
        component['showItemPicker']();
        expect(component['product']()).toBe(product);
        expect(component['amountModel']()).toBe(PRODUCT_DEFAULT_PORTION_AMOUNT);
    });
    it.each([null, 0, -1])('keeps the fallback amount when serving weight is %s', async weight => {
        const { component } = await setupComponentAsync();
        vi.spyOn(TestBed.inject(RecipeServingWeightService), 'loadServingWeight').mockReturnValue(of(weight));
        component['onRecipeSelected'](createRecipe());
        expect(component['amountModel']()).toBe(1);
    });
});
