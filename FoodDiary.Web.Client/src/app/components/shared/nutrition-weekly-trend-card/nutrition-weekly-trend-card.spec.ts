import { type ComponentFixture, TestBed } from '@angular/core/testing';
import { describe, expect, it, vi } from 'vitest';

import { provideTranslateTesting } from '../../../../testing/translate-testing.module';
import { type NutritionTrendInsight, NutritionWeeklyTrendCardComponent } from './nutrition-weekly-trend-card';

const DAILY_GOAL = 2258;
const TREND_DAYS = 7;
const SHORT_TREND_DAYS = 3;
const SEGMENTS_PER_DAY = 4;
const EXPECTED_SEGMENT_COUNT = TREND_DAYS * SEGMENTS_PER_DAY;
const FIRST_DAY = 18;
const BASE_CALORIES = 1800;
const DAILY_CALORIE_INCREMENT = 50;
const LATEST_POINT_INDEX = TREND_DAYS - 1;
const EXCESS_CARBS = 224;
const REGULAR_CARBS = 180;
const DEFAULT_INSIGHT: NutritionTrendInsight = { kind: 'carb-excess', tone: 'warning', metric: 'carbs', current: EXCESS_CARBS, goal: 115 };

describe('NutritionWeeklyTrendCardComponent', () => {
    it('renders seven accessible stacked daily bars and an excess insight', async () => {
        const fixture = await setupAsync();
        const element = fixture.nativeElement as HTMLElement;

        expect(element.querySelectorAll('.nutrition-trend__bar')).toHaveLength(TREND_DAYS);
        expect(element.querySelectorAll('.nutrition-trend__segment')).toHaveLength(EXPECTED_SEGMENT_COUNT);
        expect(element.querySelector('.nutrition-trend__insight')?.textContent).toContain('NUTRITION_TREND.INSIGHT.CARB_EXCESS_TITLE');
        expect(element.querySelector('.nutrition-trend__goal strong')?.textContent).toContain('2,258');
        expect(element.querySelectorAll('.nutrition-trend__date')).toHaveLength(TREND_DAYS);
        expect(element.querySelector('.nutrition-trend__date')?.children).toHaveLength(2);
        expect(element.querySelector('.nutrition-trend__legend')?.textContent).not.toContain('NUTRITION_TREND.GOAL');
    });

    it('emits the details action', async () => {
        const fixture = await setupAsync();
        const detailsSpy = vi.fn();
        fixture.componentInstance.details.subscribe(detailsSpy);

        (fixture.nativeElement as HTMLElement).querySelector<HTMLButtonElement>('.nutrition-trend__details')?.click();

        expect(detailsSpy).toHaveBeenCalledOnce();
    });

    it('renders a neutral empty state without a details action', async () => {
        const fixture = await setupAsync({ kind: 'empty', tone: 'neutral' });
        const element = fixture.nativeElement as HTMLElement;

        expect(element.querySelector('.nutrition-trend__insight')?.textContent).toContain('NUTRITION_TREND.INSIGHT.EMPTY_TITLE');
        expect(element.querySelector('.nutrition-trend__insight--neutral')).not.toBeNull();
        expect(element.querySelector('.nutrition-trend__details')).toBeNull();
    });

    it('switches between seven and three visible days', async () => {
        const fixture = await setupAsync();
        const element = fixture.nativeElement as HTMLElement;
        const range = element.querySelector<HTMLButtonElement>('.nutrition-trend__range .fd-ui-select__control');

        expect(range?.textContent).toContain('NUTRITION_TREND.SEVEN_DAYS');
        expect(range?.getAttribute('aria-label')).toBe('NUTRITION_TREND.RANGE_LABEL');
        expect(element.querySelectorAll('.nutrition-trend__bar')).toHaveLength(TREND_DAYS);

        fixture.componentInstance['changeVisibleDays'](SHORT_TREND_DAYS);
        fixture.detectChanges();

        expect(element.querySelectorAll('.nutrition-trend__bar')).toHaveLength(SHORT_TREND_DAYS);
        expect(element.querySelector('.nutrition-trend__bars--three-days')).not.toBeNull();
    });
});

async function setupAsync(insight: NutritionTrendInsight = DEFAULT_INSIGHT): Promise<ComponentFixture<NutritionWeeklyTrendCardComponent>> {
    await TestBed.resetTestingModule()
        .configureTestingModule({
            imports: [NutritionWeeklyTrendCardComponent],
            providers: [provideTranslateTesting()],
        })
        .compileComponents();

    const fixture = TestBed.createComponent(NutritionWeeklyTrendCardComponent);
    fixture.componentRef.setInput(
        'points',
        Array.from({ length: TREND_DAYS }, (_, index) => ({
            date: `2026-05-${String(FIRST_DAY + index).padStart(2, '0')}T00:00:00Z`,
            calories: BASE_CALORIES + index * DAILY_CALORIE_INCREMENT,
            proteins: 100,
            fats: 60,
            carbs: index === LATEST_POINT_INDEX ? EXCESS_CARBS : REGULAR_CARBS,
            fiber: 24,
        })),
    );
    fixture.componentRef.setInput('dailyGoal', DAILY_GOAL);
    fixture.componentRef.setInput('insight', insight);
    fixture.detectChanges();

    return fixture;
}
