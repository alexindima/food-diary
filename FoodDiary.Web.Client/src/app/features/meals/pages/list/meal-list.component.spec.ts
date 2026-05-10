import { BreakpointObserver } from '@angular/cdk/layout';
import { provideHttpClient } from '@angular/common/http';
import { provideHttpClientTesting } from '@angular/common/http/testing';
import { type ComponentFixture, TestBed } from '@angular/core/testing';
import { provideNoopAnimations } from '@angular/platform-browser/animations';
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
        limit: 10,
        totalPages: 1,
        totalItems: meals.length,
    };
}

describe('MealListComponent', () => {
    let component: MealListComponent;
    let fixture: ComponentFixture<MealListComponent>;

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

    beforeEach(async () => {
        vi.clearAllMocks();
        mockMealService.queryOverview.mockReturnValue(of(createOverview([])));
        mockMealService.query.mockReturnValue(of(createPageOf([])));
        mockMealService.repeat.mockReturnValue(of(createMockMeal()));
        mockMealService.deleteById.mockReturnValue(of(void 0));
        mockFavoriteMealService.getAll.mockReturnValue(of([]));
        mockFavoriteMealService.remove.mockReturnValue(of(void 0));
        mockToastService.error.mockClear();

        // Mock window.matchMedia for the component constructor
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
                provideNoopAnimations(),
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

    it('should create', () => {
        expect(component).toBeTruthy();
    });

    it('should load overview on init', () => {
        const meals = [createMockMeal()];
        mockMealService.queryOverview.mockReturnValue(of(createOverview(meals)));

        fixture.detectChanges();

        expect(mockMealService.queryOverview).toHaveBeenCalledWith(1, 10, { dateFrom: undefined, dateTo: undefined }, 10);
    });

    it('should navigate to add meal', async () => {
        await component.goToMealAddAsync();

        expect(mockNavigationService.navigateToConsumptionAddAsync).toHaveBeenCalled();
    });

    it('should handle page change', () => {
        // detectChanges to resolve viewChild #container
        fixture.detectChanges();

        // Mock scrollIntoView on the container element
        const containerEl = fixture.nativeElement.querySelector('[fdpagecontainer]') as HTMLElement | null;
        if (containerEl) {
            containerEl.scrollIntoView = vi.fn();
        }

        const meals = [createMockMeal()];
        mockMealService.query.mockReturnValue(of(createPageOf(meals, 2)));

        component.onPageChange(1);

        expect(component.currentPageIndex()).toBe(1);
        expect(mockMealService.query).toHaveBeenCalledWith(2, 10, expect.any(Object));
    });

    it('should group consumptions by date', () => {
        const meal1 = createMockMeal({ id: '1', date: new Date(2024, 2, 15, 10).toISOString() });
        const meal2 = createMockMeal({ id: '2', date: new Date(2024, 2, 15, 14).toISOString() });
        const meal3 = createMockMeal({ id: '3', date: new Date(2024, 2, 16, 10).toISOString() });

        // Directly call loadConsumptions to avoid debounceTime issues
        mockMealService.query.mockReturnValue(of(createPageOf([meal1, meal2, meal3])));
        component.loadConsumptions(1).subscribe();

        const grouped = component.groupedConsumptions();
        expect(grouped.length).toBe(2);

        const march16Group = grouped.find(g => g.date.getFullYear() === 2024 && g.date.getMonth() === 2 && g.date.getDate() === 16);
        const march15Group = grouped.find(g => g.date.getFullYear() === 2024 && g.date.getMonth() === 2 && g.date.getDate() === 15);
        expect(march16Group).toBeDefined();
        expect(march15Group).toBeDefined();
        expect(march15Group?.items.length).toBe(2);
        expect(march16Group?.items.length).toBe(1);
    });

    it('should group meals by local calendar date instead of UTC date', () => {
        const lateMeal = createMockMeal({ id: '1', date: new Date(2026, 4, 4, 22, 48).toISOString() });
        const afterMidnightMeal = createMockMeal({ id: '2', date: new Date(2026, 4, 5, 0, 30).toISOString() });

        mockMealService.query.mockReturnValue(of(createPageOf([afterMidnightMeal, lateMeal])));
        component.loadConsumptions(1).subscribe();

        const grouped = component.groupedConsumptions();
        expect(grouped.length).toBe(2);
        expect(grouped[0].date.getFullYear()).toBe(2026);
        expect(grouped[0].date.getMonth()).toBe(4);
        expect(grouped[0].date.getDate()).toBe(5);
        expect(grouped[0].items).toEqual([afterMidnightMeal]);
        expect(grouped[1].date.getDate()).toBe(4);
        expect(grouped[1].items).toEqual([lateMeal]);
    });

    it('should query selected date range using local day boundaries', () => {
        const start = new Date(2026, 4, 5);
        const end = new Date(2026, 4, 6);

        component.searchForm.controls.dateRange.setValue({ start, end });
        mockMealService.query.mockClear();

        component.loadConsumptions(1).subscribe();

        expect(mockMealService.query).toHaveBeenCalledWith(1, 10, {
            dateFrom: new Date(2026, 4, 5, 0, 0, 0, 0).toISOString(),
            dateTo: new Date(2026, 4, 6, 23, 59, 59, 999).toISOString(),
        });
    });

    it('should expose load errors for retry state', () => {
        mockMealService.query.mockReturnValue(throwError(() => new Error('Network error')));

        component.loadConsumptions(1).subscribe();

        expect(component.errorKey()).toBe('ERRORS.LOAD_FAILED_TITLE');
        expect(component.consumptionData.isLoading()).toBe(false);
        expect(component.consumptionData.items()).toEqual([]);
    });

    it('should repeat favorite meal for the current local time and meal type', () => {
        vi.useFakeTimers();
        vi.setSystemTime(new Date(2026, 4, 5, 0, 30));
        fixture.detectChanges();
        const containerEl = fixture.nativeElement.querySelector('[fdpagecontainer]') as HTMLElement;
        containerEl.scrollIntoView = vi.fn();

        component.repeatFavorite({
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
        });

        expect(mockMealService.repeat).toHaveBeenCalledWith('meal-1', new Date(2026, 4, 5, 0, 30).toISOString(), 'SNACK');
    });

    it('should show toast when favorite repeat fails', () => {
        const favorite = createFavorite();
        mockMealService.repeat.mockReturnValue(throwError(() => new Error('Repeat failed')));

        component.repeatFavorite(favorite);

        expect(mockToastService.error).toHaveBeenCalledWith('CONSUMPTION_LIST.OPERATION_ERROR_MESSAGE');
        expect(component.errorKey()).toBeNull();
        expect(mockMealService.query).not.toHaveBeenCalled();
    });

    it('should sync favorite count and meal card state when favorite is removed', () => {
        const favorite = createFavorite();
        const meal = createMockMeal({ id: favorite.mealId, isFavorite: true, favoriteMealId: favorite.id });
        component.consumptionData.setData(createPageOf([meal]));
        component.favorites.set([favorite]);
        component.favoriteTotalCount.set(1);

        component.removeFavorite(favorite);

        expect(component.favorites()).toEqual([]);
        expect(component.favoriteTotalCount()).toBe(0);
        expect(component.consumptionData.items()[0]).toMatchObject({ isFavorite: false, favoriteMealId: null });
    });

    it('should sync meal card favorite changes before refreshing favorites', () => {
        const meal = createMockMeal({ id: 'meal-1', isFavorite: false, favoriteMealId: null });
        component.consumptionData.setData(createPageOf([meal]));

        component.onMealFavoriteChanged(meal, { isFavorite: true, favoriteMealId: 'favorite-1' });

        expect(component.consumptionData.items()[0]).toMatchObject({ isFavorite: true, favoriteMealId: 'favorite-1' });
        expect(mockFavoriteMealService.getAll).toHaveBeenCalled();
    });

    it('should open meal details dialog', async () => {
        const meal = createMockMeal();

        await component.openMealDetailsAsync(meal);

        expect(mockFdDialogService.open).toHaveBeenCalled();
    });
});

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
