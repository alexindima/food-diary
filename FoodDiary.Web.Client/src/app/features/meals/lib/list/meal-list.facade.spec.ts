import { TestBed } from '@angular/core/testing';
import { TranslateService } from '@ngx-translate/core';
import { FdUiDialogService } from 'fd-ui-kit/dialog/fd-ui-dialog.service';
import { FdUiToastService } from 'fd-ui-kit/toast/fd-ui-toast.service';
import { of, throwError } from 'rxjs';
import { beforeEach, describe, expect, it, vi } from 'vitest';

import { NavigationService } from '../../../../services/navigation.service';
import type { PageOf } from '../../../../shared/models/page-of.data';
import { NutritionDataInvalidationService } from '../../../../shared/state/nutrition-data-invalidation.service';
import { FavoriteMealService } from '../../api/favorite-meal.service';
import { MealService } from '../../api/meal.service';
import type { FavoriteMeal, Meal, MealOverview } from '../../models/meal.data';
import { MEAL_LIST_OVERVIEW_FAVORITES_LIMIT, MEAL_LIST_PAGE_SIZE } from './meal-list.config';
import { MealListFacade, type MealListStructuredFilters } from './meal-list.facade';

const DEFAULT_CALORIES = 500;
const DEFAULT_PROTEINS = 30;
const DEFAULT_FATS = 20;
const DEFAULT_CARBS = 50;
const DEFAULT_FIBER = 5;
const DEFAULT_ITEM_COUNT = 2;
const DEFAULT_PAGE = 1;
const NEXT_PAGE = 2;
const TEST_YEAR = 2026;
const MAY_INDEX = 4;
const START_DAY = 5;
const END_DAY = 6;
const END_OF_DAY_HOUR = 23;
const END_OF_DAY_MINUTE = 59;
const END_OF_DAY_SECOND = 59;
const END_OF_DAY_MS = 999;

let facade: MealListFacade;
let mealService: {
    queryOverview: ReturnType<typeof vi.fn>;
    query: ReturnType<typeof vi.fn>;
    repeat: ReturnType<typeof vi.fn>;
    deleteById: ReturnType<typeof vi.fn>;
};
let favoriteMealService: {
    add: ReturnType<typeof vi.fn>;
    getPage: ReturnType<typeof vi.fn>;
    remove: ReturnType<typeof vi.fn>;
};
let toastService: { error: ReturnType<typeof vi.fn> };

function createMeal(overrides: Partial<Meal> = {}): Meal {
    return {
        id: 'meal-1',
        date: '2026-05-05T10:00:00Z',
        mealType: 'breakfast',
        comment: null,
        imageUrl: null,
        imageAssetId: null,
        totalCalories: DEFAULT_CALORIES,
        totalProteins: DEFAULT_PROTEINS,
        totalFats: DEFAULT_FATS,
        totalCarbs: DEFAULT_CARBS,
        totalFiber: DEFAULT_FIBER,
        totalAlcohol: 0,
        isNutritionAutoCalculated: true,
        preMealSatietyLevel: null,
        postMealSatietyLevel: null,
        items: [],
        aiSessions: [],
        ...overrides,
    };
}

function createFavorite(overrides: Partial<FavoriteMeal> = {}): FavoriteMeal {
    return {
        id: 'favorite-1',
        mealId: 'meal-1',
        name: 'Breakfast',
        createdAtUtc: '2026-05-05T10:00:00Z',
        mealDate: '2026-05-05T10:00:00Z',
        mealType: 'breakfast',
        totalCalories: DEFAULT_CALORIES,
        totalProteins: DEFAULT_PROTEINS,
        totalFats: DEFAULT_FATS,
        totalCarbs: DEFAULT_CARBS,
        itemCount: DEFAULT_ITEM_COUNT,
        ...overrides,
    };
}

function createPageOf(meals: Meal[], page = DEFAULT_PAGE): PageOf<Meal> {
    return {
        data: meals,
        page,
        limit: MEAL_LIST_PAGE_SIZE,
        totalItems: meals.length,
        totalPages: DEFAULT_PAGE,
    };
}

function createOverview(meals: Meal[], favorites: FavoriteMeal[] | number = []): MealOverview {
    return {
        allMeals: createPageOf(meals, typeof favorites === 'number' ? favorites : DEFAULT_PAGE),
        favoriteItems: typeof favorites === 'number' ? [] : favorites,
        favoriteTotalCount: typeof favorites === 'number' ? 0 : favorites.length,
    };
}

function emptyMealFilters(overrides: Partial<MealListStructuredFilters> = {}): MealListStructuredFilters {
    return {
        dateRange: null,
        mealTypes: [],
        caloriesFrom: null,
        caloriesTo: null,
        hasImage: null,
        hasAiSession: null,
        ...overrides,
    };
}

