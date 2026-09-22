import { TestBed } from '@angular/core/testing';
import { TranslateService } from '@ngx-translate/core';
import { FdUiDialogService } from 'fd-ui-kit/dialog/fd-ui-dialog.service';
import { FdUiToastService } from 'fd-ui-kit/toast/fd-ui-toast.service';
import { of, Subject, throwError } from 'rxjs';
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
    restore: ReturnType<typeof vi.fn>;
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
            restore: vi.fn().mockReturnValue(of(createFavorite())),
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
    registerFavoriteMutationTests();
    registerListFailureTests();
    registerListRaceTests();
    registerDetailActionTests();
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
                { limit: MEAL_LIST_OVERVIEW_FAVORITES_LIMIT, include: true },
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
            const restored = favorite;
            facade.mealData.items.set([createMeal({ id: favorite.mealId, isFavorite: false })]);
            favoriteMealService.restore.mockReturnValue(of(restored));
            favoriteMealService.getPage.mockReturnValue(of({ totalItems: NEXT_PAGE }));
            let result: boolean | undefined;
            facade.restoreFavoriteRequest(favorite).subscribe(value => {
                result = value;
            });
            expect(result).toBe(true);
            expect(favoriteMealService.restore).toHaveBeenCalledWith(favorite.id);
            expect(facade.favoriteTotalCount()).toBe(NEXT_PAGE);
            expect(facade.mealData.items()[0]).toMatchObject({ isFavorite: true, favoriteMealId: restored.id });
        });
        it('does not invent a name for unnamed favorites', () => {
            const favorite = createFavorite({ name: null });
            facade.restoreFavoriteRequest(favorite).subscribe();
            expect(favoriteMealService.restore).toHaveBeenCalledWith(favorite.id);
        });
        it('keeps diary state unchanged when restoration fails', () => {
            const favorite = createFavorite();
            const meal = createMeal({ isFavorite: false });
            facade.mealData.items.set([meal]);
            facade.favoriteTotalCount.set(NEXT_PAGE);
            favoriteMealService.restore.mockReturnValue(throwError(() => new Error('offline')));
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

function registerFavoriteMutationTests(): void {
    it('removes a known favorite and preserves other meal cards', () => {
        const meal = createMeal({ isFavorite: true, favoriteMealId: 'favorite-1' });
        const other = createMeal({ id: 'other' });
        facade.mealData.items.set([meal, other]);
        facade.toggleMealFavorite(meal);
        expect(favoriteMealService.remove).toHaveBeenCalledExactlyOnceWith('favorite-1');
        expect(facade.mealData.items()).toEqual([{ ...meal, isFavorite: false, favoriteMealId: null }, other]);
        expect(facade.favoriteLoadingIds().size).toBe(0);
    });
    it.each([null, undefined, ''])('rejects removing a favorite with missing id %s', favoriteMealId => {
        facade.toggleMealFavorite(createMeal({ isFavorite: true, favoriteMealId }));
        expect(favoriteMealService.remove).not.toHaveBeenCalled();
        expect(facade.favoriteLoadingIds().size).toBe(0);
        expect(toastService.error).toHaveBeenCalled();
    });
    it.each([true, false])('preserves state and unlocks after favorite failure (was favorite %s)', isFavorite => {
        const meal = createMeal({ isFavorite, favoriteMealId: 'favorite-1' });
        facade.mealData.items.set([meal]);
        favoriteMealService.remove.mockReturnValue(throwError(() => new Error('offline')));
        favoriteMealService.add.mockReturnValue(throwError(() => new Error('offline')));
        facade.toggleMealFavorite(meal);
        expect(facade.mealData.items()).toEqual([meal]);
        expect(facade.favoriteLoadingIds().size).toBe(0);
        expect(toastService.error).toHaveBeenCalled();
    });
    it('blocks duplicate toggles and releases the pending subscription on destruction', () => {
        const request = new Subject<FavoriteMeal>();
        favoriteMealService.add.mockReturnValue(request);
        facade.toggleMealFavorite(createMeal());
        facade.toggleMealFavorite(createMeal());
        expect(favoriteMealService.add).toHaveBeenCalledTimes(1);
        expect(facade.favoriteLoadingIds().has('meal-1')).toBe(true);
        TestBed.resetTestingModule();
        expect(request.observed).toBe(false);
        expect(facade.favoriteLoadingIds().size).toBe(0);
    });
}

function registerListFailureTests(): void {
    it('clears obsolete data on initial failure and recovers on retry', () => {
        facade.favorites.set([createFavorite()]);
        facade.favoriteTotalCount.set(1);
        facade.daySummaries.set([{ date: '2026-05-05', totalCalories: 1, mealCount: 1 }]);
        mealService.queryOverview.mockReturnValueOnce(throwError(() => new Error('offline')));
        facade.loadInitialOverview(emptyMealFilters()).subscribe();
        expect(facade.favorites()).toEqual([]);
        expect(facade.favoriteTotalCount()).toBe(0);
        expect(facade.daySummaries()).toEqual([]);
        expect(facade.errorKey()).not.toBeNull();
        facade.loadInitialOverview(emptyMealFilters()).subscribe();
        expect(facade.errorKey()).toBeNull();
    });
    it('preserves favorite count and cards when removal fails', () => {
        facade.favoriteTotalCount.set(1);
        facade.favorites.set([createFavorite()]);
        favoriteMealService.remove.mockReturnValue(throwError(() => new Error('offline')));
        facade.removeFavoriteRequest(createFavorite()).subscribe(result => {
            expect(result).toBe(false);
        });
        expect(facade.favoriteTotalCount()).toBe(1);
        expect(facade.favorites()).toEqual([createFavorite()]);
    });
    it('does not invalidate nutrition or reload after failed deletion', () => {
        const invalidation = vi.spyOn(TestBed.inject(NutritionDataInvalidationService), 'reportMealMutation');
        mealService.deleteById.mockReturnValue(throwError(() => new Error('offline')));
        facade.deleteMeal('meal-1', emptyMealFilters()).subscribe(result => {
            expect(result).toBe(false);
        });
        expect(invalidation).not.toHaveBeenCalled();
        expect(mealService.queryOverview).not.toHaveBeenCalled();
    });
    it('preserves zero and false structured filter values', () => {
        facade
            .loadMeals(1, emptyMealFilters({ caloriesFrom: 0, caloriesTo: 0, hasImage: false, hasAiSession: false, mealTypes: ['Dinner'] }))
            .subscribe();
        expect(mealService.queryOverview).toHaveBeenCalledWith(
            1,
            MEAL_LIST_PAGE_SIZE,
            expect.objectContaining({ caloriesFrom: 0, caloriesTo: 0, hasImage: false, hasAiSession: false, mealTypes: 'Dinner' }),
            expect.anything(),
        );
    });
}

function registerListRaceTests(): void {
    it('cancels an initial overview when a newer filtered page is requested', () => {
        const old = new Subject<MealOverview>();
        const current = new Subject<MealOverview>();
        mealService.queryOverview.mockReturnValueOnce(old).mockReturnValueOnce(current);
        facade.loadInitialOverview(emptyMealFilters()).subscribe();
        facade.loadMeals(2, emptyMealFilters({ hasImage: true })).subscribe();
        expect(old.observed).toBe(false);
        current.next(createOverview([createMeal({ id: 'new' })], 2));
        current.complete();
        old.next(createOverview([createMeal({ id: 'old' })]));
        expect(facade.mealData.items()[0].id).toBe('new');
        expect(facade.currentPageIndex()).toBe(1);
    });
}

function registerDetailActionTests(): void {
    it.each(['Repeat', 'Delete', 'FavoriteChanged'] as const)('settles %s when its refresh is superseded', async action => {
        const dialogs = TestBed.inject(FdUiDialogService);
        vi.spyOn(dialogs, 'open').mockReturnValue({ afterClosed: () => of({ action, id: 'meal-1' }) } as unknown as ReturnType<
            typeof dialogs.open
        >);
        const old = new Subject<MealOverview>();
        mealService.queryOverview.mockReturnValueOnce(old).mockReturnValue(of(createOverview([])));
        const pending = facade.handleMealDetailsAsync(createMeal(), emptyMealFilters());
        await vi.waitFor(() => {
            expect(old.observed).toBe(true);
        });
        facade.loadMeals(2, emptyMealFilters()).subscribe();
        await expect(pending).resolves.toBe(false);
        expect(old.observed).toBe(false);
    });

    it.each(['Edit', 'Repeat', 'Delete', 'FavoriteChanged', undefined] as const)('routes detail result %s', async action => {
        const dialogs = TestBed.inject(FdUiDialogService);
        vi.spyOn(dialogs, 'open').mockReturnValue({
            afterClosed: () => of(action === undefined ? undefined : { action, id: 'meal-1' }),
        } as unknown as ReturnType<typeof dialogs.open>);
        const changed = await facade.handleMealDetailsAsync(createMeal(), emptyMealFilters());
        expect(changed).toBe(action === 'Repeat' || action === 'Delete');
        if (action === 'Edit') {
            expect(vi.spyOn(TestBed.inject(NavigationService), 'navigateToMealEditAsync')).toHaveBeenCalledWith('meal-1');
        }
        if (action === 'Repeat') {
            expect(mealService.repeat).toHaveBeenCalled();
        }
        if (action === 'Delete') {
            expect(mealService.deleteById).toHaveBeenCalledWith('meal-1');
        }
        if (action === 'FavoriteChanged') {
            expect(favoriteMealService.getPage).toHaveBeenCalledWith(1, 1);
        }
    });
}
