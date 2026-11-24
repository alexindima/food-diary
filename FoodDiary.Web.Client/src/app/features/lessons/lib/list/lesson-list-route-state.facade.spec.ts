import { ChangeDetectionStrategy, Component, computed, inject, input } from '@angular/core';
import { TestBed } from '@angular/core/testing';
import { convertToParamMap, provideRouter, Router, withComponentInputBinding } from '@angular/router';
import { RouterTestingHarness } from '@angular/router/testing';
import { firstValueFrom, type Observable, ReplaySubject, Subject } from 'rxjs';
import { describe, expect, it } from 'vitest';

import { readLessonListQuery } from './lesson-list-query';
import { LessonListRouteStateFacade } from './lesson-list-route-state.facade';

@Component({
    template: '',
    providers: [LessonListRouteStateFacade],
    changeDetection: ChangeDetectionStrategy.OnPush,
})
class ListRoute {
    public readonly page = input<string | string[]>();
    public readonly search = input<string | string[]>();
    public readonly category = input<string | string[]>();
    public readonly difficulty = input<string | string[]>();
    public readonly sort = input<string | string[]>();
    public readonly state = inject(LessonListRouteStateFacade);

    public constructor() {
        this.state.bind(
            computed(() =>
                readLessonListQuery(
                    convertToParamMap({
                        page: this.page() ?? null,
                        search: this.search() ?? null,
                        category: this.category() ?? null,
                        difficulty: this.difficulty() ?? null,
                        sort: this.sort() ?? null,
                    }),
                ),
            ),
        );
    }
}

describe('Lesson list route query ownership', () => {
    it('writes owned filters, preserves unrelated query keys and restores committed queries', async () => {
        TestBed.configureTestingModule({
            providers: [provideRouter([{ path: 'lessons', component: ListRoute }], withComponentInputBinding())],
        });
        const harness = await RouterTestingHarness.create();
        const page = await harness.navigateByUrl('/lessons?page=2&category=Micronutrients&search=iron&campaign=qa', ListRoute);
        const subscription = page.state.changes.subscribe();
        expect(page.state.initial).toEqual({ page: 2, category: 'Micronutrients', search: 'iron', difficulty: null, sort: 'recommended' });
        await page.state.writeAsync({ ...page.state.current(), page: 1, category: null, search: '', sort: 'shortest' });
        const router = TestBed.inject(Router);
        expect(router.parseUrl(router.url).queryParams).toEqual({ sort: 'shortest', campaign: 'qa' });
        await harness.navigateByUrl('/lessons?page=2&category=Micronutrients&search=iron&campaign=qa', ListRoute);
        await harness.fixture.whenStable();
        expect(page.state.current()).toEqual(page.state.initial);
        subscription.unsubscribe();
    });

    it('replaces a malformed page without dropping filters and skips unchanged writes', async () => {
        TestBed.configureTestingModule({
            providers: [provideRouter([{ path: 'lessons', component: ListRoute }], withComponentInputBinding())],
        });
        const harness = await RouterTestingHarness.create();
        const page = await harness.navigateByUrl('/lessons?page=-2&difficulty=Advanced&campaign=qa', ListRoute);
        const subscription = page.state.changes.subscribe();
        expect(await page.state.normalizePageAsync()).toBe(true);
        const router = TestBed.inject(Router);
        expect(router.parseUrl(router.url).queryParams).toEqual({ difficulty: 'Advanced', campaign: 'qa' });
        expect(await page.state.writeAsync(page.state.current())).toBe(false);
        subscription.unsubscribe();
    });

    it('keeps the latest filter write pending when the Router cancels an older query change', async () => {
        const older = { search: 'iron', entered: new Subject<void>(), decision: new Subject<boolean>() };
        const latest = { search: 'fiber', entered: new Subject<void>(), decision: new ReplaySubject<boolean>(1) };
        TestBed.configureTestingModule({
            providers: [
                provideRouter(
                    [
                        {
                            path: 'lessons',
                            component: ListRoute,
                            runGuardsAndResolvers: 'paramsOrQueryParamsChange',
                            canActivate: [
                                (route): boolean | Observable<boolean> => {
                                    const pending = [older, latest].find(
                                        navigation => navigation.search === route.queryParamMap.get('search'),
                                    );
                                    if (pending === undefined) {
                                        return true;
                                    }
                                    pending.entered.next();
                                    return pending.decision;
                                },
                            ],
                        },
                    ],
                    withComponentInputBinding(),
                ),
            ],
        });
        const harness = await RouterTestingHarness.create();
        const page = await harness.navigateByUrl('/lessons?campaign=qa', ListRoute);
        const router = TestBed.inject(Router);
        const olderEntered = firstValueFrom(older.entered);
        const latestEntered = firstValueFrom(latest.entered);
        const firstWrite = page.state.writeAsync({ ...page.state.current(), search: older.search });
        await olderEntered;

        const latestQuery = { ...page.state.current(), search: latest.search };
        const latestWrite = page.state.writeAsync(latestQuery);
        await latestEntered;
        await expect(firstWrite).resolves.toBe(false);
        expect(page.state.current()).toEqual(latestQuery);
        expect(router.url).toBe('/lessons?campaign=qa');
        const duplicateWrite = page.state.writeAsync(latestQuery);
        latest.decision.next(true);
        await expect(duplicateWrite).resolves.toBe(false);
        await expect(latestWrite).resolves.toBe(true);
        expect(router.parseUrl(router.url).queryParams).toEqual({ campaign: 'qa', search: 'fiber' });
    });
});
