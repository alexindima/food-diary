import { signal } from '@angular/core';
import type { ComponentFixture } from '@angular/core/testing';
import { ComponentFixtureAutoDetect, TestBed } from '@angular/core/testing';
import { provideRouter } from '@angular/router';
import { provideTranslateService, TranslateService } from '@ngx-translate/core';
import { FdUiDialogService } from 'fd-ui-kit/dialog/fd-ui-dialog.service';
import { FD_UI_DIALOG_DATA } from 'fd-ui-kit/dialog/fd-ui-dialog-data';
import { FdUiDialogRef } from 'fd-ui-kit/dialog/fd-ui-dialog-ref';
import { FdUiToastService } from 'fd-ui-kit/toast/fd-ui-toast.service';
import { of, Subject } from 'rxjs';
import { describe, expect, it, onTestFinished, vi } from 'vitest';

import { APP_SEARCH_DEBOUNCE_MS } from '../../../config/runtime-ui.tokens';
import { QuickMealService } from '../../../features/meals/contracts/quick-meal';
import { FavoriteProductService } from '../../../features/products/api/favorite-product.service';
import { OpenFoodFactsService } from '../../../features/products/api/open-food-facts.service';
import { ProductService } from '../../../features/products/api/product.service';
import { RecipeService } from '../../../features/recipes/api/recipe.service';
import { NavigationService } from '../../../services/navigation.service';
import { MeasurementUnit, type Product, ProductType, ProductVisibility } from '../../models/product.data';
import { type Recipe, RecipeVisibility } from '../../models/recipe.data';
import { utcInstant } from '../../models/semantics/date-value';
import { entityId } from '../../models/semantics/entity-id';
import { ViewportService } from '../../platform/viewport.service';
import { ItemSelectDialogComponent } from './item-select-dialog';
import type { ItemSelectDialogData, ItemSelection } from './item-select-dialog-lib/item-select-dialog.types';

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
    id: entityId<'product'>('product-id'),
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
    id: entityId<'recipe'>('recipe-id'),
    name: 'Porridge',
    servings: SERVINGS,
    visibility: RecipeVisibility.Public,
    usageCount: ZERO_VALUE,
    createdAt: utcInstant('2026-01-01T00:00:00.000Z'),
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
            provideTranslateService({ lang: 'en' }),
            { provide: ComponentFixtureAutoDetect, useValue: false },
            { provide: FD_UI_DIALOG_DATA, useValue: options.dialogData ?? null },
            { provide: FdUiDialogService, useValue: options.dialogService ?? { open: vi.fn() } },
            ...(options.dialogRef !== undefined ? [{ provide: FdUiDialogRef, useValue: options.dialogRef }] : []),
        ],
    });
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

describe('Rendered ingredient picker', () => {
    it('resets the text choice when changing tabs and returns trimmed text from the visible search', () => {
        configureRenderedPicker();
        TestBed.inject(TranslateService).setTranslation('en', { RECIPE_MANAGE: { USE_AS_TEXT: 'Use {{name}} as text' } });
        const ref = openRenderedPicker({ allowText: true });
        const picked = vi.fn<(selection: ItemSelection | null | undefined) => void>();
        ref.afterClosed().subscribe(picked);
        const host = requiredPickerElement(document, 'fd-item-select-dialog', HTMLElement);

        changeSearch(host, 'fd-product-list-dialog input', '  Smoked paprika  ');
        expect(pickerButton(host, 'Use Smoked paprika as text').closest('[hidden]')).toBeNull();
        pickerButton(host, 'MEAL_MANAGE.ITEM_SELECT_DIALOG.RECIPES_TAB').click();
        TestBed.tick();
        expect(host.querySelector('fd-product-list-dialog')).toBeNull();
        expect(pickerButton(host, 'Use as text').closest('[hidden]')).not.toBeNull();

        changeSearch(host, 'fd-recipe-select-dialog input', '  Dill  ');
        pickerButton(host, 'Use Dill as text').click();
        expect(picked).toHaveBeenCalledExactlyOnceWith({ type: 'Text', name: 'Dill' });
    });

    it('selects products and existing recipes without closing the parent meal dialog', async () => {
        const parent = { close: vi.fn() };
        configureRenderedPicker(parent);
        const fixture = TestBed.createComponent(ItemSelectDialogComponent);
        fixture.componentRef.setInput('embedded', true);
        fixture.componentRef.setInput('allowRecipeCreation', false);
        const pickedProduct = vi.fn<(selection: Product) => void>();
        const pickedRecipe = vi.fn<(selection: Recipe) => void>();
        fixture.componentInstance.productSelected.subscribe(pickedProduct);
        fixture.componentInstance.recipeSelected.subscribe(pickedRecipe);
        fixture.detectChanges();
        const host = fixture.nativeElement as HTMLElement;
        expect(pickerButton(host, 'PRODUCT_LIST.ADD_PRODUCT_BUTTON').closest('[hidden]')).toBeNull();

        await vi.waitFor(() => {
            fixture.detectChanges();
            expect(host.querySelector('.product-select__item button')).not.toBeNull();
        });
        const existingProduct = requiredPickerElement(host, '.product-select__item button', HTMLButtonElement);
        existingProduct.click();
        expect(pickedProduct).toHaveBeenCalledExactlyOnceWith(product);
        expect(pickedRecipe).not.toHaveBeenCalled();

        pickerButton(host, 'MEAL_MANAGE.ITEM_SELECT_DIALOG.RECIPES_TAB').click();
        fixture.detectChanges();
        expect(pickerButton(host, 'RECIPE_SELECT_DIALOG.ADD_RECIPE_BUTTON').closest('[hidden]')).not.toBeNull();
        const existingRecipe = requiredPickerElement(host, '.recipe-select__item', HTMLButtonElement);
        expect(existingRecipe.textContent).toContain(recipe.name);
        existingRecipe.click();
        expect(pickedRecipe).toHaveBeenCalledExactlyOnceWith(recipe);
        expect(parent.close).not.toHaveBeenCalled();
    });
});

