import { signal } from '@angular/core';
import { TestBed } from '@angular/core/testing';
import { of, Subject, throwError } from 'rxjs';
import { beforeEach, describe, expect, it, vi } from 'vitest';

import { waitForAsyncTasksAsync } from '../../../../testing/async-testing';
import type { PageOf } from '../../../shared/models/page-of.data';
import type { ShoppingList } from '../../../shared/models/shopping-list.data';
import { QuickMealService } from '../../meals/contracts/quick-meal';
import { RECIPE_LOOKUP } from '../../recipes/contracts/recipe-lookup';
import { MealPlanService } from '../api/meal-plan.service';
import type { MealPlan, MealPlanSummary } from '../models/meal-plan.data';
import { MealPlanFacade } from './meal-plan.facade';

const PAGE_SIZE = 50;
const MULTI_PAGE_TOTAL = PAGE_SIZE + 1;
const OUT_OF_RANGE_PAGE = 99;
const WAIT_ATTEMPTS = 20;
const PLAN_DAYS = 7;
const TARGET_CALORIES = 1800;
const TOTAL_RECIPES = 21;

type MealPlanServiceMock = {
    adopt: ReturnType<typeof vi.fn>;
    generateShoppingList: ReturnType<typeof vi.fn>;
    getPage: ReturnType<typeof vi.fn>;
    getById: ReturnType<typeof vi.fn>;
};

let facade: MealPlanFacade;
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
        getPage: vi.fn(() => of({ data: [createSummary()], page: 1, limit: PAGE_SIZE, totalPages: 1, totalItems: 1 })),
        getById: vi.fn(() => of(createMealPlan())),
        adopt: vi.fn(() => of(createMealPlan())),
        generateShoppingList: vi.fn(() => of(createShoppingList())),
    };

    TestBed.configureTestingModule({
        providers: [MealPlanFacade, { provide: MealPlanService, useValue: mealPlanService },
            { provide: RECIPE_LOOKUP, useValue: recipeLookup }, { provide: QuickMealService, useValue: quickMeal }],
    });

    facade = TestBed.inject(MealPlanFacade);
});

describe('MealPlanFacade', () => {
    it('prepares the selected plan meal with its servings and type before navigating', () => {
        const onSuccess = vi.fn();
        facade.addMealToDiary({ id: 'meal-1', recipeId: 'recipe-1', mealType: 'Breakfast', servings: 2, calories: 200 }, onSuccess);
        expect(quickMeal.addRecipe).toHaveBeenCalledWith({ id: 'recipe-1' }, 2);
        expect(quickMeal.updateDetails).toHaveBeenCalledWith({ mealType: 'BREAKFAST' });
        expect(onSuccess).toHaveBeenCalledOnce();
    });
    it('preserves an existing diary draft and reports an unavailable recipe', () => {
        const meal = { id: 'meal-1', recipeId: 'recipe-1', mealType: 'Breakfast', servings: 2, calories: 200 };
        quickMeal.hasItems.set(true);
        facade.addMealToDiary(meal, vi.fn());
        expect(recipeLookup.getById).not.toHaveBeenCalled();
        quickMeal.hasItems.set(false);
        recipeLookup.getById.mockReturnValueOnce(of(null));
        facade.addMealToDiary(meal, vi.fn());
        expect(quickMeal.addRecipe).not.toHaveBeenCalled();
        expect(facade.actionErrorKey()).toBe('MEAL_PLANS.ERROR_ADD_MEAL');
    });
    it('loads meal plans with selected diet type filter', async () => {
        facade.loadPlans('Keto');

        await waitForAsync(() => facade.plans().length > 0);

        expect(mealPlanService.getPage).toHaveBeenLastCalledWith('Keto', 1, PAGE_SIZE);
        expect(facade.plans()).toEqual([createSummary()]);
    });

    it('loads selected plan detail and ignores empty ids', async () => {
        expect(facade.selectedPlan()).toBeNull();

        facade.loadPlan('');
        await waitForAsync(() => mealPlanService.getById.mock.calls.length === 0);
        expect(facade.selectedPlan()).toBeNull();

        facade.loadPlan('plan-1');
        await waitForAsync(() => facade.selectedPlan() !== null);

        expect(mealPlanService.getById).toHaveBeenCalledWith('plan-1');
        expect(facade.selectedPlan()).toEqual(createMealPlan());
    });

    it('runs success callback after adopting a meal plan', () => {
        const onSuccess = vi.fn();

        facade.adopt('plan-1', onSuccess);

        expect(mealPlanService.adopt).toHaveBeenCalledWith('plan-1');
        expect(onSuccess).toHaveBeenCalledOnce();
    });

    it('recovers from catalog failure while preserving the diet filter', async () => {
        mealPlanService.getPage.mockReturnValueOnce(throwError(() => new Error('Unavailable')));
        facade.loadPlans('Keto');
        await waitForAsync(() => facade.hasLoadError());
        expect(facade.hasLoadError()).toBe(true);
        expect(facade.plans()).toEqual([]);

        facade.retryPlans();
        await waitForAsync(() => facade.plans().length > 0);
        expect(facade.hasLoadError()).toBe(false);
        expect(mealPlanService.getPage).toHaveBeenLastCalledWith('Keto', 1, PAGE_SIZE);
        expect(facade.plans()).toEqual([createSummary()]);
    });

    it('runs success callback after generating a shopping list', () => {
        const onSuccess = vi.fn();

        facade.generateShoppingList('plan-1', onSuccess);

        expect(mealPlanService.generateShoppingList).toHaveBeenCalledWith('plan-1');
        expect(onSuccess).toHaveBeenCalledOnce();
    });

    it('blocks repeated actions until shopping creation fails and permits retry', () => {
        const response = new Subject<ShoppingList>();
        mealPlanService.generateShoppingList.mockReturnValueOnce(response);
        const onSuccess = vi.fn();
        facade.generateShoppingList('plan-1', onSuccess);
        facade.generateShoppingList('plan-1', onSuccess);
        facade.adopt('plan-1', onSuccess);
        expect(mealPlanService.generateShoppingList).toHaveBeenCalledOnce();
        expect(mealPlanService.adopt).not.toHaveBeenCalled();
        expect(facade.pendingAction()).toBe('shopping');

        response.error(new Error('Unavailable'));
        expect(facade.pendingAction()).toBeNull();
        expect(facade.actionErrorKey()).toBe('MEAL_PLANS.ERROR_SHOPPING_LIST');
        expect(onSuccess).not.toHaveBeenCalled();

        facade.generateShoppingList('plan-1', onSuccess);
        expect(facade.actionErrorKey()).toBeNull();
        expect(onSuccess).toHaveBeenCalledOnce();
        expect(facade.pendingAction()).toBeNull();
    });
});

