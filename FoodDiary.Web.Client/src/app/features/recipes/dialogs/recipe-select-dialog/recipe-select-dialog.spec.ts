import { type ComponentFixture, TestBed } from '@angular/core/testing';
import { FdUiDialogRef } from 'fd-ui-kit/dialog/fd-ui-dialog-ref';
import { type Observable, of, Subject, throwError } from 'rxjs';
import { describe, expect, it, vi } from 'vitest';

import { provideTranslateTesting } from '../../../../../testing/translate-testing.module';
import { APP_SEARCH_DEBOUNCE_MS } from '../../../../config/runtime-ui.tokens';
import type { PageOf } from '../../../../shared/models/page-of.data';
import { type Recipe, RecipeVisibility } from '../../../../shared/models/recipe.data';
import { utcInstant } from '../../../../shared/models/semantics/date-value';
import { entityId } from '../../../../shared/models/semantics/entity-id';
import { RecipeSelectFacade } from '../../lib/recipe-select.facade';
import { RecipeSelectDialogComponent } from './recipe-select-dialog';
import type { RecipeSelectItemViewModel } from './recipe-select-dialog-lib/recipe-select-dialog.types';

const PAGE_SIZE = 10;
const SECOND_PAGE_INDEX = 1;
const SECOND_PAGE = 2;
const ZERO_DEBOUNCE_MS = 0;
const FULL_FILTER_COUNT = 5;

describe('RecipeSelectDialogComponent', () => {
    it('loads recipes on creation and maps them to selectable items', () => {
        const recipe = createRecipe();
        const { component, recipeService } = setupComponent([recipe]);

        expect(recipeService.query).toHaveBeenCalledWith(1, PAGE_SIZE, { search: undefined }, true);
        expect(readRecipeItems(component)).toEqual([{ recipe, imageUrl: undefined }]);
    });

    it('excludes the current recipe from selectable items', () => {
        const currentRecipe = createRecipe({ id: entityId<'recipe'>('recipe-1') });
        const nestedRecipe = createRecipe({ id: entityId<'recipe'>('recipe-2') });
        const { component, fixture } = setupComponent([currentRecipe, nestedRecipe]);

        fixture.componentRef.setInput('excludedRecipeId', currentRecipe.id);
        fixture.detectChanges();

        expect(readRecipeItems(component)).toEqual([{ recipe: nestedRecipe, imageUrl: undefined }]);
    });

    it('closes dialog with selected recipe when used as dialog', () => {
        const recipe = createRecipe();
        const { component, dialogRef } = setupComponent([recipe]);

        component['onRecipeClick'](recipe);

        expect(dialogRef.close).toHaveBeenCalledWith(recipe);
    });

    it('emits selected recipe when embedded', () => {
        const recipe = createRecipe();
        const { component, fixture, dialogRef } = setupComponent([recipe]);
        const selected: Recipe[] = [];
        component['recipeSelected'].subscribe(value => {
            selected.push(value);
        });
        fixture.componentRef.setInput('embedded', true);
        fixture.detectChanges();

        component['onRecipeClick'](recipe);

        expect(dialogRef.close).not.toHaveBeenCalled();
        expect(selected).toEqual([recipe]);
    });

    it('loads requested page and scrolls to top on page change', () => {
        const { component, recipeService } = setupComponent([createRecipe()]);
        const scrollSpy = vi.spyOn(component as unknown as { scrollToTop: () => void }, 'scrollToTop').mockImplementation(() => {});

        component['onPageChange'](SECOND_PAGE_INDEX);
        TestBed.tick();

        expect(scrollSpy).toHaveBeenCalled();
        expect(recipeService.query).toHaveBeenLastCalledWith(SECOND_PAGE, PAGE_SIZE, { search: undefined }, true);
    });
});

