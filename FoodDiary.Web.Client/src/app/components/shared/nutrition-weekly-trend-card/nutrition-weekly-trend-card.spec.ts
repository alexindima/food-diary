import { type ComponentFixture, TestBed } from '@angular/core/testing';
import { provideRouter } from '@angular/router';
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

        expect(element.querySelectorAll('.fd-ui-bar-chart__categorical-bar')).toHaveLength(TREND_DAYS);
        expect(element.querySelectorAll('.fd-ui-bar-chart__categorical-bar[tabindex="0"]')).toHaveLength(TREND_DAYS);
        expect(element.querySelectorAll('.fd-ui-bar-chart__categorical-segment')).toHaveLength(EXPECTED_SEGMENT_COUNT);
        expect(element.querySelector('.nutrition-trend__insight')?.textContent).toContain('NUTRITION_TREND.INSIGHT.CARB_EXCESS_TITLE');
        expect(element.querySelector('.fd-ui-bar-chart__reference-line')?.textContent).toContain('2,258');
        expect(element.querySelector('.fd-ui-bar-chart__reference-line--outside')).toBeNull();
        expect(element.querySelectorAll('.fd-ui-bar-chart__categorical-labels span')).toHaveLength(TREND_DAYS);
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

    it('renders a historical state instead of a current-day insight for a past date', async () => {
        const fixture = await setupAsync({ kind: 'in-progress', tone: 'neutral' });
        fixture.componentRef.setInput('isToday', false);
        fixture.detectChanges();
        const element = fixture.nativeElement as HTMLElement;

        expect(element.querySelector('.nutrition-trend__insight')?.textContent).toContain('NUTRITION_TREND.INSIGHT.HISTORICAL_TITLE');
        expect(element.querySelector('.nutrition-trend__insight')?.textContent).toContain('NUTRITION_TREND.INSIGHT.HISTORICAL_HINT');
        expect(element.querySelector('.nutrition-trend__insight')?.textContent).not.toContain('NUTRITION_TREND.INSIGHT.IN_PROGRESS_TITLE');
        expect(element.querySelector('.nutrition-trend__details')).toBeNull();
    });

    it('switches between seven and three visible days', async () => {
        const fixture = await setupAsync();
        const element = fixture.nativeElement as HTMLElement;
        const range = element.querySelector('.nutrition-trend__range [role="radiogroup"]');
        const options = range?.querySelectorAll<HTMLButtonElement>('[role="radio"]');

        expect(options).toHaveLength(2);
        expect(options?.[0].textContent).toContain('NUTRITION_TREND.THREE_DAYS');
        expect(options?.[1].textContent).toContain('NUTRITION_TREND.SEVEN_DAYS');
        expect(options?.[1].getAttribute('aria-checked')).toBe('true');
        expect(range?.getAttribute('aria-label')).toBe('NUTRITION_TREND.RANGE_LABEL');
        expect(element.querySelectorAll('.fd-ui-bar-chart__categorical-bar')).toHaveLength(TREND_DAYS);

        options?.[0].click();
        fixture.detectChanges();

        expect(element.querySelectorAll('.fd-ui-bar-chart__categorical-bar')).toHaveLength(SHORT_TREND_DAYS);
        expect(options?.[0].getAttribute('aria-checked')).toBe('true');
        expect(options?.[1].getAttribute('aria-checked')).toBe('false');

        options?.[1].click();
        fixture.detectChanges();

        expect(element.querySelectorAll('.fd-ui-bar-chart__categorical-bar')).toHaveLength(TREND_DAYS);
    });
});

it('keeps coexisting trend range selections independent', async () => {
    const first = await setupAsync();
    const second = TestBed.createComponent(NutritionWeeklyTrendCardComponent);
    second.componentRef.setInput('points', []);
    second.componentRef.setInput('dailyGoal', DAILY_GOAL);
    second.componentRef.setInput('insight', DEFAULT_INSIGHT);
    second.detectChanges();

    (first.nativeElement as HTMLElement).querySelector<HTMLButtonElement>('[role="radio"]')?.click();
    first.detectChanges();
    second.detectChanges();

    expect(first.componentInstance['visibleDays']()).toBe(SHORT_TREND_DAYS);
    expect(second.componentInstance['visibleDays']()).toBe(TREND_DAYS);
    expect((second.nativeElement as HTMLElement).querySelector('[role="radio"][aria-checked="true"]')?.textContent).toContain(
        'NUTRITION_TREND.SEVEN_DAYS',
    );
});

it('selects a day without navigating and scales the stack to recorded calories', async () => {
    const fixture = await setupAsync();
    fixture.componentRef.setInput('interactive', true);
    fixture.detectChanges();
    const element = fixture.nativeElement as HTMLElement;
    const bars = element.querySelectorAll<HTMLElement>('.fd-ui-bar-chart__categorical-bar');
    bars[0].click();
    fixture.detectChanges();
    expect(bars[0].getAttribute('aria-pressed')).toBe('true');
    expect(element.querySelector('.nutrition-trend__day')?.textContent).toContain('May');
    const categories = fixture.componentInstance['barChartCategories']();
    const total = categories[0].values.reduce((sum, segment) => sum + (segment.value ?? 0), 0);
    expect(total).toBeCloseTo(BASE_CALORIES);
    bars[1].dispatchEvent(new KeyboardEvent('keydown', { key: 'Enter', bubbles: true }));
    fixture.detectChanges();
    expect(bars[1].getAttribute('aria-pressed')).toBe('true');
});
async function setupAsync(insight: NutritionTrendInsight = DEFAULT_INSIGHT): Promise<ComponentFixture<NutritionWeeklyTrendCardComponent>> {
    await TestBed.resetTestingModule()
        .configureTestingModule({
            imports: [NutritionWeeklyTrendCardComponent],
            providers: [provideTranslateTesting(), provideRouter([])],
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
