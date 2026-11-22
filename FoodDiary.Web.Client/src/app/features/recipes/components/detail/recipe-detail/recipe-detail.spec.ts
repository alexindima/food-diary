import { provideHttpClient } from '@angular/common/http';
import { provideHttpClientTesting } from '@angular/common/http/testing';
import { signal } from '@angular/core';
import { type ComponentFixture, TestBed } from '@angular/core/testing';
import { FdUiDialogService } from 'fd-ui-kit/dialog/fd-ui-dialog.service';
import { FD_UI_DIALOG_DATA } from 'fd-ui-kit/dialog/fd-ui-dialog-data';
import { FdUiDialogRef } from 'fd-ui-kit/dialog/fd-ui-dialog-ref';
import { of, throwError } from 'rxjs';
import { beforeEach, describe, expect, it, vi } from 'vitest';

import { provideTranslateTesting } from '../../../../../../testing/translate-testing.module';
import { type Recipe, RecipeVisibility } from '../../../../../shared/models/recipe.data';
import { recipeIngredientFromStored } from '../../../../../shared/models/recipe-ingredient';
import { utcInstant } from '../../../../../shared/models/semantics/date-value';
import { entityId } from '../../../../../shared/models/semantics/entity-id';
import { ShoppingListAddFacade } from '../../../../shopping-lists/contracts/shopping-list-add';
import { RecipeService } from '../../../api/recipe.service';
import { RecipeDetailComponent } from './recipe-detail';

const PREP_TIME_MINUTES = 10;
const COOK_TIME_MINUTES = 20;
const SERVINGS_COUNT = 4;
const TOTAL_TIME_MINUTES = 30;
const INGREDIENT_COUNT = 4;
const EXPECTED_TAB_COUNT = 2;
const EXPECTED_MACRO_BLOCK_COUNT = 5;
const TOTAL_CALORIES = 800;
const TOTAL_PROTEINS = 40;
const TOTAL_FATS = 30;
const TOTAL_CARBS = 80;
const TOTAL_FIBER = 8;
const BASE_AMOUNT = 100;
const FLOUR_CALORIES_PER_BASE = 364;
const FLOUR_PROTEINS_PER_BASE = 10;
const FLOUR_CARBS_PER_BASE = 76;
const FLOUR_FIBER_PER_BASE = 2.7;
const SUGAR_AMOUNT = 50;
const SUGAR_CALORIES_PER_BASE = 387;

const mockRecipe: Recipe = {
    id: entityId<'recipe'>('1'),
    name: 'Test Recipe',
    comment: null,
    isOwnedByCurrentUser: true,
    prepTime: PREP_TIME_MINUTES,
    cookTime: COOK_TIME_MINUTES,
    servings: SERVINGS_COUNT,
    totalCalories: TOTAL_CALORIES,
    totalProteins: TOTAL_PROTEINS,
    totalFats: TOTAL_FATS,
    totalCarbs: TOTAL_CARBS,
    totalFiber: TOTAL_FIBER,
    totalAlcohol: 0,
    visibility: RecipeVisibility.Private,
    usageCount: 0,
    createdAt: utcInstant('2024-01-01'),
    isNutritionAutoCalculated: true,
    steps: [
        {
            id: 's1',
            stepNumber: 1,
            instruction: 'Mix ingredients',
            ingredients: [
                recipeIngredientFromStored({
                    id: 'i1',
                    amount: BASE_AMOUNT,
                    productId: 'p1',
                    productName: 'Flour',
                    productBaseUnit: 'G',
                    productBaseAmount: BASE_AMOUNT,
                    productCaloriesPerBase: FLOUR_CALORIES_PER_BASE,
                    productProteinsPerBase: FLOUR_PROTEINS_PER_BASE,
                    productFatsPerBase: 1,
                    productCarbsPerBase: FLOUR_CARBS_PER_BASE,
                    productFiberPerBase: FLOUR_FIBER_PER_BASE,
                    productAlcoholPerBase: 0,
                }),
                recipeIngredientFromStored({
                    id: 'i2',
                    amount: SUGAR_AMOUNT,
                    productId: 'p2',
                    productName: 'Sugar',
                    productBaseUnit: 'G',
                    productBaseAmount: BASE_AMOUNT,
                    productCaloriesPerBase: SUGAR_CALORIES_PER_BASE,
                    productProteinsPerBase: 0,
                    productFatsPerBase: 0,
                    productCarbsPerBase: BASE_AMOUNT,
                    productFiberPerBase: 0,
                    productAlcoholPerBase: 0,
                }),
            ],
        },
        {
            id: 's2',
            stepNumber: 2,
            instruction: 'Season',
            ingredients: [
                recipeIngredientFromStored({ id: 'i3', amount: 0, textName: 'Salt', amountText: 'to taste' }),
                recipeIngredientFromStored({ id: 'i4', amount: BASE_AMOUNT, productId: 'missing-product' }),
            ],
        },
    ],
};

