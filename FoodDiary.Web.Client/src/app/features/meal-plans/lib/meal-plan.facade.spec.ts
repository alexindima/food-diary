import { signal } from '@angular/core';
import { TestBed } from '@angular/core/testing';
import { EMPTY, of, Subject, throwError } from 'rxjs';
import { beforeEach, describe, expect, it, vi } from 'vitest';

import { waitForAsyncTasksAsync } from '../../../../testing/async-testing';
import type { PageOf } from '../../../shared/models/page-of.data';
import { utcInstant } from '../../../shared/models/semantics/date-value';
import { entityId } from '../../../shared/models/semantics/entity-id';
import type { ShoppingList } from '../../../shared/models/shopping-list.data';
import { QuickMealService } from '../../meals/contracts/quick-meal';
import { RECIPE_LOOKUP } from '../../recipes/contracts/recipe-lookup';
import { MealPlanService } from '../api/meal-plan.service';
import type { MealPlan, MealPlanMeal, MealPlanSummary } from '../models/meal-plan.data';
import { planDurationDaysFromStored } from '../models/meal-plan-values';
import { plannedServingsFromStored } from '../models/meal-plan-values';
import { plannedMealTypeFromStored } from '../models/meal-plan-values';
import { MealPlanListFacade } from './list/meal-plan-list.facade';
import { type MealPlanListQuery, mealPlanListQueryKey } from './list/meal-plan-list-query';
import { MEAL_PLAN_LIST_QUERY_STATE, type MealPlanListQueryState } from './list/meal-plan-list-query-state';
import { MealPlanFacade } from './meal-plan.facade';

const PAGE_SIZE = 50;
const MULTI_PAGE_TOTAL = PAGE_SIZE + 1;
const OUT_OF_RANGE_PAGE = 99;
const WAIT_ATTEMPTS = 20;
const PLAN_DAYS = 7;
const TARGET_CALORIES = 1800;
const TOTAL_RECIPES = 21;

type MealPlanServiceMock = {
    deletePlan: ReturnType<typeof vi.fn>;
    adopt: ReturnType<typeof vi.fn>;
    generateShoppingList: ReturnType<typeof vi.fn>;
    getPage: ReturnType<typeof vi.fn>;
    getById: ReturnType<typeof vi.fn>;
};

let facade: MealPlanFacade;
let listFacade: MealPlanListFacade;
let mealPlanService: MealPlanServiceMock;
const recipeLookup = { getById: vi.fn() };
const quickMeal = { hasItems: signal(false), addRecipe: vi.fn(), updateDetails: vi.fn() };

beforeEach(() => {
    TestBed.resetTestingModule();
    recipeLookup.getById.mockReset().mockReturnValue(of({ id: 'recipe-1' }));
    quickMeal.hasItems.set(false);
    quickMeal.addRecipe.mockReset();
    quickMeal.updateDetails.mockReset();
    mealPlanService = {
        deletePlan: vi.fn(() => of(void 0)),
        getPage: vi.fn(() => of({ data: [createSummary()], page: 1, limit: PAGE_SIZE, totalPages: 1, totalItems: 1 })),
        getById: vi.fn(() => of(createMealPlan())),
        adopt: vi.fn(() => of(createMealPlan())),
        generateShoppingList: vi.fn(() => of(createShoppingList())),
    };

    const initial: MealPlanListQuery = { page: 1, dietType: null, source: 'all' };
    const current = signal(initial, { equal: (left, right) => mealPlanListQueryKey(left) === mealPlanListQueryKey(right) });
    const commitAsync = vi.fn<() => Promise<boolean>>().mockResolvedValue(true);
    const catalogueRoute: MealPlanListQueryState = {
        initial,
        current,
        changes: EMPTY,
        writeAsync: async (query): Promise<boolean> => {
            current.set(query);
            return commitAsync();
        },
        normalizePageAsync: vi.fn().mockResolvedValue(false),
    };
    TestBed.configureTestingModule({
        providers: [
            MealPlanFacade,
            MealPlanListFacade,
            { provide: MealPlanService, useValue: mealPlanService },
            { provide: RECIPE_LOOKUP, useValue: recipeLookup },
            { provide: QuickMealService, useValue: quickMeal },
            { provide: MEAL_PLAN_LIST_QUERY_STATE, useValue: catalogueRoute },
        ],
    });

    facade = TestBed.inject(MealPlanFacade);
    listFacade = TestBed.inject(MealPlanListFacade);
});

