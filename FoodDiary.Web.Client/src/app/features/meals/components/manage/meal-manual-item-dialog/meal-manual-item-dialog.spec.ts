/* eslint-disable @typescript-eslint/no-magic-numbers -- Independent regression examples for portions and calorie basis. */
import { Component, input, output } from '@angular/core';
import { type ComponentFixture, TestBed } from '@angular/core/testing';
import { By } from '@angular/platform-browser';
import { TranslateService } from '@ngx-translate/core';
import { FdUiDialogService } from 'fd-ui-kit/dialog/fd-ui-dialog.service';
import { FD_UI_DIALOG_DATA } from 'fd-ui-kit/dialog/fd-ui-dialog-data';
import { FdUiDialogRef } from 'fd-ui-kit/dialog/fd-ui-dialog-ref';
import { firstValueFrom, of, Subject } from 'rxjs';
import { describe, expect, it, vi } from 'vitest';

import { provideTranslateTesting } from '../../../../../../testing/translate-testing.module';
import { ItemSelectDialogComponent } from '../../../../../shared/dialogs/item-select-dialog/item-select-dialog';
import { MealSourceType } from '../../../../../shared/models/meal.data';
import { MeasurementUnit, type Product, ProductType, ProductVisibility } from '../../../../../shared/models/product.data';
import { type Recipe, RecipeVisibility } from '../../../../../shared/models/recipe.data';
import { utcInstant } from '../../../../../shared/models/semantics/date-value';
import { entityId } from '../../../../../shared/models/semantics/entity-id';
import {
    servingMassFromObservation,
    type ServingMassResult,
    UNKNOWN_SERVING_MASS,
} from '../../../lib/recipe-serving/recipe-display-amount';
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
const LEGACY_AMOUNT = 75;

type DialogSetup = {
    component: MealManualItemDialogComponent;
    dialogRef: { close: ReturnType<typeof vi.fn> };
    fdDialogService: { open: ReturnType<typeof vi.fn> };
    fixture: ComponentFixture<MealManualItemDialogComponent>;
};

