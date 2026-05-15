import { signal } from '@angular/core';
import { TestBed } from '@angular/core/testing';
import { Router } from '@angular/router';
import { describe, expect, it, vi } from 'vitest';

import { MealPlanFacade } from '../../lib/meal-plan.facade';
import type { DietType, MealPlanSummary } from '../../models/meal-plan.data';
import { MealPlansListPageComponent } from './meal-plans-list-page.component';

describe('MealPlansListPageComponent', () => {
    it('loads plans on creation and maps plan cards', () => {
        const facade = createFacadeStub([createSummary({ dietType: 'Vegan' })]);
        const component = createComponent(facade);

        expect(facade.loadPlans).toHaveBeenCalledWith();
        expect(component.planCards()[0]).toMatchObject({
            id: 'plan-1',
            dietTypeKey: 'MEAL_PLANS.DIET_TYPE.VEGAN',
        });
    });

    it('filters plans by selected diet type', () => {
        const facade = createFacadeStub();
        const component = createComponent(facade);

        component.filterByDiet('Keto');

        expect(facade.loadPlans).toHaveBeenLastCalledWith('Keto');
    });

    it('navigates to selected plan detail', () => {
        const facade = createFacadeStub();
        const router = createRouterStub();
        const component = createComponent(facade, router);

        component.openPlan('plan-1');

        expect(router.navigate).toHaveBeenCalledWith(['/meal-plans', 'plan-1']);
    });
});

type FacadeStub = {
    dietTypeFilter: ReturnType<typeof signal<DietType | null>>;
    plans: ReturnType<typeof signal<MealPlanSummary[]>>;
    isLoading: ReturnType<typeof signal<boolean>>;
    loadPlans: ReturnType<typeof vi.fn<(dietType?: DietType | null) => void>>;
};

function createComponent(facade: FacadeStub, router = createRouterStub()): MealPlansListPageComponent {
    TestBed.configureTestingModule({
        providers: [
            { provide: MealPlanFacade, useValue: facade },
            { provide: Router, useValue: router },
        ],
    });

    return TestBed.runInInjectionContext(() => new MealPlansListPageComponent());
}

function createRouterStub(): { navigate: ReturnType<typeof vi.fn<(commands: string[]) => Promise<boolean>>> } {
    return {
        navigate: vi.fn(async () => {
            await Promise.resolve();
            return true;
        }),
    };
}

function createFacadeStub(plans: MealPlanSummary[] = []): FacadeStub {
    return {
        dietTypeFilter: signal<DietType | null>(null),
        plans: signal(plans),
        isLoading: signal(false),
        loadPlans: vi.fn(),
    };
}

function createSummary(overrides: Partial<MealPlanSummary> = {}): MealPlanSummary {
    return {
        id: 'plan-1',
        name: 'Meal plan',
        description: null,
        dietType: 'Balanced',
        durationDays: 7,
        targetCaloriesPerDay: 2000,
        isCurated: true,
        totalRecipes: 21,
        ...overrides,
    };
}