let component: RecipeDetailComponent;
let fixture: ComponentFixture<RecipeDetailComponent>;

const mockDialogRef = {
    close: vi.fn(),
};

const mockConfirmDialogRef = {
    afterClosed: vi.fn().mockReturnValue(of(true)),
};

const mockFdDialogService = {
    open: vi.fn().mockReturnValue(mockConfirmDialogRef),
};

const mockRecipeService = {
    duplicate: vi.fn().mockReturnValue(of({ ...mockRecipe, id: entityId<'recipe'>('2'), name: 'Test Recipe (Copy)' })),
};

const shoppingTarget = signal<{ name: string } | null>(null);
const mockShopping = {
    setScope: vi.fn(),
    addAllAsync: vi.fn().mockResolvedValue(undefined),
    isAdded: vi.fn(() => shoppingTarget() !== null),
    target: shoppingTarget,
    busy: signal(false),
    message: signal<string | null>(null),
};

beforeEach(async () => {
    vi.clearAllMocks();
    shoppingTarget.set(null);

    await TestBed.configureTestingModule({
        imports: [RecipeDetailComponent],
        providers: [
            provideTranslateTesting(),
            provideHttpClient(),
            provideHttpClientTesting(),
            { provide: FD_UI_DIALOG_DATA, useValue: mockRecipe },
            { provide: FdUiDialogRef, useValue: mockDialogRef },
            { provide: FdUiDialogService, useValue: mockFdDialogService },
            { provide: RecipeService, useValue: mockRecipeService },
        ],
    })
        .overrideComponent(RecipeDetailComponent, {
            add: { providers: [{ provide: ShoppingListAddFacade, useValue: mockShopping }] },
        })
        .compileComponents();

    fixture = TestBed.createComponent(RecipeDetailComponent);
    component = fixture.componentInstance;
    fixture.detectChanges();
});

describe('RecipeDetailComponent summary state', () => {
    it('should create', () => {
        expect(component).toBeTruthy();
    });

    it('should extract nutrition from recipe', () => {
        expect(component['calories']).toBe(TOTAL_CALORIES / SERVINGS_COUNT);
        expect(component['proteins']).toBe(TOTAL_PROTEINS / SERVINGS_COUNT);
        expect(component['fats']).toBe(TOTAL_FATS / SERVINGS_COUNT);
        expect(component['carbs']).toBe(TOTAL_CARBS / SERVINGS_COUNT);
        expect(component['fiber']).toBe(TOTAL_FIBER / SERVINGS_COUNT);
        expect(component['alcohol']).toBe(0);
    });

    it('should calculate total time', () => {
        expect(component['totalTime']).toBe(TOTAL_TIME_MINUTES);
    });

    it('should count ingredients across steps', () => {
        expect(component['ingredientCount']).toBe(INGREDIENT_COUNT);
    });

    it('should have summary and cooking tabs', () => {
        expect(component['tabs'].length).toBe(EXPECTED_TAB_COUNT);
        expect(component['tabs'][0].value).toBe('summary');
        expect(component['tabs'][1].value).toBe('cook');
    });

    it('links tabs to their panels and selects exactly one with click and keyboard', () => {
        const host = fixture.nativeElement as HTMLElement;
        const tabs = [...host.querySelectorAll<HTMLButtonElement>('[role="tab"]')];
        const activePanel = (): HTMLElement | null => host.querySelector('[role="tabpanel"]:not([hidden])');
        for (const tab of tabs) {
            const panel = host.querySelector<HTMLElement>(`[id="${tab.getAttribute('aria-controls')}"]`);
            expect(panel?.getAttribute('role')).toBe('tabpanel');
            expect(panel?.getAttribute('aria-labelledby')).toBe(tab.id);
            expect(panel?.hidden).toBe(tab.getAttribute('aria-selected') !== 'true');
        }
        tabs[1].click();
        fixture.detectChanges();
        expect(host.querySelectorAll('[role="tabpanel"]:not([hidden])')).toHaveLength(1);
        expect(activePanel()?.getAttribute('aria-labelledby')).toBe(tabs[1].id);
        expect(activePanel()?.querySelector('fd-recipe-cook-mode')).not.toBeNull();

        tabs[1].focus();
        tabs[1].dispatchEvent(new KeyboardEvent('keydown', { key: 'ArrowLeft', bubbles: true, cancelable: true }));
        fixture.detectChanges();

        expect(document.activeElement).toBe(tabs[0]);
        expect(tabs[0].getAttribute('aria-selected')).toBe('true');
        expect(host.querySelectorAll('[role="tabpanel"]:not([hidden])')).toHaveLength(1);
        expect(activePanel()?.getAttribute('aria-labelledby')).toBe(tabs[0].id);
        expect(activePanel()?.querySelector('fd-recipe-detail-summary')).not.toBeNull();
    });
});

