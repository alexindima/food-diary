import { type ComponentFixture, TestBed } from '@angular/core/testing';
import { provideRouter } from '@angular/router';
import { TranslateService } from '@ngx-translate/core';
import { firstValueFrom } from 'rxjs';
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
const FIRST_VISIBLE_DAY = '2026-05-18';
const LATEST_VISIBLE_DAY = '2026-05-24';
const BAR = '.fd-ui-bar-chart__categorical-bar';

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

describe('Rendered trend selections', () => {
    it('keeps calories visible when no nutrient breakdown or daily goal was recorded', async () => {
        const fixture = await setupAsync();
        fixture.componentRef.setInput('interactive', true);
        fixture.componentRef.setInput('dailyGoal', 0);
        fixture.componentRef.setInput('points', [{ date: `${LATEST_VISIBLE_DAY}T12:00:00`, calories: BASE_CALORIES }]);
        fixture.detectChanges();
        const host = fixture.nativeElement as HTMLElement;
        const segments = [...host.querySelectorAll<HTMLElement>(`${BAR} .fd-ui-bar-chart__categorical-segment`)];
        expect(segments.filter(segment => Number.parseFloat(segment.style.height) > 0)).toHaveLength(1);
        expect(host.querySelector('.nutrition-trend__day-calories')?.textContent).toContain('1,800');
        expect(host.querySelector('.nutrition-trend__metric--proteins strong')?.textContent.trim()).toBe('0 GENERAL.UNITS.G');
        expect(host.querySelector('.nutrition-trend__day-goal[hidden]')).not.toBeNull();
        expect(host.querySelector('.fd-ui-bar-chart__reference-line')).toBeNull();
    });

    it('lets a historical bar open its own day and falls back to the current day when its range changes', async () => {
        const fixture = await setupAsync();
        fixture.componentRef.setInput('interactive', true);
        fixture.componentRef.setInput('currentDate', new Date(`${LATEST_VISIBLE_DAY}T12:00:00`));
        fixture.componentRef.setInput(
            'points',
            Array.from({ length: TREND_DAYS }, (_, index) => ({
                date: `2026-05-${String(FIRST_DAY + index).padStart(2, '0')}T12:00:00`,
                calories: BASE_CALORIES + index * DAILY_CALORIE_INCREMENT,
            })),
        );
        fixture.detectChanges();
        const host = fixture.nativeElement as HTMLElement;
        expect(host.querySelector('.nutrition-trend__open-day[hidden]')).not.toBeNull();
        host.querySelector<HTMLElement>(BAR)?.click();
        fixture.detectChanges();
        const link = host.querySelector<HTMLAnchorElement>('.nutrition-trend__open-day');
        expect(link?.hidden).toBe(false);
        expect(link?.getAttribute('href')).toBe(`/dashboard?date=${FIRST_VISIBLE_DAY}`);
        expect(host.querySelector(`${BAR}[aria-pressed="true"]`)?.getAttribute('aria-label')).toContain('18 May');

        host.querySelector<HTMLButtonElement>('.nutrition-trend__range [role="radio"]')?.click();
        fixture.detectChanges();
        expect(host.querySelectorAll(BAR)).toHaveLength(SHORT_TREND_DAYS);
        expect(host.querySelector(`${BAR}[aria-pressed="true"]`)?.getAttribute('aria-label')).toContain('24 May');
        expect(host.querySelector('.nutrition-trend__open-day[hidden]')).not.toBeNull();
    });
});

