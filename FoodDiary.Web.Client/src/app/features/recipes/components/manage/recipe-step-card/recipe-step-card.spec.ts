import { type ComponentFixture, TestBed } from '@angular/core/testing';
import { provideRouter } from '@angular/router';
import { describe, expect, it, vi } from 'vitest';

import { provideTranslateTesting } from '../../../../../../testing/translate-testing.module';
import { MeasurementUnit, ProductType, ProductVisibility } from '../../../../../shared/models/product.data';
import type { StepFormValues } from '../recipe-manage-lib/recipe-manage.types';
import { createRecipeIngredientValue, createRecipeStepValue } from '../recipe-manage-lib/recipe-manage-form.mapper';
import { RecipeStepCardComponent, type RecipeStepCardState } from './recipe-step-card';

describe('RecipeStepCardComponent', () => {
    it('derives first-step state from step index', () => {
        const { component, fixture } = setupComponent();

        expect(component['isFirst']()).toBe(true);

        fixture.componentRef.setInput('stepIndex', 1);
        fixture.detectChanges();

        expect(component['isFirst']()).toBe(false);
    });

    it('trims title when title editing is committed', () => {
        const { component } = setupComponent(
            createRecipeStepValue({ title: '  Cook rice  ', imageUrl: null, description: '', ingredients: [] }),
        );
        const titleChanges: Array<string | null> = [];
        component['stepTitleChange'].subscribe(value => {
            titleChanges.push(value);
        });

        component['toggleStepTitleEdit']();
        component['toggleStepTitleEdit']();

        expect(titleChanges).toContain('Cook rice');
    });

    it('builds ingredient row metadata from ingredient form state', () => {
        const { component } = setupComponent(
            createRecipeStepValue({
                title: null,
                imageUrl: null,
                description: '',
                ingredients: [
                    {
                        food: {
                            id: 'product-1',
                            name: 'Rice',
                            baseUnit: MeasurementUnit.G,
                            baseAmount: 100,
                            defaultPortionAmount: 100,
                            productType: ProductType.Grain,
                            caloriesPerBase: 100,
                            proteinsPerBase: 2,
                            fatsPerBase: 1,
                            carbsPerBase: 20,
                            fiberPerBase: 1,
                            alcoholPerBase: 0,
                            usageCount: 0,
                            visibility: ProductVisibility.Private,
                            createdAt: new Date('2026-01-01T00:00:00Z'),
                            isOwnedByCurrentUser: true,
                            qualityScore: 50,
                            qualityGrade: 'yellow',
                        },
                        productId: 'product-1',
                        foodName: 'Rice',
                        amount: 100,
                        nestedRecipe: null,
                        nestedRecipeId: null,
                        nestedRecipeName: null,
                    },
                ],
            }),
        );

        expect(component['ingredientRows']()[0]).toEqual(
            expect.objectContaining({
                prefixIcon: 'restaurant',
                amountLabel: 'RECIPE_MANAGE.INGREDIENT_AMOUNT',
                itemType: 'Product',
                amountUnit: 'PRODUCT_AMOUNT_UNITS_SHORT.G',
                selected: true,
            }),
        );
    });
});

describe('Recipe ingredient selection', () => {
    it('emits ingredient selection with requested item type', () => {
        const { component } = setupComponent();
        const selections: Array<{ ingredientIndex: number; itemType: string }> = [];
        component['selectProduct'].subscribe(event => {
            selections.push(event);
        });

        component['onIngredientTypeChange'](0, 'Recipe');
        component['onProductSelectClick'](0, 'Product');

        expect(selections).toEqual([
            { ingredientIndex: 0, itemType: 'Recipe' },
            { ingredientIndex: 0, itemType: 'Product' },
        ]);
    });
});

function setupComponent(
    step = createRecipeStepValue({ title: null, imageUrl: null, description: '', ingredients: [createRecipeIngredientValue()] }),
): {
    component: RecipeStepCardComponent;
    fixture: ComponentFixture<RecipeStepCardComponent>;
} {
    TestBed.configureTestingModule({
        imports: [RecipeStepCardComponent],
        providers: [provideRouter([]), provideTranslateTesting()],
    });

    const fixture = TestBed.createComponent(RecipeStepCardComponent);
    fixture.componentRef.setInput('step', createStepCardState(step));
    fixture.componentRef.setInput('stepIndex', 0);
    fixture.componentRef.setInput('isExpanded', true);
    fixture.componentRef.setInput('dragDisabled', false);
    fixture.detectChanges();

    return { component: fixture.componentInstance, fixture };
}

function createStepCardState(step: StepFormValues): RecipeStepCardState {
    return {
        title: { value: step.title, error: null },
        imageUrl: { value: step.imageUrl, error: null },
        images: step.images,
        description: { value: step.description, error: null },
        ingredients: step.ingredients.map(ingredient => ({
            textName: ingredient.textName,
            amountText: ingredient.amountText,
            amount: { value: ingredient.amount, error: null },
            food: ingredient.food,
            foodName: { value: ingredient.foodName, error: null },
            nestedRecipeId: ingredient.nestedRecipeId,
        })),
    };
}

