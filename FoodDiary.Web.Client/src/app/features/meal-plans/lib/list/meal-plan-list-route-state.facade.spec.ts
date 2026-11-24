import { ChangeDetectionStrategy, Component, computed, inject, input } from '@angular/core';
import { TestBed } from '@angular/core/testing';
import { convertToParamMap, provideRouter, Router, withComponentInputBinding } from '@angular/router';
import { RouterTestingHarness } from '@angular/router/testing';
import { describe, expect, it } from 'vitest';

import { readMealPlanListQuery } from './meal-plan-list-query';
import { MealPlanListRouteStateFacade } from './meal-plan-list-route-state.facade';

@Component({
    template: '',
    providers: [MealPlanListRouteStateFacade],
    changeDetection: ChangeDetectionStrategy.OnPush,
})
class ListRoute {
    public readonly page = input<string | string[]>();
    public readonly dietType = input<string | string[]>();
    public readonly source = input<string | string[]>();
    public readonly state = inject(MealPlanListRouteStateFacade);

    public constructor() {
        this.state.bind(
            computed(() =>
                readMealPlanListQuery(
                    convertToParamMap({ page: this.page() ?? null, dietType: this.dietType() ?? null, source: this.source() ?? null }),
                ),
            ),
        );
    }
}

function setupRoute(): void {
    TestBed.configureTestingModule({
        providers: [provideRouter([{ path: 'meal-plans', component: ListRoute }], withComponentInputBinding())],
    });
}

describe('Meal plan list route query ownership', () => {
    it('restores saved plans from the URL and preserves source when changing diet', async () => {
        setupRoute();
        const harness = await RouterTestingHarness.create();
        const page = await harness.navigateByUrl('/meal-plans?source=saved&dietType=Balanced&page=2&campaign=qa', ListRoute);
        expect(page.state.initial).toEqual({ page: 2, dietType: 'Balanced', source: 'saved' });
        await page.state.writeAsync({ page: 1, dietType: 'Keto', source: 'saved' });
        const router = TestBed.inject(Router);
        expect(router.parseUrl(router.url).queryParams).toEqual({ source: 'saved', dietType: 'Keto', campaign: 'qa' });
    });
    it('writes the diet filter and page while preserving unrelated query keys', async () => {
        setupRoute();
        const harness = await RouterTestingHarness.create();
        const page = await harness.navigateByUrl('/meal-plans?page=2&dietType=Balanced&campaign=qa', ListRoute);
        const subscription = page.state.changes.subscribe();
        expect(page.state.initial).toEqual({ page: 2, dietType: 'Balanced', source: 'all' });
        await page.state.writeAsync({ page: 1, dietType: 'Keto', source: 'all' });
        const router = TestBed.inject(Router);
        expect(router.parseUrl(router.url).queryParams).toEqual({ dietType: 'Keto', campaign: 'qa' });
        await harness.navigateByUrl('/meal-plans?page=2&dietType=Balanced&campaign=qa', ListRoute);
        await harness.fixture.whenStable();
        expect(page.state.current()).toEqual(page.state.initial);
        subscription.unsubscribe();
    });

    it('keeps the newest filter when routing cancels an older filter write', async () => {
        setupRoute();
        const harness = await RouterTestingHarness.create();
        const page = await harness.navigateByUrl('/meal-plans?dietType=Balanced&page=2&campaign=qa', ListRoute);
        const savedKeto = { page: 1, dietType: 'Keto', source: 'saved' } as const;

        const older = page.state.writeAsync({ page: 1, dietType: 'Balanced', source: 'curated' });
        const current = page.state.writeAsync(savedKeto);

        await expect(older).resolves.toBe(false);
        await expect(current).resolves.toBe(true);
        expect(page.state.current()).toEqual(savedKeto);
        const router = TestBed.inject(Router);
        expect(router.parseUrl(router.url).queryParams).toEqual({ dietType: 'Keto', source: 'saved', campaign: 'qa' });
    });

    it('normalizes invalid API pages and clears only its own keys when reset', async () => {
        setupRoute();
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