describe('MealListFacade', () => {
    beforeEach(() => {
        mealService = {
            queryOverview: vi.fn().mockReturnValue(of(createOverview([]))),
            query: vi.fn().mockReturnValue(of(createPageOf([]))),
            repeat: vi.fn().mockReturnValue(of(createMeal())),
            deleteById: vi.fn().mockReturnValue(of(void 0)),
        };
        favoriteMealService = {
            add: vi.fn().mockReturnValue(of(createFavorite())),
            getPage: vi.fn().mockReturnValue(of({ totalItems: 0 })),
            remove: vi.fn().mockReturnValue(of(void 0)),
        };
        toastService = {
            error: vi.fn(),
        };

        TestBed.configureTestingModule({
            providers: [
                MealListFacade,
                { provide: MealService, useValue: mealService },
                { provide: FavoriteMealService, useValue: favoriteMealService },
                { provide: FdUiToastService, useValue: toastService },
                { provide: FdUiDialogService, useValue: { open: vi.fn() } },
                { provide: NavigationService, useValue: { navigateToMealEditAsync: vi.fn() } },
                {
                    provide: TranslateService,
                    useValue: {
                        instant: vi.fn((key: string) => key),
                    },
                },
            ],
        });

        facade = TestBed.inject(MealListFacade);
    });

    registerLoadTests();
    registerMutationTests();
    registerUndoTests();
});

function registerLoadTests(): void {
    describe('loading', () => {
        it('keeps full-day totals separate from page meals and preserves favorites on pagination', () => {
            const summary = { date: '2026-05-05', totalCalories: 2500, mealCount: 8 };
            mealService.queryOverview.mockReturnValueOnce(
                of({ ...createOverview([createMeal()], [createFavorite()]), daySummaries: [summary] }),
            );
            facade.loadInitialOverview(emptyMealFilters()).subscribe();
            expect(facade.daySummaries()).toEqual([summary]);
            mealService.queryOverview.mockReturnValueOnce(of({ ...createOverview([createMeal()], NEXT_PAGE), daySummaries: [summary] }));
            facade.loadMeals(NEXT_PAGE, emptyMealFilters()).subscribe();
            expect(facade.daySummaries()).toEqual([summary]);
            expect(facade.favorites()).toHaveLength(1);
            mealService.queryOverview.mockReturnValueOnce(throwError(() => new Error('offline')));
            facade.loadMeals(NEXT_PAGE, emptyMealFilters()).subscribe();
            expect(facade.daySummaries()).toEqual([]);
        });

        it('loads overview and selected date range using local day boundaries', () => {
            const meal = createMeal();
            const favorite = createFavorite();
            mealService.queryOverview.mockReturnValue(of(createOverview([meal], [favorite])));
            const start = new Date(TEST_YEAR, MAY_INDEX, START_DAY);
            const end = new Date(TEST_YEAR, MAY_INDEX, END_DAY);

            facade.loadInitialOverview(emptyMealFilters({ dateRange: { start, end } })).subscribe();

            expect(mealService.queryOverview).toHaveBeenCalledWith(
                1,
                MEAL_LIST_PAGE_SIZE,
                {
                    dateFrom: new Date(TEST_YEAR, MAY_INDEX, START_DAY, 0, 0, 0, 0).toISOString(),
                    dateTo: new Date(
                        TEST_YEAR,
                        MAY_INDEX,
                        END_DAY,
                        END_OF_DAY_HOUR,
                        END_OF_DAY_MINUTE,
                        END_OF_DAY_SECOND,
                        END_OF_DAY_MS,
                    ).toISOString(),
                },
                { limit: MEAL_LIST_OVERVIEW_FAVORITES_LIMIT },
            );
            expect(facade.mealData.items()).toEqual([meal]);
            expect(facade.favorites()).toEqual([favorite]);
            expect(facade.favoriteTotalCount()).toBe(1);
            expect(facade.errorKey()).toBeNull();
        });

        it('sets retry error state when list load fails', () => {
            mealService.queryOverview.mockReturnValue(throwError(() => new Error('load failed')));

            facade.loadMeals(1, emptyMealFilters()).subscribe();

            expect(facade.errorKey()).toBe('ERRORS.LOAD_FAILED_TITLE');
            expect(facade.mealData.items()).toEqual([]);
            expect(facade.mealData.isLoading()).toBe(false);
        });

        it('shows a toast when favorites load fails', () => {
            favoriteMealService.getPage.mockReturnValue(throwError(() => new Error('favorites failed')));

            facade.loadFavorites();

            expect(toastService.error).toHaveBeenCalledWith('MEAL_LIST.OPERATION_ERROR_MESSAGE');
            expect(facade.favorites()).toEqual([]);
            expect(facade.isFavoritesLoadingMore()).toBe(false);
        });
    });
}