describe('RecipeDetailComponent actions', () => {
    it('adds products and imported text ingredients from all steps to a shopping list', () => {
        const host = fixture.nativeElement as HTMLElement;
        const add = [...host.querySelectorAll('button')].find(button => button.textContent.includes('RECIPE_DETAIL.ADD_TO_SHOPPING_LIST'));
        expect(add, 'Personal recipe should have an add-to-shopping-list button').toBeDefined();
        add?.click();

        expect(mockShopping.setScope).toHaveBeenCalledWith(mockRecipe.id, mockRecipe.name);
        expect(mockShopping.addAllAsync).toHaveBeenCalledExactlyOnceWith([
            { index: 0, item: { name: 'Flour', productId: 'p1', amount: BASE_AMOUNT, unit: 'G', isChecked: false } },
            { index: 1, item: { name: 'Sugar', productId: 'p2', amount: SUGAR_AMOUNT, unit: 'G', isChecked: false } },
            { index: 2, item: { name: 'Salt', note: 'to taste', isChecked: false } },
        ]);
        expect(mockDialogRef.close).not.toHaveBeenCalled();

        shoppingTarget.set({ name: 'Groceries' });
        fixture.detectChanges();
        expect(add?.disabled).toBe(true);
        expect(host.querySelector('[role="status"]')?.textContent).toContain('RECIPE_DETAIL.ADDED_TO_SHOPPING_LIST');
    });

    it('should emit edit action', () => {
        component['onEdit']();

        expect(mockDialogRef.close).toHaveBeenCalledWith(expect.objectContaining({ id: '1', action: 'Edit' }));
    });

    it('should emit add-to-meal action', () => {
        component['onAddToMeal']();

        expect(mockDialogRef.close).toHaveBeenCalledWith(expect.objectContaining({ id: '1', action: 'AddToMeal' }));
    });

    it('should emit delete action after confirmation', () => {
        component['onDelete']();

        expect(mockFdDialogService.open).toHaveBeenCalled();
        expect(mockDialogRef.close).toHaveBeenCalledWith(expect.objectContaining({ id: '1', action: 'Delete' }));
    });

    it('should not emit delete when delete is disabled', () => {
        Object.defineProperty(component, 'isDeleteDisabled', { get: () => true });

        component['onDelete']();

        expect(mockFdDialogService.open).not.toHaveBeenCalled();
        expect(mockDialogRef.close).not.toHaveBeenCalled();
    });

    it('should not emit edit when edit is disabled', () => {
        Object.defineProperty(component, 'isEditDisabled', { get: () => true });

        component['onEdit']();

        expect(mockDialogRef.close).not.toHaveBeenCalled();
    });

    it('should handle duplicate', () => {
        component['onDuplicate']();

        expect(mockRecipeService.duplicate).toHaveBeenCalledWith('1');
        expect(mockDialogRef.close).toHaveBeenCalledWith(expect.objectContaining({ id: '2', action: 'Duplicate' }));
    });

    it('renders copy failure inside the open recipe dialog and clears it on retry', () => {
        mockRecipeService.duplicate.mockReturnValueOnce(throwError(() => new Error('copy failed')));
        component['onDuplicate']();
        fixture.detectChanges();

        const alert = (fixture.nativeElement as HTMLElement).querySelector('[role="alert"]');
        expect(alert?.textContent).toContain('RECIPE_DETAIL.DUPLICATE_ERROR');
        expect(mockDialogRef.close).not.toHaveBeenCalled();

        component['onDuplicate']();
        fixture.detectChanges();
        expect((fixture.nativeElement as HTMLElement).querySelector('[role="alert"]')).toBeNull();
        expect(mockDialogRef.close).toHaveBeenCalledWith(expect.objectContaining({ id: '2', action: 'Duplicate' }));
    });

    it('should detect canModify based on ownership and usage', () => {
        expect(component['canModify']).toBe(true);
    });

    it('should build macro blocks', () => {
        expect(component['macroBlocks'].length).toBe(EXPECTED_MACRO_BLOCK_COUNT);
    });
});
