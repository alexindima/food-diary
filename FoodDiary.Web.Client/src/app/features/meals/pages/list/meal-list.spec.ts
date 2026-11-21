import { BreakpointObserver } from '@angular/cdk/layout';
import { provideHttpClient } from '@angular/common/http';
import { provideHttpClientTesting } from '@angular/common/http/testing';
import { Component, signal } from '@angular/core';
import { type ComponentFixture, DeferBlockBehavior, DeferBlockState, TestBed } from '@angular/core/testing';
import { provideRouter } from '@angular/router';
import { RouterTestingHarness } from '@angular/router/testing';
import { FdUiDialogService } from 'fd-ui-kit/dialog/fd-ui-dialog.service';
import { FdUiToastService } from 'fd-ui-kit/toast/fd-ui-toast.service';
import { type Observable, of, Subject, throwError } from 'rxjs';
import { afterEach, beforeEach, describe, expect, it, vi } from 'vitest';

import { provideTranslateTesting } from '../../../../../testing/translate-testing.module';
import { AuthService } from '../../../../services/auth.service';
import { NavigationService } from '../../../../services/navigation.service';
import { AiFoodService } from '../../../../shared/api/ai-food.service';
import { LocalizationService } from '../../../../shared/i18n/localization.service';
import type { FavoriteMeal, Meal } from '../../../../shared/models/meal.data';
import type { MealOverview } from '../../../../shared/models/meal.data';
import type { PageOf } from '../../../../shared/models/page-of.data';
import { utcInstant } from '../../../../shared/models/semantics/date-value';
import { entityId } from '../../../../shared/models/semantics/entity-id';
import { FavoriteMealService } from '../../api/favorite-meal.service';
import { MealService } from '../../api/meal.service';
import { MealListComponent } from './meal-list';

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

type TestContext = {
    component: () => MealListComponent;
    fixture: () => ComponentFixture<MealListComponent>;
    mockMealService: typeof mockMealService;
    mockNavigationService: typeof mockNavigationService;
    mockFdDialogService: typeof mockFdDialogService;
    mockFavoriteMealService: typeof mockFavoriteMealService;
};

function createMockMeal(overrides: Partial<Meal> = {}): Meal {
    return {
        id: entityId<'meal'>('1'),
        date: utcInstant('2024-03-15T10:00:00Z'),
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
        limit: 0,
        totalPages: 1,
        totalItems: meals.length,
    };
}

function createOverview(meals: Meal[], page = 1): MealOverview {
    return {
        allMeals: createPageOf(meals, page),
        favoriteItems: [],
        favoriteTotalCount: 0,
    };
}

const mockMealService = {
    create: vi.fn().mockReturnValue(of(createMockMeal())),
    queryOverview: vi.fn().mockReturnValue(of(createOverview([]))),
    query: vi.fn().mockReturnValue(of(createPageOf([]))),
    repeat: vi.fn().mockReturnValue(of(createMockMeal())),
    deleteById: vi.fn().mockReturnValue(of(void 0)),
};

const mockNavigationService = {
    navigateToMealAddAsync: vi.fn().mockResolvedValue(true),
    navigateToMealEditAsync: vi.fn().mockResolvedValue(true),
};

const mockDialogRef = {
    afterClosed: vi.fn().mockReturnValue(of(void 0)),
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
    restore: vi.fn().mockReturnValue(of({ id: 'favorite-1', mealId: 'meal-1' })),
    add: vi.fn().mockReturnValue(of({ id: 'favorite-1', mealId: 'meal-1' })),
    getPage: vi.fn().mockReturnValue(of({ totalItems: 0 })),
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
        mockFdDialogService.open.mockReturnValue(mockDialogRef);
        mockDialogRef.afterClosed.mockReturnValue(of(void 0));
        mockMealService.create.mockReturnValue(of(createMockMeal()));
        mockMealService.queryOverview.mockReturnValue(of(createOverview([])));
        mockMealService.repeat.mockReturnValue(of(createMockMeal()));
        mockMealService.deleteById.mockReturnValue(of(void 0));
        mockFavoriteMealService.getPage.mockReturnValue(of({ totalItems: 0 }));
        mockFavoriteMealService.add.mockReturnValue(of({ id: 'favorite-1', mealId: 'meal-1' }));
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
            deferBlockBehavior: DeferBlockBehavior.Manual,
            imports: [MealListComponent],
            providers: [
                provideTranslateTesting(),
                provideRouter([]),
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
        mockFavoriteMealService,
    };

    it('should create', () => {
        expect(component).toBeTruthy();
    });

    registerLoadingTests(context);
    registerNavigationTests(context);
    registerGroupingTests(context);
    registerPlannedGroupingTests(context);
    registerRangeTests(context);
    registerDialogTests(context);
    registerFilterInteractionTests(context);
    registerFavoritesPickerWiringTests(context);
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
                { limit: 0, include: true },
            );
        });

        it('should expose load errors for retry state', () => {
            context.mockMealService.queryOverview.mockReturnValue(throwError(() => new Error('Network error')));

            context.component()['loadMeals'](1).subscribe();

            expect(context.component()['errorKey']()).toBe('ERRORS.LOAD_FAILED_TITLE');
            expect(context.component()['mealData'].isLoading()).toBe(false);
            expect(context.component()['mealData'].items()).toEqual([]);
        });
    });
}

