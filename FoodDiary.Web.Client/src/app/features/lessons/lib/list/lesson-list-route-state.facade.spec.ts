import { ChangeDetectionStrategy, Component, inject } from '@angular/core';
import { TestBed } from '@angular/core/testing';
import { provideRouter, Router } from '@angular/router';
import { RouterTestingHarness } from '@angular/router/testing';
import { describe, expect, it } from 'vitest';

import { LessonListRouteStateFacade } from './lesson-list-route-state.facade';

@Component({
    template: '',
    providers: [LessonListRouteStateFacade],
    changeDetection: ChangeDetectionStrategy.OnPush,
})
class ListRoute {
    public readonly state = inject(LessonListRouteStateFacade);
}

describe('Lesson list route query ownership', () => {
    it('writes owned filters, preserves unrelated query keys and restores committed queries', async () => {
        TestBed.configureTestingModule({ providers: [provideRouter([{ path: 'lessons', component: ListRoute }])] });
        const harness = await RouterTestingHarness.create();
        const page = await harness.navigateByUrl('/lessons?page=2&category=Micronutrients&search=iron&campaign=qa', ListRoute);
        const subscription = page.state.changes.subscribe();
        expect(page.state.initial).toEqual({ page: 2, category: 'Micronutrients', search: 'iron', difficulty: null, sort: 'recommended' });
        await page.state.writeAsync({ ...page.state.current(), page: 1, category: null, search: '', sort: 'shortest' });
        const router = TestBed.inject(Router);
        expect(router.parseUrl(router.url).queryParams).toEqual({ sort: 'shortest', campaign: 'qa' });
        await harness.navigateByUrl('/lessons?page=2&category=Micronutrients&search=iron&campaign=qa', ListRoute);
        expect(page.state.current()).toEqual(page.state.initial);
        subscription.unsubscribe();
    });

    it('replaces a malformed page without dropping filters and skips unchanged writes', async () => {
        TestBed.configureTestingModule({ providers: [provideRouter([{ path: 'lessons', component: ListRoute }])] });
        const harness = await RouterTestingHarness.create();
        const page = await harness.navigateByUrl('/lessons?page=-2&difficulty=Advanced&campaign=qa', ListRoute);
        const subscription = page.state.changes.subscribe();
        expect(await page.state.normalizePageAsync()).toBe(true);
        const router = TestBed.inject(Router);
        expect(router.parseUrl(router.url).queryParams).toEqual({ difficulty: 'Advanced', campaign: 'qa' });
        expect(await page.state.writeAsync(page.state.current())).toBe(false);
        subscription.unsubscribe();
    });
});
