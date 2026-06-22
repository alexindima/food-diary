import { type ComponentFixture, TestBed } from '@angular/core/testing';
import { FdUiDialogService } from 'fd-ui-kit/dialog/fd-ui-dialog.service';
import { FdUiDialogRef } from 'fd-ui-kit/dialog/fd-ui-dialog-ref';
import { of, throwError } from 'rxjs';
import { describe, expect, it, vi } from 'vitest';

import { APP_SEARCH_DEBOUNCE_MS } from '../../../../config/runtime-ui.tokens';
import type { PageOf } from '../../../../shared/models/page-of.data';
import { RecipeListFiltersDialogComponent } from '../../components/list/recipe-list-filters-dialog/recipe-list-filters-dialog';
import { RecipeSelectFacade } from '../../lib/recipe-select.facade';
import { type Recipe, RecipeVisibility } from '../../models/recipe.data';
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
        expect(readRecipeItems(component)).toEqual([{ recipe, imageUrl: 'assets/images/stubs/receipt.png' }]);
    });

    it('excludes the current recipe from selectable items', () => {
        const currentRecipe = createRecipe({ id: 'recipe-1' });
        const nestedRecipe = createRecipe({ id: 'recipe-2' });
        const { component, fixture } = setupComponent([currentRecipe, nestedRecipe]);

        fixture.componentRef.setInput('excludedRecipeId', currentRecipe.id);
        fixture.detectChanges();

        expect(readRecipeItems(component)).toEqual([{ recipe: nestedRecipe, imageUrl: 'assets/images/stubs/receipt.png' }]);
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

        expect(scrollSpy).toHaveBeenCalled();
        expect(recipeService.query).toHaveBeenLastCalledWith(SECOND_PAGE, PAGE_SIZE, { search: undefined }, true);
    });
});

describe('RecipeSelectDialogComponent filters and actions', () => {
    it('opens structured filters and reloads recipes with applied values', () => {
        const { component, dialogService, recipeService } = setupComponent([createRecipe()]);
        dialogService.open.mockReturnValueOnce({
            afterClosed: () =>
                of({
                    onlyMine: true,
                    category: 'Dinner',
                    maxTotalTime: 30,
                    caloriesFrom: 100,
                    caloriesTo: 500,
                    hasImage: true,
                }),
        });

        component['openFilters']();

        expect(dialogService.open).toHaveBeenCalledWith(RecipeListFiltersDialogComponent, {
            preset: 'form',
            data: {
                onlyMine: false,
                category: null,
                maxTotalTime: null,
                caloriesFrom: null,
                caloriesTo: null,
                hasImage: null,
            },
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

    it('clears data and resets loading on load failure', () => {
        const { component, recipeService } = setupComponent([createRecipe()]);
        recipeService.query.mockReturnValueOnce(throwError(() => new Error('load failed')));

        component['loadRecipes'](1).subscribe();

        expect(component['recipeData'].items()).toEqual([]);
        expect(component['recipeData'].isLoading()).toBe(false);
    });
});

function setupComponent(recipes: Recipe[]): {
    component: RecipeSelectDialogComponent;
    dialogService: { open: ReturnType<typeof vi.fn> };
    dialogRef: { close: ReturnType<typeof vi.fn> };
    fixture: ComponentFixture<RecipeSelectDialogComponent>;
    recipeService: { query: ReturnType<typeof vi.fn> };
} {
    const recipeService = {
        query: vi.fn().mockReturnValue(of(createPage(recipes))),
    };
    const dialogService = {
        open: vi.fn().mockReturnValue({ afterClosed: () => of(null) }),
    };
    const dialogRef = { close: vi.fn() };

    TestBed.configureTestingModule({
        imports: [RecipeSelectDialogComponent],
        providers: [
            { provide: RecipeSelectFacade, useValue: recipeService },
            { provide: FdUiDialogService, useValue: dialogService },
            { provide: FdUiDialogRef, useValue: dialogRef },
            { provide: APP_SEARCH_DEBOUNCE_MS, useValue: ZERO_DEBOUNCE_MS },
        ],
    });
    TestBed.overrideComponent(RecipeSelectDialogComponent, {
        set: {
            template: '<div #container></div>',
        },
    });

    const fixture = TestBed.createComponent(RecipeSelectDialogComponent);
    fixture.detectChanges();

    return { component: fixture.componentInstance, dialogService, dialogRef, fixture, recipeService };
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
        id: 'recipe-1',
        name: 'Recipe',
        imageUrl: null,
        servings: 2,
        visibility: RecipeVisibility.Private,
        usageCount: 0,
        createdAt: '2026-01-01T00:00:00Z',
        isOwnedByCurrentUser: true,
        isNutritionAutoCalculated: true,
        steps: [],
        ...overrides,
    };
}
