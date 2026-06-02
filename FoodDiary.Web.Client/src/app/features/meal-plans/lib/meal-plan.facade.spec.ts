import { TestBed } from '@angular/core/testing';
import { of } from 'rxjs';
import { beforeEach, describe, expect, it, vi } from 'vitest';

import type { ShoppingList } from '../../shopping-lists/models/shopping-list.data';
import { MealPlanService } from '../api/meal-plan.service';
import type { MealPlan, MealPlanSummary } from '../models/meal-plan.data';
import { MealPlanFacade } from './meal-plan.facade';

const WAIT_ATTEMPTS = 20;
const PLAN_DAYS = 7;
const TARGET_CALORIES = 1800;
const TOTAL_RECIPES = 21;

type MealPlanServiceMock = {
    adopt: ReturnType<typeof vi.fn>;
    generateShoppingList: ReturnType<typeof vi.fn>;
    getAll: ReturnType<typeof vi.fn>;
    getById: ReturnType<typeof vi.fn>;
};

let facade: MealPlanFacade;
let mealPlanService: MealPlanServiceMock;

describe('MealPlanFacade', () => {
    beforeEach(() => {
        TestBed.resetTestingModule();
        mealPlanService = {
            getAll: vi.fn(() => of([createSummary()])),
            getById: vi.fn(() => of(createMealPlan())),
            adopt: vi.fn(() => of(createMealPlan())),
            generateShoppingList: vi.fn(() => of(createShoppingList())),
        };

        TestBed.configureTestingModule({
            providers: [MealPlanFacade, { provide: MealPlanService, useValue: mealPlanService }],
        });

        facade = TestBed.inject(MealPlanFacade);
    });

    it('loads meal plans with selected diet type filter', async () => {
        facade.loadPlans('Keto');

        await waitForAsync(() => facade.plans().length > 0);

        expect(mealPlanService.getAll).toHaveBeenLastCalledWith('Keto');
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

    it('runs success callback after generating a shopping list', () => {
        const onSuccess = vi.fn();

        facade.generateShoppingList('plan-1', onSuccess);

        expect(mealPlanService.generateShoppingList).toHaveBeenCalledWith('plan-1');
        expect(onSuccess).toHaveBeenCalledOnce();
    });
});

async function waitForAsync(predicate: () => boolean): Promise<void> {
    for (let attempt = 0; attempt < WAIT_ATTEMPTS; attempt++) {
        TestBed.tick();

        if (predicate()) {
            return;
        }

        await Promise.resolve();
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
