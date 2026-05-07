import { TestBed } from '@angular/core/testing';
import { TranslateService } from '@ngx-translate/core';
import { FdUiToastService } from 'fd-ui-kit/toast/fd-ui-toast.service';
import { of, throwError } from 'rxjs';
import { beforeEach, describe, expect, it, vi } from 'vitest';

import { type PageOf } from '../../../shared/models/page-of.data';
import { FavoriteMealService } from '../api/favorite-meal.service';
import { MealService } from '../api/meal.service';
import { type FavoriteMeal, type Meal, type MealOverview } from '../models/meal.data';
import { MealListFacade } from './meal-list.facade';

function createMeal(overrides: Partial<Meal> = {}): Meal {
    return {
        id: 'meal-1',
        date: '2026-05-05T10:00:00Z',
        mealType: 'breakfast',
        comment: null,
        imageUrl: null,
        imageAssetId: null,
        totalCalories: 500,
        totalProteins: 30,
        totalFats: 20,
        totalCarbs: 50,
        totalFiber: 5,
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
        totalCalories: 500,
        totalProteins: 30,
        totalFats: 20,
        totalCarbs: 50,
        itemCount: 2,
        ...overrides,
    };
}

function createPageOf(meals: Meal[], page = 1): PageOf<Meal> {
    return {
        data: meals,
        page,
        limit: 10,
        totalItems: meals.length,
        totalPages: 1,
    };
}

function createOverview(meals: Meal[], favorites: FavoriteMeal[] = []): MealOverview {
    return {
        allConsumptions: createPageOf(meals),
        favoriteItems: favorites,
        favoriteTotalCount: favorites.length,
    };
}

describe('MealListFacade', () => {
    let facade: MealListFacade;
    let mealService: {
        queryOverview: ReturnType<typeof vi.fn>;
        query: ReturnType<typeof vi.fn>;
        repeat: ReturnType<typeof vi.fn>;
        deleteById: ReturnType<typeof vi.fn>;
    };
    let favoriteMealService: {
        getAll: ReturnType<typeof vi.fn>;
        remove: ReturnType<typeof vi.fn>;
    };
    let toastService: { error: ReturnType<typeof vi.fn> };

    beforeEach(() => {
        mealService = {
            queryOverview: vi.fn().mockReturnValue(of(createOverview([]))),
            query: vi.fn().mockReturnValue(of(createPageOf([]))),
            repeat: vi.fn().mockReturnValue(of(createMeal())),
            deleteById: vi.fn().mockReturnValue(of(void 0)),
        };
        favoriteMealService = {
            getAll: vi.fn().mockReturnValue(of([])),
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

    it('loads overview and selected date range using local day boundaries', () => {
        const meal = createMeal();
        const favorite = createFavorite();
        mealService.queryOverview.mockReturnValue(of(createOverview([meal], [favorite])));
        const start = new Date(2026, 4, 5);
        const end = new Date(2026, 4, 6);

        facade.loadInitialOverview({ start, end }).subscribe();

        expect(mealService.queryOverview).toHaveBeenCalledWith(
            1,
            10,
            {
                dateFrom: new Date(2026, 4, 5, 0, 0, 0, 0).toISOString(),
                dateTo: new Date(2026, 4, 6, 23, 59, 59, 999).toISOString(),
            },
            10,
        );
        expect(facade.consumptionData.items()).toEqual([meal]);
        expect(facade.favorites()).toEqual([favorite]);
        expect(facade.favoriteTotalCount()).toBe(1);
        expect(facade.errorKey()).toBeNull();
    });

    it('sets retry error state when list load fails', () => {
        mealService.query.mockReturnValue(throwError(() => new Error('load failed')));

        facade.loadConsumptions(1, null).subscribe();

        expect(facade.errorKey()).toBe('ERRORS.LOAD_FAILED_TITLE');
        expect(facade.consumptionData.items()).toEqual([]);
        expect(facade.consumptionData.isLoading()).toBe(false);
    });

    it('shows a toast when favorites load fails', () => {
        favoriteMealService.getAll.mockReturnValue(throwError(() => new Error('favorites failed')));

        facade.loadFavorites();

        expect(toastService.error).toHaveBeenCalledWith('CONSUMPTION_LIST.OPERATION_ERROR_MESSAGE');
        expect(facade.favorites()).toEqual([]);
        expect(facade.isFavoritesLoadingMore()).toBe(false);
    });

    it('repeats meal and reloads the current page', () => {
        mealService.query.mockReturnValue(of(createPageOf([createMeal()], 2)));
        facade.currentPageIndex.set(1);
        let result = false;

        facade.repeatMeal('meal-1', '2026-05-05T08:30:00.000Z', 'BREAKFAST', null).subscribe(value => {
            result = value;
        });

        expect(result).toBe(true);
        expect(mealService.repeat).toHaveBeenCalledWith('meal-1', '2026-05-05T08:30:00.000Z', 'BREAKFAST');
        expect(mealService.query).toHaveBeenCalledWith(2, 10, { dateFrom: undefined, dateTo: undefined });
    });

    it('returns false and shows a toast when repeat fails', () => {
        mealService.repeat.mockReturnValue(throwError(() => new Error('repeat failed')));
        let result = true;

        facade.repeatMeal('meal-1', '2026-05-05T08:30:00.000Z', 'BREAKFAST', null).subscribe(value => {
            result = value;
        });

        expect(result).toBe(false);
        expect(toastService.error).toHaveBeenCalledWith('CONSUMPTION_LIST.OPERATION_ERROR_MESSAGE');
        expect(mealService.query).not.toHaveBeenCalled();
    });

    it('deletes meal and reloads the current page', () => {
        facade.currentPageIndex.set(1);
        let result = false;

        facade.deleteMeal('meal-1', null).subscribe(value => {
            result = value;
        });

        expect(result).toBe(true);
        expect(mealService.deleteById).toHaveBeenCalledWith('meal-1');
        expect(mealService.query).toHaveBeenCalledWith(2, 10, { dateFrom: undefined, dateTo: undefined });
    });

    it('removes favorite and syncs meal card state', () => {
        const favorite = createFavorite();
        const meal = createMeal({ id: favorite.mealId, isFavorite: true, favoriteMealId: favorite.id });
        facade.consumptionData.setData(createPageOf([meal]));
        facade.favorites.set([favorite]);
        facade.favoriteTotalCount.set(1);

        facade.removeFavorite(favorite);

        expect(favoriteMealService.remove).toHaveBeenCalledWith(favorite.id);
        expect(facade.favorites()).toEqual([]);
        expect(facade.favoriteTotalCount()).toBe(0);
        expect(facade.consumptionData.items()[0]).toMatchObject({ isFavorite: false, favoriteMealId: null });
    });
});