function registerMutationTests(): void {
    describe('mutations', () => {
        it('repeats meal and reloads the current page', () => {
            mealService.queryOverview.mockReturnValue(of(createOverview([createMeal()], NEXT_PAGE)));
            facade.currentPageIndex.set(1);
            let result = false;

            facade.repeatMeal('meal-1', '2026-05-05T08:30:00.000Z', 'BREAKFAST', emptyMealFilters()).subscribe(value => {
                result = value;
            });

            expect(result).toBe(true);
            expect(TestBed.inject(NutritionDataInvalidationService).dashboardVersion()).toBe(1);
            expect(mealService.repeat).toHaveBeenCalledWith('meal-1', '2026-05-05T08:30:00.000Z', 'BREAKFAST');
            expect(mealService.queryOverview).toHaveBeenCalledWith(
                NEXT_PAGE,
                MEAL_LIST_PAGE_SIZE,
                { dateFrom: undefined, dateTo: undefined },
                { limit: MEAL_LIST_OVERVIEW_FAVORITES_LIMIT, include: false },
            );
        });

        it('returns false and shows a toast when repeat fails', () => {
            mealService.repeat.mockReturnValue(throwError(() => new Error('repeat failed')));
            let result = true;

            facade.repeatMeal('meal-1', '2026-05-05T08:30:00.000Z', 'BREAKFAST', emptyMealFilters()).subscribe(value => {
                result = value;
            });

            expect(result).toBe(false);
            expect(toastService.error).toHaveBeenCalledWith('MEAL_LIST.OPERATION_ERROR_MESSAGE');
            expect(mealService.query).not.toHaveBeenCalled();
        });

        it('deletes meal and reloads the current page', () => {
            facade.currentPageIndex.set(1);
            let result = false;

            facade.deleteMeal('meal-1', emptyMealFilters()).subscribe(value => {
                result = value;
            });

            expect(result).toBe(true);
            expect(TestBed.inject(NutritionDataInvalidationService).dashboardVersion()).toBe(1);
            expect(mealService.deleteById).toHaveBeenCalledWith('meal-1');
            expect(mealService.queryOverview).toHaveBeenCalledWith(
                NEXT_PAGE,
                MEAL_LIST_PAGE_SIZE,
                { dateFrom: undefined, dateTo: undefined },
                { limit: MEAL_LIST_OVERVIEW_FAVORITES_LIMIT, include: false },
            );
        });

        it('removes favorite and syncs meal card state', () => {
            const favorite = createFavorite();
            const meal = createMeal({ id: favorite.mealId, isFavorite: true, favoriteMealId: favorite.id });
            facade.mealData.setData(createPageOf([meal]));
            facade.favorites.set([favorite]);
            facade.favoriteTotalCount.set(1);

            facade.removeFavorite(favorite);

            expect(favoriteMealService.remove).toHaveBeenCalledWith(favorite.id);
            expect(facade.favorites()).toEqual([]);
            expect(facade.favoriteTotalCount()).toBe(0);
            expect(facade.mealData.items()[0]).toMatchObject({ isFavorite: false, favoriteMealId: null });
        });
    });
}

function registerUndoTests(): void {
    describe('restoring favorites', () => {
        it('preserves the saved name and refreshes the count and meal favorite identifier', () => {
            const favorite = createFavorite({ name: 'My saved lunch' });
            const restored = { ...favorite, id: 'new-favorite-id' };
            facade.mealData.items.set([createMeal({ id: favorite.mealId, isFavorite: false })]);
            favoriteMealService.add.mockReturnValue(of(restored));
            favoriteMealService.getPage.mockReturnValue(of({ totalItems: NEXT_PAGE }));
            let result: boolean | undefined;
            facade.restoreFavoriteRequest(favorite).subscribe(value => {
                result = value;
            });
            expect(result).toBe(true);
            expect(favoriteMealService.add).toHaveBeenCalledWith(favorite.mealId, favorite.name);
            expect(facade.favoriteTotalCount()).toBe(NEXT_PAGE);
            expect(facade.mealData.items()[0]).toMatchObject({ isFavorite: true, favoriteMealId: restored.id });
        });
        it('does not invent a name for unnamed favorites', () => {
            const favorite = createFavorite({ name: null });
            facade.restoreFavoriteRequest(favorite).subscribe();
            expect(favoriteMealService.add).toHaveBeenCalledWith(favorite.mealId, undefined);
        });
        it('keeps diary state unchanged when restoration fails', () => {
            const favorite = createFavorite();
            const meal = createMeal({ isFavorite: false });
            facade.mealData.items.set([meal]);
            facade.favoriteTotalCount.set(NEXT_PAGE);
            favoriteMealService.add.mockReturnValue(throwError(() => new Error('offline')));
            let result: boolean | undefined;
            facade.restoreFavoriteRequest(favorite).subscribe(value => {
                result = value;
            });
            expect(result).toBe(false);
            expect(facade.mealData.items()).toEqual([meal]);
            expect(facade.favoriteTotalCount()).toBe(NEXT_PAGE);
            expect(favoriteMealService.getPage).not.toHaveBeenCalled();
        });
    });
}
