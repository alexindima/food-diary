import { signal } from '@angular/core';
import { TestBed } from '@angular/core/testing';
import { of, Subject } from 'rxjs';
import { beforeEach, describe, expect, it, vi } from 'vitest';

import { waitForAsyncTasksAsync } from '../../../../../testing/async-testing';
import type { PageOf } from '../../../../shared/models/page-of.data';
import { MealPlanService } from '../../api/meal-plan.service';
import type { MealPlanSummary } from '../../models/meal-plan.data';
import { MealPlanListFacade } from './meal-plan-list.facade';
import { type MealPlanListQuery, mealPlanListQueryKey } from './meal-plan-list-query';
import { MEAL_PLAN_LIST_QUERY_STATE } from './meal-plan-list-query-state';

const initial: MealPlanListQuery = { page: 2, dietType: 'Balanced', source: 'all' };
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
            MealPlanListFacade,
            { provide: MealPlanService, useValue: { getPage } },
            {
                provide: MEAL_PLAN_LIST_QUERY_STATE,
                useValue: {
                    initial,
                    current,
                    changes,
                    isCurrent: (query: MealPlanListQuery): boolean => mealPlanListQueryKey(query) === mealPlanListQueryKey(current()),
                    writeAsync,
                    normalizePageAsync: vi.fn().mockResolvedValue(false),
                },
            },
        ],
    });
});
describe('MealPlanListFacade route list state', () => {
    it('switches to saved plans on page one while keeping the diet and excluding previous counts', async () => {
        const facade = TestBed.inject(MealPlanListFacade);
        await settleAsync();
        const pending = new Subject<PageOf<MealPlanSummary>>();
        getPage.mockReturnValueOnce(pending);
        facade.filterBySource('saved');
        await settleAsync();
        expect(current()).toEqual({ page: 1, dietType: 'Balanced', source: 'saved' });
        expect(getPage).toHaveBeenLastCalledWith('Balanced', 1, PAGE_SIZE, false);
        expect(facade.totalItems()).toBe(0);
        pending.next({ ...response(1, 1), totalItems: 1 });
        pending.complete();
        await settleAsync();
        expect(facade.totalItems()).toBe(1);
        facade.loadPlans('Keto');
        await settleAsync();
        expect(getPage).toHaveBeenLastCalledWith('Keto', 1, PAGE_SIZE, false);
        const back: MealPlanListQuery = { page: 2, dietType: 'Balanced', source: 'curated' };
        current.set(back);
        changes.next(back);
        await settleAsync();
        expect(facade.sourceFilter()).toBe('curated');
        expect(getPage).toHaveBeenLastCalledWith('Balanced', 2, PAGE_SIZE, true);
    });
    it('restores URL diet/page and does not reset on initial load', async () => {
        const f = TestBed.inject(MealPlanListFacade);
        await settleAsync();
        expect(getPage).toHaveBeenLastCalledWith('Balanced', 2, PAGE_SIZE, undefined);
        expect(f.pageIndex()).toBe(1);
    });
    it('resets a new diet to page one and preserves a valid empty result without looping', async () => {
        const f = TestBed.inject(MealPlanListFacade);
        await settleAsync();
        getPage.mockImplementation((_diet: string, page: number) => of({ ...response(page, 0), totalItems: 0 }));
        f.loadPlans('Keto');
        await settleAsync();
        expect(current()).toEqual({ page: 1, dietType: 'Keto', source: 'all' });
        const count = getPage.mock.calls.length;
        await settleAsync();
        expect(getPage).toHaveBeenCalledTimes(count);
    });
    it('recovers a current stale page once with replacement history', async () => {
        getPage.mockImplementation((_diet: string, page: number) => of(response(page, 1)));
        TestBed.inject(MealPlanListFacade);
        await settleAsync();
        await settleAsync();
        expect(writeAsync).toHaveBeenCalledWith({ page: 1, dietType: 'Balanced', source: 'all' }, { replaceUrl: true });
        expect(getPage).toHaveBeenLastCalledWith('Balanced', 1, PAGE_SIZE, undefined);
    });
    it('never lets an older response overwrite a newer browser diet', async () => {
        const old = new Subject<PageOf<MealPlanSummary>>();
        getPage.mockReturnValueOnce(old);
        TestBed.inject(MealPlanListFacade);
        await settleAsync();
        const next: MealPlanListQuery = { page: 1, dietType: 'Keto', source: 'all' };
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

describe('MealPlanListFacade result identity', () => {
    it('ignores an obsolete page limit delivered while a newer browser filter is being reconciled', async () => {
        const old = new Subject<PageOf<MealPlanSummary>>();
        const pending = new Subject<PageOf<MealPlanSummary>>();
        getPage.mockReturnValueOnce(old).mockReturnValueOnce(pending);
        const facade = TestBed.inject(MealPlanListFacade);
        await settleAsync();
        expect(old.observed).toBe(true);
        const next: MealPlanListQuery = { page: 1, dietType: 'Keto', source: 'saved' };

        current.set(next);
        changes.next(next);
        old.next(response(initial.page, 1));
        old.complete();

        expect(current()).toEqual(next);
        expect(facade.totalItems()).toBe(0);
        expect(writeAsync).not.toHaveBeenCalledWith(expect.anything(), { replaceUrl: true });
        await settleAsync();
        expect(pending.observed).toBe(true);
        expect(getPage).toHaveBeenLastCalledWith('Keto', next.page, PAGE_SIZE, false);
        pending.next(response(next.page));
        pending.complete();
        await settleAsync();
        expect(current()).toEqual(next);
        expect(facade.totalItems()).toBe(TOTAL_ITEMS);
    });

    it('never exposes another diet count while a browser query loads or fails', async () => {
        const facade = TestBed.inject(MealPlanListFacade);
        await settleAsync();
        await settleAsync();
        expect(facade.totalItems()).toBe(TOTAL_ITEMS);
        const pending = new Subject<PageOf<MealPlanSummary>>();
        getPage.mockReturnValueOnce(pending);
        const next: MealPlanListQuery = { page: 1, dietType: 'Keto', source: 'all' };
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
        const facade = TestBed.inject(MealPlanListFacade);
        await settleAsync();
        await settleAsync();
        const pending = new Subject<PageOf<MealPlanSummary>>();
        getPage.mockReturnValueOnce(pending);
        facade.retryPlans();
        await settleAsync();
        expect(facade.totalItems()).toBe(TOTAL_ITEMS);
    });
});