describe('MealPlanFacade pagination', () => {
    beforeEach(() => {
        mealPlanService.getPage.mockReset();
        mealPlanService.getPage.mockReturnValue(of(createPage(1, MULTI_PAGE_TOTAL)));
    });
    it('keeps pagination while loading and retries the requested page after failure', async () => {
        mealPlanService.getPage.mockReturnValueOnce(of(createPage(1, MULTI_PAGE_TOTAL)));
        await waitForAsync(() => facade.totalItems() === MULTI_PAGE_TOTAL);
        const pending = new Subject<PageOf<MealPlanSummary>>();
        mealPlanService.getPage.mockReturnValueOnce(pending);
        facade.changePage(1);
        await waitForAsync(() => facade.isLoading());
        expect(facade.totalItems()).toBe(MULTI_PAGE_TOTAL);
        expect(mealPlanService.getPage).toHaveBeenLastCalledWith(undefined, 2, PAGE_SIZE);
        pending.error(new Error('Unavailable'));
        await waitForAsync(() => facade.hasLoadError());
        expect(facade.pageIndex()).toBe(1);
        expect(facade.totalItems()).toBe(MULTI_PAGE_TOTAL);
        mealPlanService.getPage.mockReturnValueOnce(of(createPage(2, MULTI_PAGE_TOTAL)));
        facade.retryPlans();
        await waitForAsync(() => facade.plans()[0]?.id === 'page-2');
        expect(mealPlanService.getPage).toHaveBeenLastCalledWith(undefined, 2, PAGE_SIZE);
        expect(facade.hasLoadError()).toBe(false);
    });

    it('resets pagination on filtering and ignores a late response from the previous filter', async () => {
        mealPlanService.getPage.mockReturnValueOnce(of(createPage(1, MULTI_PAGE_TOTAL)));
        await waitForAsync(() => facade.totalItems() === MULTI_PAGE_TOTAL);
        const stalePage = new Subject<PageOf<MealPlanSummary>>();
        mealPlanService.getPage.mockReturnValueOnce(stalePage);
        facade.changePage(1);
        await waitForAsync(() => facade.isLoading());
        mealPlanService.getPage.mockReturnValueOnce(of(createPage(1, 1)));
        facade.loadPlans('Keto');
        await waitForAsync(() => facade.plans()[0]?.id === 'page-1');
        stalePage.next(createPage(2, MULTI_PAGE_TOTAL));
        await waitForAsyncTasksAsync();
        TestBed.tick();
        expect(facade.pageIndex()).toBe(0);
        expect(facade.totalItems()).toBe(1);
        expect(facade.plans()[0]?.id).toBe('page-1');
        expect(mealPlanService.getPage).toHaveBeenLastCalledWith('Keto', 1, PAGE_SIZE);
    });

    it('bounds page requests and avoids reloading the current page', async () => {
        mealPlanService.getPage.mockReturnValueOnce(of(createPage(1, MULTI_PAGE_TOTAL)));
        await waitForAsync(() => facade.totalItems() === MULTI_PAGE_TOTAL);
        facade.changePage(-1);
        TestBed.tick();
        expect(mealPlanService.getPage).toHaveBeenCalledOnce();
        mealPlanService.getPage.mockReturnValueOnce(of(createPage(2, MULTI_PAGE_TOTAL)));
        facade.changePage(OUT_OF_RANGE_PAGE);
        await waitForAsync(() => facade.plans()[0]?.id === 'page-2');
        expect(facade.pageIndex()).toBe(1);
        expect(mealPlanService.getPage).toHaveBeenLastCalledWith(undefined, 2, PAGE_SIZE);
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

function createSummary(): MealPlanSummary {
    return {
        id: 'plan-1',
        name: 'Keto plan',
        description: null,
        dietType: 'Keto',
        durationDays: PLAN_DAYS,
        targetCaloriesPerDay: TARGET_CALORIES,
        isCurated: true,
        totalRecipes: TOTAL_RECIPES,
    };
}

function createPage(page: number, totalItems: number): PageOf<MealPlanSummary> {
    return {
        data: [{ ...createSummary(), id: `page-${page}` }],
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
        id: 'shopping-list-1',
        name: 'Keto plan',
        items: [],
        createdAt: '2026-05-15T00:00:00Z',
    };
}
