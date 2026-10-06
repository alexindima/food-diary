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
    it('writes the diet filter and page while preserving unrelated query keys', async () => {
        TestBed.configureTestingModule({ providers: [provideRouter([{ path: 'meal-plans', component: ListRoute }])] });
        const harness = await RouterTestingHarness.create();
        const page = await harness.navigateByUrl('/meal-plans?page=2&dietType=Balanced&campaign=qa', ListRoute);
        const subscription = page.state.changes.subscribe();
        expect(page.state.initial).toEqual({ page: 2, dietType: 'Balanced' });
        await page.state.writeAsync({ page: 1, dietType: 'Keto' });
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
        await page.state.writeAsync({ page: 1, dietType: null });
        expect(router.parseUrl(router.url).queryParams).toEqual({ campaign: 'qa' });
        expect(await page.state.writeAsync(page.state.current())).toBe(false);
        subscription.unsubscribe();
    });
});
