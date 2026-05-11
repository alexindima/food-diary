import { BreakpointObserver } from '@angular/cdk/layout';
import { provideHttpClient } from '@angular/common/http';
import { provideHttpClientTesting } from '@angular/common/http/testing';
import { type ComponentFixture, TestBed } from '@angular/core/testing';
import { TranslateModule } from '@ngx-translate/core';
import { FdUiDialogService } from 'fd-ui-kit/dialog/fd-ui-dialog.service';
import { FdUiToastService } from 'fd-ui-kit/toast/fd-ui-toast.service';
import { of, throwError } from 'rxjs';
import { afterEach, beforeEach, describe, expect, it, vi } from 'vitest';

import { LocalizationService } from '../../../../services/localization.service';
import { NavigationService } from '../../../../services/navigation.service';
import { AiFoodService } from '../../../../shared/api/ai-food.service';
import type { PageOf } from '../../../../shared/models/page-of.data';
import { FavoriteMealService } from '../../api/favorite-meal.service';
import { MealService } from '../../api/meal.service';
import type { FavoriteMeal, Meal } from '../../models/meal.data';
import type { MealOverview } from '../../models/meal.data';
import { MealListComponent } from './meal-list.component';

const PAGE_LIMIT = 10;
const NEXT_PAGE_INDEX = 1;
const NEXT_PAGE_NUMBER = 2;
const LOCAL_GROUP_YEAR = 2024;
const LOCAL_GROUP_MONTH = 2;
const LOCAL_GROUP_DAY_15 = 15;
const LOCAL_GROUP_DAY_16 = 16;
const MORNING_HOUR = 10;
const AFTERNOON_HOUR = 14;
const CURRENT_YEAR = 2026;
const MAY_MONTH_INDEX = 4;
const MAY_4 = 4;
const MAY_5 = 5;
const MAY_6 = 6;
const LATE_MEAL_HOUR = 22;
const LATE_MEAL_MINUTE = 48;
const HALF_PAST_MIDNIGHT_MINUTES = 30;
const END_OF_DAY_HOURS = 23;
const END_OF_DAY_MINUTES = 59;
const END_OF_DAY_SECONDS = 59;
const END_OF_DAY_MS = 999;

interface TestContext {
    component: () => MealListComponent;
    fixture: () => ComponentFixture<MealListComponent>;
    mockMealService: typeof mockMealService;
    mockNavigationService: typeof mockNavigationService;
    mockFdDialogService: typeof mockFdDialogService;
    mockToastService: typeof mockToastService;
    mockFavoriteMealService: typeof mockFavoriteMealService;
}