describe('MealPlanFacade request lifetime', () => {
    it('unsubscribes catalogue and detail reads on owner destruction', async () => {
        const page = new Subject<PageOf<MealPlanSummary>>();
        const detail = new Subject<MealPlan>();
        mealPlanService.getPage.mockReturnValue(page);
        mealPlanService.getById.mockReturnValue(detail);
        facade.loadPlan(entityId<'meal-plan'>('plan-1'));
        await waitForAsync(() => page.observed && detail.observed);
        expect([page.observed, detail.observed]).toEqual([true, true]);

        TestBed.resetTestingModule();

        expect([page.observed, detail.observed]).toEqual([false, false]);
    });

    it('unsubscribes obsolete catalogue and detail reads after selection changes', async () => {
        const page = new Subject<PageOf<MealPlanSummary>>();
        const detail = new Subject<MealPlan>();
        mealPlanService.getPage.mockReturnValueOnce(page);
        mealPlanService.getById
            .mockReturnValueOnce(detail)
            .mockReturnValueOnce(of({ ...createMealPlan(), id: entityId<'meal-plan'>('plan-2') }));
        facade.loadPlan(entityId<'meal-plan'>('plan-1'));
        await waitForAsync(() => page.observed && detail.observed);
        listFacade.loadPlans('Vegan');
        facade.loadPlan(entityId<'meal-plan'>('plan-2'));
        await waitForAsync(() => facade.selectedPlan()?.id === 'plan-2' && !listFacade.isLoading());

        expect([page.observed, detail.observed]).toEqual([false, false]);
        expect(facade.selectedPlan()?.id).toBe('plan-2');
    });

    it('does not normalize a late catalogue response after its owner is destroyed', async () => {
        TestBed.resetTestingModule();
        const query: MealPlanListQuery = { page: OUT_OF_RANGE_PAGE, dietType: null, source: 'all' };
        const pending = new Subject<PageOf<MealPlanSummary>>();
        mealPlanService.getPage.mockReturnValue(pending);
        const writeAsync = vi.fn().mockResolvedValue(true);
        TestBed.configureTestingModule({
            providers: [
                MealPlanListFacade,
                { provide: MealPlanService, useValue: mealPlanService },
                {
                    provide: MEAL_PLAN_LIST_QUERY_STATE,
                    useValue: {
                        initial: query,
                        current: signal(query),
                        changes: EMPTY,
                        writeAsync,
                        normalizePageAsync: vi.fn().mockResolvedValue(false),
                    },
                },
            ],
        });
        listFacade = TestBed.inject(MealPlanListFacade);
        await waitForAsync(() => pending.observed);
        expect(pending.observed).toBe(true);
        writeAsync.mockClear();
        TestBed.resetTestingModule();
        pending.next(createPage(OUT_OF_RANGE_PAGE, 1));
        pending.complete();
        await waitForAsyncTasksAsync();

        expect(writeAsync).not.toHaveBeenCalled();
    });
});

