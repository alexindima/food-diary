import { type ComponentFixture, TestBed } from '@angular/core/testing';
import { TranslateService } from '@ngx-translate/core';
import { firstValueFrom } from 'rxjs';
import { describe, expect, it, vi } from 'vitest';

import { provideTranslateTesting } from '../../../../../../testing/translate-testing.module';
import { recipeFromSdk } from '../../../../../shared/api/sdk/recipe-sdk.mapper';
import { MealSourceType } from '../../../../../shared/models/meal.data';
import { MeasurementUnit, type Product, ProductType, ProductVisibility } from '../../../../../shared/models/product.data';
import { type Recipe, RecipeVisibility } from '../../../../../shared/models/recipe.data';
import { utcInstant } from '../../../../../shared/models/semantics/date-value';
import { entityId } from '../../../../../shared/models/semantics/entity-id';
import { servingMassFromObservation, UNKNOWN_SERVING_MASS } from '../../../lib/recipe-serving/recipe-display-amount';
import type { MealItemFormValues } from '../meal-manage-lib/meal-manage.types';
import { MealItemsListComponent, type MealItemsListItemState } from './meal-items-list';

const PRODUCT_AMOUNT = 150;
const PRODUCT_BASE_AMOUNT = 100;
const RECIPE_AMOUNT_GRAMS = 120;
const RECIPE_SERVING_WEIGHT = 60;
const RECIPE_FALLBACK_SERVINGS = 1.5;
const RECIPE_SINGLE_SERVING = 1;
const RECIPE_TWO_SERVINGS = 2;
const RECIPE_ROUNDED_SINGLE_SERVING = 1.001;
const RECIPE_DECIMAL_SINGLE_SERVING = 1.01;
const SINGLE_SERVING_UNIT = 'MEAL_MANAGE.AMOUNT_UNIT_SERVING';
const MULTIPLE_SERVINGS_UNIT = 'MEAL_MANAGE.AMOUNT_UNIT_SERVINGS';
const EDIT_ITEM_INDEX = 1;
const OPEN_ITEM_INDEX = 3;

describe('MealItemsListComponent rows', () => {
    it('should build product row with calculated nutrition totals', async () => {
        const product = createProduct();
        const { component } = await setupComponentAsync({
            items: [createItemState({ sourceType: MealSourceType.Product, product, amount: PRODUCT_AMOUNT })],
        });

        expect(component['manualItemRows']()).toEqual([
            expect.objectContaining({
                index: 0,
                imageUrl: product.imageUrl,
                icon: 'restaurant',
                sourceName: product.name,
                amountLabel: '150 GENERAL.UNITS.G',
                caloriesLabel: '180 GENERAL.UNITS.KCAL',
                proteinsLabel: '15 GENERAL.UNITS.G',
                fatsLabel: '7.5 GENERAL.UNITS.G',
                carbsLabel: '45 GENERAL.UNITS.G',
            }),
        ]);
    });

    it('should build recipe row with serving weight conversion', async () => {
        const recipe = createRecipe();
        const { component } = await setupComponentAsync({
            items: [
                createItemState({
                    sourceType: MealSourceType.Recipe,
                    recipe,
                    amount: RECIPE_AMOUNT_GRAMS,
                    recipeMass: servingMassFromObservation(RECIPE_SERVING_WEIGHT),
                }),
            ],
        });

        expect(component['manualItemRows']()).toEqual([
            expect.objectContaining({
                index: 0,
                imageUrl: recipe.imageUrl,
                icon: 'menu_book',
                sourceName: recipe.name,
                amountLabel: '120 GENERAL.UNITS.G',
                caloriesLabel: '300 GENERAL.UNITS.KCAL',
                proteinsLabel: '20 GENERAL.UNITS.G',
                fatsLabel: '10 GENERAL.UNITS.G',
                carbsLabel: '40 GENERAL.UNITS.G',
            }),
        ]);
    });

    it('should hide empty manual item rows when only external items exist', async () => {
        const { fixture } = await setupComponentAsync({
            hasExternalItems: true,
            items: [createItemState({ sourceType: MealSourceType.Product, product: null, amount: null })],
        });

        const host = fixture.nativeElement as HTMLElement;
        expect(host.querySelector('.meal-manage__manual-summary')).toBeNull();
        expect(host.querySelector('.meal-manage__items-empty')).toBeNull();
    });
    it('displays recipe servings and matching nutrition when weight is unavailable', async () => {
        const { component } = await setupComponentAsync({
            items: [
                createItemState({
                    sourceType: MealSourceType.Recipe,
                    recipe: createRecipe(),
                    amount: RECIPE_FALLBACK_SERVINGS,
                    recipeMass: UNKNOWN_SERVING_MASS,
                }),
            ],
        });
        expect(component['manualItemRows']()[0]).toMatchObject({
            amountLabel: '1.5 MEAL_MANAGE.AMOUNT_UNIT_SERVINGS',
            caloriesLabel: '225 GENERAL.UNITS.KCAL',
        });
    });
});