describe('Rendered item picker product creation', () => {
    it('keeps the picker open when product creation is cancelled and returns the next created product', () => {
        configureRenderedPicker();
        const openCreation = vi.fn();
        const closeCreation = vi.fn();
        openCreation
            .mockReturnValueOnce({ afterClosed: () => of(null) })
            .mockReturnValueOnce({ afterClosed: () => of(product), close: closeCreation });
        const ref = openRenderedPicker({}, openCreation);
        const picked = vi.fn<(selection: ItemSelection | null | undefined) => void>();
        ref.afterClosed().subscribe(picked);
        const host = requiredPickerElement(document, 'fd-item-select-dialog', HTMLElement);
        const add = pickerButton(host, 'PRODUCT_LIST.ADD_PRODUCT_BUTTON');
        add.click();
        expect(host.isConnected).toBe(true);
        expect(picked).not.toHaveBeenCalled();

        add.click();
        expect(picked).toHaveBeenCalledExactlyOnceWith({ type: 'Product', product });
        expect(host.isConnected).toBe(false);
        expect(closeCreation).not.toHaveBeenCalled();
    });

    it('disposes pending product creation when the parent picker closes', () => {
        configureRenderedPicker();
        const openCreation = vi.fn();
        const creation = new Subject<Product | null>();
        const closeCreation = vi.fn(() => {
            creation.next(null);
            creation.complete();
        });
        openCreation.mockReturnValue({ afterClosed: () => creation, close: closeCreation });
        const ref = openRenderedPicker({}, openCreation);
        const picked = vi.fn<(selection: ItemSelection | null | undefined) => void>();
        ref.afterClosed().subscribe(picked);
        const host = requiredPickerElement(document, 'fd-item-select-dialog', HTMLElement);
        pickerButton(host, 'PRODUCT_LIST.ADD_PRODUCT_BUTTON').click();
        expect(creation.observed).toBe(true);

        ref.close(null);
        expect(host.isConnected).toBe(false);
        expect(creation.observed).toBe(false);
        expect(closeCreation).toHaveBeenCalledOnce();
        creation.next(product);
        expect(picked).toHaveBeenCalledExactlyOnceWith(null);
    });
});

describe('Rendered item picker recipe actions', () => {
    it('selects an existing recipe without offering unsupported recipe creation in the standalone ingredient picker', () => {
        configureRenderedPicker();
        const openCreation = vi.fn();
        const ref = openRenderedPicker({ initialTab: 'Recipe', lockInitialTab: true, allowText: true }, openCreation);
        const picked = vi.fn<(selection: ItemSelection | null | undefined) => void>();
        ref.afterClosed().subscribe(picked);
        const host = requiredPickerElement(document, 'fd-item-select-dialog', HTMLElement);
        const existingRecipe = requiredPickerElement(host, '.recipe-select__item', HTMLButtonElement);
        expect(existingRecipe.textContent).toContain(recipe.name);

        const unsupportedCreate = Array.from(host.querySelectorAll('button')).find(
            button => button.closest('[hidden]') === null && button.textContent.includes('RECIPE_SELECT_DIALOG.ADD_RECIPE_BUTTON'),
        );
        unsupportedCreate?.click();
        expect(picked).not.toHaveBeenCalled();
        expect(host.isConnected).toBe(true);
        expect(unsupportedCreate).toBeUndefined();

        existingRecipe.click();
        expect(picked).toHaveBeenCalledExactlyOnceWith({ type: 'Recipe', recipe });
        expect(openCreation).not.toHaveBeenCalled();
        expect(host.isConnected).toBe(false);
    });

    it('emits the configured recipe creation request from the rendered embedded picker', () => {
        configureRenderedPicker();
        const openCreation = vi.spyOn(TestBed.inject(FdUiDialogService), 'open');
        const fixture = TestBed.createComponent(ItemSelectDialogComponent);
        fixture.componentRef.setInput('embedded', true);
        fixture.componentRef.setInput('allowRecipeCreation', true);
        const createRequested = vi.fn<() => void>();
        fixture.componentInstance.createRecipeRequested.subscribe(createRequested);
        fixture.detectChanges();
        const host = fixture.nativeElement as HTMLElement;

        pickerButton(host, 'MEAL_MANAGE.ITEM_SELECT_DIALOG.RECIPES_TAB').click();
        fixture.detectChanges();
        const addRecipe = pickerButton(host, 'RECIPE_SELECT_DIALOG.ADD_RECIPE_BUTTON');
        expect(addRecipe.closest('[hidden]')).toBeNull();
        addRecipe.click();

        expect(createRequested).toHaveBeenCalledOnce();
        expect(openCreation).not.toHaveBeenCalled();
    });
});

