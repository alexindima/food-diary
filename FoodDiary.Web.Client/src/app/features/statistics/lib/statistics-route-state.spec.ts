import { ChangeDetectionStrategy, Component, signal } from '@angular/core';
import { TestBed } from '@angular/core/testing';
import { provideRouter, Router } from '@angular/router';
import { RouterTestingHarness } from '@angular/router/testing';
import { describe, expect, it } from 'vitest';

import type { NutritionChartTab, StatisticsRange } from './statistics-data-mapper';
import { connectStatisticsRouteState } from './statistics-route-state';

@Component({ template: '', changeDetection: ChangeDetectionStrategy.OnPush })
class StatisticsRoute {
    public readonly selectedRange = signal<StatisticsRange>('week');
    public readonly selectedNutritionTab = signal<NutritionChartTab>('calories');
    public readonly customRangeModel = signal<{ range: { start: Date | null; end: Date | null } | null }>({ range: null });
    public constructor() {
        connectStatisticsRouteState(this);
    }
}

describe('Statistics route state ownership', () => {
    it('restores a report from its URL, writes changed selections, and restores query navigation', async () => {
        TestBed.configureTestingModule({ providers: [provideRouter([{ path: 'statistics', component: StatisticsRoute }])] });
        const harness = await RouterTestingHarness.create();
        const original = '/statistics?range=custom&dateFrom=2025-01-01&dateTo=2025-01-25&nutritionTab=macros&campaign=qa';
        const page = await harness.navigateByUrl(original, StatisticsRoute);
        expect(page.selectedRange()).toBe('custom');
        expect(page.selectedNutritionTab()).toBe('macros');
        expect(page.customRangeModel().range?.start).toEqual(new Date('2025-01-01T00:00:00'));
        page.selectedRange.set('year');
        page.selectedNutritionTab.set('distribution');
        TestBed.tick();
        await harness.fixture.whenStable();
        const router = TestBed.inject(Router);
        expect(router.parseUrl(router.url).queryParams).toEqual({ range: 'year', nutritionTab: 'distribution', campaign: 'qa' });
        await harness.navigateByUrl(original, StatisticsRoute);
        expect(page.selectedRange()).toBe('custom');
        expect(page.selectedNutritionTab()).toBe('macros');
        expect(page.customRangeModel().range?.end).toEqual(new Date('2025-01-25T00:00:00'));
    });

    it('normalizes malformed report filters while preserving unrelated URL keys', async () => {
        TestBed.configureTestingModule({ providers: [provideRouter([{ path: 'statistics', component: StatisticsRoute }])] });
        const harness = await RouterTestingHarness.create();
        const page = await harness.navigateByUrl(
            '/statistics?range=custom&dateFrom=2025-02-29&dateTo=2025-03-01&nutritionTab=wrong&campaign=qa',
            StatisticsRoute,
        );
        await harness.fixture.whenStable();
        expect(page.selectedRange()).toBe('week');
        expect(TestBed.inject(Router).parseUrl(TestBed.inject(Router).url).queryParams).toEqual({ campaign: 'qa' });
    });
});
