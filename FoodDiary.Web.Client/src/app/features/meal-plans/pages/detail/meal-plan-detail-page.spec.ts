import { signal } from '@angular/core';
import { TestBed } from '@angular/core/testing';
import { ActivatedRoute, convertToParamMap, provideRouter, Router } from '@angular/router';
import { RouterTestingHarness } from '@angular/router/testing';
import { FdUiDialogService } from 'fd-ui-kit/dialog/fd-ui-dialog.service';
import { of, Subject } from 'rxjs';
import { describe, expect, it, vi } from 'vitest';

const TEST_PLAN_DURATION_DAYS = 7;

import { waitForAsyncTasksAsync } from '../../../../../testing/async-testing';
import { provideTranslateTesting } from '../../../../../testing/translate-testing.module';
import { entityId } from '../../../../shared/models/semantics/entity-id';
import { MealPlanFacade } from '../../lib/meal-plan.facade';
import type { MealPlan } from '../../models/meal-plan.data';
import { plannedServingsFromStored } from '../../models/meal-plan-values';
import { plannedMealTypeFromStored } from '../../models/meal-plan-values';
import { planDayNumberFromStored } from '../../models/meal-plan-values';
import { planDurationDaysFromStored } from '../../models/meal-plan-values';
import { MealPlanDetailPageComponent } from './meal-plan-detail-page';

describe('MealPlanDetailPageComponent', () => {
    it('loads and acts on the current plan when Angular reuses the detail route', async () => {
        const facade = createFacadeStub(null);
        facade.loadPlan.mockImplementation(id => {
            facade.selectedPlan.set({ ...createMealPlan(), id: entityId<'meal-plan'>(id), name: id });
        });
        TestBed.configureTestingModule({
            imports: [MealPlanDetailPageComponent],
            providers: [provideTranslateTesting(), provideRouter([{ path: 'meal-plans/:id', component: MealPlanDetailPageComponent }])],
        });
        TestBed.overrideComponent(MealPlanDetailPageComponent, {
            set: { providers: [{ provide: MealPlanFacade, useValue: facade }], template: '' },
        });
        const harness = await RouterTestingHarness.create();
        const first = await harness.navigateByUrl('/meal-plans/plan-1', MealPlanDetailPageComponent);
        const second = await harness.navigateByUrl('/meal-plans/plan-2', MealPlanDetailPageComponent);

        expect(second).toBe(first);
        expect(facade.loadPlan.mock.calls).toEqual([['plan-1'], ['plan-2']]);
        second['adopt']();
        expect(facade.adopt).toHaveBeenCalledWith('plan-2', expect.any(Function));
        await harness.navigateByUrl('/meal-plans/plan-2?from=notice', MealPlanDetailPageComponent);
        expect(facade.loadPlan).toHaveBeenCalledTimes(2);
    });
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
        expect(router.navigate).toHaveBeenCalledWith(['/meal-plans'], { queryParams: { source: 'saved' } });
    });

    it('prepares the diary draft without leaving the plan page', () => {
        const plan = createMealPlan();
        const facade = createFacadeStub(plan);
        const router = createRouterStub();
        const component = createComponent(facade, 'plan-1', router);

        component['addMealToDiary'](plan.days[0].meals[0]);

        expect(facade.addMealToDiary).toHaveBeenCalledWith(plan.days[0].meals[0]);
        expect(router.navigate).not.toHaveBeenCalled();
    });

    it('preserves list filters when returning to the list', () => {
        const router = createRouterStub();
        const component = createComponent(createFacadeStub(createMealPlan()), 'plan-1', router);
        component['goBack']();
        expect(router.navigate).toHaveBeenCalledWith(['/meal-plans'], { queryParamsHandling: 'preserve' });
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
    addMealToDiary: ReturnType<typeof vi.fn>;
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
                    paramMap: of(convertToParamMap(routeId === null ? {} : { id: routeId })),
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
        addMealToDiary: vi.fn(),
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
        expect(router.navigate).toHaveBeenCalledWith(['/meal-plans'], { queryParamsHandling: 'preserve' });
        facade.selectedPlan.set(createMealPlan());
        facade.deletePlan.mockClear();
        component['confirmDeletePlan']();
        expect(facade.deletePlan).not.toHaveBeenCalled();
    });
});

function createMealPlan(): MealPlan {
    return {
        id: entityId<'meal-plan'>('plan-1'),
        name: 'Meal plan',
        description: null,
        dietType: 'Balanced',
        durationDays: planDurationDaysFromStored(TEST_PLAN_DURATION_DAYS),
        targetCaloriesPerDay: 2000,
        isCurated: true,
        days: [
            {
                id: entityId<'meal-plan-day'>('day-1'),
                dayNumber: planDayNumberFromStored(1),
                meals: [
                    {
                        id: entityId<'meal-plan-meal'>('meal-1'),
                        mealType: plannedMealTypeFromStored('Lunch'),
                        recipeId: entityId<'recipe'>('recipe-1'),
                        recipeName: null,
                        servings: plannedServingsFromStored(1),
                        calories: 500,
                    },
                ],
            },
        ],
    };
}