describe('MealPlanFacade selection action lifetime', () => {
    it.each(['adopt', 'generateShoppingList', 'deletePlan'] as const)(
        'cancels %s callbacks when another plan is selected',
        async action => {
            mealPlanService.getById.mockReturnValueOnce(of({ ...createMealPlan(), isCurated: false }));
            facade.loadPlan(entityId<'meal-plan'>('plan-1'));
            await waitForAsync(() => facade.selectedPlan() !== null);
            const pending = new Subject<MealPlan | ShoppingList | void>();
            mealPlanService[action].mockReturnValueOnce(pending);
            const success = vi.fn();
            facade[action](entityId<'meal-plan'>('plan-1'), success);
            mealPlanService.getById.mockReturnValueOnce(of({ ...createMealPlan(), id: entityId<'meal-plan'>('plan-2') }));
            facade.loadPlan(entityId<'meal-plan'>('plan-2'));
            await waitForAsync(() => facade.selectedPlan()?.id === 'plan-2');

            expect(pending.observed).toBe(false);
            expect(facade.pendingAction()).toBeNull();
            pending.next(action === 'adopt' ? createMealPlan() : action === 'generateShoppingList' ? createShoppingList() : undefined);
            pending.complete();
            expect(success).not.toHaveBeenCalled();
            expect(facade.selectedPlan()?.id).toBe('plan-2');
            expect(facade.actionErrorKey()).toBeNull();
        },
    );

    it('finishes adding a recipe when the same meal plan is selected again', async () => {
        const planId = entityId<'meal-plan'>('plan-1');
        facade.loadPlan(planId);
        await waitForAsync(() => facade.selectedPlan()?.id === planId);
        const recipe = { id: 'recipe-1' };
        const lookup = new Subject<typeof recipe>();
        recipeLookup.getById.mockReturnValueOnce(lookup);
        const meal = createPlannedMeal();
        facade.addMealToDiary(meal);

        facade.loadPlan(planId);
        expect(facade.addingMealId()).toBe(meal.id);
        expect(lookup.observed).toBe(true);
        lookup.next(recipe);
        lookup.complete();

        expect(quickMeal.addRecipe).toHaveBeenCalledExactlyOnceWith(recipe, meal.servings);
        expect(facade.addingMealId()).toBeNull();
        expect(facade.actionErrorKey()).toBeNull();
    });

    it('cancels an old recipe lookup without applying its error to the new plan', async () => {
        facade.loadPlan(entityId<'meal-plan'>('plan-1'));
        await waitForAsync(() => facade.selectedPlan() !== null);
        const pending = new Subject<null>();
        recipeLookup.getById.mockReturnValueOnce(pending);
        facade.addMealToDiary(createPlannedMeal());
        mealPlanService.getById.mockReturnValueOnce(of({ ...createMealPlan(), id: entityId<'meal-plan'>('plan-2') }));
        facade.loadPlan(entityId<'meal-plan'>('plan-2'));
        await waitForAsync(() => facade.selectedPlan()?.id === 'plan-2');

        expect(pending.observed).toBe(false);
        expect(facade.addingMealId()).toBeNull();
        pending.next(null);
        pending.complete();
        expect(quickMeal.addRecipe).not.toHaveBeenCalled();
        expect(facade.actionErrorKey()).toBeNull();
    });
});

describe('MealPlanFacade personal deletion', () => {
    it('rejects a curated plan and an unrelated selected id before issuing DELETE', async () => {
        facade.loadPlan(entityId<'meal-plan'>('plan-1'));
        await waitForAsync(() => facade.selectedPlan() !== null);
        facade.deletePlan(entityId<'meal-plan'>('plan-1'), vi.fn());
        facade.deletePlan(entityId<'meal-plan'>('other'), vi.fn());
        expect(mealPlanService.deletePlan).not.toHaveBeenCalled();
    });

    it('locks repeated actions while deleting, reports errors and permits retry without losing the plan', async () => {
        mealPlanService.getById.mockReturnValue(of({ ...createMealPlan(), isCurated: false }));
        facade.loadPlan(entityId<'meal-plan'>('plan-1'));
        await waitForAsync(() => facade.selectedPlan() !== null);
        const response = new Subject<void>();
        mealPlanService.deletePlan.mockReturnValueOnce(response);
        const onSuccess = vi.fn();
        facade.deletePlan(entityId<'meal-plan'>('plan-1'), onSuccess);
        facade.deletePlan(entityId<'meal-plan'>('plan-1'), onSuccess);
        facade.generateShoppingList(entityId<'meal-plan'>('plan-1'), vi.fn());
        expect(facade.pendingAction()).toBe('delete');
        expect(mealPlanService.deletePlan).toHaveBeenCalledOnce();
        expect(mealPlanService.generateShoppingList).not.toHaveBeenCalled();
        response.error(new Error('Unavailable'));
        expect(facade.pendingAction()).toBeNull();
        expect(facade.actionErrorKey()).toBe('MEAL_PLANS.ERROR_DELETE');
        expect(facade.selectedPlan()?.id).toBe('plan-1');
        expect(onSuccess).not.toHaveBeenCalled();
        facade.deletePlan(entityId<'meal-plan'>('plan-1'), onSuccess);
        expect(facade.actionErrorKey()).toBeNull();
        expect(onSuccess).toHaveBeenCalledOnce();
        await waitForAsync(() => facade.selectedPlan() === null);
    });
});

