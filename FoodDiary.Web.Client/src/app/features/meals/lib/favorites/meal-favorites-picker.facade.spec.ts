import { TestBed } from '@angular/core/testing';
import { of, Subject, throwError } from 'rxjs';
import { beforeEach, describe, expect, it, vi } from 'vitest';

import type { PageOf } from '../../../../shared/models/page-of.data';
import { FavoriteMealService } from '../../api/favorite-meal.service';
import type { FavoriteMeal } from '../../models/meal.data';
import { MealFavoritesPickerFacade } from './meal-favorites-picker.facade';

const PAGE_SIZE = 10;
const TOTAL_ITEMS = 23;
const STALE_TOTAL = 99;
const page = (totalItems = TOTAL_ITEMS): PageOf<FavoriteMeal> => ({ data: [], page: 1, limit: 10, totalItems, totalPages: 3 });
describe('MealFavoritesPickerFacade', () => {
    let facade: MealFavoritesPickerFacade;
    const api = { getPage: vi.fn() };
    beforeEach(() => {
        api.getPage.mockReset().mockReturnValue(of(page()));
        TestBed.configureTestingModule({ providers: [MealFavoritesPickerFacade, { provide: FavoriteMealService, useValue: api }] });
        facade = TestBed.inject(MealFavoritesPickerFacade);
    });
    it('loads nothing until requested and sends bounded pages and normalized search', () => {
        expect(api.getPage).not.toHaveBeenCalled();
        facade.load(2, ' rice ');
        expect(api.getPage).toHaveBeenCalledWith(2, PAGE_SIZE, 'rice');
        expect(facade.total()).toBe(TOTAL_ITEMS);
        expect(facade.page()).toBe(2);
        expect(facade.loading()).toBe(false);
    });
    it('cancels obsolete requests so late results cannot overwrite a new search', () => {
        const old = new Subject<PageOf<FavoriteMeal>>();
        const latest = new Subject<PageOf<FavoriteMeal>>();
        api.getPage.mockReturnValueOnce(old).mockReturnValueOnce(latest);
        facade.load();
        facade.load(1, 'rice');
        expect(old.observed).toBe(false);
        expect(facade.loading()).toBe(true);
        old.next(page(STALE_TOTAL));
        latest.next(page(2));
        latest.complete();
        expect(facade.total()).toBe(2);
        expect(facade.failed()).toBe(false);
    });
    it('keeps an error distinct from an empty result and retries the same page', () => {
        api.getPage.mockReturnValueOnce(throwError(() => new Error('offline')));
        facade.load(2, 'rice');
        expect(facade.failed()).toBe(true);
        expect(facade.loading()).toBe(false);
        facade.load(facade.page());
        expect(api.getPage).toHaveBeenLastCalledWith(2, PAGE_SIZE, 'rice');
        expect(facade.failed()).toBe(false);
    });
    it('unsubscribes an in-flight request when the dialog scope is destroyed', () => {
        const request = new Subject<PageOf<FavoriteMeal>>();
        api.getPage.mockReturnValue(request);
        facade.load();
        TestBed.resetTestingModule();
        expect(request.observed).toBe(false);
    });
    it('ignores mutations for unknown rows', () => {
        facade.load();
        facade.markRemoved('unknown');
        facade.markRestored('unknown');
        expect(facade.total()).toBe(TOTAL_ITEMS);
        expect(facade.removedIds().size).toBe(0);
        expect(api.getPage).toHaveBeenCalledTimes(1);
    });
});