function createMockMeal(overrides: Partial<Meal> = {}): Meal {
    return {
        id: '1',
        date: '2024-03-15T10:00:00Z',
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

function createPageOf(meals: Meal[], page = 1): PageOf<Meal> {
    return {
        data: meals,
        page,
        limit: PAGE_LIMIT,
        totalPages: 1,
        totalItems: meals.length,
    };
}

function createOverview(meals: Meal[]): MealOverview {
    return {
        allConsumptions: createPageOf(meals),
        favoriteItems: [],
        favoriteTotalCount: 0,
    };
}

const mockMealService = {
    queryOverview: vi.fn().mockReturnValue(of(createOverview([]))),
    query: vi.fn().mockReturnValue(of(createPageOf([]))),
    repeat: vi.fn().mockReturnValue(of(createMockMeal())),
    deleteById: vi.fn().mockReturnValue(of(void 0)),
};

const mockNavigationService = {
    navigateToConsumptionAddAsync: vi.fn().mockResolvedValue(true),
    navigateToConsumptionEditAsync: vi.fn().mockResolvedValue(true),
};

const mockDialogRef = {
    afterClosed: vi.fn().mockReturnValue(of(undefined)),
};

const mockFdDialogService = {
    open: vi.fn().mockReturnValue(mockDialogRef),
};

const mockToastService = {
    error: vi.fn(),
};

const mockBreakpointObserver = {
    observe: vi.fn().mockReturnValue(of({ matches: false, breakpoints: {} })),
};

const mockFavoriteMealService = {
    getAll: vi.fn().mockReturnValue(of([])),
    remove: vi.fn().mockReturnValue(of(void 0)),
};

const mockAiFoodService = {
    parseFoodText: vi.fn().mockReturnValue(of({ items: [] })),
    calculateNutrition: vi.fn().mockReturnValue(of({ calories: 0, protein: 0, fat: 0, carbs: 0, fiber: 0, alcohol: 0, items: [] })),
};

const mockLocalizationService = {
    getCurrentLanguage: vi.fn().mockReturnValue('en'),
};

describe('MealListComponent', () => {
    let component: MealListComponent;
    let fixture: ComponentFixture<MealListComponent>;

    beforeEach(async () => {
        vi.clearAllMocks();
        mockMealService.queryOverview.mockReturnValue(of(createOverview([])));
        mockMealService.query.mockReturnValue(of(createPageOf([])));
        mockMealService.repeat.mockReturnValue(of(createMockMeal()));
        mockMealService.deleteById.mockReturnValue(of(void 0));
        mockFavoriteMealService.getAll.mockReturnValue(of([]));
        mockFavoriteMealService.remove.mockReturnValue(of(void 0));
        mockToastService.error.mockClear();

        Object.defineProperty(window, 'matchMedia', {
            writable: true,
            value: vi.fn().mockImplementation((query: string) => ({
                matches: false,
                media: query,
                onchange: null,
                addListener: vi.fn(),
                removeListener: vi.fn(),
                addEventListener: vi.fn(),
                removeEventListener: vi.fn(),
                dispatchEvent: vi.fn(),
            })),
        });

        await TestBed.configureTestingModule({
            imports: [MealListComponent, TranslateModule.forRoot()],
            providers: [
                provideHttpClient(),
                provideHttpClientTesting(),
                { provide: MealService, useValue: mockMealService },
                { provide: NavigationService, useValue: mockNavigationService },
                { provide: FdUiDialogService, useValue: mockFdDialogService },
                { provide: FdUiToastService, useValue: mockToastService },
                { provide: BreakpointObserver, useValue: mockBreakpointObserver },
                { provide: FavoriteMealService, useValue: mockFavoriteMealService },
                { provide: AiFoodService, useValue: mockAiFoodService },
                { provide: LocalizationService, useValue: mockLocalizationService },
            ],
        }).compileComponents();

        fixture = TestBed.createComponent(MealListComponent);
        component = fixture.componentInstance;
    });

    afterEach(() => {
        vi.useRealTimers();
    });

    const context: TestContext = {
        component: () => component,
        fixture: () => fixture,
        mockMealService,
        mockNavigationService,
        mockFdDialogService,
        mockToastService,
        mockFavoriteMealService,
    };

    it('should create', () => {
        expect(component).toBeTruthy();
    });

    registerLoadingTests(context);
    registerNavigationTests(context);
    registerGroupingTests(context);
    registerRangeTests(context);
    registerFavoriteTests(context);
    registerDialogTests(context);
});

function registerLoadingTests(context: TestContext): void {
    describe('loading', () => {
        it('should load overview on init', () => {
            const meals = [createMockMeal()];
            context.mockMealService.queryOverview.mockReturnValue(of(createOverview(meals)));

            context.fixture().detectChanges();

            expect(context.mockMealService.queryOverview).toHaveBeenCalledWith(
                1,
                PAGE_LIMIT,
                { dateFrom: undefined, dateTo: undefined },
                PAGE_LIMIT,
            );
        });

        it('should expose load errors for retry state', () => {
            context.mockMealService.query.mockReturnValue(throwError(() => new Error('Network error')));

            context.component().loadConsumptions(1).subscribe();

            expect(context.component().errorKey()).toBe('ERRORS.LOAD_FAILED_TITLE');
            expect(context.component().consumptionData.isLoading()).toBe(false);
            expect(context.component().consumptionData.items()).toEqual([]);
        });
    });
}

function registerNavigationTests(context: TestContext): void {
    describe('navigation', () => {
        it('should navigate to add meal', async () => {
            await context.component().goToMealAddAsync();

            expect(context.mockNavigationService.navigateToConsumptionAddAsync).toHaveBeenCalled();
        });

        it('should handle page change', () => {
            context.fixture().detectChanges();
            setPageContainerScrollMock(context.fixture());

            const meals = [createMockMeal()];
            context.mockMealService.query.mockReturnValue(of(createPageOf(meals, NEXT_PAGE_NUMBER)));

            context.component().onPageChange(NEXT_PAGE_INDEX);

            expect(context.component().currentPageIndex()).toBe(NEXT_PAGE_INDEX);
            expect(context.mockMealService.query).toHaveBeenCalledWith(NEXT_PAGE_NUMBER, PAGE_LIMIT, expect.any(Object));
        });
    });
}

function registerGroupingTests(context: TestContext): void {
    describe('grouping', () => {
        it('should group consumptions by date', () => {
            const meal1 = createMockMeal({
                id: '1',
                date: new Date(LOCAL_GROUP_YEAR, LOCAL_GROUP_MONTH, LOCAL_GROUP_DAY_15, MORNING_HOUR).toISOString(),
            });
            const meal2 = createMockMeal({
                id: '2',
                date: new Date(LOCAL_GROUP_YEAR, LOCAL_GROUP_MONTH, LOCAL_GROUP_DAY_15, AFTERNOON_HOUR).toISOString(),
            });
            const meal3 = createMockMeal({
                id: '3',
                date: new Date(LOCAL_GROUP_YEAR, LOCAL_GROUP_MONTH, LOCAL_GROUP_DAY_16, MORNING_HOUR).toISOString(),
            });

            context.mockMealService.query.mockReturnValue(of(createPageOf([meal1, meal2, meal3])));
            context.component().loadConsumptions(1).subscribe();

            const grouped = context.component().groupedConsumptions();
            expect(grouped.length).toBe(NEXT_PAGE_NUMBER);

            const march16Group = grouped.find(
                g =>
                    g.date.getFullYear() === LOCAL_GROUP_YEAR &&
                    g.date.getMonth() === LOCAL_GROUP_MONTH &&
                    g.date.getDate() === LOCAL_GROUP_DAY_16,
            );
            const march15Group = grouped.find(
                g =>
                    g.date.getFullYear() === LOCAL_GROUP_YEAR &&
                    g.date.getMonth() === LOCAL_GROUP_MONTH &&
                    g.date.getDate() === LOCAL_GROUP_DAY_15,
            );
            expect(march16Group).toBeDefined();
            expect(march15Group).toBeDefined();
            expect(march15Group?.items.length).toBe(NEXT_PAGE_NUMBER);
            expect(march16Group?.items.length).toBe(1);
        });

        it('should group meals by local calendar date instead of UTC date', () => {
            const lateMeal = createMockMeal({
                id: '1',
                date: new Date(CURRENT_YEAR, MAY_MONTH_INDEX, MAY_4, LATE_MEAL_HOUR, LATE_MEAL_MINUTE).toISOString(),
            });
            const afterMidnightMeal = createMockMeal({
                id: '2',
                date: new Date(CURRENT_YEAR, MAY_MONTH_INDEX, MAY_5, 0, HALF_PAST_MIDNIGHT_MINUTES).toISOString(),
            });

            context.mockMealService.query.mockReturnValue(of(createPageOf([afterMidnightMeal, lateMeal])));
            context.component().loadConsumptions(1).subscribe();

            const grouped = context.component().groupedConsumptions();
            expect(grouped.length).toBe(NEXT_PAGE_NUMBER);
            expect(grouped[0].date.getFullYear()).toBe(CURRENT_YEAR);
            expect(grouped[0].date.getMonth()).toBe(MAY_MONTH_INDEX);
            expect(grouped[0].date.getDate()).toBe(MAY_5);
            expect(grouped[0].items).toEqual([afterMidnightMeal]);
            expect(grouped[1].date.getDate()).toBe(MAY_4);
            expect(grouped[1].items).toEqual([lateMeal]);
        });
    });
}

function registerRangeTests(context: TestContext): void {
    describe('date range', () => {
        it('should query selected date range using local day boundaries', () => {
            const start = new Date(CURRENT_YEAR, MAY_MONTH_INDEX, MAY_5);
            const end = new Date(CURRENT_YEAR, MAY_MONTH_INDEX, MAY_6);

            context.component().searchForm.controls.dateRange.setValue({ start, end });
            context.mockMealService.query.mockClear();

            context.component().loadConsumptions(1).subscribe();

            expect(context.mockMealService.query).toHaveBeenCalledWith(1, PAGE_LIMIT, {
                dateFrom: new Date(CURRENT_YEAR, MAY_MONTH_INDEX, MAY_5, 0, 0, 0, 0).toISOString(),
                dateTo: new Date(
                    CURRENT_YEAR,
                    MAY_MONTH_INDEX,
                    MAY_6,
                    END_OF_DAY_HOURS,
                    END_OF_DAY_MINUTES,
                    END_OF_DAY_SECONDS,
                    END_OF_DAY_MS,
                ).toISOString(),
            });
        });
    });
}

function registerFavoriteTests(context: TestContext): void {
    describe('favorites', () => {
        it('should repeat favorite meal for the current local time and meal type', () => {
            vi.useFakeTimers();
            vi.setSystemTime(new Date(CURRENT_YEAR, MAY_MONTH_INDEX, MAY_5, 0, HALF_PAST_MIDNIGHT_MINUTES));
            context.fixture().detectChanges();
            setPageContainerScrollMock(context.fixture(), true);

            context.component().repeatFavorite(createFavorite());

            expect(context.mockMealService.repeat).toHaveBeenCalledWith(
                'meal-1',
                new Date(CURRENT_YEAR, MAY_MONTH_INDEX, MAY_5, 0, HALF_PAST_MIDNIGHT_MINUTES).toISOString(),
                'SNACK',
            );
        });

        it('should show toast when favorite repeat fails', () => {
            const favorite = createFavorite();
            context.mockMealService.repeat.mockReturnValue(throwError(() => new Error('Repeat failed')));

            context.component().repeatFavorite(favorite);

            expect(context.mockToastService.error).toHaveBeenCalledWith('CONSUMPTION_LIST.OPERATION_ERROR_MESSAGE');
            expect(context.component().errorKey()).toBeNull();
            expect(context.mockMealService.query).not.toHaveBeenCalled();
        });

        it('should sync favorite count and meal card state when favorite is removed', () => {
            const favorite = createFavorite();
            const meal = createMockMeal({ id: favorite.mealId, isFavorite: true, favoriteMealId: favorite.id });
            context.component().consumptionData.setData(createPageOf([meal]));
            context.component().favorites.set([favorite]);
            context.component().favoriteTotalCount.set(1);

            context.component().removeFavorite(favorite);

            expect(context.component().favorites()).toEqual([]);
            expect(context.component().favoriteTotalCount()).toBe(0);
            expect(context.component().consumptionData.items()[0]).toMatchObject({ isFavorite: false, favoriteMealId: null });
        });

        it('should sync meal card favorite changes before refreshing favorites', () => {
            const meal = createMockMeal({ id: 'meal-1', isFavorite: false, favoriteMealId: null });
            context.component().consumptionData.setData(createPageOf([meal]));

            context.component().onMealFavoriteChanged(meal, { isFavorite: true, favoriteMealId: 'favorite-1' });

            expect(context.component().consumptionData.items()[0]).toMatchObject({ isFavorite: true, favoriteMealId: 'favorite-1' });
            expect(context.mockFavoriteMealService.getAll).toHaveBeenCalled();
        });
    });
}

function registerDialogTests(context: TestContext): void {
    describe('dialogs', () => {
        it('should open meal details dialog', async () => {
            const meal = createMockMeal();

            await context.component().openMealDetailsAsync(meal);

            expect(context.mockFdDialogService.open).toHaveBeenCalled();
        });
    });
}

function setPageContainerScrollMock(fixture: ComponentFixture<MealListComponent>, throwOnMissing = false): void {
    const host = fixture.nativeElement as HTMLElement;
    const containerEl = host.querySelector<HTMLElement>('[fdpagecontainer]');
    if (containerEl === null) {
        if (throwOnMissing) {
            throw new Error('Expected page container to exist.');
        }
        return;
    }

    containerEl.scrollIntoView = vi.fn();
}

function createFavorite(overrides: Partial<FavoriteMeal> = {}): FavoriteMeal {
    return {
        id: 'favorite-1',
        mealId: 'meal-1',
        name: null,
        createdAtUtc: '2026-05-04T20:00:00Z',
        mealDate: '2026-05-04T20:00:00Z',
        mealType: null,
        totalCalories: 100,
        totalProteins: 1,
        totalFats: 1,
        totalCarbs: 1,
        itemCount: 1,
        ...overrides,
    };
}
