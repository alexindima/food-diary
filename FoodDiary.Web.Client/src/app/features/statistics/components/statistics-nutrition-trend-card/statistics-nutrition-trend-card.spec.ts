import { TestBed } from '@angular/core/testing';
import { describe, expect, it, vi } from 'vitest';

import { provideTranslateTesting } from '../../../../../testing/translate-testing.module';
import { StatisticsNutritionTrendCardComponent } from './statistics-nutrition-trend-card';

const CALORIE_GOAL = 2258;
const NUTRIENT_COUNT = 4;
const NUTRIENT_SCALE_MAXIMUM = 40;
const NUTRIENT_THREE_QUARTER_TICK = 30;
const NUTRIENT_HALF_TICK = 20;
const NUTRIENT_QUARTER_TICK = 10;
const NUTRIENT_TICKS = [NUTRIENT_SCALE_MAXIMUM, NUTRIENT_THREE_QUARTER_TICK, NUTRIENT_HALF_TICK, NUTRIENT_QUARTER_TICK, 0] as const;

describe('StatisticsNutritionTrendCardComponent', () => {
    it('distinguishes missing entries, switches chart mode, and emits tab changes', async () => {
        await TestBed.configureTestingModule({
            imports: [StatisticsNutritionTrendCardComponent],
            providers: [provideTranslateTesting()],
        }).compileComponents();
        const fixture = TestBed.createComponent(StatisticsNutritionTrendCardComponent);
        fixture.componentRef.setInput('tabs', [
            { value: 'calories', labelKey: 'STATISTICS.NUTRITION_TABS.CALORIES' },
            { value: 'macros', labelKey: 'STATISTICS.NUTRITION_TABS.MACROS' },
        ]);
        fixture.componentRef.setInput('selectedTab', 'calories');
        fixture.componentRef.setInput('calorieGoal', CALORIE_GOAL);
        fixture.componentRef.setInput('days', [
            { date: '2026-08-02', label: '2 Aug', calories: 1800, protein: 100, fat: 60, carbs: 180, fiber: 20 },
            { date: '2026-08-03', label: '3 Aug', calories: null, protein: 0, fat: 0, carbs: 0, fiber: 0 },
        ]);
        fixture.componentRef.setInput('insights', []);
        const tabChange = vi.fn<(value: string) => void>();
        fixture.componentInstance.selectedTabChange.subscribe(tabChange);
        fixture.detectChanges();

        const root = fixture.nativeElement as HTMLElement;
        expect(root.querySelectorAll('.fd-ui-bar-chart__categorical-bar[role="img"]')).toHaveLength(1);
        expect(root.querySelector('.statistics-nutrition-trend-card__missing')).toBeNull();
        expect(root.querySelector('.statistics-nutrition-trend-card__chart-header fd-ui-select')).not.toBeNull();
        expect(root.querySelectorAll('.fd-ui-bar-chart__categorical-segment')).toHaveLength(1);
        expect(root.querySelector('.statistics-nutrition-trend-card__legend')).toBeNull();

        fixture.componentRef.setInput('selectedTab', 'macros');
        fixture.detectChanges();
        expect(root.querySelector('.fd-ui-bar-chart__categorical-bar--grouped')?.querySelectorAll('span')).toHaveLength(NUTRIENT_COUNT);
        expect(root.querySelector('.fd-ui-bar-chart__axis-unit')?.textContent).toContain('GENERAL.UNITS.G');

        fixture.componentRef.setInput('days', [
            { date: '2026-08-02', label: '2 Aug', calories: 1800, protein: 13, fat: 6, carbs: 29, fiber: 2 },
            { date: '2026-08-03', label: '3 Aug', calories: null, protein: 0, fat: 0, carbs: 0, fiber: 0 },
        ]);
        fixture.detectChanges();
        expect(fixture.componentInstance['chartMaximum']()).toBe(NUTRIENT_SCALE_MAXIMUM);
        expect(fixture.componentInstance['ticks']()).toEqual(NUTRIENT_TICKS);

        fixture.componentRef.setInput('selectedTab', 'distribution');
        fixture.detectChanges();
        expect(
            root.querySelector('.fd-ui-bar-chart__categorical-bar')?.querySelectorAll('.fd-ui-bar-chart__categorical-segment'),
        ).toHaveLength(NUTRIENT_COUNT);

        fixture.componentInstance['changeChartMode']('line');
        fixture.detectChanges();
        expect(root.querySelector('fd-ui-line-chart')).not.toBeNull();
        expect(fixture.componentInstance['distributionLineSeries']()).toHaveLength(NUTRIENT_COUNT);
        expect(fixture.componentInstance['calorieLinePoints']()[0]?.label).toBe('2 Aug');

        fixture.componentRef.setInput('selectedTab', 'calories');
        fixture.detectChanges();
        expect(fixture.componentInstance['calorieReferenceLines']()).toHaveLength(1);

        fixture.componentInstance['onTabChange']('macros');
        expect(tabChange).toHaveBeenCalledWith('macros');
    });
});
