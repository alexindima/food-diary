import { TestBed } from '@angular/core/testing';
import { describe, expect, it } from 'vitest';

import { provideTranslateTesting } from '../../../../../testing/translate-testing.module';
import { StatisticsOverviewCardComponent } from './statistics-overview-card';

const PROGRESS_BAR_COUNT = 5;

describe('StatisticsOverviewCardComponent', () => {
    it('renders calorie, diary, and established nutrient progress', async () => {
        await TestBed.configureTestingModule({
            imports: [StatisticsOverviewCardComponent],
            providers: [provideTranslateTesting()],
        }).compileComponents();
        const fixture = TestBed.createComponent(StatisticsOverviewCardComponent);
        fixture.componentRef.setInput('data', {
            daysWithinGoal: 3,
            trackedDays: 4,
            periodDays: 7,
            averageCalories: 1840,
            calorieGoal: 2258,
            calorieChangePercent: -6,
            nutrients: [
                { key: 'protein', current: 109, goal: 140 },
                { key: 'fat', current: 62, goal: 80 },
                { key: 'carbs', current: 214, goal: 280 },
                { key: 'fiber', current: 16, goal: 25 },
            ],
        });
        fixture.detectChanges();
        const root = fixture.nativeElement as HTMLElement;

        expect(root.querySelectorAll('[role="progressbar"]')).toHaveLength(PROGRESS_BAR_COUNT);
        expect(root.querySelector('.statistics-overview-card__nutrient--protein')).not.toBeNull();
        expect(root.textContent).toContain('1,840');
        expect(root.textContent).toContain('4 / 7');
    });
});