describe('RecipeStepCardComponent editing boundaries', () => {
    it.each([
        ['', null],
        ['  ', null],
        ['12.5', FRACTIONAL_AMOUNT],
        ['bad', null],
        ['Infinity', null],
        ['0', 0],
    ])('normalizes ingredient amount %s', (value, expected) => {
        const { component } = setupComponent();
        const changed = vi.fn();
        component.ingredientAmountChange.subscribe(changed);
        component['onIngredientAmountInput'](0, value);
        expect(changed).toHaveBeenCalledExactlyOnceWith({ ingredientIndex: 0, amount: expected });
    });

    it('ignores input for an ingredient that no longer exists', () => {
        const { component } = setupComponent();
        const changed = vi.fn();
        component.ingredientAmountChange.subscribe(changed);
        component['onIngredientAmountInput'](-1, '10');
        component['onIngredientAmountInput'](MISSING_INGREDIENT_INDEX, '10');
        expect(changed).not.toHaveBeenCalled();
    });

    it('commits a blank title on blur and exits editing', () => {
        const { component } = setupComponent(createRecipeStepValue({ title: '  ', imageUrl: null, description: '', ingredients: [] }));
        const changed = vi.fn();
        component.stepTitleChange.subscribe(changed);
        component['toggleStepTitleEdit']();
        component['onStepTitleBlur']();
        expect(changed).toHaveBeenCalledExactlyOnceWith(null);
        expect(component['isStepTitleEditing']()).toBe(false);
    });

    it('wires the rendered expand and remove controls and protects the first step', () => {
        const { component, fixture } = setupComponent();
        const toggle = vi.fn();
        const remove = vi.fn();
        component.toggleExpanded.subscribe(toggle);
        component.removeStep.subscribe(remove);
        const buttons: HTMLButtonElement[] = [
            ...(fixture.nativeElement as HTMLElement).querySelectorAll<HTMLButtonElement>('.recipe-step-card__header-actions button'),
        ];
        buttons[0].click();
        buttons[1].click();
        fixture.detectChanges();
        const removeButton = [...document.querySelectorAll<HTMLButtonElement>('[role="menuitem"]')].find(button =>
            button.textContent.includes('RECIPE_MANAGE.REMOVE_STEP'),
        );
        expect(removeButton).toBeDefined();
        expect(removeButton?.disabled).toBe(true);
        expect(toggle).toHaveBeenCalledTimes(1);
        expect(remove).not.toHaveBeenCalled();
        fixture.componentRef.setInput('stepIndex', 1);
        fixture.detectChanges();
        expect(removeButton?.disabled).toBe(false);
        removeButton?.click();
        expect(remove).toHaveBeenCalledTimes(1);
    });
});

const FRACTIONAL_AMOUNT = 12.5;
const MISSING_INGREDIENT_INDEX = 10;

describe('RecipeStepCardComponent gallery', () => {
    it('emits all photos in the selected cover order and preserves upload state', () => {
        const photos = [
            { assetId: 'first', url: '/first.jpg' },
            { assetId: 'second', url: '/second.jpg' },
        ];
        const { component } = setupComponent({ ...createRecipeStepValue(), images: photos });
        const changed = vi.fn();
        const uploading = vi.fn();
        component.stepPhotosChange.subscribe(changed);
        component.photosUploading.subscribe(uploading);
        component['onCoverChange'](photos[1]);
        expect(changed).toHaveBeenCalledWith([photos[1], photos[0]]);
        component['onPhotosChange']([]);
        expect(changed).toHaveBeenLastCalledWith([]);
        component['onUploadingChange'](true);
        expect(uploading).toHaveBeenCalledWith(true);
        expect(component['uploading']()).toBe(true);
    });
});

describe('Recipe step presentation', () => {
    it('renders editable text ingredients and emits free-form amounts', () => {
        const { component, fixture } = setupComponent({
            ...createRecipeStepValue(),
            ingredients: [createRecipeIngredientValue({ textName: 'Salt', amountText: 'to taste' })],
        });
        const changed = vi.fn();
        component.ingredientTextChange.subscribe(changed);
        expect(component['ingredientRows']()[0].itemType).toBe('Text');
        expect((fixture.nativeElement as HTMLElement).querySelector('input[type="number"]')).toBeNull();
        component['onIngredientTextInput'](0, 'amountText', 'a pinch');
        expect(changed).toHaveBeenCalledWith({ ingredientIndex: 0, field: 'amountText', value: 'a pinch' });
    });
    it('shows servings for a nested recipe and no unit before selection', () => {
        const { component, fixture } = setupComponent();
        expect(component['ingredientRows']()[0].amountUnit).toBeUndefined();
        const state = component.step();
        fixture.componentRef.setInput('step', { ...state, ingredients: [{ ...state.ingredients[0], nestedRecipeId: 'nested-1' }] });
        fixture.detectChanges();
        expect(component['ingredientRows']()[0].amountUnit).toBe('RECIPE_MANAGE.STEP_SERVINGS_UNIT');
        expect(component['ingredientRows']()[0].selected).toBe(true);
    });

    it('marks an empty description in the collapsed summary', () => {
        const { component, fixture } = setupComponent();
        fixture.componentRef.setInput('isExpanded', false);
        fixture.detectChanges();
        expect(component['descriptionMissing']()).toBe(true);
        expect(
            (fixture.nativeElement as HTMLElement).querySelector('.recipe-step-card__summary-description--empty')?.textContent,
        ).toContain('RECIPE_MANAGE.STEP_NO_DESCRIPTION');
    });
});
