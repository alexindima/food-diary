import { TestBed } from '@angular/core/testing';
import { TranslateService } from '@ngx-translate/core';
import { describe, expect, it } from 'vitest';

import { provideTranslateTesting } from '../../../../../testing/translate-testing.module';
import { StatisticsNutrientBalanceCardComponent } from './statistics-nutrient-balance-card';

const COMPARISON_ROW_COUNT = 5;
const COMPARISON_COLUMN_COUNT = 3;

describe('StatisticsNutrientBalanceCardComponent', () => {
    it('compares recorded averages with explicit calorie and gram targets, including an unset goal', async () => {
        await TestBed.configureTestingModule({
            imports: [StatisticsNutrientBalanceCardComponent],
            providers: [provideTranslateTesting()],
        }).compileComponents();
        const translate = TestBed.inject(TranslateService);
        translate.setTranslation('en', {
            GENERAL: { UNITS: { KCAL: 'kcal', G: 'g' } },
            STATISTICS: { DASHBOARD: { BALANCE: { NO_GOAL: 'Not set' } } },
        });
        translate.use('en');
        const fixture = TestBed.createComponent(StatisticsNutrientBalanceCardComponent);
        fixture.componentRef.setInput('data', {
            daysWithinGoal: 0,
            trackedDays: 2,
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
        expect(root.querySelector('[role="progressbar"]')).toBeNull();
    });
});