describe('MealItemsListComponent recipes with incomplete nutrition', () => {
    it.each([
        { name: 'Soup with stored nutrition', totalCalories: 600, totalProteins: 40, caloriesLabel: '300', proteinsLabel: '20' },
        { name: 'Soup without calculated nutrition', totalCalories: null, totalProteins: null, caloriesLabel: '0', proteinsLabel: '0' },
    ])(
        'renders known nutrition and preserves servings for $name',
        async ({ name, totalCalories, totalProteins, caloriesLabel, proteinsLabel }) => {
            const recipe = recipeFromSdk({
                id: 'sparse-recipe',
                name,
                imageUrl: null,
                servings: 4,
                visibility: RecipeVisibility.Private,
                usageCount: 0,
                createdAt: '2026-04-05T10:30:00Z',
                isOwnedByCurrentUser: true,
                isNutritionAutoCalculated: true,
                totalCalories,
                totalProteins,
                totalFats: null,
                totalCarbs: null,
                totalFiber: null,
                totalAlcohol: null,
                steps: [{ id: 'sparse-recipe-step', stepNumber: 1, instruction: 'Warm and serve', ingredients: [] }],
            });
            const { fixture } = await setupComponentAsync({
                items: [createItemState({ sourceType: MealSourceType.Recipe, recipe, amount: RECIPE_TWO_SERVINGS })],
            });

            const row = (fixture.nativeElement as HTMLElement).querySelector('.meal-manage__manual-summary');
            if (row === null) {
                throw new Error('Expected the selected recipe row');
            }
            expect(row.querySelector('.meal-manage__ai-summary-name')?.textContent.trim()).toBe(name);
            expect(row.querySelector('.meal-manage__ai-summary-amount')?.textContent.trim()).toBe(`2 ${MULTIPLE_SERVINGS_UNIT}`);
            expect(row.querySelector('.meal-manage__ai-summary-calories span:last-child')?.textContent.trim()).toBe(
                `${caloriesLabel} GENERAL.UNITS.KCAL`,
            );
            const macros = [...row.querySelectorAll('.meal-manage__ai-summary-nutrients span')];
            expect(macros.map(macro => macro.textContent.trim())).toEqual([
                `GENERAL.NUTRIENTS.PROTEIN: ${proteinsLabel} GENERAL.UNITS.G`,
                'GENERAL.NUTRIENTS.FAT: 0 GENERAL.UNITS.G',
                'GENERAL.NUTRIENTS.CARB: 0 GENERAL.UNITS.G',
            ]);
            expect(row.querySelector('.meal-manage__ai-summary-image img')).toBeNull();
            expect(row.querySelector('.meal-manage__ai-summary-image fd-ui-icon')?.textContent.trim()).toBe('menu_book');
        },
    );
});

