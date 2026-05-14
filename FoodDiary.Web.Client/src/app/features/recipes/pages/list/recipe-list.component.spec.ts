import { signal } from '@angular/core';
import { type ComponentFixture, TestBed } from '@angular/core/testing';
import { FdUiDialogService } from 'fd-ui-kit/dialog/fd-ui-dialog.service';
import { EMPTY, type Observable, of } from 'rxjs';
import { beforeEach, describe, expect, it, vi } from 'vitest';

import { APP_SEARCH_DEBOUNCE_MS } from '../../../../config/runtime-ui.tokens';
import { ViewportService } from '../../../../services/viewport.service';
import { PagedData } from '../../../../shared/lib/paged-data.data';
import { RecipeDetailActionResult } from '../../components/detail/recipe-detail-lib/recipe-detail.types';
import { RecipeListFacade } from '../../lib/recipe-list.facade';
import { type FavoriteRecipe, type Recipe, RecipeVisibility } from '../../models/recipe.data';
import { RecipeListComponent } from './recipe-list.component';

const PAGE_SIZE = 10;
const SECOND_PAGE_INDEX = 1;
const SECOND_PAGE = 2;
const ASYNC_IMPORT_FLUSH_DELAY_MS = 5;
const WAIT_ATTEMPTS = 200;
const ZERO_DEBOUNCE_MS = 0;

let facade: RecipeListFacadeMock;
let dialogService: { open: ReturnType<typeof vi.fn> };
let isMobile: ReturnType<typeof signal<boolean>>;

beforeEach(() => {
    facade = createRecipeListFacadeMock();
    dialogService = { open: vi.fn().mockReturnValue({ afterClosed: (): Observable<never> => EMPTY }) };
    isMobile = signal(false);

    TestBed.configureTestingModule({
        imports: [RecipeListComponent],
        providers: [
            { provide: ViewportService, useValue: { isMobile } },
            { provide: APP_SEARCH_DEBOUNCE_MS, useValue: ZERO_DEBOUNCE_MS },
        ],
    });
    TestBed.overrideComponent(RecipeListComponent, {
        set: {
            template: '<div #container></div>',
            providers: [
                { provide: RecipeListFacade, useValue: facade },
                { provide: FdUiDialogService, useValue: dialogService },
            ],
        },
    });
});

describe('RecipeListComponent initial loading and filters', () => {
    it('loads initial overview on creation', () => {
        setupComponent();

        expect(facade.loadInitialOverview).toHaveBeenCalledWith(1, PAGE_SIZE, null, false);
    });

    it('reloads recipes when only-mine filter changes', () => {
        const { component } = setupComponent();

        component.searchForm.controls.onlyMine.setValue(true);

        expect(facade.loadRecipes).toHaveBeenCalledWith(1, PAGE_SIZE, null, true);
    });

    it('applies changed filter dialog result', () => {
        const { component } = setupComponent({ filterResult: { onlyMine: true } });

        component.openFilters();

        expect(component.searchForm.controls.onlyMine.value).toBe(true);
    });
});

describe('RecipeListComponent detail actions', () => {
    it('reloads favorites and current page after favorite change in detail dialog', async () => {
        const { component } = setupComponent({
            detailResult: new RecipeDetailActionResult('recipe-1', 'FavoriteChanged'),
        });

        component.onRecipeClick(createRecipe());
        await waitForAsync(() => facade.loadFavorites.mock.calls.length > 0);

        expect(facade.loadFavorites).toHaveBeenCalled();
        expect(facade.loadRecipes).toHaveBeenCalledWith(1, PAGE_SIZE, null, false);
    });

    it('delegates edit action from detail dialog to facade', async () => {
        const { component } = setupComponent({
            detailResult: new RecipeDetailActionResult('recipe-1', 'Edit'),
        });
        const recipe = createRecipe();

        component.onRecipeClick(recipe);
        await waitForAsync(() => facade.handleDetailActionAsync.mock.calls.length > 0);

        expect(facade.handleDetailActionAsync).toHaveBeenCalledWith(expect.any(RecipeDetailActionResult), recipe, null, false);
    });
});

describe('RecipeListComponent actions', () => {
    it('loads selected page and scrolls to top', () => {
        const { component } = setupComponent();
        const scrollSpy = vi.fn();
        component['container']().nativeElement.scrollIntoView = scrollSpy;

        component.onPageChange(SECOND_PAGE_INDEX);

        expect(scrollSpy).toHaveBeenCalledWith({ behavior: 'smooth', block: 'start' });
        expect(facade.loadRecipes).toHaveBeenCalledWith(SECOND_PAGE, PAGE_SIZE, null, false);
    });

    it('opens favorite recipe from favorite entry', () => {
        const { component } = setupComponent();
        const recipe = createRecipe();
        facade.getFavoriteRecipe.mockReturnValueOnce(of(recipe));
        const clickSpy = vi.spyOn(component, 'onRecipeClick').mockImplementation(() => undefined);

        component.openFavoriteRecipe(createFavoriteRecipe());

        expect(facade.getFavoriteRecipe).toHaveBeenCalledWith(createFavoriteRecipe());
        expect(clickSpy).toHaveBeenCalledWith(recipe);
    });

    it('adds favorite recipe to meal after resolving recipe', () => {
        const { component } = setupComponent();
        const recipe = createRecipe();
        facade.getFavoriteRecipe.mockReturnValueOnce(of(recipe));

        component.addFavoriteRecipeToMeal(createFavoriteRecipe());

        expect(facade.addToMeal).toHaveBeenCalledWith(recipe);
    });
});