describe('MealPlanFacade', () => {
    it('prepares the selected plan meal with its servings and type in the diary draft', () => {
        facade.addMealToDiary(createPlannedMeal());
        expect(quickMeal.addRecipe).toHaveBeenCalledWith({ id: 'recipe-1' }, 2);
        expect(quickMeal.updateDetails).toHaveBeenCalledWith({ mealType: 'BREAKFAST' });
    });
    it('preserves an existing diary draft and reports an unavailable recipe', () => {
        const meal = createPlannedMeal();
        quickMeal.hasItems.set(true);
        facade.addMealToDiary(meal);
        expect(recipeLookup.getById).not.toHaveBeenCalled();
        quickMeal.hasItems.set(false);
        recipeLookup.getById.mockReturnValueOnce(of(null));
        facade.addMealToDiary(meal);
        expect(quickMeal.addRecipe).not.toHaveBeenCalled();
        expect(facade.actionErrorKey()).toBe('MEAL_PLANS.ERROR_ADD_MEAL');
    });
    it('loads meal plans with selected diet type filter', async () => {
        await waitForAsync(() => listFacade.plans().length > 0);
        listFacade.loadPlans('Keto');

        await waitForAsync(() => listFacade.plans().length > 0);

        expect(mealPlanService.getPage).toHaveBeenLastCalledWith('Keto', 1, PAGE_SIZE, undefined);
        expect(listFacade.plans()).toEqual([createSummary()]);
    });

    it('loads selected plan detail and ignores empty ids', async () => {
        expect(facade.selectedPlan()).toBeNull();

        facade.loadPlan(entityId<'meal-plan'>(''));
        await waitForAsync(() => mealPlanService.getById.mock.calls.length === 0);
        expect(facade.selectedPlan()).toBeNull();

        facade.loadPlan(entityId<'meal-plan'>('plan-1'));
        await waitForAsync(() => facade.selectedPlan() !== null);

        expect(mealPlanService.getById).toHaveBeenCalledWith('plan-1');
        expect(facade.selectedPlan()).toEqual(createMealPlan());
    });

    it('runs success callback after adopting a meal plan', () => {
        const onSuccess = vi.fn();

        facade.adopt(entityId<'meal-plan'>('plan-1'), onSuccess);

        expect(mealPlanService.adopt).toHaveBeenCalledWith('plan-1');
        expect(onSuccess).toHaveBeenCalledOnce();
    });

    it('recovers from catalog failure while preserving the diet filter', async () => {
        await waitForAsync(() => listFacade.plans().length > 0);
        mealPlanService.getPage.mockReturnValueOnce(throwError(() => new Error('Unavailable')));
        listFacade.loadPlans('Keto');
        await waitForAsync(() => listFacade.hasLoadError());
        expect(listFacade.hasLoadError()).toBe(true);
        expect(listFacade.plans()).toEqual([]);

        listFacade.retryPlans();
        await waitForAsync(() => listFacade.plans().length > 0);
        expect(listFacade.hasLoadError()).toBe(false);
        expect(mealPlanService.getPage).toHaveBeenLastCalledWith('Keto', 1, PAGE_SIZE, undefined);
        expect(listFacade.plans()).toEqual([createSummary()]);
    });

    it('runs success callback after generating a shopping list', () => {
        const onSuccess = vi.fn();

        facade.generateShoppingList(entityId<'meal-plan'>('plan-1'), onSuccess);

        expect(mealPlanService.generateShoppingList).toHaveBeenCalledWith('plan-1');
        expect(onSuccess).toHaveBeenCalledOnce();
    });

    it('blocks repeated actions until shopping creation fails and permits retry', () => {
        const response = new Subject<ShoppingList>();
        mealPlanService.generateShoppingList.mockReturnValueOnce(response);
        const onSuccess = vi.fn();
        facade.generateShoppingList(entityId<'meal-plan'>('plan-1'), onSuccess);
        facade.generateShoppingList(entityId<'meal-plan'>('plan-1'), onSuccess);
        facade.adopt(entityId<'meal-plan'>('plan-1'), onSuccess);
        expect(mealPlanService.generateShoppingList).toHaveBeenCalledOnce();
        expect(mealPlanService.adopt).not.toHaveBeenCalled();
        expect(facade.pendingAction()).toBe('shopping');

        response.error(new Error('Unavailable'));
        expect(facade.pendingAction()).toBeNull();
        expect(facade.actionErrorKey()).toBe('MEAL_PLANS.ERROR_SHOPPING_LIST');
        expect(onSuccess).not.toHaveBeenCalled();

        facade.generateShoppingList(entityId<'meal-plan'>('plan-1'), onSuccess);
        expect(facade.actionErrorKey()).toBeNull();
        expect(onSuccess).toHaveBeenCalledOnce();
        expect(facade.pendingAction()).toBeNull();
    });
});