describe('Rendered trend comparisons', () => {
    it('requires both comparison values before announcing a recorded calorie ratio', async () => {
        const fixture = await setupAsync({ kind: 'in-progress', tone: 'neutral', metric: 'calories', goal: DAILY_GOAL });
        const host = fixture.nativeElement as HTMLElement;
        expect(host.querySelector('.nutrition-trend__insight')?.textContent).not.toContain('CALORIE_COMPARISON');
        fixture.componentRef.setInput('insight', { kind: 'in-progress', tone: 'neutral', metric: 'calories', current: BASE_CALORIES });
        fixture.detectChanges();
        expect(host.querySelector('.nutrition-trend__insight')?.textContent).not.toContain('CALORIE_COMPARISON');

        const translate = TestBed.inject(TranslateService);
        translate.setTranslation('en', { NUTRITION_TREND: { INSIGHT: { CALORIE_COMPARISON: '{{ current }} of {{ goal }} calories' } } });
        await firstValueFrom(translate.use('en'));
        fixture.componentRef.setInput('insight', {
            kind: 'in-progress',
            tone: 'neutral',
            metric: 'calories',
            current: BASE_CALORIES,
            goal: DAILY_GOAL,
        });
        fixture.detectChanges();
        expect(host.querySelector('.nutrition-trend__insight')?.textContent).toContain('1,800 of 2,258 calories');
    });
});

it('removes stale day details when history clears and restores them for the new period', async () => {
    const fixture = await setupAsync();
    fixture.componentRef.setInput('interactive', true);
    fixture.detectChanges();
    const host = fixture.nativeElement as HTMLElement;
    host.querySelector<HTMLElement>(BAR)?.click();
    fixture.detectChanges();
    expect(host.querySelector<HTMLAnchorElement>('.nutrition-trend__open-day')?.getAttribute('href')).toBe(
        `/dashboard?date=${FIRST_VISIBLE_DAY}`,
    );

    fixture.componentRef.setInput('points', []);
    fixture.detectChanges();
    expect(host.querySelectorAll(BAR)).toHaveLength(0);
    expect(host.querySelector('.nutrition-trend__day')).toBeNull();

    fixture.componentRef.setInput('points', [{ date: `${LATEST_VISIBLE_DAY}T12:00:00`, calories: BASE_CALORIES, proteins: 100 }]);
    fixture.detectChanges();
    expect(host.querySelector(`${BAR}[aria-pressed="true"]`)).not.toBeNull();
    expect(host.querySelector<HTMLAnchorElement>('.nutrition-trend__open-day')?.getAttribute('href')).toBe(
        `/dashboard?date=${LATEST_VISIBLE_DAY}`,
    );
    expect(host.querySelector('.nutrition-trend__metric--proteins strong')?.textContent.trim()).toBe('100 GENERAL.UNITS.G');
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

describe('Static weekly nutrition chart', () => {
    it('plots each recorded macro in partial daily data and keeps the actual calorie labels', async () => {
        const proteinGrams = 40;
        const fiberGrams = 25;
        const fixture = await setupAsync();
        fixture.componentRef.setInput('points', [
            { date: `${FIRST_VISIBLE_DAY}T12:00:00`, calories: BASE_CALORIES, proteins: proteinGrams },
            { date: `${LATEST_VISIBLE_DAY}T12:00:00`, calories: DAILY_GOAL, fiber: fiberGrams },
        ]);
        fixture.detectChanges();

        const root = fixture.nativeElement as HTMLElement;
        const bars = Array.from(root.querySelectorAll<HTMLElement>(BAR));
        expect(bars).toHaveLength(2);
        expect(bars[0].getAttribute('aria-label')).toBe(`18 May: ${BASE_CALORIES}`);
        expect(bars[1].getAttribute('aria-label')).toBe(`24 May: ${DAILY_GOAL}`);
        const visibleSegments = bars.map(bar =>
            Array.from(bar.querySelectorAll<HTMLElement>('.fd-ui-bar-chart__categorical-segment')).flatMap((segment, index) =>
                Number.parseFloat(segment.style.height) > 0 ? [index] : [],
            ),
        );
        expect(visibleSegments).toEqual([[0], [SEGMENTS_PER_DAY - 1]]);
        expect(root.querySelector('.nutrition-trend__day')).toBeNull();
    });
});