describe('MealManualItemDialogComponent selection', () => {
    it.each([
        { variant: 'two calculated servings', data: { totalCalories: 400, servings: 2 }, text: '200 kcal per serving' },
        { variant: 'one calculated serving', data: { totalCalories: 200, servings: 1 }, text: '200 kcal per serving' },
        {
            variant: 'explicit manual zero',
            data: { totalCalories: 400, manualCalories: 0, isNutritionAutoCalculated: false },
            text: '0 kcal per serving',
        },
        { variant: 'null calories', data: { totalCalories: null, manualCalories: null }, text: 'Recipe' },
        { variant: 'omitted calories', data: {}, text: 'Recipe' },
    ])('shows $text for $variant and still allows changing the amount', async ({ data, text }) => {
        const recipe = { ...createRecipe(), ...data };
        const { fixture, dialogRef } = await setupComponentAsync();
        const translate = TestBed.inject(TranslateService);
        translate.setTranslation('en', {
            MEAL_MANAGE: {
                ITEM_TYPE_OPTIONS: { Recipe: 'Recipe' },
                MANUAL_ITEM_RECIPE_META: '{{calories}} kcal per serving',
            },
        });
        await firstValueFrom(translate.use('en'));
        const picker = fixture.debugElement.query(By.directive(ItemSelectStub)).componentInstance as ItemSelectStub;
        picker.recipeSelected.emit(recipe);
        fixture.detectChanges();

        const host = fixture.nativeElement as HTMLElement;
        expect(host.querySelector('.meal-manual-item-dialog__source-name')?.textContent).toBe(recipe.name);
        expect(host.querySelector('.meal-manual-item-dialog__source-meta')?.textContent).toBe(text);
        const amount = host.querySelector<HTMLInputElement>('input[type="number"]');
        const save = Array.from(host.querySelectorAll('button')).find(button =>
            button.textContent.trim().endsWith('MEAL_MANAGE.MANUAL_ITEM_DIALOG_SAVE'),
        );
        if (amount === null || save === undefined) {
            throw new Error('Expected an amount input and a native Save button');
        }
        expect(amount.valueAsNumber).toBe(RECIPE_SERVING_WEIGHT);
        const editedAmount = 120;
        amount.value = String(editedAmount);
        amount.dispatchEvent(new Event('input', { bubbles: true }));
        fixture.detectChanges();
        expect(save.disabled).toBe(false);
        save.click();
        expect(dialogRef.close).toHaveBeenCalledExactlyOnceWith({
            sourceType: MealSourceType.Recipe,
            product: null,
            recipe,
            amount: editedAmount,
            recipeMass: servingMassFromObservation(RECIPE_SERVING_WEIGHT),
        });
    });
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
        TestBed.tick();
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
            recipeMass: UNKNOWN_SERVING_MASS,
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
                    loadServingMass: vi.fn().mockReturnValue(of(servingMassFromObservation(RECIPE_SERVING_WEIGHT))),
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

function createItemValue(
    values: Partial<{ product: Product; recipe: Recipe; amount: number; servingWeightAvailable: boolean }>,
): MealItemFormValues {
    return {
        sourceType: values.recipe === undefined ? MealSourceType.Product : MealSourceType.Recipe,
        product: values.product ?? null,
        recipe: values.recipe ?? null,
        amount: values.amount ?? null,
        recipeMass:
            values.recipe === undefined || values.servingWeightAvailable === false
                ? UNKNOWN_SERVING_MASS
                : servingMassFromObservation(RECIPE_SERVING_WEIGHT),
    };
}

function createProduct(): Product {
    return {
        id: entityId<'product'>('product-1'),
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
        id: entityId<'recipe'>('recipe-1'),
        name: 'Soup',
        comment: null,
        servings: 2,
        visibility: RecipeVisibility.Private,
        usageCount: 0,
        createdAt: utcInstant('2026-04-05T10:30:00Z'),
        isOwnedByCurrentUser: true,
        isNutritionAutoCalculated: true,
        steps: [],
    };
}

describe('MealManualItemDialogComponent asynchronous serving weight', () => {
    it.each([RECIPE_SERVING_WEIGHT, null])('blocks amount editing and save until weight resolves to %s', async servingWeight => {
        const { component, fixture, dialogRef } = await setupComponentAsync();
        const weight = new Subject<ServingMassResult>();
        vi.spyOn(TestBed.inject(RecipeServingWeightService), 'loadServingMass').mockReturnValue(weight);
        component['onRecipeSelected'](createRecipe());
        fixture.detectChanges();
        const amountInput = (fixture.nativeElement as HTMLElement).querySelector<HTMLInputElement>('input[type="number"]');
        expect(component['canSave']()).toBe(false);
        expect(amountInput?.disabled).toBe(true);
        component['save']();
        expect(dialogRef.close).not.toHaveBeenCalled();

        weight.next(servingMassFromObservation(servingWeight));
        await fixture.whenStable();
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
        const weight = new Subject<ServingMassResult>();
        vi.spyOn(TestBed.inject(RecipeServingWeightService), 'loadServingMass').mockReturnValue(weight);
        embeddedPicker(fixture).recipeSelected.emit(createRecipe());
        fixture.detectChanges();
        if (action === 'product') {
            reopenEmbeddedPicker(fixture).productSelected.emit(createProduct());
            fixture.detectChanges();
        } else if (action === 'amount') {
            component['amount']().value.set(PRODUCT_DEFAULT_PORTION_AMOUNT);
        } else {
            fixture.destroy();
        }
        const expected = component['amountModel']();
        weight.next(servingMassFromObservation(RECIPE_SERVING_WEIGHT));
        expect(component['amountModel']()).toBe(expected);
        if (action !== 'amount') {
            expect(weight.observed).toBe(false);
        }
    });
});

describe('Meal manual item native quantity errors', () => {
    it.each([
        {
            scenario: 'a cleared product quantity',
            item: { product: createProduct(), amount: PRODUCT_DEFAULT_PORTION_AMOUNT },
            entered: '',
            error: 'Enter a quantity.',
        },
        {
            scenario: 'a product quantity below the minimum',
            item: { product: createProduct(), amount: PRODUCT_DEFAULT_PORTION_AMOUNT },
            entered: '0',
            error: 'Enter at least 0.01.',
        },
        {
            scenario: 'a product quantity above the maximum',
            item: { product: createProduct(), amount: 1_000_000 },
            entered: '1000001',
            error: 'Enter no more than 1000000.',
        },
        {
            scenario: 'a recipe quantity above the maximum in grams',
            item: { recipe: createRecipe(), amount: 80_000_000 },
            entered: '80000001',
            error: 'Enter no more than 80000000.',
        },
        {
            scenario: 'a recipe quantity above the maximum in servings',
            item: { recipe: createRecipe(), servingWeightAvailable: false, amount: 1_000_000 },
            entered: '1000001',
            error: 'Enter no more than 1000000.',
        },
    ])('announces $scenario and saves only after correction', async ({ item, entered, error }) => {
        const { fixture, dialogRef } = await setupComponentAsync(item);
        const translate = TestBed.inject(TranslateService);
        translate.setTranslation('en', {
            FORM_ERRORS: {
                REQUIRED: 'Enter a quantity.',
                INVALID_MIN_AMOUNT_MUST_BE_MORE_ZERO: 'Enter at least {{min}}.',
            },
            MEAL_MANAGE: { MAX_AMOUNT_ERROR: 'Enter no more than {{max}}.' },
        });
        await firstValueFrom(translate.use('en'));
        const host = fixture.nativeElement as HTMLElement;
        const amount = host.querySelector<HTMLInputElement>('input[type="number"]');
        if (amount === null) {
            throw new Error('Expected a native quantity input');
        }
        const save = dialogAction(host, 'MEAL_MANAGE.MANUAL_ITEM_DIALOG_SAVE');
        fixture.detectChanges();
        expect(save.disabled).toBe(false);

        enterAndBlurQuantity(amount, entered);
        fixture.detectChanges();
        expect(host.querySelector('.fd-ui-input__error[role="alert"]')?.textContent.trim()).toBe(error);
        expect(amount.getAttribute('aria-invalid')).toBe('true');
        expect(save.disabled).toBe(true);
        save.click();
        expect(dialogRef.close).not.toHaveBeenCalled();

        enterAndBlurQuantity(amount, String(item.amount));
        fixture.detectChanges();
        expect(host.querySelector('.fd-ui-input__error[role="alert"]')).toBeNull();
        expect(save.disabled).toBe(false);
        save.click();
        expect(dialogRef.close).toHaveBeenCalledExactlyOnceWith(expect.objectContaining({ amount: item.amount }));
    });
});

describe('MealManualItemDialogComponent validation and selection boundaries', () => {
    it.each([
        [80_000_000, true],
        [80_000_001, false],
    ])('applies the serving limit in displayed recipe grams at %s', async (amount, valid) => {
        const { component, dialogRef } = await setupComponentAsync({ recipe: createRecipe() });
        component['amount']().value.set(amount);
        component['save']();
        expect(component['canSave']()).toBe(valid);
        expect(dialogRef.close).toHaveBeenCalledTimes(valid ? 1 : 0);
    });
    it.each([
        [1_000_000, true],
        [1_000_001, false],
    ])('validates product quantity %s before returning a draft', async (amount, valid) => {
        const { component, dialogRef } = await setupComponentAsync({ product: createProduct() });
        component['amount']().value.set(amount);
        component['save']();
        expect(component['canSave']()).toBe(valid);
        expect(dialogRef.close).toHaveBeenCalledTimes(valid ? 1 : 0);
        if (!valid) {
            expect(component['amountError']()).toBe('MEAL_MANAGE.MAX_AMOUNT_ERROR');
        }
    });
    it.each([null, 0, -1])('rejects invalid amount %s and exposes an error', async amount => {
        const { component, dialogRef } = await setupComponentAsync({ product: createProduct() });
        component['amount']().value.set(amount);
        component['save']();
        expect(component['canSave']()).toBe(false);
        expect(component['amountError']()).not.toBeNull();
        expect(dialogRef.close).not.toHaveBeenCalled();
    });
    it('keeps the saved portion while reopening the picker and cancels without applying another item', async () => {
        const product = createProduct();
        const { fixture, dialogRef } = await setupComponentAsync({ product, amount: PRODUCT_DEFAULT_PORTION_AMOUNT });
        const host = fixture.nativeElement as HTMLElement;
        expect(host.querySelector('.meal-manual-item-dialog__source-name')?.textContent).toBe(product.name);
        expect(reopenEmbeddedPicker(fixture).embedded()).toBe(true);
        expect(host.querySelector<HTMLInputElement>('input[type="number"]')?.valueAsNumber).toBe(PRODUCT_DEFAULT_PORTION_AMOUNT);

        dialogAction(host, 'PRODUCT_MANAGE.CANCEL_BUTTON').click();
        expect(dialogRef.close).toHaveBeenCalledExactlyOnceWith(null);
    });
    it.each([null, 0, -1])('keeps the fallback amount when serving weight is %s', async weight => {
        const { component } = await setupComponentAsync();
        vi.spyOn(TestBed.inject(RecipeServingWeightService), 'loadServingMass').mockReturnValue(of(servingMassFromObservation(weight)));
        component['onRecipeSelected'](createRecipe());
        expect(component['amountModel']()).toBe(1);
    });
});

describe('Meal item picker legacy portions', () => {
    it.each([
        { source: { product: { ...createProduct(), name: '' } }, label: 'MEAL_MANAGE.MANUAL_ITEM_CHOOSE_PRODUCT' },
        { source: { recipe: { ...createRecipe(), name: '' } }, label: 'MEAL_MANAGE.MANUAL_ITEM_CHOOSE_RECIPE' },
    ])('offers $label for a saved source whose name is unavailable, then applies a replacement', async ({ source, label }) => {
        const { fixture, dialogRef } = await setupComponentAsync({ ...source, amount: LEGACY_AMOUNT });
        const host = fixture.nativeElement as HTMLElement;
        expect(host.querySelector('.meal-manual-item-dialog__source-placeholder')?.textContent.trim()).toBe(label);
        expect(host.querySelector<HTMLInputElement>('input[type="number"]')?.valueAsNumber).toBe(LEGACY_AMOUNT);

        const replacement = { ...createProduct(), id: entityId<'product'>('replacement-product'), name: 'Pear' };
        reopenEmbeddedPicker(fixture).productSelected.emit(replacement);
        fixture.detectChanges();
        expect(host.querySelector('.meal-manual-item-dialog__source-name')?.textContent).toBe(replacement.name);
        expect(host.querySelector<HTMLInputElement>('input[type="number"]')?.valueAsNumber).toBe(PRODUCT_DEFAULT_PORTION_AMOUNT);
        dialogAction(host, 'MEAL_MANAGE.MANUAL_ITEM_DIALOG_SAVE').click();
        expect(dialogRef.close).toHaveBeenCalledExactlyOnceWith({
            sourceType: MealSourceType.Product,
            product: replacement,
            recipe: null,
            amount: PRODUCT_DEFAULT_PORTION_AMOUNT,
            recipeMass: UNKNOWN_SERVING_MASS,
        });
    });

    it.each([
        [0, 100, 100],
        [-1, 100, 100],
        [Number.NaN, 100, 100],
        [Number.POSITIVE_INFINITY, 100, 100],
        [0, 0, 1],
    ])(
        'uses a valid product amount when the saved portion is %s and the base amount is %s',
        async (defaultPortionAmount, baseAmount, expectedAmount) => {
            const { fixture, dialogRef } = await setupComponentAsync();
            const host = fixture.nativeElement as HTMLElement;
            const picker = fixture.debugElement.query(By.directive(ItemSelectStub)).componentInstance as ItemSelectStub;
            expect(picker.embedded()).toBe(true);
            const product = { ...createProduct(), baseAmount, defaultPortionAmount };
            picker.productSelected.emit(product);
            fixture.detectChanges();

            expect(host.querySelector('.meal-manual-item-dialog__source-name')?.textContent).toBe(product.name);
            expect(host.querySelector<HTMLInputElement>('input[type="number"]')?.value).toBe(String(expectedAmount));
            const save = Array.from(host.querySelectorAll('button')).find(button =>
                button.textContent.trim().endsWith('MEAL_MANAGE.MANUAL_ITEM_DIALOG_SAVE'),
            );
            expect(save?.disabled).toBe(false);
            save?.click();

            expect(dialogRef.close).toHaveBeenCalledExactlyOnceWith({
                sourceType: MealSourceType.Product,
                product,
                recipe: null,
                amount: expectedAmount,
                recipeMass: UNKNOWN_SERVING_MASS,
            });
        },
    );
});

function embeddedPicker(fixture: ComponentFixture<MealManualItemDialogComponent>): ItemSelectStub {
    return fixture.debugElement.query(By.directive(ItemSelectStub)).injector.get(ItemSelectStub);
}

function reopenEmbeddedPicker(fixture: ComponentFixture<MealManualItemDialogComponent>): ItemSelectStub {
    const host = fixture.nativeElement as HTMLElement;
    const source = host.querySelector<HTMLButtonElement>('.meal-manual-item-dialog__source-selector');
    if (source === null) {
        throw new Error('Expected the native source picker button');
    }
    source.click();
    fixture.detectChanges();
    return embeddedPicker(fixture);
}

function dialogAction(host: HTMLElement, label: string): HTMLButtonElement {
    const button = Array.from(host.querySelectorAll('button')).find(
        control => control.querySelector('.fd-ui-button__label')?.textContent.trim() === label,
    );
    if (button === undefined) {
        throw new Error(`Expected the native ${label} action`);
    }
    return button;
}

function enterAndBlurQuantity(control: HTMLInputElement, value: string): void {
    control.focus();
    control.value = value;
    control.dispatchEvent(new Event('input', { bubbles: true }));
    control.blur();
}
