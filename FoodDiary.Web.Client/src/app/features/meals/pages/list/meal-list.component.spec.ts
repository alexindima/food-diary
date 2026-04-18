import { beforeEach, describe, expect, it, vi } from 'vitest';
import { ComponentFixture, TestBed } from '@angular/core/testing';
import { provideHttpClient } from '@angular/common/http';
import { provideHttpClientTesting } from '@angular/common/http/testing';
import { provideNoopAnimations } from '@angular/platform-browser/animations';
import { TranslateModule } from '@ngx-translate/core';
import { BreakpointObserver } from '@angular/cdk/layout';
import { of } from 'rxjs';

import { MealListComponent } from './meal-list.component';
import { MealService } from '../../api/meal.service';
import { FavoriteMealService } from '../../api/favorite-meal.service';
import { AiFoodService } from '../../../../shared/api/ai-food.service';
import { LocalizationService } from '../../../../services/localization.service';
import { NavigationService } from '../../../../services/navigation.service';
import { FdUiDialogService } from 'fd-ui-kit/dialog/fd-ui-dialog.service';
import { Meal } from '../../models/meal.data';
import { PageOf } from '../../../../shared/models/page-of.data';
import { MealOverview } from '../../models/meal.data';

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
        deleteById: vi.fn().mockReturnValue(of(void 0)),
    };

    const mockNavigationService = {
        navigateToConsumptionAdd: vi.fn().mockResolvedValue(true),
        navigateToConsumptionEdit: vi.fn().mockResolvedValue(true),
    };

    const mockDialogRef = {
        afterClosed: vi.fn().mockReturnValue(of(undefined)),
    };

    const mockFdDialogService = {
        open: vi.fn().mockReturnValue(mockDialogRef),
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
                { provide: BreakpointObserver, useValue: mockBreakpointObserver },
                { provide: FavoriteMealService, useValue: mockFavoriteMealService },
                { provide: AiFoodService, useValue: mockAiFoodService },
                { provide: LocalizationService, useValue: mockLocalizationService },
            ],
        }).compileComponents();

        fixture = TestBed.createComponent(MealListComponent);
        component = fixture.componentInstance;
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
        await component.goToMealAdd();

        expect(mockNavigationService.navigateToConsumptionAdd).toHaveBeenCalled();
    });

    it('should handle page change', () => {
        // detectChanges to resolve viewChild #container
        fixture.detectChanges();

        // Mock scrollIntoView on the container element
        const containerEl = fixture.nativeElement.querySelector('[fdpagecontainer]') as HTMLElement;
        if (containerEl) {
            containerEl.scrollIntoView = vi.fn();
        }

        const meals = [createMockMeal()];
        mockMealService.query.mockReturnValue(of(createPageOf(meals, 2)));

        component.onPageChange(1);

        expect(component.currentPageIndex).toBe(1);
        expect(mockMealService.query).toHaveBeenCalledWith(2, 10, expect.any(Object));
    });

    it('should group consumptions by date', () => {
        const meal1 = createMockMeal({ id: '1', date: '2024-03-15T10:00:00Z' });
        const meal2 = createMockMeal({ id: '2', date: '2024-03-15T14:00:00Z' });
        const meal3 = createMockMeal({ id: '3', date: '2024-03-16T10:00:00Z' });

        // Directly call loadConsumptions to avoid debounceTime issues
        mockMealService.query.mockReturnValue(of(createPageOf([meal1, meal2, meal3])));
        component.loadConsumptions(1).subscribe();

        const grouped = component.groupedConsumptions();
        expect(grouped.length).toBe(2);

        const march16Group = grouped.find(g => g.date.toISOString().startsWith('2024-03-16'));
        const march15Group = grouped.find(g => g.date.toISOString().startsWith('2024-03-15'));
        expect(march16Group).toBeDefined();
        expect(march15Group).toBeDefined();
        expect(march15Group!.items.length).toBe(2);
        expect(march16Group!.items.length).toBe(1);
    });

    it('should open meal details dialog', () => {
        const meal = createMockMeal();

        component.openMealDetails(meal);

        expect(mockFdDialogService.open).toHaveBeenCalled();
    });
});
