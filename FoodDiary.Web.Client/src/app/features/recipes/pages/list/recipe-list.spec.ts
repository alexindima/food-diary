import { signal } from '@angular/core';
import { type ComponentFixture, TestBed } from '@angular/core/testing';
import { FdUiDialogService } from 'fd-ui-kit/dialog/fd-ui-dialog.service';
import { EMPTY, type Observable, of } from 'rxjs';
import { beforeEach, describe, expect, it, vi } from 'vitest';

import { waitForAsyncTasksAsync } from '../../../../../testing/async-testing';
import { APP_SEARCH_DEBOUNCE_MS } from '../../../../config/runtime-ui.tokens';
import { PagedData } from '../../../../shared/lib/paged-data.data';
import { ViewportService } from '../../../../shared/platform/viewport.service';
import { RecipeDetailActionResult } from '../../components/detail/recipe-detail-lib/recipe-detail.types';
import { RecipeListFacade } from '../../lib/recipe-list.facade';
import { type FavoriteRecipe, type Recipe, type RecipeFilters, RecipeVisibility } from '../../models/recipe.data';
import { RecipeListComponent } from './recipe-list';

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

        expect(facade.loadInitialOverview).toHaveBeenCalledWith(1, PAGE_SIZE, emptyRecipeFilters(), false);
    });

    it('reloads recipes when only-mine filter changes', async () => {
        const { component } = setupComponent();

        component['searchForm'].onlyMine().value.set(true);
        await flushPromisesAsync();

        expect(facade.loadRecipes).toHaveBeenCalledWith(1, PAGE_SIZE, emptyRecipeFilters(), true);
    });

    it('applies changed filter dialog result', () => {
        const { component } = setupComponent({ filterResult: { onlyMine: true } });

        component['openFilters']();

        expect(component['searchModel']().onlyMine).toBe(true);
    });
});

describe('RecipeListComponent detail actions', () => {
    it('reloads the overview including favorite state after a detail change', async () => {
        const { component } = setupComponent({
            detailResult: new RecipeDetailActionResult('recipe-1', 'FavoriteChanged'),
        });

        component['onRecipeClick'](createRecipe());
        await waitForAsync(() => facade.loadRecipes.mock.calls.length > 0);

        expect(facade.loadFavorites).not.toHaveBeenCalled();
        expect(facade.loadRecipes).toHaveBeenCalledWith(1, PAGE_SIZE, emptyRecipeFilters(), false);
    });

    it('delegates edit action from detail dialog to facade', async () => {
        const { component } = setupComponent({
            detailResult: new RecipeDetailActionResult('recipe-1', 'Edit'),
        });
        const recipe = createRecipe();

        component['onRecipeClick'](recipe);
        await waitForAsync(() => facade.handleDetailActionAsync.mock.calls.length > 0);

        expect(facade.handleDetailActionAsync).toHaveBeenCalledWith(expect.any(RecipeDetailActionResult), recipe, null, false);
    });
});

describe('RecipeListComponent actions', () => {
    it('loads selected page and scrolls to top', () => {
        const { component } = setupComponent();
        const scrollSpy = vi.fn();
        component['container']().nativeElement.scrollIntoView = scrollSpy;

        component['onPageChange'](SECOND_PAGE_INDEX);

        expect(scrollSpy).toHaveBeenCalledWith({ behavior: 'smooth', block: 'start' });
        expect(facade.loadRecipes).toHaveBeenCalledWith(SECOND_PAGE, PAGE_SIZE, emptyRecipeFilters(), false);
    });

    it('opens favorite recipe from favorite entry', () => {
        const { component } = setupComponent();
        const recipe = createRecipe();
        facade.getFavoriteRecipe.mockReturnValueOnce(of(recipe));
        const clickSpy = vi
            .spyOn(component as unknown as { onRecipeClick: (value: ReturnType<typeof createRecipe>) => void }, 'onRecipeClick')
            .mockImplementation(() => {});

        component['openFavoriteRecipe'](createFavoriteRecipe());

        expect(facade.getFavoriteRecipe).toHaveBeenCalledWith(createFavoriteRecipe());
        expect(clickSpy).toHaveBeenCalledWith(recipe);
    });

    it('adds favorite recipe to meal after resolving recipe', () => {
        const { component } = setupComponent();
        const recipe = createRecipe();
        facade.getFavoriteRecipe.mockReturnValueOnce(of(recipe));

        component['addFavoriteRecipeToMeal'](createFavoriteRecipe());

        expect(facade.addToMeal).toHaveBeenCalledWith(recipe);
    });
});

