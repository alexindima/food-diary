import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { TestBed } from '@angular/core/testing';
import { provideRouter, Router } from '@angular/router';
import { RouterTestingHarness } from '@angular/router/testing';
import { FdTourService } from 'fd-tour';
import { FdUiDialogService } from 'fd-ui-kit/dialog/fd-ui-dialog.service';
import { of, Subject } from 'rxjs';
import { afterEach, describe, expect, it, type Mock, vi } from 'vitest';

import { provideTranslateTesting } from '../../../testing/translate-testing.module';
import type { PageOf } from '../../shared/models/page-of.data';
import { type Recipe, RecipeVisibility } from '../../shared/models/recipe.data';
import { utcInstant } from '../../shared/models/semantics/date-value';
import { entityId } from '../../shared/models/semantics/entity-id';
import { QuickMealService } from '../meals/contracts/quick-meal';
import { RECIPE_LOOKUP, type RecipeLookup } from '../recipes/contracts/recipe-lookup';
import { MealPlanService } from './api/meal-plan.service';
import mealPlanRoutes from './meal-plan.routes';
import type { MealPlan, MealPlanSummary } from './models/meal-plan.data';
import {
    planDayNumberFromStored,
    planDurationDaysFromStored,
    plannedMealTypeFromStored,
    plannedServingsFromStored,
} from './models/meal-plan-values';

const CATALOGUE_URL = '/meal-plans';
const CURATED_PLAN_URL = '/meal-plans/curated';
const PERSONAL_PLAN_URL = '/meal-plans/personal';
const INVALID_PLAN_URL = '/meal-plans/%20';
const SAVED_PLANS_URL = '/meal-plans?source=saved';
const BACK = 'MEAL_PLANS.BACK';
const ADOPT = 'MEAL_PLANS.ADOPT';
const DELETE_ACTION = 'MEAL_PLANS.DELETE_PLAN';
const SHOPPING_ACTION = 'MEAL_PLANS.GENERATE_SHOPPING_LIST';
const DELETE_CONFIRMATION = 'fd-confirm-delete-dialog';
const TITLE = '.meal-plan-detail__title';
const CARD = '.meal-plan-card';
const SECOND_PAGE = 2;
const PAGE_SIZE = 50;
const PLAN_DAYS = 7;
const PLANNED_SERVINGS = 2;
const ADD_TO_DIARY = 'MEAL_PLANS.ADD_TO_DIARY';

const CURATED_PLAN: MealPlan = {
    id: entityId<'meal-plan'>('curated'),
    name: 'Curated Mediterranean week',
    dietType: 'Mediterranean',
    durationDays: planDurationDaysFromStored(PLAN_DAYS),
    isCurated: true,
    days: [],
};
const PERSONAL_PLAN: MealPlan = {
    ...CURATED_PLAN,
    id: entityId<'meal-plan'>('personal'),
    name: 'My Mediterranean week',
    isCurated: false,
};

type MealPlansApi = {
    [Method in 'getPage' | 'getById' | 'adopt' | 'deletePlan']: Mock<MealPlanService[Method]>;
};

afterEach(() => {
    TestBed.inject(FdUiDialogService).closeAll();
    TestBed.inject(HttpTestingController).verify();
    vi.unstubAllGlobals();
});

function setupMealPlans(getRecipe = vi.fn<RecipeLookup['getById']>()): MealPlansApi {
    vi.stubGlobal(
        'matchMedia',
        vi.fn(() => ({ matches: false })),
    );
    const api: MealPlansApi = {
        getPage: vi
            .fn<MealPlanService['getPage']>()
            .mockImplementation((_diet, page = 1, _limit, curated) =>
                of(cataloguePage(curated === false ? PERSONAL_PLAN : CURATED_PLAN, page)),
            ),
        getById: vi.fn<MealPlanService['getById']>().mockImplementation(id => of(id === PERSONAL_PLAN.id ? PERSONAL_PLAN : CURATED_PLAN)),
        adopt: vi.fn<MealPlanService['adopt']>().mockReturnValue(of(PERSONAL_PLAN)),
        deletePlan: vi.fn<MealPlanService['deletePlan']>(),
    };
    TestBed.configureTestingModule({
        providers: [
            provideRouter([{ path: 'meal-plans', children: mealPlanRoutes }]),
            provideTranslateTesting(),
            provideHttpClient(),
            provideHttpClientTesting(),
            { provide: MealPlanService, useValue: api },
            { provide: RECIPE_LOOKUP, useValue: { getById: getRecipe } },
        ],
    });
    return api;
}

