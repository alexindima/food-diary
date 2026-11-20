import { TestBed } from '@angular/core/testing';
import { describe, expect, it, vi } from 'vitest';

import { provideTranslateTesting } from '../../../../../testing/translate-testing.module';
import { STATISTICS_NUTRITION_TABS } from '../../lib/statistics-tabs.config';
import type { StatisticsTrendInsight } from '../../models/statistics-dashboard-card.models';
import { StatisticsNutritionTrendCardComponent } from './statistics-nutrition-trend-card';

const CALORIE_GOAL = 2258;
const NUTRIENT_COUNT = 4;
const NUTRIENT_SCALE_MAXIMUM = 40;
const NUTRIENT_THREE_QUARTER_TICK = 30;
const NUTRIENT_HALF_TICK = 20;
const NUTRIENT_QUARTER_TICK = 10;
const NUTRIENT_TICKS = [NUTRIENT_SCALE_MAXIMUM, NUTRIENT_THREE_QUARTER_TICK, NUTRIENT_HALF_TICK, NUTRIENT_QUARTER_TICK, 0] as const;

describe('StatisticsNutritionTrendCardComponent colors', () => {
    it('preserves nutrient colors when switching from bars to lines', async () => {
        await TestBed.configureTestingModule({
            imports: [StatisticsNutritionTrendCardComponent],
            providers: [provideTranslateTesting()],
        }).compileComponents();
        const fixture = TestBed.createComponent(StatisticsNutritionTrendCardComponent);
        fixture.componentRef.setInput('tabs', []);
        fixture.componentRef.setInput('selectedTab', 'macros');
        fixture.componentRef.setInput('calorieGoal', CALORIE_GOAL);
        fixture.componentRef.setInput('days', [
            { date: '2026-08-02', label: '2 Aug', calories: 1800, protein: 100, fat: 60, carbs: 180, fiber: 20 },
        ]);
        fixture.componentRef.setInput('insights', []);
        fixture.detectChanges();

        const barColors = fixture.componentInstance['barChartCategories']()[0].values.map(value => value.color);
        const lineColors = fixture.componentInstance['nutrientLineSeries']().map(series => series.color);
        expect(lineColors, 'Nutrient colors must stay the same between chart modes').toEqual(barColors);
    });
});

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
        fixture.componentInstance.selectedTab.subscribe(tabChange);
        fixture.detectChanges();

        const root = fixture.nativeElement as HTMLElement;
        expect(root.querySelectorAll('.fd-ui-bar-chart__categorical-bar[role="img"]')).toHaveLength(1);
        expect(root.querySelector('.statistics-nutrition-trend-card__missing')).toBeNull();
        const chartModeButtons = root.querySelectorAll<HTMLButtonElement>('.statistics-nutrition-trend-card__chart-mode button');
        expect(chartModeButtons).toHaveLength(2);
        expect(chartModeButtons[0].getAttribute('aria-checked')).toBe('true');
        expect(root.querySelector('.statistics-nutrition-trend-card__chart-mode [role="radiogroup"]')?.getAttribute('aria-label')).toBe(
            'STATISTICS.DASHBOARD.TREND.CHART_TYPE_LABEL',
        );
        expect(root.querySelector('.statistics-nutrition-trend-card__data-toggle [role="radiogroup"]')?.getAttribute('aria-label')).toBe(
            'STATISTICS.NUTRITION_TABS_LABEL',
        );
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

        chartModeButtons[1].click();
        fixture.detectChanges();
        expect(root.querySelector('fd-ui-line-chart')).not.toBeNull();
        expect(chartModeButtons[1].getAttribute('aria-checked')).toBe('true');
        expect(fixture.componentInstance['distributionLineSeries']()).toHaveLength(NUTRIENT_COUNT);
        expect(fixture.componentInstance['calorieLinePoints']()[0]?.label).toBe('2 Aug');

        fixture.componentRef.setInput('selectedTab', 'calories');
        fixture.detectChanges();
        expect(fixture.componentInstance['calorieReferenceLines']()).toHaveLength(1);

        root.querySelectorAll<HTMLButtonElement>('.statistics-nutrition-trend-card__data-toggle button')[1].click();
        expect(tabChange).toHaveBeenCalledWith('macros');
    });
});

