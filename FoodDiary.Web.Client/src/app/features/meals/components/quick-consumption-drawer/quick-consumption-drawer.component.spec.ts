import { computed, signal } from '@angular/core';
import { type ComponentFixture, TestBed } from '@angular/core/testing';
import { FormControl, FormGroup } from '@angular/forms';
import { TranslateModule } from '@ngx-translate/core';
import { FdUiDialogService } from 'fd-ui-kit/dialog/fd-ui-dialog.service';
import { of } from 'rxjs';
import { describe, expect, it, vi } from 'vitest';

import { MeasurementUnit, type Product, ProductType, ProductVisibility } from '../../../products/models/product.data';
import { type Recipe, RecipeVisibility } from '../../../recipes/models/recipe.data';
import { MealManageFacade } from '../../lib/manage/meal-manage.facade';
import { type QuickMealDetails, type QuickMealItem, QuickMealService } from '../../lib/quick/quick-meal.service';
import { ConsumptionSourceType } from '../../models/meal.data';
import type { ConsumptionItemFormData } from '../manage/meal-manage-lib/meal-manage.types';
import { QuickConsumptionDrawerComponent } from './quick-consumption-drawer.component';

const PRODUCT_AMOUNT = 150;
const RECIPE_SERVINGS = 2;
const RECIPE_GRAMS = 300;
const PRE_MEAL_SATIETY_LEVEL = 3;
const DEFAULT_SATIETY_LEVEL = 5;

const product: Product = {
    id: 'product-1',
    name: 'Yogurt',
    productType: ProductType.Dairy,
    baseUnit: MeasurementUnit.G,
    baseAmount: 100,
    defaultPortionAmount: 100,
    caloriesPerBase: 80,
    proteinsPerBase: 5,
    fatsPerBase: 3,
    carbsPerBase: 8,
    fiberPerBase: 0,
    alcoholPerBase: 0,
    usageCount: 0,
    visibility: ProductVisibility.Private,
    createdAt: new Date('2026-05-14T00:00:00Z'),
    isOwnedByCurrentUser: true,
    qualityScore: 80,
    qualityGrade: 'green',
};

const recipe: Recipe = {
    id: 'recipe-1',
    name: 'Soup',
    servings: 4,
    visibility: RecipeVisibility.Private,
    usageCount: 0,
    createdAt: '2026-05-14T00:00:00Z',
    isOwnedByCurrentUser: true,
    isNutritionAutoCalculated: true,
    steps: [],
};

type QuickMealServiceMock = {
    clear: ReturnType<typeof vi.fn>;
    details: ReturnType<typeof signal<QuickMealDetails>>;
    hasItems: ReturnType<typeof computed<boolean>>;
    isSaving: ReturnType<typeof signal<boolean>>;
    items: ReturnType<typeof signal<QuickMealItem[]>>;
    removeItem: ReturnType<typeof vi.fn>;
    saveDraft: ReturnType<typeof vi.fn>;
    updateDetails: ReturnType<typeof vi.fn>;
    updateItem: ReturnType<typeof vi.fn>;
};

type MealManageFacadeMock = {
    convertRecipeGramsToServings: ReturnType<typeof vi.fn>;
    createConsumptionItem: ReturnType<typeof vi.fn>;
    resolveRecipeServingsToGramsAsync: ReturnType<typeof vi.fn>;
};

describe('QuickConsumptionDrawerComponent state', () => {
    it('should hide when empty unless forceShow is enabled', async () => {
        const { component, fixture, quickService } = await setupComponentAsync([]);

        expect(component.shouldRender()).toBe(false);

        fixture.componentRef.setInput('forceShow', true);
        fixture.detectChanges();

        expect(component.shouldRender()).toBe(true);
        expect(quickService.hasItems()).toBe(false);
    });

    it('should build product and recipe item views', async () => {
        const { component } = await setupComponentAsync([
            { key: 'product-product-1', type: 'product', product, amount: PRODUCT_AMOUNT, flashId: 1 },
            { key: 'recipe-recipe-1', type: 'recipe', recipe, amount: RECIPE_SERVINGS },
        ]);

        expect(component.itemViews().map(item => ({ name: item.name, amount: item.amount, unitKey: item.unitKey }))).toEqual([
            { name: 'Yogurt', amount: PRODUCT_AMOUNT, unitKey: 'GENERAL.UNITS.G' },
            { name: 'Soup', amount: RECIPE_SERVINGS, unitKey: 'QUICK_CONSUMPTION.SERVINGS' },
        ]);
    });

    it('should update details and action commands through quick service', async () => {
        const { component, quickService } = await setupComponentAsync([
            { key: 'product-product-1', type: 'product', product, amount: PRODUCT_AMOUNT },
        ]);

        component.updateDate('2026-05-15');
        component.updateTime('08:30');
        component.updateComment('Breakfast');
        component.updatePreMealSatietyLevel(PRE_MEAL_SATIETY_LEVEL);
        component.updatePostMealSatietyLevel(null);
        component.remove('product-product-1');
        component.clear();
        component.save();

        expect(quickService.updateDetails).toHaveBeenCalledWith({ date: '2026-05-15' });
        expect(quickService.updateDetails).toHaveBeenCalledWith({ time: '08:30' });
        expect(quickService.updateDetails).toHaveBeenCalledWith({ comment: 'Breakfast' });
        expect(quickService.updateDetails).toHaveBeenCalledWith({ preMealSatietyLevel: PRE_MEAL_SATIETY_LEVEL });
        expect(quickService.updateDetails).not.toHaveBeenCalledWith({ postMealSatietyLevel: null });
        expect(quickService.removeItem).toHaveBeenCalledWith('product-product-1');
        expect(quickService.clear).toHaveBeenCalled();
        expect(quickService.saveDraft).toHaveBeenCalled();
    });

    it('should reset collapsed and details state when items are cleared', async () => {
        const { component, quickService, fixture } = await setupComponentAsync([
            { key: 'product-product-1', type: 'product', product, amount: PRODUCT_AMOUNT },
        ]);
        component.toggleCollapsed();
        component.toggleDetails();

        quickService.items.set([]);
        fixture.detectChanges();

        expect(component.isCollapsed()).toBe(false);
        expect(component.isDetailsExpanded()).toBe(false);
    });
});