function configureRenderedPicker(parent?: { close: ReturnType<typeof vi.fn> }): void {
    vi.stubGlobal('matchMedia', vi.fn().mockReturnValue({ matches: false }));
    TestBed.configureTestingModule({
        imports: [ItemSelectDialogComponent],
        providers: [
            provideRouter([]),
            provideTranslateService({ lang: 'en' }),
            { provide: APP_SEARCH_DEBOUNCE_MS, useValue: 0 },
            { provide: NavigationService, useValue: {} },
            { provide: QuickMealService, useValue: {} },
            { provide: ViewportService, useValue: { isMobile: signal(false) } },
            { provide: FdUiToastService, useValue: { error: vi.fn() } },
            ...(parent === undefined ? [] : [{ provide: FdUiDialogRef, useValue: parent }]),
            {
                provide: FavoriteProductService,
                useValue: { getLookupPage: vi.fn<FavoriteProductService['getLookupPage']>().mockReturnValue(of([])) },
            },
            { provide: OpenFoodFactsService, useValue: { search: vi.fn<OpenFoodFactsService['search']>().mockReturnValue(of([])) } },
            {
                provide: ProductService,
                useValue: {
                    query: vi.fn<ProductService['query']>().mockReturnValue(of(pickerPage([product]))),
                    queryOverview: vi
                        .fn<ProductService['queryOverview']>()
                        .mockReturnValue(
                            of({ allProducts: pickerPage([product]), recentItems: [], favoriteItems: [], favoriteTotalCount: 0 }),
                        ),
                },
            },
            {
                provide: RecipeService,
                useValue: { queryForPicker: vi.fn<RecipeService['queryForPicker']>().mockReturnValue(of(pickerPage([recipe]))) },
            },
        ],
    });
    const dialogs = TestBed.inject(FdUiDialogService);
    onTestFinished(() => {
        dialogs.closeAll();
        vi.unstubAllGlobals();
    });
}

function pickerPage<T>(data: T[]): { data: T[]; page: number; totalPages: number; totalItems: number; limit: number } {
    return { data, page: 1, totalPages: 1, totalItems: data.length, limit: data.length };
}

function openRenderedPicker(
    data: ItemSelectDialogData,
    openCreation?: ReturnType<typeof vi.fn>,
): FdUiDialogRef<ItemSelectDialogComponent, ItemSelection | null> {
    const providers = openCreation === undefined ? [] : [{ provide: FdUiDialogService, useValue: { open: openCreation } }];
    const ref = TestBed.inject(FdUiDialogService).open<ItemSelectDialogComponent, ItemSelectDialogData, ItemSelection | null>(
        ItemSelectDialogComponent,
        { data, providers },
    );
    TestBed.tick();
    return ref;
}

function changeSearch(host: HTMLElement, selector: string, value: string): void {
    const search = requiredPickerElement(host, selector, HTMLInputElement);
    search.value = value;
    search.dispatchEvent(new Event('input', { bubbles: true }));
    TestBed.tick();
}

function pickerButton(host: HTMLElement, label: string): HTMLButtonElement {
    const found = Array.from(host.querySelectorAll('button')).find(button =>
        button.textContent.replaceAll(/\s+/g, ' ').trim().endsWith(label),
    );
    if (found === undefined) {
        throw new Error(`Missing picker control: ${label}`);
    }
    return found;
}

function requiredPickerElement<T extends HTMLElement>(host: ParentNode, selector: string, type: new () => T): T {
    const element = host.querySelector(selector);
    if (!(element instanceof type)) {
        throw new Error(`Missing ${selector} in the item picker.`);
    }
    return element;
}