function cataloguePage(plan: MealPlan, page: number): PageOf<MealPlanSummary> {
    return {
        data: [{ ...plan, totalRecipes: 0 }],
        page,
        limit: PAGE_SIZE,
        totalItems: PAGE_SIZE * (page - 1) + 1,
        totalPages: page,
    };
}

function pageHost(harness: RouterTestingHarness): HTMLElement {
    const host = harness.routeNativeElement;
    if (host === null) {
        throw new Error('Expected a rendered Meal Plans route');
    }
    return host;
}

function renderedAction(host: ParentNode, label: string): HTMLButtonElement {
    const action = Array.from(host.querySelectorAll('button')).find(
        button => button.getAttribute('aria-label') === label || button.querySelector('.fd-ui-button__label')?.textContent.trim() === label,
    );
    if (action === undefined) {
        throw new Error(`Expected rendered Meal Plans action ${label}`);
    }
    return action;
}

async function waitForPlanAsync(harness: RouterTestingHarness, plan: MealPlan): Promise<HTMLElement> {
    await vi.waitFor(() => {
        harness.detectChanges();
        expect(pageHost(harness).querySelector(TITLE)?.textContent).toBe(plan.name);
    });
    return pageHost(harness);
}

describe('Meal Plans help controls', () => {
    it.each([
        {
            route: CATALOGUE_URL,
            label: 'MEAL_PLANS.TOUR.START_TOUR',
            tourId: 'meal-plans-list',
            step: { id: 'filters', target: '[data-tour-id="meal-plans-filters"]', title: 'MEAL_PLANS.TOUR.FILTERS_TITLE' },
        },
        {
            route: CURATED_PLAN_URL,
            label: 'MEAL_PLANS.DETAIL_TOUR.START_TOUR',
            tourId: 'meal-plan-detail',
            step: { id: 'header', target: '[data-tour-id="meal-plan-detail-header"]', title: 'MEAL_PLANS.DETAIL_TOUR.HEADER_TITLE' },
        },
    ])('reopens the completed $tourId guide from its visible help button', async ({ route, label, tourId, step }) => {
        setupMealPlans();
        const harness = await RouterTestingHarness.create(route);
        const host = pageHost(harness);
        await vi.waitFor(() => {
            harness.detectChanges();
            expect(host.querySelector(step.target)).not.toBeNull();
        });
        const help = renderedAction(host, label);
        const tour = TestBed.inject(FdTourService);

        help.click();
        expect(tour.activeTour()?.id).toBe(tourId);
        expect(tour.activeStep()).toMatchObject(step);
        tour.complete();
        expect(tour.activeTour()).toBeNull();

        help.click();
        expect(tour.activeTour()?.id).toBe(tourId);
        expect(tour.activeStep()).toMatchObject(step);
        tour.close();
    });
});

