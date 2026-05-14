import { type ComponentFixture, TestBed } from '@angular/core/testing';
import { FormControl, FormGroup } from '@angular/forms';
import { TranslateModule } from '@ngx-translate/core';
import { FdUiDialogService } from 'fd-ui-kit/dialog/fd-ui-dialog.service';
import { FD_UI_DIALOG_DATA } from 'fd-ui-kit/dialog/fd-ui-dialog-data';
import { FdUiDialogRef } from 'fd-ui-kit/dialog/fd-ui-dialog-ref';
import { of } from 'rxjs';
import { describe, expect, it, vi } from 'vitest';

import { MeasurementUnit, type Product, ProductType, ProductVisibility } from '../../../../products/models/product.data';
import { type Recipe, RecipeVisibility } from '../../../../recipes/models/recipe.data';
import { MealManageFacade } from '../../../lib/manage/meal-manage.facade';
import { RecipeServingWeightService } from '../../../lib/recipe-serving/recipe-serving-weight.service';
import { ConsumptionSourceType } from '../../../models/meal.data';
import type { ConsumptionItemFormData } from '../meal-manage-lib/meal-manage.types';
import { MealManualItemDialogComponent, type MealManualItemDialogData } from './meal-manual-item-dialog.component';

const PRODUCT_DEFAULT_PORTION_AMOUNT = 125;
const RECIPE_SERVING_WEIGHT = 80;

type DialogSetup = {
    component: MealManualItemDialogComponent;
    dialogRef: { close: ReturnType<typeof vi.fn> };
    fdDialogService: { open: ReturnType<typeof vi.fn> };
    fixture: ComponentFixture<MealManualItemDialogComponent>;
    group: FormGroup<ConsumptionItemFormData>;
    mealManageFacade: { configureItemType: ReturnType<typeof vi.fn> };
};

describe('MealManualItemDialogComponent selection', () => {
    it('should apply selected product default portion amount', async () => {
        const product = createProduct();
        const { component, fdDialogService } = await setupComponentAsync();
        fdDialogService.open.mockReturnValue({ afterClosed: () => of({ type: 'Product', product }) });

        await component.chooseItemAsync();

        expect(component.product()).toBe(product);
        expect(component.recipe()).toBeNull();
        expect(component.amount.value).toBe(PRODUCT_DEFAULT_PORTION_AMOUNT);
        expect(component.sourceType()).toBe(ConsumptionSourceType.Product);
    });

    it('should apply selected recipe serving weight', async () => {
        const recipe = createRecipe();
        const { component, fdDialogService } = await setupComponentAsync();
        fdDialogService.open.mockReturnValue({ afterClosed: () => of({ type: 'Recipe', recipe }) });

        await component.chooseItemAsync();

        expect(component.recipe()).toBe(recipe);
        expect(component.product()).toBeNull();
        expect(component.amount.value).toBe(RECIPE_SERVING_WEIGHT);
        expect(component.sourceType()).toBe(ConsumptionSourceType.Recipe);
    });
});

describe('MealManualItemDialogComponent save', () => {
    it('should patch form group and close with true on valid save', async () => {
        const product = createProduct();
        const { component, dialogRef, group, mealManageFacade } = await setupComponentAsync({
            product,
            amount: PRODUCT_DEFAULT_PORTION_AMOUNT,
        });

        component.save();

        expect(group.controls.product.value).toBe(product);
        expect(group.controls.amount.value).toBe(PRODUCT_DEFAULT_PORTION_AMOUNT);
        expect(mealManageFacade.configureItemType).toHaveBeenCalledWith(group, ConsumptionSourceType.Product);
        expect(dialogRef.close).toHaveBeenCalledWith(true);
    });

    it('should keep dialog open when source is missing', async () => {
        const { component, dialogRef } = await setupComponentAsync();

        component.save();

        expect(component.sourceError()).toBe('CONSUMPTION_MANAGE.ITEM_SOURCE_ERROR');
        expect(dialogRef.close).not.toHaveBeenCalled();
    });

    it('should close with false on cancel', async () => {
        const { component, dialogRef } = await setupComponentAsync();

        component.cancel();

        expect(dialogRef.close).toHaveBeenCalledWith(false);
    });
});

async function setupComponentAsync(values: Partial<{ product: Product; recipe: Recipe; amount: number }> = {}): Promise<DialogSetup> {
    const group = createItemGroup(values);
    const dialogRef = { close: vi.fn() };
    const fdDialogService = {
        open: vi.fn().mockReturnValue({ afterClosed: () => of(null) }),
    };
    const mealManageFacade = {
        configureItemType: vi.fn(),
    };

    await TestBed.configureTestingModule({
        imports: [MealManualItemDialogComponent, TranslateModule.forRoot()],
        providers: [
            { provide: FD_UI_DIALOG_DATA, useValue: { group } satisfies MealManualItemDialogData },
            { provide: FdUiDialogRef, useValue: dialogRef },
            { provide: FdUiDialogService, useValue: fdDialogService },
            { provide: MealManageFacade, useValue: mealManageFacade },
            {
                provide: RecipeServingWeightService,
                useValue: {
                    loadServingWeight: vi.fn().mockReturnValue(of(RECIPE_SERVING_WEIGHT)),
                },
            },
        ],
    }).compileComponents();

    const fixture = TestBed.createComponent(MealManualItemDialogComponent);
    fixture.detectChanges();

    return {
        component: fixture.componentInstance,
        dialogRef,
        fdDialogService,
        fixture,
        group,
        mealManageFacade,
    };
}

function createItemGroup(values: Partial<{ product: Product; recipe: Recipe; amount: number }>): FormGroup<ConsumptionItemFormData> {
    return new FormGroup<ConsumptionItemFormData>({
        sourceType: new FormControl(values.recipe === undefined ? ConsumptionSourceType.Product : ConsumptionSourceType.Recipe, {
            nonNullable: true,
        }),
        product: new FormControl(values.product ?? null),
        recipe: new FormControl(values.recipe ?? null),
        amount: new FormControl(values.amount ?? null),
    });
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