describe('RecipeSelectDialogComponent query ownership', () => {
    it('cancels a pending initial query when a debounced search starts', async () => {
        const initial = new Subject<PageOf<Recipe>>();
        const search = new Subject<PageOf<Recipe>>();
        const { component, fixture, recipeService } = setupComponent([], initial);
        recipeService.query.mockReturnValueOnce(search);
        component['searchForm'].search().value.set('Soup');
        fixture.detectChanges();
        await vi.waitFor(() => {
            expect(recipeService.query).toHaveBeenLastCalledWith(1, PAGE_SIZE, { search: 'Soup' }, true);
        });
        expect(initial.observed).toBe(false);
        search.next(createPage([createRecipe({ name: 'Soup' })]));
        search.complete();
        initial.error(new Error('late initial failure'));
        expect(readRecipeItems(component)[0]?.recipe.name).toBe('Soup');
    });
    it('cancels the initial request when pagination begins and ignores its late response', async () => {
        const initial = new Subject<PageOf<Recipe>>();
        const page = new Subject<PageOf<Recipe>>();
        const { component, fixture, recipeService } = setupComponent([], initial);
        recipeService.query.mockReturnValueOnce(page);

        component['onPageChange'](SECOND_PAGE_INDEX);
        expect(initial.observed).toBe(false);
        expect(component['isLoading']()).toBe(true);
        fixture.detectChanges();
        initial.next(createPage([createRecipe({ name: 'Stale initial result' })]));
        expect(readRecipeItems(component)).toEqual([]);
        page.next({ ...createPage([createRecipe({ name: 'New page' })]), page: SECOND_PAGE });
        page.complete();
        await fixture.whenStable();
        expect(readRecipeItems(component)[0]?.recipe.name).toBe('New page');
        expect(component['currentPageIndex']).toBe(SECOND_PAGE_INDEX);
        expect(component['isLoading']()).toBe(false);
    });

    it('cancels a pending page when filters change and cancels the replacement on destroy', () => {
        const pending = new Subject<PageOf<Recipe>>();
        const filtered = new Subject<PageOf<Recipe>>();
        const { component, fixture, recipeService } = setupComponent([], pending);
        recipeService.openFilters.mockReturnValueOnce(of({ ...component['searchModel'](), onlyMine: true }));
        recipeService.query.mockReturnValueOnce(filtered);
        component['openFilters']();
        expect(pending.observed).toBe(false);
        fixture.detectChanges();
        expect(filtered.observed).toBe(true);
        fixture.destroy();
        expect(filtered.observed).toBe(false);
        filtered.next(createPage([createRecipe()]));
        expect(readRecipeItems(component)).toEqual([]);
    });
});

describe('RecipeSelectDialogComponent filters and actions', () => {
    it('opens structured filters and reloads recipes with applied values', () => {
        const { component, recipeService } = setupComponent([createRecipe()]);
        recipeService.openFilters.mockReturnValueOnce(
            of({
                onlyMine: true,
                category: 'Dinner',
                maxTotalTime: 30,
                caloriesFrom: 100,
                caloriesTo: 500,
                hasImage: true,
            }),
        );

        component['openFilters']();
        TestBed.tick();

        expect(recipeService.openFilters).toHaveBeenCalledWith({
            onlyMine: false,
            category: null,
            maxTotalTime: null,
            caloriesFrom: null,
            caloriesTo: null,
            hasImage: null,
        });
        expect(component['activeFilterCount']()).toBe(FULL_FILTER_COUNT);
        expect(recipeService.query).toHaveBeenLastCalledWith(
            1,
            PAGE_SIZE,
            {
                search: undefined,
                category: 'Dinner',
                maxTotalTime: 30,
                caloriesFrom: 100,
                caloriesTo: 500,
                hasImage: true,
            },
            false,
        );
    });

    it('clears search through form control', () => {
        const { component } = setupComponent([createRecipe()]);
        component['searchForm'].search().value.set('salad');

        component['clearSearch']();

        expect(component['searchModel']().search).toBe('');
        expect(component['searchValue']()).toBe('');
    });

    it('emits create recipe request', () => {
        const { component } = setupComponent([createRecipe()]);
        const requests: void[] = [];
        component['createRecipeRequested'].subscribe(() => {
            requests.push(undefined);
        });

        component['onCreateRecipeClick']();

        expect(requests.length).toBe(1);
    });
});