describe('Meal plan route ownership', () => {
    it('clears the previously opened plan and confirmation for an invalid detail URL, then recovers on a valid URL', async () => {
        const api = setupMealPlans();
        const harness = await RouterTestingHarness.create(PERSONAL_PLAN_URL);
        const host = await waitForPlanAsync(harness, PERSONAL_PLAN);
        renderedAction(host, DELETE_ACTION).click();
        harness.detectChanges();
        expect(document.querySelector(DELETE_CONFIRMATION)).not.toBeNull();

        await harness.navigateByUrl(INVALID_PLAN_URL);
        await vi.waitFor(() => {
            harness.detectChanges();
            expect(pageHost(harness).querySelector(TITLE)).toBeNull();
            expect(pageHost(harness).textContent).toContain('MEAL_PLANS.DETAIL_UNAVAILABLE');
        });
        expect(document.querySelector(DELETE_CONFIRMATION)).toBeNull();
        expect(pageHost(harness).textContent).not.toContain(DELETE_ACTION);
        expect(pageHost(harness).textContent).not.toContain(SHOPPING_ACTION);
        expect(api.getById).toHaveBeenCalledExactlyOnceWith(PERSONAL_PLAN.id);
        expect(api.deletePlan).not.toHaveBeenCalled();

        await harness.navigateByUrl(CURATED_PLAN_URL);
        await waitForPlanAsync(harness, CURATED_PLAN);
        expect(api.getById.mock.calls).toEqual([[PERSONAL_PLAN.id], [CURATED_PLAN.id]]);
        expect(api.deletePlan).not.toHaveBeenCalled();
    });

    it('opens plan detail from the keyboard and preserves the selected catalogue query when going back', async () => {
        const api = setupMealPlans();
        const query = 'source=curated&dietType=Mediterranean&page=2&from=plan-review';
        const listUrl = `${CATALOGUE_URL}?${query}`;
        const harness = await RouterTestingHarness.create(listUrl);
        const router = TestBed.inject(Router);
        await vi.waitFor(() => {
            harness.detectChanges();
            expect(pageHost(harness).querySelector(CARD)?.textContent).toContain(CURATED_PLAN.name);
        });
        const card = pageHost(harness).querySelector<HTMLElement>(CARD);
        if (card === null) {
            throw new Error('Expected a meal plan card in the filtered catalogue');
        }
        card.dispatchEvent(new KeyboardEvent('keydown', { key: 'Enter', bubbles: true, cancelable: true }));
        const detail = await waitForPlanAsync(harness, CURATED_PLAN);
        expect(router.url).toBe(`${CURATED_PLAN_URL}?${query}`);
        expect(api.getPage).toHaveBeenCalledWith('Mediterranean', SECOND_PAGE, PAGE_SIZE, true);

        renderedAction(detail, BACK).click();
        await vi.waitFor(() => {
            harness.detectChanges();
            expect(router.url).toBe(listUrl);
            expect(pageHost(harness).querySelector(CARD)?.textContent).toContain(CURATED_PLAN.name);
        });
        expect(renderedAction(pageHost(harness), 'MEAL_PLANS.DIET_TYPE.MEDITERRANEAN').getAttribute('aria-pressed')).toBe('true');
        expect(api.getPage).toHaveBeenLastCalledWith('Mediterranean', SECOND_PAGE, PAGE_SIZE, true);
    });
});

describe('Meal plan adoption lifecycle', () => {
    it('keeps the curated detail on failure, unlocks its actions and opens saved plans after retry succeeds', async () => {
        const api = setupMealPlans();
        const firstWrite = new Subject<MealPlan>();
        api.adopt.mockReturnValueOnce(firstWrite);
        const harness = await RouterTestingHarness.create(CURATED_PLAN_URL);
        const router = TestBed.inject(Router);
        const detail = await waitForPlanAsync(harness, CURATED_PLAN);
        const adopt = renderedAction(detail, ADOPT);
        adopt.click();
        harness.detectChanges();

        expect(adopt.getAttribute('aria-busy')).toBe('true');
        expect(adopt.disabled).toBe(true);
        expect(renderedAction(detail, SHOPPING_ACTION).disabled).toBe(true);
        adopt.click();
        expect(api.adopt).toHaveBeenCalledExactlyOnceWith(CURATED_PLAN.id);

        firstWrite.error(new Error('Adoption temporarily unavailable'));
        harness.detectChanges();
        expect(detail.querySelector('[role="alert"]')?.textContent).toContain('MEAL_PLANS.ERROR_ADOPT');
        expect(router.url).toBe(CURATED_PLAN_URL);
        expect(adopt.disabled).toBe(false);
        adopt.click();

        await vi.waitFor(() => {
            harness.detectChanges();
            expect(router.url).toBe(SAVED_PLANS_URL);
            expect(pageHost(harness).querySelector(CARD)?.textContent).toContain(PERSONAL_PLAN.name);
        });
        expect(api.adopt.mock.calls).toEqual([[CURATED_PLAN.id], [CURATED_PLAN.id]]);
        expect(api.getPage).toHaveBeenLastCalledWith(undefined, 1, PAGE_SIZE, false);
        expect(pageHost(harness).textContent).not.toContain(CURATED_PLAN.name);
    });
});

