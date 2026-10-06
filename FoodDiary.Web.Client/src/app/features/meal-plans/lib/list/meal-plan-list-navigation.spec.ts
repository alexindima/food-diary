import { signal } from '@angular/core';
import { TestBed } from '@angular/core/testing';
import { of, Subject } from 'rxjs';
import { beforeEach, describe, expect, it, vi } from 'vitest';

import { waitForAsyncTasksAsync } from '../../../../../testing/async-testing';
import type { PageOf } from '../../../../shared/models/page-of.data';
import { QuickMealService } from '../../../meals/contracts/quick-meal';
import { RECIPE_LOOKUP } from '../../../recipes/contracts/recipe-lookup';
import { MealPlanService } from '../../api/meal-plan.service';
import type { MealPlanSummary } from '../../models/meal-plan.data';
import { MealPlanFacade } from '../meal-plan.facade';
import { type MealPlanListQuery, mealPlanListQueryKey } from './meal-plan-list-query';
import { MEAL_PLAN_LIST_QUERY_STATE } from './meal-plan-list-query-state';

const initial: MealPlanListQuery = { page: 2, dietType: 'Balanced' };
const current = signal(initial);
const changes = new Subject<MealPlanListQuery>();
const getPage = vi.fn();
const TOTAL_PAGES = 3;
const PAGE_SIZE = 50;
const TOTAL_ITEMS = 100;
const commitAsync = vi.fn<() => Promise<boolean>>().mockResolvedValue(true);
const writeAsync = vi.fn(async (query: MealPlanListQuery, _options?: { replaceUrl?: boolean }): Promise<boolean> => {
    if (mealPlanListQueryKey(query) === mealPlanListQueryKey(current())) {
        return false;
    }
    current.set(query);
    changes.next(query);
    const committed = await commitAsync();
    return committed;
});
function response(page = 2, totalPages = TOTAL_PAGES): PageOf<MealPlanSummary> {
    return { data: [], page, limit: PAGE_SIZE, totalPages, totalItems: TOTAL_ITEMS };
}
async function settleAsync(): Promise<void> {
    TestBed.tick();
    await waitForAsyncTasksAsync();
    TestBed.tick();
    await waitForAsyncTasksAsync();
}
beforeEach(() => {
    TestBed.resetTestingModule();
    current.set({ ...initial });
    writeAsync.mockClear();
    getPage.mockReset().mockImplementation((_diet: string, page: number) => of(response(page)));
    TestBed.configureTestingModule({
        providers: [
            MealPlanFacade,
            { provide: MealPlanService, useValue: { getPage, getById: vi.fn() } },
            { provide: QuickMealService, useValue: { hasItems: signal(false) } },
            { provide: RECIPE_LOOKUP, useValue: { getById: vi.fn() } },
            {
                provide: MEAL_PLAN_LIST_QUERY_STATE,
                useValue: { initial, current, changes, writeAsync, normalizePageAsync: vi.fn().mockResolvedValue(false) },
            },
        ],
    });
});
describe('MealPlanFacade route list state', () => {
    it('restores URL diet/page and does not reset on initial load', async () => {
        const f = TestBed.inject(MealPlanFacade);
        f.loadPlans();
        await settleAsync();
        expect(getPage).toHaveBeenLastCalledWith('Balanced', 2, PAGE_SIZE);
        expect(f.pageIndex()).toBe(1);
    });
    it('resets a new diet to page one and preserves a valid empty result without looping', async () => {
        const f = TestBed.inject(MealPlanFacade);
        await settleAsync();
        getPage.mockImplementation((_diet: string, page: number) => of({ ...response(page, 0), totalItems: 0 }));
        f.loadPlans('Keto');
        await settleAsync();
        expect(current()).toEqual({ page: 1, dietType: 'Keto' });
        const count = getPage.mock.calls.length;
        await settleAsync();
        expect(getPage).toHaveBeenCalledTimes(count);
    });
    it('recovers a current stale page once with replacement history', async () => {
        getPage.mockImplementation((_diet: string, page: number) => of(response(page, 1)));
        TestBed.inject(MealPlanFacade);
        await settleAsync();
        await settleAsync();
        expect(writeAsync).toHaveBeenCalledWith({ page: 1, dietType: 'Balanced' }, { replaceUrl: true });
        expect(getPage).toHaveBeenLastCalledWith('Balanced', 1, PAGE_SIZE);
    });
    it('never lets an older response overwrite a newer browser diet', async () => {
        const old = new Subject<PageOf<MealPlanSummary>>();
        getPage.mockReturnValueOnce(old);
        TestBed.inject(MealPlanFacade);
        await settleAsync();
        const next: MealPlanListQuery = { page: 1, dietType: 'Keto' };
        current.set(next);
        changes.next(next);
        await settleAsync();
        writeAsync.mockClear();
        old.next(response(2, 0));
        old.complete();
        await settleAsync();
        expect(current()).toEqual(next);
        expect(writeAsync).not.toHaveBeenCalledWith(expect.anything(), { replaceUrl: true });
    });
});

describe('MealPlanFacade result identity', () => {
    it('never exposes another diet count while a browser query loads or fails', async () => {
        const facade = TestBed.inject(MealPlanFacade);
        await settleAsync();
        await settleAsync();
        expect(facade.totalItems()).toBe(TOTAL_ITEMS);
        const pending = new Subject<PageOf<MealPlanSummary>>();
        getPage.mockReturnValueOnce(pending);
        const next: MealPlanListQuery = { page: 1, dietType: 'Keto' };
        current.set(next);
        changes.next(next);
        await settleAsync();
        expect(facade.totalItems()).toBe(0);
        pending.error(new Error('Unavailable'));
        await settleAsync();
        expect(facade.hasLoadError()).toBe(true);
        expect(facade.totalItems()).toBe(0);
    });

    it('retains the current count during a retry for the same query', async () => {
        const facade = TestBed.inject(MealPlanFacade);
        await settleAsync();
        await settleAsync();
        const pending = new Subject<PageOf<MealPlanSummary>>();
        getPage.mockReturnValueOnce(pending);
        facade.retryPlans();
        await settleAsync();
        expect(facade.totalItems()).toBe(TOTAL_ITEMS);
    });
});
