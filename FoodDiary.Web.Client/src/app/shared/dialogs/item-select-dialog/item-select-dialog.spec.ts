import type { ComponentFixture } from '@angular/core/testing';
import { TestBed } from '@angular/core/testing';
import { FdUiDialogService } from 'fd-ui-kit/dialog/fd-ui-dialog.service';
import { FD_UI_DIALOG_DATA } from 'fd-ui-kit/dialog/fd-ui-dialog-data';
import { FdUiDialogRef } from 'fd-ui-kit/dialog/fd-ui-dialog-ref';
import { of } from 'rxjs';
import { describe, expect, it, vi } from 'vitest';

import { MeasurementUnit, type Product, ProductType, ProductVisibility } from '../../../features/products/models/product.data';
import { type Recipe, RecipeVisibility } from '../../../features/recipes/models/recipe.data';
import { ItemSelectDialogComponent } from './item-select-dialog';

const BASE_AMOUNT = 100;
const DEFAULT_PORTION_AMOUNT = 100;
const CALORIES = 52;
const PROTEINS = 0.3;
const FATS = 0.2;
const CARBS = 14;
const FIBER = 2.4;
const ZERO_VALUE = 0;
const QUALITY_SCORE = 85;
const SERVINGS = 2;

const product: Product = {
    id: 'product-id',
    name: 'Apple',
    productType: ProductType.Fruit,
    baseUnit: MeasurementUnit.G,
    baseAmount: BASE_AMOUNT,
    defaultPortionAmount: DEFAULT_PORTION_AMOUNT,
    caloriesPerBase: CALORIES,
    proteinsPerBase: PROTEINS,
    fatsPerBase: FATS,
    carbsPerBase: CARBS,
    fiberPerBase: FIBER,
    alcoholPerBase: ZERO_VALUE,
    usageCount: ZERO_VALUE,
    visibility: ProductVisibility.Public,
    createdAt: new Date('2026-01-01T00:00:00.000Z'),
    isOwnedByCurrentUser: true,
    qualityScore: QUALITY_SCORE,
    qualityGrade: 'green',
};
const recipe: Recipe = {
    id: 'recipe-id',
    name: 'Porridge',
    servings: SERVINGS,
    visibility: RecipeVisibility.Public,
    usageCount: ZERO_VALUE,
    createdAt: '2026-01-01T00:00:00.000Z',
    isOwnedByCurrentUser: true,
    isNutritionAutoCalculated: true,
    steps: [],
};

function configureComponent(
    options: {
        dialogData?: unknown;
        dialogRef?: { close: ReturnType<typeof vi.fn> };
        dialogService?: { open: ReturnType<typeof vi.fn> };
    } = {},
): ComponentFixture<ItemSelectDialogComponent> {
    TestBed.resetTestingModule();
    TestBed.configureTestingModule({
        imports: [ItemSelectDialogComponent],
        providers: [
            { provide: FD_UI_DIALOG_DATA, useValue: options.dialogData ?? null },
            { provide: FdUiDialogService, useValue: options.dialogService ?? { open: vi.fn() } },
            ...(options.dialogRef !== undefined ? [{ provide: FdUiDialogRef, useValue: options.dialogRef }] : []),
        ],
    });
    TestBed.overrideComponent(ItemSelectDialogComponent, { set: { template: '' } });
    return TestBed.createComponent(ItemSelectDialogComponent);
}

describe('ItemSelectDialogComponent', () => {
    it('offers text only for a nonempty valid ingredient query and uses the recipe title', () => {
        const component = configureComponent({ dialogData: { allowText: true } }).componentInstance;
        expect(component['titleKey']).toBe('RECIPE_MANAGE.INGREDIENT_PICKER_TITLE');
        expect(component['canUseText']()).toBe(false);
        component['textQuery'].set('   ');
        expect(component['canUseText']()).toBe(false);
        component['textQuery'].set('Salt');
        expect(component['canUseText']()).toBe(true);
        const overlongNameLength = 257;
        component['textQuery'].set('x'.repeat(overlongNameLength));
        expect(component['canUseText']()).toBe(false);
        expect(configureComponent().componentInstance['titleKey']).toBe('MEAL_MANAGE.ITEM_SELECT_DIALOG.TITLE');
    });

    it('uses trimmed search text only when the caller enables text ingredients', () => {
        const dialogRef = { close: vi.fn() };
        const component = configureComponent({ dialogRef, dialogData: { allowText: true } }).componentInstance;
        component['textQuery'].set('  Tomatoes  ');
        component['useText']();
        expect(dialogRef.close).toHaveBeenCalledWith({ type: 'Text', name: 'Tomatoes' });
        dialogRef.close.mockClear();
        const mealPicker = configureComponent({ dialogRef }).componentInstance;
        mealPicker['textQuery'].set('Tomatoes');
        mealPicker['useText']();
        expect(dialogRef.close).not.toHaveBeenCalled();
    });

    it('uses product tab by default and recipe tab from dialog data', () => {
        expect(configureComponent().componentInstance['activeTab']()).toBe('Product');
        expect(configureComponent({ dialogData: { initialTab: 'Recipe' } }).componentInstance['activeTab']()).toBe('Recipe');
    });

    it('locks tab navigation when initial tab is fixed by caller', () => {
        const component = configureComponent({ dialogData: { initialTab: 'Recipe', lockInitialTab: true } }).componentInstance;

        expect(component['activeTab']()).toBe('Recipe');
        expect(component['isInitialTabLocked']()).toBe(true);
    });

    it('closes dialog with selected product in dialog mode', () => {
        const dialogRef = { close: vi.fn() };
        const component = configureComponent({ dialogRef }).componentInstance;

        component['onProductSelected'](product);

        expect(dialogRef.close).toHaveBeenCalledWith({ type: 'Product', product });
    });

    it('updates active tab from tabs output and ignores unknown values', () => {
        const component = configureComponent().componentInstance;

        component['onTabChange']('Recipe');
        component['onTabChange']('Unknown');

        expect(component['activeTab']()).toBe('Recipe');
    });

    it('emits selection in embedded mode', () => {
        const fixture = configureComponent();
        const component = fixture.componentInstance;
        const productSelected = vi.fn();
        const recipeSelected = vi.fn();
        fixture.componentRef.setInput('embedded', true);
        component['productSelected'].subscribe(productSelected);
        component['recipeSelected'].subscribe(recipeSelected);

        component['onProductSelected'](product);
        component['onRecipeSelected'](recipe);

        expect(productSelected).toHaveBeenCalledWith(product);
        expect(recipeSelected).toHaveBeenCalledWith(recipe);
    });

    it('emits create recipe request in embedded mode', () => {
        const fixture = configureComponent();
        const component = fixture.componentInstance;
        const createRecipeRequested = vi.fn();
        fixture.componentRef.setInput('embedded', true);
        component['createRecipeRequested'].subscribe(createRecipeRequested);

        component['onCreateRecipeRequested']();

        expect(createRecipeRequested).toHaveBeenCalledTimes(1);
    });

    it('opens product creation dialog and completes with created product', () => {
        const dialogRef = { close: vi.fn() };
        const dialogService = { open: vi.fn().mockReturnValue({ afterClosed: () => of(product) }) };
        const component = configureComponent({ dialogRef, dialogService }).componentInstance;

        component['onCreateAction']();

        expect(dialogService.open).toHaveBeenCalled();
        expect(dialogRef.close).toHaveBeenCalledWith({ type: 'Product', product });
    });
});