function registerNavigationTests(context: TestContext): void {
    describe('navigation', () => {
        it('should navigate to add meal', async () => {
            await context.component()['goToMealAddAsync']();

            expect(context.mockNavigationService.navigateToMealAddAsync).toHaveBeenCalled();
        });

        it('should handle page change', () => {
            context.fixture().detectChanges();
            setPageContainerScrollMock(context.fixture());

            const meals = [createMockMeal()];
            context.mockMealService.queryOverview.mockReturnValue(of(createOverview(meals, NEXT_PAGE_NUMBER)));

            context.component()['onPageChange'](NEXT_PAGE_INDEX);

            expect(context.component()['currentPageIndex']()).toBe(NEXT_PAGE_INDEX);
            expect(context.mockMealService.queryOverview).toHaveBeenCalledWith(NEXT_PAGE_NUMBER, PAGE_LIMIT, expect.any(Object), {
                limit: 0,
                include: false,
            });
        });
    });
}

function registerGroupingTests(context: TestContext): void {
    describe('grouping', () => {
        it('should group meals by date', () => {
            const meal1 = createMockMeal({
                id: entityId<'meal'>('1'),
                date: utcInstant(new Date(LOCAL_GROUP_YEAR, LOCAL_GROUP_MONTH, LOCAL_GROUP_DAY_15, MORNING_HOUR).toISOString()),
            });
            const meal2 = createMockMeal({
                id: entityId<'meal'>('2'),
                date: utcInstant(new Date(LOCAL_GROUP_YEAR, LOCAL_GROUP_MONTH, LOCAL_GROUP_DAY_15, AFTERNOON_HOUR).toISOString()),
            });
            const meal3 = createMockMeal({
                id: entityId<'meal'>('3'),
                date: utcInstant(new Date(LOCAL_GROUP_YEAR, LOCAL_GROUP_MONTH, LOCAL_GROUP_DAY_16, MORNING_HOUR).toISOString()),
            });

            context.mockMealService.queryOverview.mockReturnValue(
                of({
                    ...createOverview([meal1, meal2, meal3]),
                    daySummaries: [{ date: '2024-03-15', totalCalories: 2500, mealCount: 8 }],
                }),
            );
            context.component()['loadMeals'](1).subscribe();

            const grouped = context.component()['groupedMeals']();
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
            expect(march15Group?.totalCalories?.replaceAll(/\D/g, '')).toBe('2500');
            expect(march16Group?.totalCalories).toBeUndefined();
            expect(march16Group?.items.length).toBe(1);
        });

        it('should group meals by local calendar date instead of UTC date', () => {
            const lateMeal = createMockMeal({
                id: entityId<'meal'>('1'),
                date: utcInstant(new Date(CURRENT_YEAR, MAY_MONTH_INDEX, MAY_4, LATE_MEAL_HOUR, LATE_MEAL_MINUTE).toISOString()),
            });
            const afterMidnightMeal = createMockMeal({
                id: entityId<'meal'>('2'),
                date: utcInstant(new Date(CURRENT_YEAR, MAY_MONTH_INDEX, MAY_5, 0, HALF_PAST_MIDNIGHT_MINUTES).toISOString()),
            });

            context.mockMealService.queryOverview.mockReturnValue(of(createOverview([afterMidnightMeal, lateMeal])));
            context.component()['loadMeals'](1).subscribe();

            const grouped = context.component()['groupedMeals']();
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

function registerPlannedGroupingTests(context: TestContext): void {
    describe('planned grouping', () => {
        it('should separate future meals from current diary groups', () => {
            vi.useFakeTimers();
            vi.setSystemTime(new Date(CURRENT_YEAR, MAY_MONTH_INDEX, MAY_5, MORNING_HOUR));
            const currentMeal = createMockMeal({
                id: entityId<'meal'>('current-meal'),
                date: utcInstant(new Date(CURRENT_YEAR, MAY_MONTH_INDEX, MAY_5, AFTERNOON_HOUR).toISOString()),
            });
            const laterFutureMeal = createMockMeal({
                id: entityId<'meal'>('later-future-meal'),
                date: utcInstant(new Date(CURRENT_YEAR, MAY_MONTH_INDEX, MAY_6, AFTERNOON_HOUR).toISOString()),
            });
            const nearerFutureMeal = createMockMeal({
                id: entityId<'meal'>('nearer-future-meal'),
                date: utcInstant(new Date(CURRENT_YEAR, MAY_MONTH_INDEX, MAY_6, MORNING_HOUR).toISOString()),
            });

            context.mockMealService.queryOverview.mockReturnValue(of(createOverview([laterFutureMeal, currentMeal, nearerFutureMeal])));
            context.component()['loadMeals'](1).subscribe();

            expect(context.component()['groupedMeals']()).toHaveLength(1);
            expect(context.component()['groupedMeals']()[0].items).toEqual([currentMeal]);
            expect(context.component()['plannedGroups']()).toHaveLength(1);
            expect(context.component()['plannedGroups']()[0].items).toEqual([nearerFutureMeal, laterFutureMeal]);
        });
    });
}

function registerRangeTests(context: TestContext): void {
    describe('date range', () => {
        it('should query selected date range using local day boundaries', () => {
            const start = new Date(CURRENT_YEAR, MAY_MONTH_INDEX, MAY_5);
            const end = new Date(CURRENT_YEAR, MAY_MONTH_INDEX, MAY_6);

            context.component()['searchModel'].update(value => ({ ...value, dateRange: { start, end } }));
            context.mockMealService.queryOverview.mockClear();

            context.component()['loadMeals'](1).subscribe();

            expect(context.mockMealService.queryOverview).toHaveBeenCalledWith(
                1,
                PAGE_LIMIT,
                {
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
                },
                { limit: 0, include: false },
            );
        });
    });
}

function registerDialogTests(context: TestContext): void {
    describe('dialogs', () => {
        it('should open meal details dialog', async () => {
            const meal = createMockMeal();

            await context.component()['openMealDetailsAsync'](meal);

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
        id: entityId<'favorite-meal'>('favorite-1'),
        mealId: entityId<'meal'>('meal-1'),
        name: null,
        createdAtUtc: utcInstant('2026-05-04T20:00:00Z'),
        mealDate: utcInstant('2026-05-04T20:00:00Z'),
        mealType: null,
        totalCalories: 100,
        totalProteins: 1,
        totalFats: 1,
        totalCarbs: 1,
        itemCount: 1,
        ...overrides,
    };
}

@Component({ template: '<h1>Statistics</h1>' })
class DiaryDestinationTestPage {}

describe('Meal diary detail navigation', () => {
    afterEach(() => {
        vi.unstubAllGlobals();
    });

    it('dismisses the open detail overlay when navigation removes the diary page', async () => {
        const { harness, mealService } = await openRenderedMealDetailsAsync();

        await harness.navigateByUrl('/statistics', DiaryDestinationTestPage);

        expect(harness.routeNativeElement?.querySelector('h1')?.textContent).toBe('Statistics');
        expect(document.querySelector('fd-meal-detail')).toBeNull();
        expect(mealService.queryOverview).toHaveBeenCalledTimes(1);
        expect(mealService.repeat).not.toHaveBeenCalled();
        expect(mealService.deleteById).not.toHaveBeenCalled();
    });

    it('dismisses a pending delete confirmation when navigation removes the diary page', async () => {
        const { harness, mealService } = await openRenderedMealDetailsAsync();
        clickDetailAction('fd-meal-detail', 'MEAL_DETAIL.DELETE_BUTTON');
        harness.detectChanges();
        expect(document.querySelector('fd-confirm-delete-dialog')).not.toBeNull();

        await harness.navigateByUrl('/statistics', DiaryDestinationTestPage);

        expect(harness.routeNativeElement?.querySelector('h1')?.textContent).toBe('Statistics');
        expect(document.querySelector('fd-meal-detail')).toBeNull();
        expect(document.querySelector('fd-confirm-delete-dialog')).toBeNull();
        expect(mealService.deleteById).not.toHaveBeenCalled();
    });
});

describe('Meal diary rendered detail actions', () => {
    afterEach(() => {
        vi.unstubAllGlobals();
    });

    it('leaves the meal after cancel and removes it from the diary only after confirming deletion', async () => {
        const { harness, meal, mealService } = await openRenderedMealDetailsAsync();
        clickDetailAction('fd-meal-detail', 'MEAL_DETAIL.DELETE_BUTTON');
        harness.detectChanges();
        clickDetailAction('fd-confirm-delete-dialog', 'CONFIRM_DELETE.CANCEL');
        harness.detectChanges();
        expect(document.querySelector('fd-meal-detail')).not.toBeNull();
        expect(mealService.deleteById).not.toHaveBeenCalled();

        mealService.queryOverview.mockReturnValueOnce(of(createOverview([])));
        clickDetailAction('fd-meal-detail', 'MEAL_DETAIL.DELETE_BUTTON');
        harness.detectChanges();
        clickDetailAction('fd-confirm-delete-dialog', 'CONFIRM_DELETE.CONFIRM');
        await vi.waitFor(() => {
            harness.detectChanges();
            expect(harness.routeNativeElement?.querySelector('fd-meal-card')).toBeNull();
        });
        expect(document.querySelector('fd-meal-detail')).toBeNull();
        expect(mealService.deleteById).toHaveBeenCalledExactlyOnceWith(meal.id);
    });

    it('refreshes the diary and scrolls after repeating the open meal', async () => {
        const { harness, meal, mealService } = await openRenderedMealDetailsAsync();
        const list = harness.routeNativeElement?.querySelector<HTMLElement>('.meal-list');
        if (list === null || list === undefined) {
            throw new Error('Diary container was not rendered');
        }
        const scroll = vi.fn();
        list.scrollIntoView = scroll;
        const repeated = createMockMeal({ id: entityId<'meal'>('repeated-meal'), comment: 'Repeated meal' });
        mealService.repeat.mockReturnValueOnce(of(repeated));
        mealService.queryOverview.mockReturnValueOnce(of(createOverview([meal, repeated])));

        clickDetailAction('fd-meal-detail', 'MEAL_DETAIL.REPEAT_BUTTON');

        await vi.waitFor(() => {
            harness.detectChanges();
            expect(harness.routeNativeElement?.querySelectorAll('fd-meal-card')).toHaveLength(2);
        });
        expect(mealService.repeat).toHaveBeenCalledExactlyOnceWith(meal.id, expect.any(String), expect.any(String));
        expect(mealService.queryOverview).toHaveBeenCalledTimes(2);
        expect(scroll).toHaveBeenCalledTimes(1);
    });
});

describe('Meal diary rendered favorite update', () => {
    afterEach(() => {
        vi.unstubAllGlobals();
    });

    it('updates the diary favorite button after the detail save completes and the dialog closes', async () => {
        const { harness, meal, mealService, favoriteService } = await openRenderedMealDetailsAsync();
        const pending = new Subject<FavoriteMeal>();
        const favorite = createFavorite({ mealId: meal.id });
        favoriteService.add.mockReturnValueOnce(pending);
        const save = requireButton(document, 'fd-meal-detail button[aria-label="MEAL_DETAIL.ADD_FAVORITE"]');
        save.click();
        harness.detectChanges();
        expect(save.disabled).toBe(true);
        expect(favoriteService.add).toHaveBeenCalledExactlyOnceWith(meal.id);

        const saved = { ...meal, isFavorite: true, favoriteMealId: favorite.id };
        mealService.queryOverview.mockReturnValueOnce(of({ ...createOverview([saved]), favoriteItems: [favorite], favoriteTotalCount: 1 }));
        pending.next(favorite);
        pending.complete();
        harness.detectChanges();
        expect(save.getAttribute('aria-label')).toBe('MEAL_DETAIL.REMOVE_FAVORITE');
        requireButton(document, 'fd-meal-detail button[aria-label="COMMON.CLOSE"]').click();

        await vi.waitFor(() => {
            harness.detectChanges();
            expect(
                harness.routeNativeElement?.querySelector('fd-meal-card button[aria-label="MEAL_DETAIL.REMOVE_FAVORITE"]'),
            ).not.toBeNull();
        });
        expect(document.querySelector('fd-meal-detail')).toBeNull();
        expect(mealService.queryOverview).toHaveBeenLastCalledWith(1, PAGE_LIMIT, {}, expect.objectContaining({ include: true }));
    });
});

describe('Meal diary stale favorite recovery', () => {
    afterEach(() => {
        vi.unstubAllGlobals();
    });

    it('clears a stale favorite star when the lookup page contains only a different meal', async () => {
        const { harness, meal, favoriteService, mealService } = await openRenderedMealDetailsAsync({
            isFavorite: true,
            favoriteMealId: null,
        });
        const otherFavorite = createFavorite({ mealId: entityId<'meal'>('other-favorite-meal') });
        favoriteService.getLookupPage.mockReturnValueOnce(of([otherFavorite]));
        const remove = requireButton(document, 'fd-meal-detail button[aria-label="MEAL_DETAIL.REMOVE_FAVORITE"]');
        remove.click();
        harness.detectChanges();

        expect(favoriteService.getLookupPage).toHaveBeenCalledOnce();
        expect(favoriteService.remove).not.toHaveBeenCalled();
        expect(remove.getAttribute('aria-label')).toBe('MEAL_DETAIL.ADD_FAVORITE');
        mealService.queryOverview.mockReturnValueOnce(
            of({
                ...createOverview([{ ...meal, isFavorite: false }]),
                favoriteItems: [otherFavorite],
                favoriteTotalCount: 1,
            }),
        );
        requireButton(document, 'fd-meal-detail button[aria-label="COMMON.CLOSE"]').click();
        await vi.waitFor(() => {
            harness.detectChanges();
            expect(harness.routeNativeElement?.querySelector('fd-meal-card button[aria-label="MEAL_DETAIL.ADD_FAVORITE"]')).not.toBeNull();
        });
        expect(document.querySelector('fd-meal-detail')).toBeNull();
        expect(mealService.queryOverview).toHaveBeenCalledTimes(2);
    });
});

describe('Meal diary favorite repeat recovery', () => {
    afterEach(() => {
        vi.unstubAllGlobals();
        vi.useRealTimers();
    });

    it('keeps the chosen meal type after a failed repeat and scrolls the updated diary on retry', async () => {
        const now = new Date('2026-05-04T08:00:00');
        vi.useFakeTimers({ toFake: ['Date'] });
        vi.setSystemTime(now);
        const { harness, meal, mealService, favoriteService } = await renderMealDiaryAsync();
        const favorite = createFavorite({ mealId: meal.id, name: 'Saved breakfast' });
        const repeated = createMockMeal({ id: entityId<'meal'>('repeated-favorite'), mealType: 'DINNER' });
        favoriteService.getPage.mockReturnValueOnce(of({ data: [favorite], page: 1, limit: PAGE_LIMIT, totalItems: 1, totalPages: 1 }));
        mealService.repeat.mockReturnValueOnce(throwError(() => new Error('Repeat unavailable'))).mockReturnValueOnce(of(repeated));
        mealService.queryOverview.mockReturnValueOnce(of(createOverview([meal, repeated])));
        for (const block of await harness.fixture.getDeferBlocks()) {
            await block.render(DeferBlockState.Complete);
        }
        harness.detectChanges();
        const list = harness.routeNativeElement?.querySelector<HTMLElement>('.meal-list');
        if (!(list instanceof HTMLElement)) {
            throw new Error('Expected the rendered meal diary');
        }
        const scroll = vi.fn();
        list.scrollIntoView = scroll;
        requireButton(list, '[data-tour-id="meal-list-favorites"] button').click();
        await vi.dynamicImportSettled();
        harness.detectChanges();
        const picker = document.querySelector<HTMLElement>('fd-meal-favorites-picker');
        if (picker === null) {
            throw new Error('Expected the rendered meal favorites picker');
        }
        const type = requireButton(picker, 'fd-ui-select button[aria-haspopup="listbox"]');
        expect(type.textContent).toContain('MEAL_TYPES.BREAKFAST');
        type.click();
        harness.detectChanges();
        const dinner = Array.from(document.querySelectorAll<HTMLButtonElement>('button[role="option"]')).find(
            option => option.textContent.trim() === 'MEAL_TYPES.DINNER',
        );
        if (dinner === undefined) {
            throw new Error('Expected Dinner in the meal type picker');
        }
        dinner.click();
        harness.detectChanges();

        clickDetailAction('fd-meal-favorites-picker', 'MEAL_FAVORITES.ADD');
        harness.detectChanges();
        expect(picker.querySelector('[role="alert"]')?.textContent).toBe('MEAL_FAVORITES.ADD_ERROR');
        expect(type.textContent).toContain('MEAL_TYPES.DINNER');
        expect(mealService.queryOverview).toHaveBeenCalledOnce();
        expect(scroll).not.toHaveBeenCalled();

        clickDetailAction('fd-meal-favorites-picker', 'MEAL_FAVORITES.ADD');
        await vi.waitFor(() => {
            harness.detectChanges();
            expect(picker.isConnected).toBe(false);
            expect(list.querySelectorAll('fd-meal-card')).toHaveLength(2);
        });
        expect(mealService.repeat.mock.calls).toEqual([
            [meal.id, now.toISOString(), 'DINNER'],
            [meal.id, now.toISOString(), 'DINNER'],
        ]);
        expect(scroll).toHaveBeenCalledOnce();
    });
});

describe('Meal diary favorite undo ownership', () => {
    afterEach(() => {
        TestBed.inject(FdUiDialogService).closeAll();
        vi.unstubAllGlobals();
    });

    it('announces a failed undo and restores the same favorite on retry', async () => {
        const favorite = createFavorite({ name: 'Saved breakfast' });
        const diary = await renderMealDiaryAsync({ id: favorite.mealId, isFavorite: true, favoriteMealId: favorite.id }, [favorite]);
        const { harness, favoriteService } = diary;
        favoriteService.remove.mockReturnValueOnce(of(undefined));
        favoriteService.restore.mockReturnValueOnce(throwError(() => new Error('Restore unavailable'))).mockReturnValueOnce(of(favorite));
        const picker = await openRenderedFavoritesPickerAsync(diary, favorite);
        const badge = (): Element | null | undefined => harness.routeNativeElement?.querySelector('.meal-list__favorite-count');
        expect(badge()?.textContent).toBe('1');

        requireButton(picker, 'button[aria-label="MEAL_DETAIL.REMOVE_FAVORITE"]').click();
        harness.detectChanges();
        expect(picker.querySelector('[role="status"]')?.textContent).toContain('MEAL_FAVORITES.REMOVED');
        expect(badge()).toBeNull();
        clickDetailAction('fd-meal-favorites-picker', 'MEAL_FAVORITES.UNDO');
        harness.detectChanges();

        expect(picker.querySelector('[role="status"]')?.textContent).toContain('MEAL_FAVORITES.RESTORE_ERROR');
        expect(badge()).toBeNull();
        expect(picker.querySelector('[role="status"]')?.textContent).toContain(favorite.name);
        expect(requireButton(picker, '.favorite-row__undo button').disabled).toBe(false);
        expect(harness.routeNativeElement?.querySelector('fd-meal-card button[aria-label="MEAL_DETAIL.ADD_FAVORITE"]')).not.toBeNull();

        clickDetailAction('fd-meal-favorites-picker', 'MEAL_FAVORITES.UNDO');
        harness.detectChanges();
        expect(picker.querySelector('.favorite-row__undo')).toBeNull();
        expect(badge()?.textContent).toBe('1');
        expect(picker.querySelector('.favorite-row__heading strong')?.textContent).toBe(favorite.name);
        expect(favoriteService.remove).toHaveBeenCalledExactlyOnceWith(favorite.id);
        expect(favoriteService.restore.mock.calls).toEqual([[favorite.id], [favorite.id]]);
        expect(harness.routeNativeElement?.querySelector('fd-meal-card button[aria-label="MEAL_DETAIL.REMOVE_FAVORITE"]')).not.toBeNull();
    });

    it('dismisses the open picker and cancels its pending undo when leaving the diary', async () => {
        const diary = await renderMealDiaryAsync();
        const { harness, meal, favoriteService } = diary;
        const favorite = createFavorite({ mealId: meal.id, name: 'Saved lunch' });
        const pendingUndo = new Subject<FavoriteMeal>();
        favoriteService.remove.mockReturnValueOnce(of(undefined));
        favoriteService.restore.mockReturnValueOnce(pendingUndo);
        const picker = await openRenderedFavoritesPickerAsync(diary, favorite);

        requireButton(picker, 'button[aria-label="MEAL_DETAIL.REMOVE_FAVORITE"]').click();
        harness.detectChanges();
        clickDetailAction('fd-meal-favorites-picker', 'MEAL_FAVORITES.UNDO');
        harness.detectChanges();
        expect(pendingUndo.observed).toBe(true);

        await harness.navigateByUrl('/statistics', DiaryDestinationTestPage);
        expect(harness.routeNativeElement?.querySelector('h1')?.textContent).toBe('Statistics');
        expect(picker.isConnected).toBe(false);
        expect(pendingUndo.observed).toBe(false);
    });
});

async function openRenderedFavoritesPickerAsync(
    { harness, favoriteService }: RenderedMealDiary,
    favorite: FavoriteMeal,
): Promise<HTMLElement> {
    favoriteService.getPage.mockReturnValue(of({ data: [favorite], page: 1, limit: PAGE_LIMIT, totalItems: 1, totalPages: 1 }));
    for (const block of await harness.fixture.getDeferBlocks()) {
        await block.render(DeferBlockState.Complete);
    }
    harness.detectChanges();
    const route = harness.routeNativeElement;
    if (route === null) {
        throw new Error('Meal diary was not rendered');
    }
    requireButton(route, '[data-tour-id="meal-list-favorites"] button').click();
    await vi.dynamicImportSettled();
    harness.detectChanges();
    const picker = document.querySelector<HTMLElement>('fd-meal-favorites-picker');
    if (picker === null) {
        throw new Error('Expected meal favorites picker');
    }
    return picker;
}

type RenderedMealDiary = {
    harness: RouterTestingHarness;
    meal: Meal;
    mealService: {
        queryOverview: ReturnType<typeof vi.fn<MealService['queryOverview']>>;
        repeat: ReturnType<typeof vi.fn<MealService['repeat']>>;
        deleteById: ReturnType<typeof vi.fn<MealService['deleteById']>>;
    };
    favoriteService: {
        add: ReturnType<typeof vi.fn<FavoriteMealService['add']>>;
        getLookupPage: ReturnType<typeof vi.fn<FavoriteMealService['getLookupPage']>>;
        getPage: ReturnType<typeof vi.fn<FavoriteMealService['getPage']>>;
        remove: ReturnType<typeof vi.fn<FavoriteMealService['remove']>>;
        restore: ReturnType<typeof vi.fn<FavoriteMealService['restore']>>;
    };
};

async function renderMealDiaryAsync(overrides: Partial<Meal> = {}, initialFavorites: FavoriteMeal[] = []): Promise<RenderedMealDiary> {
    const meal = createMockMeal({ id: entityId<'meal'>('diary-meal'), ...overrides });
    const mealService = {
        queryOverview: vi
            .fn<MealService['queryOverview']>()
            .mockReturnValue(
                of({ ...createOverview([meal]), favoriteItems: initialFavorites, favoriteTotalCount: initialFavorites.length }),
            ),
        repeat: vi.fn<MealService['repeat']>().mockReturnValue(of(meal)),
        deleteById: vi.fn<MealService['deleteById']>().mockReturnValue(of(undefined)),
    };
    const favoriteService = {
        isFavorite: vi.fn<FavoriteMealService['isFavorite']>().mockReturnValue(of(meal.isFavorite ?? false)),
        add: vi.fn<FavoriteMealService['add']>(),
        getLookupPage: vi.fn<FavoriteMealService['getLookupPage']>(),
        getPage: vi.fn<FavoriteMealService['getPage']>(),
        remove: vi.fn<FavoriteMealService['remove']>(),
        restore: vi.fn<FavoriteMealService['restore']>(),
    };
    vi.stubGlobal('matchMedia', (media: string) => ({ matches: false, media }));
    TestBed.configureTestingModule({
        deferBlockBehavior: DeferBlockBehavior.Manual,
        providers: [
            provideTranslateTesting(),
            provideRouter([
                { path: 'meals', component: MealListComponent },
                { path: 'statistics', component: DiaryDestinationTestPage },
            ]),
            provideHttpClient(),
            provideHttpClientTesting(),
            { provide: AuthService, useValue: { isAuthenticated: signal(true), isPremium: signal(false), getUserId: (): null => null } },
            { provide: MealService, useValue: mealService },
            { provide: FavoriteMealService, useValue: favoriteService },
            { provide: BreakpointObserver, useValue: mockBreakpointObserver },
        ],
    });
    const harness = await RouterTestingHarness.create('/meals');
    return { harness, meal, mealService, favoriteService };
}

async function openRenderedMealDetailsAsync(overrides: Partial<Meal> = {}): Promise<RenderedMealDiary> {
    const diary = await renderMealDiaryAsync(overrides);
    const { harness } = diary;
    const route = harness.routeNativeElement;
    if (route === null) {
        throw new Error('Meal diary was not rendered');
    }
    requireButton(route, 'fd-meal-card .entity-card__open-button').click();
    await vi.dynamicImportSettled();
    await harness.fixture.whenStable();
    harness.detectChanges();
    expect(document.querySelector('fd-meal-detail .meal-detail__title')?.textContent).toBe('MEAL_DETAIL.TITLE');
    return diary;
}

function clickDetailAction(selector: string, label: string): void {
    const control = [...document.querySelectorAll<HTMLButtonElement>(`${selector} button`)].find(
        button => button.querySelector('.fd-ui-button__label')?.textContent.trim() === label,
    );
    if (control === undefined) {
        throw new Error(`Details action ${label} was not rendered`);
    }
    control.click();
}

function requireButton(root: Document | HTMLElement, selector: string): HTMLButtonElement {
    const control = root.querySelector<HTMLButtonElement>(selector);
    if (control === null) {
        throw new Error(`Details control ${selector} was not rendered`);
    }
    return control;
}

function registerFilterInteractionTests(context: TestContext): void {
    it('applies changed filters, resets to page one, and removes individual chips', () => {
        const component = context.component();
        const filters = {
            dateRange: { start: new Date('2026-01-01T00:00:00'), end: new Date('2026-01-02T00:00:00') },
            mealTypes: ['Dinner'],
            caloriesFrom: 0,
            caloriesTo: 500,
            hasImage: false,
            hasAiSession: true,
        };
        context.mockFdDialogService.open.mockReturnValue({ afterClosed: () => of(filters) });
        component['openFilters']();
        expect(component['searchModel']()).toMatchObject(filters);
        expect(component['activeFilterChips']().length).toBeGreaterThan(0);
        expect(component['hasActiveFilters']()).toBe(true);
        expect(context.mockMealService.queryOverview).toHaveBeenLastCalledWith(
            1,
            expect.any(Number),
            expect.objectContaining({ caloriesFrom: 0, hasImage: false }),
            expect.anything(),
        );
        component['removeFilter']('hasImage');
        component['resetFilters']();
        expect(component['hasActiveFilters']()).toBe(false);
    });
    it.each([null, undefined])('does not reload after cancelling filters: %s', result => {
        context.mockFdDialogService.open.mockReturnValue({ afterClosed: () => of(result) });
        const previousCalls = context.mockMealService.queryOverview.mock.calls.length;
        context.component()['openFilters']();
        expect(context.mockMealService.queryOverview).toHaveBeenCalledTimes(previousCalls);
    });
    it('ignores equivalent filters including reordered meal types', () => {
        const component = context.component();
        component['searchModel'].update(value => ({ ...value, mealTypes: ['Dinner', 'Snack'] }));
        const filters = { ...component['searchModel'](), mealTypes: ['Snack', 'Dinner'] };
        context.mockFdDialogService.open.mockReturnValue({ afterClosed: () => of(filters) });
        const previousCalls = context.mockMealService.queryOverview.mock.calls.length;
        component['openFilters']();
        expect(context.mockMealService.queryOverview).toHaveBeenCalledTimes(previousCalls);
    });
    it('retries both the unfiltered overview and filtered results', () => {
        context.component()['retryLoad']();
        expect(context.mockMealService.queryOverview).toHaveBeenLastCalledWith(
            1,
            expect.any(Number),
            expect.anything(),
            expect.objectContaining({ include: true }),
        );
        context.component()['searchModel'].update(value => ({ ...value, hasImage: true }));
        context.component()['retryLoad']();
        expect(context.mockMealService.queryOverview).toHaveBeenLastCalledWith(
            1,
            expect.any(Number),
            expect.objectContaining({ hasImage: true }),
            expect.objectContaining({ include: false }),
        );
    });
}

function registerFavoritesPickerWiringTests(context: TestContext): void {
    it('should add meal favorite and refresh favorites', () => {
        const meal = createMockMeal({ id: entityId<'meal'>('meal-1'), isFavorite: false, favoriteMealId: null });
        context.component()['mealData'].setData(createPageOf([meal]));

        context.component()['onMealFavoriteToggle'](meal);

        expect(context.mockFavoriteMealService.add).toHaveBeenCalledWith('meal-1');
        expect(context.component()['mealData'].items()[0]).toMatchObject({ isFavorite: true, favoriteMealId: 'favorite-1' });
        expect(context.mockFavoriteMealService.getPage).toHaveBeenCalled();
    });

    it('wires remove, restore and repeat to the current diary filters', async () => {
        const fixture = context.fixture();
        const favorite = createFavorite();
        const closed = new Subject<boolean>();
        mockDialogRef.afterClosed.mockReturnValueOnce(closed);
        const button = await renderFavoritesButtonAsync(fixture);
        const scroll = vi.fn();
        const container = button.closest<HTMLElement>('[fdpagecontainer]');
        if (container === null) {
            throw new Error('Expected the diary container');
        }
        container.scrollIntoView = scroll;

        button.click();
        await vi.dynamicImportSettled();
        const options = context.mockFdDialogService.open.mock.calls.at(-1)?.[1] as {
            data: {
                remove: (item: FavoriteMeal) => Observable<boolean>;
                restore: (item: FavoriteMeal) => Observable<boolean>;
                repeat: (item: FavoriteMeal, mealType: string) => Observable<boolean>;
            };
        };
        options.data.remove(favorite).subscribe();
        options.data.restore(favorite).subscribe();
        options.data.repeat(favorite, 'DINNER').subscribe();
        expect(context.mockFavoriteMealService.remove).toHaveBeenCalledWith('favorite-1');
        expect(context.mockFavoriteMealService.restore).toHaveBeenCalledWith('favorite-1');
        expect(context.mockMealService.repeat).toHaveBeenCalledWith('meal-1', expect.any(String), 'DINNER');
        expect(scroll).not.toHaveBeenCalled();

        closed.next(true);
        closed.complete();

        expect(scroll).toHaveBeenCalledOnce();
    });

    it('does not open the favorites picker after leaving the diary during its lazy load', async () => {
        const fixture = context.fixture();
        const button = await renderFavoritesButtonAsync(fixture);

        button.click();
        fixture.destroy();
        await vi.dynamicImportSettled();

        expect(context.mockFdDialogService.open).not.toHaveBeenCalled();
    });
    it('refreshes the current page after creating a meal and toggles the planned section', () => {
        const component = context.component();
        context.fixture().detectChanges();
        component['togglePlanned']();
        expect(component['isPlannedOpen']()).toBe(false);
        component['container']().nativeElement.scrollIntoView = vi.fn();
        component['onMealCreated']();
        expect(context.mockMealService.queryOverview).toHaveBeenLastCalledWith(
            1,
            expect.any(Number),
            expect.anything(),
            expect.objectContaining({ include: false }),
        );
    });
}

async function renderFavoritesButtonAsync(fixture: ComponentFixture<MealListComponent>): Promise<HTMLButtonElement> {
    fixture.detectChanges();
    const [actions] = await fixture.getDeferBlocks();
    await actions.render(DeferBlockState.Complete);
    const host = fixture.nativeElement as HTMLElement;
    const button = host.querySelector<HTMLButtonElement>('[data-tour-id="meal-list-favorites"] button');
    if (button === null) {
        throw new Error('Expected the rendered favorites action');
    }
    return button;
}