describe('MealItemsListComponent serving labels', () => {
    it.each([
        { language: 'en', amount: 0, number: '0', unit: MULTIPLE_SERVINGS_UNIT },
        { language: 'en', amount: RECIPE_SINGLE_SERVING, number: '1', unit: SINGLE_SERVING_UNIT },
        { language: 'en', amount: RECIPE_TWO_SERVINGS, number: '2', unit: MULTIPLE_SERVINGS_UNIT },
        { language: 'en', amount: RECIPE_FALLBACK_SERVINGS, number: '1.5', unit: MULTIPLE_SERVINGS_UNIT },
        { language: 'en', amount: RECIPE_ROUNDED_SINGLE_SERVING, number: '1', unit: SINGLE_SERVING_UNIT },
        { language: 'en', amount: RECIPE_DECIMAL_SINGLE_SERVING, number: '1.0', unit: MULTIPLE_SERVINGS_UNIT },
        { language: 'ru', amount: RECIPE_SINGLE_SERVING, number: '1', unit: SINGLE_SERVING_UNIT },
        { language: 'ru', amount: RECIPE_FALLBACK_SERVINGS, number: '1,5', unit: MULTIPLE_SERVINGS_UNIT },
    ])('matches the displayed serving count $number in $language', async ({ language, amount, number, unit }) => {
        const { fixture } = await setupComponentAsync({
            items: [
                createItemState({ sourceType: MealSourceType.Recipe, recipe: createRecipe(), amount, recipeMass: UNKNOWN_SERVING_MASS }),
            ],
        });
        TestBed.inject(TranslateService).use(language);
        fixture.detectChanges();

        const host = fixture.nativeElement as HTMLElement;
        expect(host.querySelector('.meal-manage__ai-summary-amount')?.textContent.trim()).toBe(`${number} ${unit}`);
    });
});

describe('MealItemsListComponent validation', () => {
    it('displays the collection error and hides incomplete rows', async () => {
        const { fixture } = await setupComponentAsync({
            arrayError: 'FORM_ERRORS.NON_EMPTY_ARRAY',
            items: [createItemState({ sourceType: MealSourceType.Product, product: null, amount: null })],
        });

        const host = fixture.nativeElement as HTMLElement;
        expect(host.querySelector('.meal-item__error')?.textContent).toContain('FORM_ERRORS.NON_EMPTY_ARRAY');
        expect(host.querySelector('.meal-manage__items-empty')?.textContent).toContain('MEAL_MANAGE.ITEMS_EMPTY');
        expect(host.querySelector('.meal-manage__manual-summary')).toBeNull();
    });

    it('renders finite totals for older items whose product base or recipe serving count is zero', async () => {
        const product = { ...createProduct(), baseAmount: 0 };
        const recipe = { ...createRecipe(), servings: 0 };
        const { fixture } = await setupComponentAsync({
            items: [
                createItemState({ sourceType: MealSourceType.Product, product, amount: PRODUCT_AMOUNT }),
                createItemState({ sourceType: MealSourceType.Recipe, recipe, amount: RECIPE_FALLBACK_SERVINGS }),
            ],
        });

        const host = fixture.nativeElement as HTMLElement;
        const rows = Array.from(host.querySelectorAll('.meal-manage__manual-summary'));
        expect(rows.map(row => row.querySelector('.meal-manage__ai-summary-name')?.textContent.trim())).toEqual([
            product.name,
            recipe.name,
        ]);
        expect(rows.map(row => row.querySelector('.meal-manage__ai-summary-calories span:last-child')?.textContent.trim())).toEqual([
            '0 GENERAL.UNITS.KCAL',
            '0 GENERAL.UNITS.KCAL',
        ]);
    });
});

describe('MealItemsListComponent actions', () => {
    it('emits item indices for edit and source selection', async () => {
        const { component } = await setupComponentAsync();
        const editHandler = vi.fn();
        const openHandler = vi.fn();
        component['editItem'].subscribe(editHandler);
        component['openItemSelect'].subscribe(openHandler);

        component['onEditItem'](EDIT_ITEM_INDEX);
        component['onItemSourceClick'](OPEN_ITEM_INDEX);

        expect(editHandler).toHaveBeenCalledWith(EDIT_ITEM_INDEX);
        expect(openHandler).toHaveBeenCalledWith(OPEN_ITEM_INDEX);
    });
});