type SetupOptions = {
    detailResult?: RecipeDetailActionResult;
    filterResult?: { onlyMine: boolean } | null;
};

function setupComponent(options: SetupOptions = {}): { fixture: ComponentFixture<RecipeListComponent>; component: RecipeListComponent } {
    facade.openFilters.mockReturnValue(of(options.filterResult ?? null));
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
    | 'openFilters'
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
    openFilters: ReturnType<typeof vi.fn>;
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
        handleDetailActionAsync: vi.fn().mockResolvedValue(void 0),
        hasActiveFilters: vi.fn((onlyMine: boolean) => onlyMine),
        hasSearch: vi.fn((search: string | null) => search !== null && search.length > 0),
        hasVisibleRecipes: signal(true),
        isDeleting: signal(false),
        isFavoritesLoadingMore: signal(false),
        loadFavorites: vi.fn().mockReturnValue(of(void 0)),
        loadInitialOverview: vi.fn().mockReturnValue(of(void 0)),
        loadRecipes: vi.fn().mockReturnValue(of(void 0)),
        navigateToAddRecipeAsync: vi.fn().mockResolvedValue(true),
        openFilters: vi.fn().mockReturnValue(of(null)),
        pageSize: PAGE_SIZE,
        recipeData,
        recentRecipes: signal<Recipe[]>([]),
        removeFavorite: vi.fn().mockReturnValue(of(void 0)),
        showRecentSection: signal(false),
        toggleRecipeFavorite: vi.fn().mockReturnValue(of(void 0)),
    } as unknown as RecipeListFacadeMock;
}