describe('Meal plan catalogue recovery', () => {
    it('retries the selected catalogue page and lets the user clear a filter after the page becomes empty', async () => {
        const api = setupMealPlans();
        const firstPage = new Subject<PageOf<MealPlanSummary>>();
        api.getPage
            .mockImplementation((_diet, page = 1, _limit, curated) =>
                of(curated === true ? cataloguePage(CURATED_PLAN, page) : emptyCataloguePage(page)),
            )
            .mockReturnValueOnce(firstPage);
        const harness = await RouterTestingHarness.create('/meal-plans?source=saved&dietType=Mediterranean&page=2&from=dashboard');
        const router = TestBed.inject(Router);
        const host = pageHost(harness);

        await vi.waitFor(() => {
            expect(api.getPage).toHaveBeenCalledExactlyOnceWith('Mediterranean', SECOND_PAGE, PAGE_SIZE, false);
        });
        firstPage.error(new Error('Meal plan catalogue temporarily unavailable'));
        await vi.waitFor(() => {
            harness.detectChanges();
            expect(host.querySelector('[role="alert"]')?.textContent).toContain('MEAL_PLANS.ERROR_LOAD');
        });
        renderedAction(host, 'MEAL_PLANS.RETRY').click();

        await vi.waitFor(() => {
            harness.detectChanges();
            expect(api.getPage.mock.calls).toEqual([
                ['Mediterranean', SECOND_PAGE, PAGE_SIZE, false],
                ['Mediterranean', SECOND_PAGE, PAGE_SIZE, false],
                ['Mediterranean', 1, PAGE_SIZE, false],
            ]);
            expect(router.parseUrl(router.url).queryParams).toEqual({ source: 'saved', dietType: 'Mediterranean', from: 'dashboard' });
            expect(host.querySelector('[role="alert"]')).toBeNull();
            expect(host.querySelector('.meal-plans-list__empty')?.textContent).toContain('MEAL_PLANS.EMPTY_FILTERED');
        });
        renderedAction(host, 'MEAL_PLANS.RESET_FILTER').click();

        await vi.waitFor(() => {
            harness.detectChanges();
            expect(api.getPage).toHaveBeenLastCalledWith(undefined, 1, PAGE_SIZE, false);
            expect(router.parseUrl(router.url).queryParams).toEqual({ source: 'saved', from: 'dashboard' });
            expect(host.querySelector('.meal-plans-list__empty')?.textContent).toContain('MEAL_PLANS.EMPTY_SAVED');
        });
        const curated = Array.from(host.querySelectorAll<HTMLButtonElement>('button[role="radio"]')).find(
            radio => radio.textContent.trim() === 'MEAL_PLANS.SOURCE.CURATED',
        );
        if (curated === undefined) {
            throw new Error('Expected the curated meal plan source radio');
        }
        curated.click();

        await vi.waitFor(() => {
            harness.detectChanges();
            expect(api.getPage).toHaveBeenLastCalledWith(undefined, 1, PAGE_SIZE, true);
            expect(router.parseUrl(router.url).queryParams).toEqual({ source: 'curated', from: 'dashboard' });
            expect(curated.getAttribute('aria-checked')).toBe('true');
            expect(host.querySelector(CARD)?.textContent).toContain(CURATED_PLAN.name);
        });
    });
});

describe('Meal plan removal and recovery', () => {
    it('keeps a personal plan after cancellation and a failed delete, then returns to the saved catalogue after retry', async () => {
        const api = setupMealPlans();
        const firstDelete = new Subject<void>();
        api.deletePlan.mockReturnValueOnce(firstDelete).mockReturnValueOnce(of(undefined));
        api.getPage.mockImplementation((_diet, page = 1) => of(emptyCataloguePage(page)));
        const planUrl = '/meal-plans/personal?source=saved';
        const harness = await RouterTestingHarness.create(planUrl);
        const router = TestBed.inject(Router);
        const detail = await waitForPlanAsync(harness, PERSONAL_PLAN);
        const remove = renderedAction(detail, DELETE_ACTION);

        remove.click();
        await clickDeleteConfirmationAsync('CONFIRM_DELETE.CANCEL');
        expect(api.deletePlan).not.toHaveBeenCalled();
        expect(router.url).toBe(planUrl);
        expect(detail.querySelector(TITLE)?.textContent).toBe(PERSONAL_PLAN.name);

        remove.click();
        await clickDeleteConfirmationAsync(DELETE_ACTION);
        harness.detectChanges();
        expect(remove.disabled).toBe(true);
        expect(remove.getAttribute('aria-busy')).toBe('true');
        expect(renderedAction(detail, SHOPPING_ACTION).disabled).toBe(true);
        expect(api.deletePlan).toHaveBeenCalledExactlyOnceWith(PERSONAL_PLAN.id);

        firstDelete.error(new Error('Meal plan removal temporarily unavailable'));
        harness.detectChanges();
        expect(remove.disabled).toBe(false);
        expect(detail.querySelector('[role="alert"]')?.textContent).toContain('MEAL_PLANS.ERROR_DELETE');
        expect(detail.querySelector(TITLE)?.textContent).toBe(PERSONAL_PLAN.name);
        remove.click();
        await clickDeleteConfirmationAsync(DELETE_ACTION);

        await vi.waitFor(() => {
            harness.detectChanges();
            expect(router.url).toBe(SAVED_PLANS_URL);
            expect(pageHost(harness).querySelector('.meal-plans-list__empty')?.textContent).toContain('MEAL_PLANS.EMPTY_SAVED');
        });
        expect(api.deletePlan.mock.calls).toEqual([[PERSONAL_PLAN.id], [PERSONAL_PLAN.id]]);
    });
});