describe('RecipeSelectDialogComponent load failure and retry', () => {
    it('clears data and resets loading on load failure', () => {
        const { component, recipeService } = setupComponent([createRecipe()]);
        recipeService.query.mockReturnValueOnce(throwError(() => new Error('load failed')));

        component['loadRecipes'](1);
        TestBed.tick();

        expect(component['recipeData'].items()).toEqual([]);
        expect(component['isLoading']()).toBe(false);
    });

    it('shows a load failure and retries the same filtered search page before showing a successful empty result', async () => {
        const { component, fixture, recipeService } = setupComponent([createRecipe()], undefined, true);
        const element = fixture.nativeElement as HTMLElement;
        const search = 'Soup';
        const category = 'Dinner';
        recipeService.openFilters.mockReturnValueOnce(of({ ...component['searchModel'](), category, onlyMine: true }));
        component['openFilters']();
        fixture.detectChanges();
        component['searchForm'].search().value.set(search);
        fixture.detectChanges();
        await vi.waitFor(() => {
            expect(recipeService.query).toHaveBeenLastCalledWith(1, PAGE_SIZE, { search, category }, false);
        });

        vi.spyOn(component as unknown as { scrollToTop: () => void }, 'scrollToTop').mockImplementation(() => {});
        recipeService.query.mockReturnValueOnce(throwError(() => new Error('load failed')));
        component['onPageChange'](SECOND_PAGE_INDEX);
        fixture.detectChanges();

        expect(element.querySelector('fd-error-state [role="alert"]')).not.toBeNull();
        expect(element.querySelector('.recipe-select__no-results')).toBeNull();
        expect(element.querySelector('.recipe-select__item')).toBeNull();

        const retry = element.querySelector<HTMLButtonElement>('fd-error-state button');
        if (retry === null) {
            throw new Error('Recipe retry button was not rendered');
        }
        recipeService.query.mockReturnValueOnce(of({ ...createPage([]), page: SECOND_PAGE }));
        retry.click();
        fixture.detectChanges();

        expect(recipeService.query).toHaveBeenLastCalledWith(SECOND_PAGE, PAGE_SIZE, { search, category }, false);
        expect(element.querySelector('fd-error-state')).toBeNull();
        expect(element.querySelector('.recipe-select__no-results')?.textContent).toContain('RECIPE_SELECT_DIALOG.NO_RECIPES_FOUND');
    });
});

