import { type ComponentFixture, TestBed } from '@angular/core/testing';
import { describe, expect, it, vi } from 'vitest';

import { provideTranslateTesting } from '../../../../../testing/translate-testing.module';
import { NutritionWeeklyTrendCardComponent } from './nutrition-weekly-trend-card';

const DAILY_GOAL = 2258;
const CARB_GOAL = 115;
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

describe('NutritionWeeklyTrendCardComponent', () => {
    it('renders seven accessible stacked daily bars and an excess insight', async () => {
        const fixture = await setupAsync();
        const element = fixture.nativeElement as HTMLElement;

        expect(element.querySelectorAll('.nutrition-trend__bar')).toHaveLength(TREND_DAYS);
        expect(element.querySelectorAll('.nutrition-trend__segment')).toHaveLength(EXPECTED_SEGMENT_COUNT);
        expect(element.querySelector('.nutrition-trend__insight')?.textContent).toContain('DASHBOARD.NUTRITION_TREND.EXCESS_TITLE');
        expect(element.querySelector('.nutrition-trend__goal strong')?.textContent).toContain('2,258');
        expect(element.querySelectorAll('.nutrition-trend__date')).toHaveLength(TREND_DAYS);
        expect(element.querySelector('.nutrition-trend__date')?.children).toHaveLength(2);
        expect(element.querySelector('.nutrition-trend__legend')?.textContent).not.toContain('DASHBOARD.NUTRITION_TREND.GOAL');
    });

    it('emits the details action', async () => {
        const fixture = await setupAsync();
        const detailsSpy = vi.fn();
        fixture.componentInstance.details.subscribe(detailsSpy);

        (fixture.nativeElement as HTMLElement).querySelector<HTMLButtonElement>('.nutrition-trend__details')?.click();

        expect(detailsSpy).toHaveBeenCalledOnce();
    });

    it('switches between seven and three visible days', async () => {
        const fixture = await setupAsync();
        const element = fixture.nativeElement as HTMLElement;
        const range = element.querySelector<HTMLSelectElement>('.nutrition-trend__range-select');

        expect(range?.value).toBe('7');
        expect(element.querySelectorAll('.nutrition-trend__bar')).toHaveLength(TREND_DAYS);

        if (range !== null) {
            range.value = '3';
            range.dispatchEvent(new Event('change'));
            fixture.detectChanges();
        }

        expect(element.querySelectorAll('.nutrition-trend__bar')).toHaveLength(SHORT_TREND_DAYS);
        expect(element.querySelector('.nutrition-trend__bars--three-days')).not.toBeNull();
    });
});

async function setupAsync(): Promise<ComponentFixture<NutritionWeeklyTrendCardComponent>> {
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
    fixture.componentRef.setInput('carbGoal', CARB_GOAL);
    fixture.detectChanges();

    return fixture;
}