describe('MealItemsListComponent archived sources without display names', () => {
    it('identifies both sources and keeps their original row actions after omitting an empty draft row', async () => {
        const recipeRow = createItemState({
            sourceType: MealSourceType.Recipe,
            recipe: { ...createRecipe(), name: '  ', imageUrl: null },
            amount: RECIPE_AMOUNT_GRAMS,
            recipeMass: servingMassFromObservation(RECIPE_SERVING_WEIGHT),
        });
        const { component, fixture } = await setupComponentAsync({
            items: [
                createItemState(),
                createItemState({ product: { ...createProduct(), name: '', imageUrl: null }, amount: PRODUCT_AMOUNT }),
                recipeRow,
            ],
        });
        const edit = vi.fn();
        const remove = vi.fn<(rendered: MealItemFormValues) => void>();
        component.editItem.subscribe(edit);
        component.removeItemEvent.subscribe(remove);
        const host = fixture.nativeElement as HTMLElement;
        const rows = Array.from(host.querySelectorAll('.meal-manage__manual-summary'));
        const sourceLabels = (): Array<string | undefined> =>
            rows.map(row => row.querySelector('.meal-manage__ai-summary-name')?.textContent.trim());

        const translations = TestBed.inject(TranslateService);
        translations.setTranslation('en', { MEAL_MANAGE: { ITEM_TYPE_OPTIONS: { Product: 'Product', Recipe: 'Recipe' } } });
        translations.setTranslation('ru', { MEAL_MANAGE: { ITEM_TYPE_OPTIONS: { Product: 'Продукт', Recipe: 'Рецепт' } } });
        await firstValueFrom(translations.use('ru'));
        fixture.detectChanges();

        expect(sourceLabels()).toEqual(['Продукт', 'Рецепт']);
        expect(rows.map(row => row.querySelector('.meal-manage__ai-summary-image fd-ui-icon')?.textContent.trim())).toEqual([
            'restaurant',
            'menu_book',
        ]);
        expect(rows.map(row => row.querySelector('.meal-manage__ai-summary-amount')?.textContent.trim())).toEqual([
            '150 GENERAL.UNITS.G',
            '120 GENERAL.UNITS.G',
        ]);

        await firstValueFrom(translations.use('en'));
        fixture.detectChanges();
        expect(sourceLabels()).toEqual(['Product', 'Recipe']);

        const editButton = rows[0].querySelector<HTMLButtonElement>('button[aria-label="MEAL_MANAGE.MANUAL_ITEM_EDIT"]');
        const removeButton = rows[1].querySelector<HTMLButtonElement>('button[aria-label="MEAL_MANAGE.REMOVE_ITEM"]');
        expect(editButton).not.toBeNull();
        expect(removeButton).not.toBeNull();
        editButton?.click();
        removeButton?.click();
        expect(edit).toHaveBeenCalledExactlyOnceWith(EDIT_ITEM_INDEX);
        expect(remove).toHaveBeenCalledOnce();
        expect(remove.mock.calls[0][0]).toBe(recipeRow.formItem);
    });
});
type MealItemsListSetupOptions = {
    arrayError?: string | null;
    hasExternalItems?: boolean;
    items?: MealItemsListItemState[];
};

async function setupComponentAsync(
    options: MealItemsListSetupOptions = {},
): Promise<{ component: MealItemsListComponent; fixture: ComponentFixture<MealItemsListComponent> }> {
    await TestBed.configureTestingModule({
        imports: [MealItemsListComponent],
        providers: [provideTranslateTesting()],
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

function createItemState(values: Partial<MealItemFormValues & Omit<MealItemsListItemState, 'formItem'>> = {}): MealItemsListItemState {
    const { amountError = null, productInvalid = false, recipeInvalid = false, sourceError = null, ...itemValues } = values;
    const formItem: MealItemFormValues = {
        sourceType: MealSourceType.Product,
        product: null,
        recipe: null,
        amount: null,
        recipeMass: UNKNOWN_SERVING_MASS,
        ...itemValues,
    };
    return { formItem, amountError, productInvalid, recipeInvalid, sourceError };
}

function createProduct(): Product {
    return {
        id: entityId<'product'>('product-1'),
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
        id: entityId<'recipe'>('recipe-1'),
        name: 'Soup',
        imageUrl: 'https://example.test/soup.jpg',
        comment: null,
        servings: 4,
        visibility: RecipeVisibility.Private,
        usageCount: 0,
        createdAt: utcInstant('2026-04-05T10:30:00Z'),
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