describe('RecipeSelectDialogComponent catalogue controls', () => {
    it('applies an upper calorie bound and leaves the results alone if filters are unchanged or cancelled', () => {
        const { fixture, recipeService } = setupComponent([createRecipe()], undefined, true);
        const filters = { onlyMine: false, category: null, maxTotalTime: null, caloriesFrom: null, caloriesTo: 250, hasImage: null };
        recipeService.openFilters.mockReturnValueOnce(of(filters)).mockReturnValueOnce(of(filters)).mockReturnValueOnce(of(undefined));
        const element = fixture.nativeElement as HTMLElement;
        const button = element.querySelector<HTMLButtonElement>('.recipe-select__filter-button button');
        if (button === null) {
            throw new Error('Recipe filter button was not rendered');
        }

        button.click();
        fixture.detectChanges();
        expect(element.querySelector('.recipe-select__filter-count')?.textContent.trim()).toBe('1');
        expect(recipeService.query).toHaveBeenLastCalledWith(1, PAGE_SIZE, { search: undefined, caloriesTo: 250 }, true);
        button.click();
        button.click();
        fixture.detectChanges();

        expect(recipeService.openFilters).toHaveBeenLastCalledWith(filters);
        expect(recipeService.query).toHaveBeenCalledTimes(2);
        expect(element.querySelector('.recipe-select__item')?.textContent).toContain('Recipe');
    });

    it('shows the clear control only while searching and resets the results from that native control', async () => {
        const { component, fixture, recipeService } = setupComponent([createRecipe()], undefined, true);
        const clearSelector = 'button[aria-label="RECIPE_SELECT_DIALOG.SEARCH_CLEAR"]';
        const element = fixture.nativeElement as HTMLElement;
        const input = element.querySelector<HTMLInputElement>('input');
        if (input === null) {
            throw new Error('Recipe search field was not rendered');
        }
        const changed = vi.fn();
        component.searchChanged.subscribe(changed);
        expect(element.querySelector(clearSelector)).toBeNull();

        input.value = 'Soup';
        input.dispatchEvent(new Event('input', { bubbles: true }));
        fixture.detectChanges();
        await vi.waitFor(() => {
            expect(recipeService.query).toHaveBeenLastCalledWith(1, PAGE_SIZE, { search: 'Soup' }, true);
        });
        expect(changed).toHaveBeenLastCalledWith('Soup');
        const clear = element.querySelector<HTMLButtonElement>(clearSelector);
        if (clear === null) {
            throw new Error('Recipe search clear button was not rendered');
        }
        clear.click();
        fixture.detectChanges();
        await vi.waitFor(() => {
            expect(recipeService.query).toHaveBeenLastCalledWith(1, PAGE_SIZE, { search: '' }, true);
        });

        expect(input.value).toBe('');
        expect(changed).toHaveBeenLastCalledWith('');
        expect(element.querySelector(clearSelector)).toBeNull();
    });
});

function setupComponent(
    recipes: Recipe[],
    initial?: Observable<PageOf<Recipe>>,
    renderTemplate = false,
): {
    component: RecipeSelectDialogComponent;
    dialogRef: { close: ReturnType<typeof vi.fn> };
    fixture: ComponentFixture<RecipeSelectDialogComponent>;
    recipeService: { openFilters: ReturnType<typeof vi.fn>; query: ReturnType<typeof vi.fn> };
} {
    const recipeService = {
        openFilters: vi.fn().mockReturnValue(of(null)),
        query: vi.fn().mockReturnValue(initial ?? of(createPage(recipes))),
    };
    const dialogRef = { close: vi.fn() };

    TestBed.configureTestingModule({
        imports: [RecipeSelectDialogComponent],
        providers: [
            provideTranslateTesting(),
            { provide: RecipeSelectFacade, useValue: recipeService },
            { provide: FdUiDialogRef, useValue: dialogRef },
            { provide: APP_SEARCH_DEBOUNCE_MS, useValue: ZERO_DEBOUNCE_MS },
        ],
    });
    if (!renderTemplate) {
        TestBed.overrideTemplate(RecipeSelectDialogComponent, '<div #container></div>');
    }

    const fixture = TestBed.createComponent(RecipeSelectDialogComponent);
    fixture.detectChanges();

    return { component: fixture.componentInstance, dialogRef, fixture, recipeService };
}

function readRecipeItems(component: RecipeSelectDialogComponent): RecipeSelectItemViewModel[] {
    return (component as unknown as { recipeItems: () => RecipeSelectItemViewModel[] }).recipeItems();
}

function createPage(data: Recipe[]): PageOf<Recipe> {
    return {
        data,
        page: 1,
        limit: PAGE_SIZE,
        totalPages: 1,
        totalItems: data.length,
    };
}

function createRecipe(overrides: Partial<Recipe> = {}): Recipe {
    return {
        id: entityId<'recipe'>('recipe-1'),
        name: 'Recipe',
        imageUrl: null,
        servings: 2,
        visibility: RecipeVisibility.Private,
        usageCount: 0,
        createdAt: utcInstant('2026-01-01T00:00:00Z'),
        isOwnedByCurrentUser: true,
        isNutritionAutoCalculated: true,
        steps: [],
        ...overrides,
    };
}
