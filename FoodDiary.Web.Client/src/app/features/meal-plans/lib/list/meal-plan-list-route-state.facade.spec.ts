import { ChangeDetectionStrategy, Component, inject } from '@angular/core';
import { TestBed } from '@angular/core/testing';
import { provideRouter, Router } from '@angular/router';
import { RouterTestingHarness } from '@angular/router/testing';
import { describe, expect, it } from 'vitest';

import { MealPlanListRouteStateFacade } from './meal-plan-list-route-state.facade';

@Component({
    template: '',
    providers: [MealPlanListRouteStateFacade],
    changeDetection: ChangeDetectionStrategy.OnPush,
})
class ListRoute {
    public readonly state = inject(MealPlanListRouteStateFacade);
}

describe('Meal plan list route query ownership', () => {
    it('restores saved plans from the URL and preserves source when changing diet', async () => {
        TestBed.configureTestingModule({ providers: [provideRouter([{ path: 'meal-plans', component: ListRoute }])] });
        const harness = await RouterTestingHarness.create();
        const page = await harness.navigateByUrl('/meal-plans?source=saved&dietType=Balanced&page=2&campaign=qa', ListRoute);
        expect(page.state.initial).toEqual({ page: 2, dietType: 'Balanced', source: 'saved' });
        await page.state.writeAsync({ page: 1, dietType: 'Keto', source: 'saved' });
        const router = TestBed.inject(Router);
        expect(router.parseUrl(router.url).queryParams).toEqual({ source: 'saved', dietType: 'Keto', campaign: 'qa' });
    });
    it('writes the diet filter and page while preserving unrelated query keys', async () => {
        TestBed.configureTestingModule({ providers: [provideRouter([{ path: 'meal-plans', component: ListRoute }])] });
        const harness = await RouterTestingHarness.create();
        const page = await harness.navigateByUrl('/meal-plans?page=2&dietType=Balanced&campaign=qa', ListRoute);
        const subscription = page.state.changes.subscribe();
        expect(page.state.initial).toEqual({ page: 2, dietType: 'Balanced', source: 'all' });
        await page.state.writeAsync({ page: 1, dietType: 'Keto', source: 'all' });
        const router = TestBed.inject(Router);
        expect(router.parseUrl(router.url).queryParams).toEqual({ dietType: 'Keto', campaign: 'qa' });
        await harness.navigateByUrl('/meal-plans?page=2&dietType=Balanced&campaign=qa', ListRoute);
        expect(page.state.current()).toEqual(page.state.initial);
        subscription.unsubscribe();
    });

    it('normalizes invalid API pages and clears only its own keys when reset', async () => {
        TestBed.configureTestingModule({ providers: [provideRouter([{ path: 'meal-plans', component: ListRoute }])] });
        const harness = await RouterTestingHarness.create();
        const page = await harness.navigateByUrl('/meal-plans?page=99999&dietType=Balanced&campaign=qa', ListRoute);
        const subscription = page.state.changes.subscribe();
        expect(await page.state.normalizePageAsync()).toBe(true);
        const router = TestBed.inject(Router);
        expect(router.parseUrl(router.url).queryParams).toEqual({ dietType: 'Balanced', campaign: 'qa' });
        await page.state.writeAsync({ page: 1, dietType: null, source: 'all' });
        expect(router.parseUrl(router.url).queryParams).toEqual({ campaign: 'qa' });
        expect(await page.state.writeAsync(page.state.current())).toBe(false);
        subscription.unsubscribe();
    });
});
