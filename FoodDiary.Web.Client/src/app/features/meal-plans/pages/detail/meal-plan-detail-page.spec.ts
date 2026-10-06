import { signal } from '@angular/core';
import { TestBed } from '@angular/core/testing';
import { ActivatedRoute, convertToParamMap, Router } from '@angular/router';
import { FdUiDialogService } from 'fd-ui-kit/dialog/fd-ui-dialog.service';
import { Subject } from 'rxjs';
import { describe, expect, it, vi } from 'vitest';

import { waitForAsyncTasksAsync } from '../../../../../testing/async-testing';
import { provideTranslateTesting } from '../../../../../testing/translate-testing.module';
import { MealPlanFacade } from '../../lib/meal-plan.facade';
import type { MealPlan } from '../../models/meal-plan.data';
import { MealPlanDetailPageComponent } from './meal-plan-detail-page';

describe('MealPlanDetailPageComponent', () => {
    it('loads plan from route param and maps detail view', () => {
        const facade = createFacadeStub(createMealPlan());
        const component = createComponent(facade, 'plan-1');

        expect(facade.loadPlan).toHaveBeenCalledWith('plan-1');
        expect(component['selectedPlanView']()?.days[0].meals[0].mealTypeKey).toBe('MEAL_PLANS.MEAL_TYPE.LUNCH');
    });

    it('does not call actions when plan is missing', () => {
        const facade = createFacadeStub(null);
        const component = createComponent(facade, null);

        component['adopt']();
        component['generateShoppingList']();

        expect(facade.adopt).not.toHaveBeenCalled();
        expect(facade.generateShoppingList).not.toHaveBeenCalled();
    });

    it('adopts selected plan and navigates back through facade callback', () => {
        const facade = createFacadeStub(createMealPlan());
        const router = createRouterStub();
        const component = createComponent(facade, 'plan-1', router);

        component['adopt']();
        const onSuccess = facade.adopt.mock.calls[0][1];
        onSuccess();

        expect(facade.adopt).toHaveBeenCalledWith('plan-1', expect.any(Function));
        expect(router.navigate).toHaveBeenCalledWith(['/meal-plans']);
    });

    it('generates shopping list and navigates to shopping lists through facade callback', () => {
        const facade = createFacadeStub(createMealPlan());
        const router = createRouterStub();
        const component = createComponent(facade, 'plan-1', router);

        component['generateShoppingList']();
        const onSuccess = facade.generateShoppingList.mock.calls[0][1];
        onSuccess();

        expect(facade.generateShoppingList).toHaveBeenCalledWith('plan-1', expect.any(Function));
        expect(router.navigate).toHaveBeenCalledWith(['/shopping-lists']);
    });
});

type FacadeStub = {
    pendingAction: ReturnType<typeof signal<'adopt' | 'shopping' | 'delete' | null>>;
    deletePlan: ReturnType<typeof vi.fn<(id: string, onSuccess: () => void) => void>>;
    selectedPlan: ReturnType<typeof signal<MealPlan | null>>;
    isDetailLoading: ReturnType<typeof signal<boolean>>;
    loadPlan: ReturnType<typeof vi.fn<(id: string) => void>>;
    adopt: ReturnType<typeof vi.fn<(id: string, onSuccess: () => void) => void>>;
    generateShoppingList: ReturnType<typeof vi.fn<(id: string, onSuccess: () => void) => void>>;
};

function createComponent(
    facade: FacadeStub,
    routeId: string | null,
    router = createRouterStub(),
    closed = new Subject<boolean>(),
): MealPlanDetailPageComponent {
    TestBed.configureTestingModule({
        providers: [
            provideTranslateTesting(),
            {
                provide: FdUiDialogService,
                useValue: { open: vi.fn(() => ({ afterClosed: (): Subject<boolean> => closed, close: vi.fn() })) },
            },
            { provide: MealPlanFacade, useValue: facade },
            {
                provide: ActivatedRoute,
                useValue: {
                    snapshot: {
                        paramMap: convertToParamMap(routeId === null ? {} : { id: routeId }),
                    },
                },
            },
            { provide: Router, useValue: router },
        ],
    });

    return TestBed.runInInjectionContext(() => new MealPlanDetailPageComponent());
}

function createRouterStub(): { navigate: ReturnType<typeof vi.fn<(commands: string[]) => Promise<boolean>>> } {
    return {
        navigate: vi.fn(async () => {
            await waitForAsyncTasksAsync();
            return true;
        }),
    };
}

function createFacadeStub(plan: MealPlan | null): FacadeStub {
    return {
        pendingAction: signal(null),
        deletePlan: vi.fn(),
        selectedPlan: signal(plan),
        isDetailLoading: signal(false),
        loadPlan: vi.fn(),
        adopt: vi.fn(),
        generateShoppingList: vi.fn(),
    };
}

describe('Meal plan deletion confirmation', () => {
    it('does not delete or navigate when confirmation is cancelled', () => {
        const facade = createFacadeStub({ ...createMealPlan(), isCurated: false });
        const router = createRouterStub();
        const cancelled = new Subject<boolean>();
        const component = createComponent(facade, 'plan-1', router, cancelled);
        component['confirmDeletePlan']();
        cancelled.next(false);
        cancelled.complete();
        expect(facade.deletePlan).not.toHaveBeenCalled();
        expect(router.navigate).not.toHaveBeenCalled();
    });

    it('deletes only after explicit confirmation and keeps curated plans protected', () => {
        const facade = createFacadeStub({ ...createMealPlan(), isCurated: false });
        const router = createRouterStub();
        const closed = new Subject<boolean>();
        const component = createComponent(facade, 'plan-1', router, closed);
        component['confirmDeletePlan']();
        expect(facade.deletePlan).not.toHaveBeenCalled();
        closed.next(true);
        expect(facade.deletePlan).toHaveBeenCalledWith('plan-1', expect.any(Function));
        expect(router.navigate).not.toHaveBeenCalled();
        facade.deletePlan.mock.calls[0][1]();
        expect(router.navigate).toHaveBeenCalledWith(['/meal-plans']);
        facade.selectedPlan.set(createMealPlan());
        facade.deletePlan.mockClear();
        component['confirmDeletePlan']();
        expect(facade.deletePlan).not.toHaveBeenCalled();
    });
});

function createMealPlan(): MealPlan {
    return {
        id: 'plan-1',
        name: 'Meal plan',
        description: null,
        dietType: 'Balanced',
        durationDays: 7,
        targetCaloriesPerDay: 2000,
        isCurated: true,
        days: [
            {
                id: 'day-1',
                dayNumber: 1,
                meals: [
                    {
                        id: 'meal-1',
                        mealType: 'Lunch',
                        recipeId: 'recipe-1',
                        recipeName: null,
                        servings: 1,
                        calories: 500,
                    },
                ],
            },
        ],
    };
}