describe('MealPlanListFacade pagination', () => {
    beforeEach(() => {
        mealPlanService.getPage.mockReset();
        mealPlanService.getPage.mockReturnValue(of(createPage(1, MULTI_PAGE_TOTAL)));
    });
    it('keeps pagination while loading and retries the requested page after failure', async () => {
        mealPlanService.getPage.mockReturnValueOnce(of(createPage(1, MULTI_PAGE_TOTAL)));
        await waitForAsync(() => listFacade.totalItems() === MULTI_PAGE_TOTAL);
        const pending = new Subject<PageOf<MealPlanSummary>>();
        mealPlanService.getPage.mockReturnValueOnce(pending);
        listFacade.changePage(1);
        await waitForAsync(() => listFacade.isLoading());
        expect(listFacade.totalItems()).toBe(MULTI_PAGE_TOTAL);
        expect(mealPlanService.getPage).toHaveBeenLastCalledWith(undefined, 2, PAGE_SIZE, undefined);
        pending.error(new Error('Unavailable'));
        await waitForAsync(() => listFacade.hasLoadError());
        expect(listFacade.pageIndex()).toBe(1);
        expect(listFacade.totalItems()).toBe(MULTI_PAGE_TOTAL);
        mealPlanService.getPage.mockReturnValueOnce(of(createPage(2, MULTI_PAGE_TOTAL)));
        listFacade.retryPlans();
        await waitForAsync(() => listFacade.plans()[0]?.id === 'page-2');
        expect(mealPlanService.getPage).toHaveBeenLastCalledWith(undefined, 2, PAGE_SIZE, undefined);
        expect(listFacade.hasLoadError()).toBe(false);
    });

    it('resets pagination on filtering and ignores a late response from the previous filter', async () => {
        mealPlanService.getPage.mockReturnValueOnce(of(createPage(1, MULTI_PAGE_TOTAL)));
        await waitForAsync(() => listFacade.totalItems() === MULTI_PAGE_TOTAL);
        const stalePage = new Subject<PageOf<MealPlanSummary>>();
        mealPlanService.getPage.mockReturnValueOnce(stalePage);
        listFacade.changePage(1);
        await waitForAsync(() => listFacade.isLoading());
        mealPlanService.getPage.mockReturnValueOnce(of(createPage(1, 1)));
        listFacade.loadPlans('Keto');
        await waitForAsync(() => listFacade.plans()[0]?.id === 'page-1');
        stalePage.next(createPage(2, MULTI_PAGE_TOTAL));
        await waitForAsyncTasksAsync();
        TestBed.tick();
        expect(listFacade.pageIndex()).toBe(0);
        expect(listFacade.totalItems()).toBe(1);
        expect(listFacade.plans()[0]?.id).toBe('page-1');
        expect(mealPlanService.getPage).toHaveBeenLastCalledWith('Keto', 1, PAGE_SIZE, undefined);
    });

    it('bounds page requests and avoids reloading the current page', async () => {
        mealPlanService.getPage.mockReturnValueOnce(of(createPage(1, MULTI_PAGE_TOTAL)));
        await waitForAsync(() => listFacade.totalItems() === MULTI_PAGE_TOTAL);
        listFacade.changePage(-1);
        TestBed.tick();
        expect(mealPlanService.getPage).toHaveBeenCalledOnce();
        mealPlanService.getPage.mockReturnValueOnce(of(createPage(2, MULTI_PAGE_TOTAL)));
        listFacade.changePage(OUT_OF_RANGE_PAGE);
        await waitForAsync(() => listFacade.plans()[0]?.id === 'page-2');
        expect(listFacade.pageIndex()).toBe(1);
        expect(mealPlanService.getPage).toHaveBeenLastCalledWith(undefined, 2, PAGE_SIZE, undefined);
    });

    it('keeps the current page when either active filter is selected again', async () => {
        await waitForAsync(() => listFacade.totalItems() === MULTI_PAGE_TOTAL);
        mealPlanService.getPage.mockReturnValueOnce(of(createPage(2, MULTI_PAGE_TOTAL)));
        listFacade.changePage(1);
        await waitForAsync(() => listFacade.plans()[0]?.id === 'page-2');

        listFacade.loadPlans(null);
        TestBed.tick();
        expect(listFacade.pageIndex()).toBe(1);
        listFacade.filterBySource('all');
        TestBed.tick();

        expect(listFacade.pageIndex()).toBe(1);
        expect(listFacade.plans()[0]?.id).toBe('page-2');
        expect(mealPlanService.getPage).toHaveBeenCalledTimes(2);
    });
});