function emptyCataloguePage(page: number): PageOf<MealPlanSummary> {
    return { data: [], page, limit: PAGE_SIZE, totalItems: 0, totalPages: 0 };
}

async function clickDeleteConfirmationAsync(label: string): Promise<void> {
    const action = await vi.waitFor(() => {
        const dialog = document.querySelector(DELETE_CONFIRMATION);
        if (dialog === null) {
            throw new Error('Expected the rendered delete confirmation');
        }
        return renderedAction(dialog, label);
    });
    action.click();
}

describe('Meal plan diary entry handoff recovery', () => {
    it('keeps a meal available to retry after its recipe fails to load', async () => {
        const planned = mealPlanRecipe('recovered-lunch', 'Planned lunch');
        const plan = planWithRecipe(CURATED_PLAN, planned, 'Lunch');
        const lookup = new Subject<Recipe | null>();
        const getRecipe = vi.fn<RecipeLookup['getById']>().mockReturnValueOnce(lookup).mockReturnValueOnce(of(planned));
        const api = setupMealPlans(getRecipe);
        api.getById.mockReturnValueOnce(of(plan));
        const harness = await RouterTestingHarness.create(CURATED_PLAN_URL);
        const detail = await waitForPlanAsync(harness, plan);
        const add = renderedAction(detail, ADD_TO_DIARY);
        const draft = TestBed.inject(QuickMealService);

        add.click();
        harness.detectChanges();
        expect(add.disabled).toBe(true);
        lookup.error(new Error('Recipe module unavailable'));
        harness.detectChanges();

        expect(draft.items()).toEqual([]);
        expect(detail.querySelector('[role="alert"]')?.textContent).toContain('MEAL_PLANS.ERROR_ADD_MEAL');
        expect(add.disabled).toBe(false);
        add.click();
        harness.detectChanges();

        expect(getRecipe.mock.calls).toEqual([[planned.id], [planned.id]]);
        expect(draft.items()).toMatchObject([{ recipe: planned, amount: PLANNED_SERVINGS }]);
        expect(detail.querySelector('[role="alert"]')).toBeNull();
    });
});

