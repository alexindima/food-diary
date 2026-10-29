import { finalize, firstValueFrom, type Observable, of, Subject, throwError } from 'rxjs';
import { describe, expect, it, vi } from 'vitest';

import { nutritionMealFixture, nutritionSnapshotFixture } from '../../../../testing/nutrition-details.fixtures';
import type { DashboardSnapshot } from '../../../shared/models/dashboard.data';
import type { DashboardSnapshotQuery } from '../api/dashboard.service';
import { getDashboardDateUtc } from './dashboard-date.utils';
import { loadNutritionDay } from './nutrition-details.loader';

const date = new Date('2026-10-28T00:00:00');
const PAGE_SIZE = 100;
const MEAL_COUNT = 102;
const query = { date: getDashboardDateUtc(date), timeZoneOffsetMinutes: -date.getTimezoneOffset(), timeZoneId: 'Asia/Tbilisi' };
const meals = Array.from({ length: MEAL_COUNT }, (_, index) => nutritionMealFixture({ id: `meal-${index}` }));
const page = (start: number, end: number): DashboardSnapshot =>
    nutritionSnapshotFixture(meals.slice(start, end), { meals: { items: meals.slice(start, end), total: meals.length } });

describe('Complete nutrition day loading', () => {
    it('loads beyond the dashboard preview and publishes only after the last page', () => {
        const secondPage = new Subject<DashboardSnapshot>();
        const request = vi
            .fn<(query: DashboardSnapshotQuery) => Observable<DashboardSnapshot>>()
            .mockReturnValueOnce(of(page(0, PAGE_SIZE)))
            .mockReturnValueOnce(secondPage);
        const next = vi.fn();
        loadNutritionDay(query, request).subscribe(next);
        expect(next).not.toHaveBeenCalled();
        expect(request.mock.calls.map(([value]) => value)).toEqual([
            { ...query, page: 1, pageSize: 100 },
            { ...query, page: 2, pageSize: 100 },
        ]);
        expect(request.mock.calls[0][0].date.toISOString()).toBe('2026-10-28T00:00:00.000Z');
        secondPage.next(page(PAGE_SIZE, MEAL_COUNT));
        secondPage.complete();
        expect(next).toHaveBeenCalledExactlyOnceWith({ snapshot: page(0, PAGE_SIZE), meals });
    });

    it('cancels in-flight loading when its dialog subscriber is destroyed', () => {
        const cancelled = vi.fn();
        const pending = new Subject<DashboardSnapshot>();
        const subscription = loadNutritionDay(query, () => pending.pipe(finalize(cancelled))).subscribe();
        subscription.unsubscribe();
        expect(cancelled).toHaveBeenCalledOnce();
    });

    it('fails closed on a missing page, duplicate rows, or a changing day', async () => {
        const incomplete = vi
            .fn<(query: DashboardSnapshotQuery) => Observable<DashboardSnapshot>>()
            .mockReturnValueOnce(of(page(0, PAGE_SIZE)))
            .mockReturnValueOnce(of(page(MEAL_COUNT, MEAL_COUNT)));
        await expect(firstValueFrom(loadNutritionDay(query, incomplete))).rejects.toThrow('Incomplete nutrition day');
        const duplicate = vi
            .fn<(query: DashboardSnapshotQuery) => Observable<DashboardSnapshot>>()
            .mockReturnValueOnce(of(page(0, PAGE_SIZE)))
            .mockReturnValueOnce(of(page(0, 2)));
        await expect(firstValueFrom(loadNutritionDay(query, duplicate))).rejects.toThrow('Incomplete nutrition day');
        const changed = vi
            .fn<(query: DashboardSnapshotQuery) => Observable<DashboardSnapshot>>()
            .mockReturnValueOnce(of(page(0, PAGE_SIZE)))
            .mockReturnValueOnce(of(nutritionSnapshotFixture([], { meals: { items: meals.slice(PAGE_SIZE), total: 103 } })));
        await expect(firstValueFrom(loadNutritionDay(query, changed))).rejects.toThrow('Nutrition day changed');
    });

    it('supports no meals and propagates load failures instead of producing a partial breakdown', async () => {
        const day = await firstValueFrom(loadNutritionDay(query, () => of(nutritionSnapshotFixture())));
        expect(day.meals).toEqual([]);
        await expect(firstValueFrom(loadNutritionDay(query, () => throwError(() => new Error('offline'))))).rejects.toThrow('offline');
    });
});