describe('Statistics nutrient lines and insight rows', () => {
    it('keeps recorded-diary context visible while switching to separate nutrient lines', async () => {
        await TestBed.configureTestingModule({
            imports: [StatisticsNutritionTrendCardComponent],
            providers: [provideTranslateTesting()],
        }).compileComponents();
        const fixture = TestBed.createComponent(StatisticsNutritionTrendCardComponent);
        const insights: StatisticsTrendInsight[] = [
            {
                key: 'calories',
                labelKey: 'STATISTICS.NUTRITION_TABS.CALORIES',
                value: 5,
                unitKey: 'STATISTICS.DASHBOARD.TREND.PERCENT_UNIT',
                tone: 'positive',
                detailKey: 'STATISTICS.DASHBOARD.TREND.VS_GOAL',
            },
            {
                key: 'protein',
                labelKey: 'STATISTICS.DASHBOARD.NUTRIENTS.PROTEIN',
                value: null,
                unitKey: 'STATISTICS.DASHBOARD.TREND.GRAM_UNIT',
                tone: 'neutral',
                detailKey: 'STATISTICS.DASHBOARD.TREND.VS_GOAL',
            },
            {
                key: 'completeness',
                labelKey: 'STATISTICS.DASHBOARD.TREND.INCOMPLETE_DATA',
                value: null,
                unitKey: '',
                tone: 'neutral',
                detailKey: 'STATISTICS.DASHBOARD.TREND.INCOMPLETE_DATA_HINT',
            },
        ];
        fixture.componentRef.setInput('tabs', STATISTICS_NUTRITION_TABS);
        fixture.componentRef.setInput('selectedTab', 'calories');
        fixture.componentRef.setInput('calorieGoal', CALORIE_GOAL);
        fixture.componentRef.setInput('days', [
            { date: '2026-08-02', label: '2 Aug', calories: 1800, protein: 100, fat: 60, carbs: 180, fiber: 20 },
            { date: '2026-08-03', label: '3 Aug', calories: 1900, protein: 110, fat: 65, carbs: 190, fiber: 25 },
        ]);
        fixture.componentRef.setInput('insights', insights);
        fixture.detectChanges();
        const root = fixture.nativeElement as HTMLElement;

        root.querySelectorAll<HTMLButtonElement>('.statistics-nutrition-trend-card__chart-mode button')[1].click();
        fixture.detectChanges();
        expect(root.querySelectorAll('.fd-ui-line-chart__line')).toHaveLength(1);
        root.querySelectorAll<HTMLButtonElement>('.statistics-nutrition-trend-card__data-toggle button')[1].click();
        fixture.detectChanges();

        expect(root.querySelectorAll('.fd-ui-line-chart__line')).toHaveLength(NUTRIENT_COUNT);
        expect(root.querySelector('fd-ui-line-chart')?.textContent).toContain('GENERAL.UNITS.G');
        const rows = root.querySelectorAll('.statistics-nutrition-trend-card__insight');
        expect(rows[0].querySelector('.statistics-nutrition-trend-card__insight-value--positive')?.textContent).toContain('+5');
        expect(rows[1].querySelector('.fd-ui-stat-value')?.textContent).toBe('—');
        expect(rows[2].textContent).toContain('STATISTICS.DASHBOARD.TREND.INCOMPLETE_DATA_HINT');
        expect(rows[2].querySelector('.fd-ui-stat-value')).toBeNull();
    });
});

describe('Statistics nutrition insight icon mapping', () => {
    it('distinguishes fat insights from other nutrient insights in the rendered card', async () => {
        await TestBed.configureTestingModule({
            imports: [StatisticsNutritionTrendCardComponent],
            providers: [provideTranslateTesting()],
        }).compileComponents();
        const fixture = TestBed.createComponent(StatisticsNutritionTrendCardComponent);
        const insights: StatisticsTrendInsight[] = [
            {
                key: 'fat',
                labelKey: 'STATISTICS.DASHBOARD.NUTRIENTS.FAT',
                detailKey: 'STATISTICS.DASHBOARD.TREND.VS_GOAL',
                value: null,
                unitKey: 'STATISTICS.DASHBOARD.TREND.GRAM_UNIT',
                tone: 'neutral',
            },
            {
                key: 'fiber',
                labelKey: 'STATISTICS.DASHBOARD.NUTRIENTS.FIBER',
                detailKey: 'STATISTICS.DASHBOARD.TREND.VS_GOAL',
                value: null,
                unitKey: 'STATISTICS.DASHBOARD.TREND.GRAM_UNIT',
                tone: 'neutral',
            },
        ];
        fixture.componentRef.setInput('tabs', STATISTICS_NUTRITION_TABS);
        fixture.componentRef.setInput('selectedTab', 'calories');
        fixture.componentRef.setInput('calorieGoal', CALORIE_GOAL);
        fixture.componentRef.setInput('days', []);
        fixture.componentRef.setInput('insights', insights);
        fixture.detectChanges();

        const root = fixture.nativeElement as HTMLElement;
        const rows = Array.from(root.querySelectorAll('.statistics-nutrition-trend-card__insight'));
        expect(rows.map(row => row.querySelector('.fd-ui-icon__glyph')?.textContent)).toEqual(['water_drop', 'monitoring']);
        expect(rows.map(row => row.querySelector('.fd-ui-stat-value')?.textContent)).toEqual(['—', '—']);
    });
});