async function flushPromisesAsync(): Promise<void> {
    await waitForAsyncTasksAsync();
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

function emptyRecipeFilters(): RecipeFilters {
    return {
        search: null,
        category: null,
        maxTotalTime: null,
        caloriesFrom: null,
        caloriesTo: null,
        hasImage: null,
    };
}

describe('RecipeListComponent search and recovery', () => {
    it('uses the latest search and reloads the first page when cleared', async () => {
        const { component } = setupComponent();
        await flushPromisesAsync();
        component['searchForm'].search().value.set('Rice');
        TestBed.tick();
        await flushPromisesAsync();
        expect(facade.loadRecipes).toHaveBeenCalledWith(1, PAGE_SIZE, { ...emptyRecipeFilters(), search: 'Rice' }, false);
        component['clearSearch']();
        TestBed.tick();
        await flushPromisesAsync();
        expect(component['searchModel']().search).toBe('');
        expect(facade.loadRecipes).toHaveBeenLastCalledWith(1, PAGE_SIZE, { ...emptyRecipeFilters(), search: '' }, false);
    });

    it('does not reload when the filter dialog is cancelled', () => {
        const { component } = setupComponent();
        component['openFilters']();
        expect(facade.loadRecipes).not.toHaveBeenCalled();
    });

    it('retries loading and delegates new recipe navigation', async () => {
        const { component } = setupComponent();
        component['retryLoad']();
        expect(facade.loadInitialOverview).toHaveBeenCalledTimes(2);
        await component['onAddRecipeClickAsync']();
        expect(facade.navigateToAddRecipeAsync).toHaveBeenCalledTimes(1);
    });

    it('keeps unavailable favorites from opening or adding a meal', () => {
        const { component } = setupComponent();
        facade.getFavoriteRecipe.mockReturnValue(of(null));
        component['openFavoriteRecipe'](createFavoriteRecipe());
        component['addFavoriteRecipeToMeal'](createFavoriteRecipe());
        expect(dialogService.open).not.toHaveBeenCalled();
        expect(facade.addToMeal).not.toHaveBeenCalled();
    });

    it('toggles mobile search and delegates favorite actions', () => {
        const { component } = setupComponent();
        component['toggleMobileSearch']();
        expect(component['isMobileSearchOpen']()).toBe(true);
        component['toggleMobileSearch']();
        expect(component['isMobileSearchOpen']()).toBe(false);
        component['onRecipeFavoriteToggle'](createRecipe());
        component['removeFavorite'](createFavoriteRecipe());
        component['loadFavorites']();
        expect(facade.toggleRecipeFavorite).toHaveBeenCalledWith(createRecipe());
        expect(facade.removeFavorite).toHaveBeenCalledWith(createFavoriteRecipe());
        expect(facade.loadFavorites).toHaveBeenCalledTimes(1);
    });
});

describe('RecipeListComponent presentation state', () => {
    it('distinguishes an empty collection from an empty search result', () => {
        const { component } = setupComponent();
        vi.spyOn(facade, 'hasVisibleRecipes').mockReturnValue(false);
        expect(component['emptyState']()).toBe('empty');
        component['searchForm'].search().value.set('Rice');
        expect(component['emptyState']()).toBe('no-results');
        expect(component['isMobileSearchVisible']()).toBe(true);
        expect(component['allRecipesSectionLabelKey']()).toBe('RECIPE_LIST.ALL_RECIPES');
        expect(component['pageIndex']()).toBe(0);
    });

    it('maps cover images and provides placeholders for absent images', () => {
        const { component } = setupComponent();
        facade.recentRecipes.set([createRecipe({ imageUrl: '/cover.jpg' })]);
        expect(component['recentRecipeItems']()[0].imageUrl).toBe('/cover.jpg');
        expect(component['allRecipeItems']()[0].imageUrl).toBeUndefined();
    });

    it('shows distinct filter chips for no photo, with photo, limits and ownership', () => {
        const { component } = setupComponent();
        component['searchModel'].update(model => ({
            ...model,
            onlyMine: true,
            category: 'Soup',
            maxTotalTime: 0,
            caloriesFrom: 0,
            caloriesTo: 120,
            hasImage: false,
        }));
        expect(component['activeFilterKeys']()).toEqual([
            'RECIPE_LIST.FILTER_MY_RECIPES',
            'RECIPE_LIST.FILTER_CATEGORY_ACTIVE',
            'RECIPE_LIST.FILTER_MAX_TOTAL_TIME_ACTIVE',
            'RECIPE_LIST.FILTER_CALORIES_ACTIVE',
            'RECIPE_LIST.FILTER_IMAGE_WITHOUT',
        ]);
        component['searchModel'].update(model => ({ ...model, hasImage: true }));
        expect(component['activeFilterKeys']()).toContain('RECIPE_LIST.FILTER_IMAGE_WITH');
        expect(component['activeFilterKeys']()).not.toContain('RECIPE_LIST.FILTER_IMAGE_WITHOUT');
    });

    it('does not reload when filters are unchanged', () => {
        const { component } = setupComponent();
        facade.openFilters.mockReturnValue(
            of({ onlyMine: false, category: null, maxTotalTime: null, caloriesFrom: null, caloriesTo: null, hasImage: null }),
        );
        component['openFilters']();
        expect(facade.loadRecipes).not.toHaveBeenCalled();
    });

    it('opens the favorites picker lazily without querying every favorite', () => {
        const { component } = setupComponent();
        component['toggleFavorites']();
        expect(dialogService.open).toHaveBeenCalledTimes(1);
        expect(facade.loadFavorites).not.toHaveBeenCalled();
    });
});
