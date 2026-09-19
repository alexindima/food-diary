import { TestBed } from '@angular/core/testing';
import { describe, expect, it } from 'vitest';

import { provideTranslateTesting } from '../../../../testing/translate-testing.module';
import { DayNutritionSummaryComponent } from './day-nutrition-summary';

describe('Day summary empty state', () => {
    it.each([0, 1])('distinguishes %s meals with zero calories', async mealCount => {
        await TestBed.configureTestingModule({
            imports: [DayNutritionSummaryComponent],
            providers: [provideTranslateTesting()],
        })
            .overrideComponent(DayNutritionSummaryComponent, { set: { template: '' } })
            .compileComponents();
        const fixture = TestBed.createComponent(DayNutritionSummaryComponent);
        fixture.componentRef.setInput('data', {
            mealCount,
            dailyGoal: 2000,
            dailyConsumed: 0,
            weeklyConsumed: 0,
            weeklyGoal: null,
            nutrientBars: [],
        });
        const title = fixture.componentInstance['insights']()[0].title;
        expect(title).toBe(mealCount === 0 ? 'DASHBOARD.DAY_SUMMARY.PULSE_EMPTY_TITLE' : 'DASHBOARD.DAY_SUMMARY.PULSE_CALORIES');
    });
});