type SetupOptions = {
    detailResult?: RecipeDetailActionResult;
    filterResult?: { onlyMine: boolean } | null;
};

function setupComponent(options: SetupOptions = {}): { fixture: ComponentFixture<RecipeListComponent>; component: RecipeListComponent } {
    dialogService.open.mockImplementation((_component: unknown, config: { data?: unknown }): { afterClosed: () => Observable<unknown> } => {
        if (config.data !== undefined && 'onlyMine' in (config.data as Record<string, unknown>)) {
            return { afterClosed: () => of(options.filterResult ?? null) };
        }

        return { afterClosed: () => (options.detailResult === undefined ? EMPTY : of(options.detailResult)) };
    });

    const fixture = TestBed.createComponent(RecipeListComponent);
    fixture.detectChanges();

    return { fixture, component: fixture.componentInstance };
}

type RecipeListFacadeMock = Omit<
    RecipeListFacade,
    | 'addToMeal'
    | 'getFavoriteRecipe'
    | 'handleDetailActionAsync'
    | 'hasActiveFilters'
    | 'hasSearch'
    | 'loadFavorites'
    | 'loadInitialOverview'
    | 'loadRecipes'
    | 'navigateToAddRecipeAsync'
    | 'removeFavorite'
    | 'toggleRecipeFavorite'
> & {
    addToMeal: ReturnType<typeof vi.fn>;
    getFavoriteRecipe: ReturnType<typeof vi.fn>;
    handleDetailActionAsync: ReturnType<typeof vi.fn>;
    hasActiveFilters: ReturnType<typeof vi.fn>;
    hasSearch: ReturnType<typeof vi.fn>;
    loadFavorites: ReturnType<typeof vi.fn>;
    loadInitialOverview: ReturnType<typeof vi.fn>;
    loadRecipes: ReturnType<typeof vi.fn>;
    navigateToAddRecipeAsync: ReturnType<typeof vi.fn>;
    removeFavorite: ReturnType<typeof vi.fn>;
    toggleRecipeFavorite: ReturnType<typeof vi.fn>;
};

function createRecipeListFacadeMock(): RecipeListFacadeMock {
    const recipeData = new PagedData<Recipe>();
    recipeData.setData({
        data: [createRecipe()],
        page: 1,
        limit: PAGE_SIZE,
        totalPages: 1,
        totalItems: 1,
    });

    return {
        addToMeal: vi.fn(),
        allRecipesSectionItems: signal([createRecipe()]),
        allRecipesSectionLabelKey: signal('RECIPE_LIST.ALL_RECIPES'),
        currentPageIndex: signal(0),
        errorKey: signal(null),
        favoriteLoadingIds: signal<ReadonlySet<string>>(new Set<string>()),
        favoriteRecipes: signal<FavoriteRecipe[]>([]),
        favoriteTotalCount: signal(0),
        getFavoriteRecipe: vi.fn().mockReturnValue(of(createRecipe())),
        handleDetailActionAsync: vi.fn().mockResolvedValue(undefined),
        hasActiveFilters: vi.fn((onlyMine: boolean) => onlyMine),
        hasSearch: vi.fn((search: string | null) => search !== null && search.length > 0),
        hasVisibleRecipes: signal(true),
        isDeleting: signal(false),
        isFavoritesLoadingMore: signal(false),
        loadFavorites: vi.fn().mockReturnValue(of(undefined)),
        loadInitialOverview: vi.fn().mockReturnValue(of(undefined)),
        loadRecipes: vi.fn().mockReturnValue(of(undefined)),
        navigateToAddRecipeAsync: vi.fn().mockResolvedValue(true),
        pageSize: PAGE_SIZE,
        recipeData,
        recentRecipes: signal<Recipe[]>([]),
        removeFavorite: vi.fn().mockReturnValue(of(undefined)),
        showRecentSection: signal(false),
        toggleRecipeFavorite: vi.fn().mockReturnValue(of(undefined)),
    } as unknown as RecipeListFacadeMock;
}

async function flushPromisesAsync(): Promise<void> {
    await Promise.resolve();
    await new Promise(resolve => {
        setTimeout(resolve, ASYNC_IMPORT_FLUSH_DELAY_MS);
    });
}

async function waitForAsync(predicate: () => boolean): Promise<void> {
    for (let attempt = 0; attempt < WAIT_ATTEMPTS; attempt++) {
        if (predicate()) {
            return;
        }

        await flushPromisesAsync();
    }

    expect(predicate()).toBe(true);
}

function createRecipe(overrides: Partial<Recipe> = {}): Recipe {
    return {
        id: 'recipe-1',
        name: 'Recipe',
        servings: 1,
        visibility: RecipeVisibility.Public,
        usageCount: 0,
        createdAt: '2026-01-01T00:00:00Z',
        isOwnedByCurrentUser: true,
        isNutritionAutoCalculated: true,
        imageUrl: null,
        steps: [],
        ...overrides,
    };
}

function createFavoriteRecipe(): FavoriteRecipe {
    return {
        id: 'favorite-1',
        recipeId: 'recipe-1',
        name: 'Recipe',
        createdAtUtc: '2026-01-01T00:00:00Z',
        recipeName: 'Recipe',
        servings: 1,
        totalTimeMinutes: null,
        ingredientCount: 0,
    };
}
