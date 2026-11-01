import { TestBed } from '@angular/core/testing';
import { TranslateService } from '@ngx-translate/core';
import { describe, expect, it } from 'vitest';

import { provideTranslateTesting } from '../../../../../testing/translate-testing.module';
import { StatisticsOverviewCardComponent } from './statistics-overview-card';

const COMPARISON_ROW_COUNT = 5;
const COMPARISON_COLUMN_COUNT = 3;
const FULLY_RECORDED_WEEK_DAYS = 7;

describe('StatisticsOverviewCardComponent', () => {
    it.each([1, 2, FULLY_RECORDED_WEEK_DAYS])('shows averages, goals and diary coverage for %s recorded days', async trackedDays => {
        await TestBed.configureTestingModule({
            imports: [StatisticsOverviewCardComponent],
            providers: [provideTranslateTesting()],
        }).compileComponents();
        const translate = TestBed.inject(TranslateService);
        translate.setTranslation('en', {
            GENERAL: { UNITS: { KCAL: 'kcal', G: 'g' } },
            STATISTICS: {
                DASHBOARD: {
                    OVERVIEW: {
                        TITLE: 'Nutrition overview',
                        RECORDED_DAYS: 'Entries on {{current}} of {{total}} days',
                        COMPLETENESS_HINT: 'Days without entries are excluded. Recorded days may be incomplete.',
                    },
                    BALANCE: { NO_GOAL: 'Not set', SUBTITLE: 'Average per recorded day and base daily goals from your profile.' },
                },
            },
        });
        translate.use('en');
        const fixture = TestBed.createComponent(StatisticsOverviewCardComponent);
        fixture.componentRef.setInput('data', {
            daysWithinGoal: 0,
            trackedDays,
            periodDays: 7,
            averageCalories: 485,
            calorieGoal: 2000,
            calorieChangePercent: -76,
            nutrients: [
                { key: 'protein', current: 180, goal: 150 },
                { key: 'fat', current: 2.8, goal: 65 },
                { key: 'carbs', current: 111.5, goal: 200 },
                { key: 'fiber', current: 14, goal: 0 },
            ],
        });
        fixture.detectChanges();
        const root = fixture.nativeElement as HTMLElement;
        const rows = root.querySelectorAll<HTMLTableRowElement>('tbody tr');

        expect(rows).toHaveLength(COMPARISON_ROW_COUNT);
        expect(root.querySelectorAll('thead th[scope="col"]')).toHaveLength(COMPARISON_COLUMN_COUNT);
        expect(rows[0].cells[1].textContent.trim()).toBe('485 kcal');
        expect(rows[0].cells[2].textContent.trim()).toBe('2,000 kcal');
        expect(rows[1].cells[1].textContent.trim()).toBe('180 g');
        expect(rows[1].cells[2].textContent.trim()).toBe('150 g');
        expect(rows[4].cells[2].textContent.trim()).toBe('Not set');
        expect(root.textContent).toContain('Nutrition overview');
        expect(root.textContent).toContain('Average per recorded day');
        expect(root.textContent.split(`Entries on ${trackedDays} of 7 days`)).toHaveLength(2);
        expect(root.textContent).toContain('Days without entries are excluded. Recorded days may be incomplete.');
        expect(root.querySelector('[role="progressbar"]')).toBeNull();

        translate.use('ru');
        fixture.detectChanges();
        expect(root.textContent).toContain('2,8');
    });
});