describe('MealPlanListFacade catalogue navigation', () => {
    it('keeps the filtered catalogue available when normalizing and writing its URL fail', async () => {
        TestBed.resetTestingModule();
        const query: MealPlanListQuery = { page: 1, dietType: 'Keto', source: 'curated' };
        const routeState: MealPlanListQueryState = {
            initial: query,
            current: signal(query),
            changes: EMPTY,
            normalizePageAsync: vi.fn().mockRejectedValue(new Error('Could not normalize the catalogue URL')),
            writeAsync: vi.fn().mockRejectedValue(new Error('Could not update the catalogue URL')),
        };
        TestBed.configureTestingModule({
            providers: [
                MealPlanListFacade,
                { provide: MealPlanService, useValue: mealPlanService },
                { provide: MEAL_PLAN_LIST_QUERY_STATE, useValue: routeState },
            ],
        });
        listFacade = TestBed.inject(MealPlanListFacade);
        await waitForAsync(() => listFacade.plans().length > 0);
        await waitForAsyncTasksAsync();

        expect(routeState.normalizePageAsync).toHaveBeenCalledOnce();
        expect(routeState.writeAsync).toHaveBeenCalledWith(query, { replaceUrl: false });
        expect(mealPlanService.getPage).toHaveBeenCalledExactlyOnceWith('Keto', 1, PAGE_SIZE, true);
        expect(listFacade.plans()).toEqual([createSummary()]);
        expect(listFacade.hasLoadError()).toBe(false);
    });
});

async function waitForAsync(predicate: () => boolean): Promise<void> {
    for (let attempt = 0; attempt < WAIT_ATTEMPTS; attempt++) {
        TestBed.tick();

        if (predicate()) {
            return;
        }

        await waitForAsyncTasksAsync();
    }
}

function createPlannedMeal(): MealPlanMeal {
    return {
        id: entityId<'meal-plan-meal'>('meal-1'),
        recipeId: entityId<'recipe'>('recipe-1'),
        mealType: plannedMealTypeFromStored('Breakfast'),
        servings: plannedServingsFromStored(2),
        calories: 200,
    };
}

function createSummary(): MealPlanSummary {
    return {
        id: entityId<'meal-plan'>('plan-1'),
        name: 'Keto plan',
        description: null,
        dietType: 'Keto',
        durationDays: planDurationDaysFromStored(PLAN_DAYS),
        targetCaloriesPerDay: TARGET_CALORIES,
        isCurated: true,
        totalRecipes: TOTAL_RECIPES,
    };
}

function createPage(page: number, totalItems: number): PageOf<MealPlanSummary> {
    return {
        data: [{ ...createSummary(), id: entityId<'meal-plan'>(`page-${page}`) }],
        page,
        limit: PAGE_SIZE,
        totalPages: Math.ceil(totalItems / PAGE_SIZE),
        totalItems,
    };
}

function createMealPlan(): MealPlan {
    return {
        ...createSummary(),
        days: [],
    };
}

function createShoppingList(): ShoppingList {
    return {
        revision: 'aaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaa',
        id: entityId<'shopping-list'>('shopping-list-1'),
        name: 'Keto plan',
        items: [],
        createdAt: utcInstant('2026-05-15T00:00:00Z'),
    };
}