describe('Meal plan diary entry handoff', () => {
    it('preserves a diary draft added during recipe loading, then retries with the planned servings and a legacy meal type', async () => {
        const planned = mealPlanRecipe('planned-salad', 'Planned salad');
        const existing = mealPlanRecipe('existing-soup', 'Current diary soup');
        const plan = planWithRecipe(CURATED_PLAN, planned, 'legacy-supper');
        const lookup = new Subject<Recipe | null>();
        const getRecipe = vi.fn<RecipeLookup['getById']>().mockReturnValueOnce(lookup).mockReturnValueOnce(of(planned));
        const api = setupMealPlans(getRecipe);
        api.getById.mockReturnValueOnce(of(plan));
        const harness = await RouterTestingHarness.create(CURATED_PLAN_URL);
        const detail = await waitForPlanAsync(harness, plan);
        const add = renderedAction(detail, ADD_TO_DIARY);
        const draft = TestBed.inject(QuickMealService);
        add.click();
        harness.detectChanges();
        expect(add.disabled).toBe(true);
        expect(add.getAttribute('aria-busy')).toBe('true');
        expect(getRecipe).toHaveBeenCalledExactlyOnceWith(planned.id);

        draft.addRecipe(existing);
        draft.updateDetails({ comment: 'Keep my existing diary draft', mealType: 'DINNER' });
        lookup.next(planned);
        lookup.complete();
        harness.detectChanges();
        expect(detail.querySelector('[role="alert"]')?.textContent).toContain('MEAL_PLANS.ERROR_ADD_MEAL');
        expect(add.disabled).toBe(true);
        expect(draft.items()).toMatchObject([{ recipe: existing }]);
        expect(draft.details()).toMatchObject({ comment: 'Keep my existing diary draft', mealType: 'DINNER' });

        draft.clear();
        harness.detectChanges();
        expect(add.disabled).toBe(false);
        add.click();
        harness.detectChanges();
        expect(getRecipe.mock.calls).toEqual([[planned.id], [planned.id]]);
        expect(draft.items()).toMatchObject([{ recipe: planned, amount: PLANNED_SERVINGS }]);
        expect(draft.details().mealType).toBe('OTHER');
        expect(detail.querySelector('[role="alert"]')).toBeNull();
        expect(TestBed.inject(Router).url).toBe(CURATED_PLAN_URL);
    });

    it('cancels an obsolete recipe lookup when opening another plan and adds only the newly selected meal', async () => {
        const oldRecipe = mealPlanRecipe('old-lunch', 'Former planned lunch');
        const nextRecipe = mealPlanRecipe('new-lunch', 'Current planned lunch');
        const oldPlan = planWithRecipe(CURATED_PLAN, oldRecipe, 'Lunch');
        const nextPlan = planWithRecipe(PERSONAL_PLAN, nextRecipe, 'Lunch');
        const obsolete = new Subject<Recipe | null>();
        const getRecipe = vi.fn<RecipeLookup['getById']>().mockReturnValueOnce(obsolete).mockReturnValueOnce(of(nextRecipe));
        const api = setupMealPlans(getRecipe);
        api.getById.mockReturnValueOnce(of(oldPlan)).mockReturnValueOnce(of(nextPlan));
        const harness = await RouterTestingHarness.create(CURATED_PLAN_URL);
        renderedAction(await waitForPlanAsync(harness, oldPlan), ADD_TO_DIARY).click();
        expect(obsolete.observed).toBe(true);

        await harness.navigateByUrl(PERSONAL_PLAN_URL);
        const detail = await waitForPlanAsync(harness, nextPlan);
        expect(obsolete.observed).toBe(false);
        obsolete.next(oldRecipe);
        obsolete.complete();
        const draft = TestBed.inject(QuickMealService);
        expect(draft.items()).toEqual([]);
        expect(detail.querySelector('[role="alert"]')).toBeNull();

        renderedAction(detail, ADD_TO_DIARY).click();
        harness.detectChanges();
        expect(getRecipe.mock.calls).toEqual([[oldRecipe.id], [nextRecipe.id]]);
        expect(draft.items()).toMatchObject([{ recipe: nextRecipe, amount: PLANNED_SERVINGS }]);
        expect(draft.details().mealType).toBe('LUNCH');
    });
});

function mealPlanRecipe(id: string, name: string): Recipe {
    return {
        id: entityId<'recipe'>(id),
        name,
        servings: PLANNED_SERVINGS,
        visibility: RecipeVisibility.Private,
        usageCount: 0,
        createdAt: utcInstant('2026-05-14T08:00:00Z'),
        isOwnedByCurrentUser: true,
        isNutritionAutoCalculated: true,
        steps: [],
    };
}

function planWithRecipe(plan: MealPlan, recipe: Recipe, mealType: string): MealPlan {
    return {
        ...plan,
        days: [
            {
                id: entityId<'meal-plan-day'>(`${plan.id}-day`),
                dayNumber: planDayNumberFromStored(1),
                meals: [
                    {
                        id: entityId<'meal-plan-meal'>(`${plan.id}-meal`),
                        mealType: plannedMealTypeFromStored(mealType),
                        recipeId: recipe.id,
                        recipeName: recipe.name,
                        servings: plannedServingsFromStored(PLANNED_SERVINGS),
                    },
                ],
            },
        ],
    };
}