describe('QuickConsumptionDrawerComponent edit', () => {
    it('should update product item after edit dialog is saved', async () => {
        const { component, quickService, mealManageFacade } = await setupComponentAsync([
            { key: 'product-product-1', type: 'product', product, amount: PRODUCT_AMOUNT },
        ]);
        mealManageFacade.createConsumptionItem.mockReturnValue(
            createItemGroup(ConsumptionSourceType.Product, product, null, PRODUCT_AMOUNT),
        );

        component.edit({ key: 'product-product-1', type: 'product', product, amount: PRODUCT_AMOUNT });
        await flushPromisesAsync();

        expect(quickService.updateItem).toHaveBeenCalledWith('product-product-1', {
            key: 'product-product-1',
            type: 'product',
            product,
            amount: PRODUCT_AMOUNT,
        });
    });

    it('should convert recipe grams back to servings after edit dialog is saved', async () => {
        const { component, quickService, mealManageFacade } = await setupComponentAsync([
            { key: 'recipe-recipe-1', type: 'recipe', recipe, amount: RECIPE_SERVINGS },
        ]);
        mealManageFacade.resolveRecipeServingsToGramsAsync.mockResolvedValue(RECIPE_GRAMS);
        mealManageFacade.createConsumptionItem.mockReturnValue(createItemGroup(ConsumptionSourceType.Recipe, null, recipe, RECIPE_GRAMS));

        component.edit({ key: 'recipe-recipe-1', type: 'recipe', recipe, amount: RECIPE_SERVINGS });
        await flushPromisesAsync();

        expect(mealManageFacade.resolveRecipeServingsToGramsAsync).toHaveBeenCalledWith(recipe, RECIPE_SERVINGS);
        expect(mealManageFacade.convertRecipeGramsToServings).toHaveBeenCalledWith(recipe, RECIPE_GRAMS);
        expect(quickService.updateItem).toHaveBeenCalledWith('recipe-recipe-1', {
            key: 'recipe-recipe-1',
            type: 'recipe',
            recipe,
            amount: RECIPE_SERVINGS,
        });
    });
});

async function setupComponentAsync(items: QuickMealItem[]): Promise<{
    component: QuickConsumptionDrawerComponent;
    fixture: ComponentFixture<QuickConsumptionDrawerComponent>;
    mealManageFacade: MealManageFacadeMock;
    quickService: QuickMealServiceMock;
}> {
    const quickService = createQuickMealServiceMock(items);
    const mealManageFacade = createMealManageFacadeMock();

    await TestBed.resetTestingModule()
        .configureTestingModule({
            imports: [QuickConsumptionDrawerComponent, TranslateModule.forRoot()],
            providers: [
                { provide: QuickMealService, useValue: quickService },
                { provide: MealManageFacade, useValue: mealManageFacade },
                {
                    provide: FdUiDialogService,
                    useValue: {
                        open: vi.fn().mockReturnValue({ afterClosed: () => of(true) }),
                    },
                },
            ],
        })
        .compileComponents();

    const fixture = TestBed.createComponent(QuickConsumptionDrawerComponent);
    fixture.detectChanges();

    return {
        component: fixture.componentInstance,
        fixture,
        mealManageFacade,
        quickService,
    };
}

function createQuickMealServiceMock(items: QuickMealItem[]): QuickMealServiceMock {
    const itemSignal = signal(items);
    return {
        clear: vi.fn(() => {
            itemSignal.set([]);
        }),
        details: signal({
            date: '2026-05-14',
            time: '08:00',
            comment: '',
            preMealSatietyLevel: DEFAULT_SATIETY_LEVEL,
            postMealSatietyLevel: DEFAULT_SATIETY_LEVEL,
        }),
        hasItems: computed(() => itemSignal().length > 0),
        isSaving: signal(false),
        items: itemSignal,
        removeItem: vi.fn(),
        saveDraft: vi.fn(),
        updateDetails: vi.fn(),
        updateItem: vi.fn(),
    };
}

function createMealManageFacadeMock(): MealManageFacadeMock {
    return {
        convertRecipeGramsToServings: vi.fn().mockReturnValue(RECIPE_SERVINGS),
        createConsumptionItem: vi.fn(
            (selectedProduct: Product | null, selectedRecipe: Recipe | null, amount: number, sourceType: ConsumptionSourceType) =>
                createItemGroup(sourceType, selectedProduct, selectedRecipe, amount),
        ),
        resolveRecipeServingsToGramsAsync: vi.fn().mockResolvedValue(RECIPE_GRAMS),
    };
}

async function flushPromisesAsync(): Promise<void> {
    await Promise.resolve();
    await Promise.resolve();
}

function createItemGroup(
    sourceType: ConsumptionSourceType,
    selectedProduct: Product | null,
    selectedRecipe: Recipe | null,
    amount: number,
): FormGroup<ConsumptionItemFormData> {
    return new FormGroup<ConsumptionItemFormData>({
        sourceType: new FormControl(sourceType, { nonNullable: true }),
        product: new FormControl(selectedProduct),
        recipe: new FormControl(selectedRecipe),
        amount: new FormControl(amount),
    });
}
