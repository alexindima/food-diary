import { TestBed } from '@angular/core/testing';
import { TranslateService } from '@ngx-translate/core';
import { describe, expect, it } from 'vitest';

import { provideTranslateTesting } from '../../../../../testing/translate-testing.module';
import { StatisticsOverviewCardComponent } from './statistics-overview-card';

/* eslint-disable @typescript-eslint/no-magic-numbers -- Recorded-day scenarios reproduce sparse and fully recorded periods. */

describe('StatisticsOverviewCardComponent', () => {
    it.each([1, 2, 7])('keeps units and the averaging basis visible with %s recorded days', async trackedDays => {
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
                        AVERAGE_PER_DAY: 'Average per recorded day',
                        RECORDED_DAYS: 'Entries on {{current}} of {{total}} days',
                    },
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
                { key: 'protein', current: 10.4, goal: 150 },
                { key: 'fat', current: 2.8, goal: 65 },
                { key: 'carbs', current: 111.5, goal: 200 },
                { key: 'fiber', current: 14, goal: 28 },
            ],
        });
        fixture.detectChanges();
        const root = fixture.nativeElement as HTMLElement;

        const visibleUnits = Array.from(root.querySelectorAll('span')).filter(
            element => element.textContent.includes('kcal') && element.closest('[hidden]') === null,
        );

        expect(visibleUnits, 'calorie units remain visible for sparse diary entries').not.toHaveLength(0);
        expect(root.textContent).toContain('485');
        expect(root.textContent).toContain('Average per recorded day');
        expect(root.textContent.split(`Entries on ${trackedDays} of 7 days`)).toHaveLength(2);
        expect(root.querySelector('[role="progressbar"]')).toBeNull();
        expect(root.textContent).not.toContain('150');
        expect(root.textContent).toContain('10.4');

        translate.use('ru');
        fixture.detectChanges();
        expect(root.textContent).toContain('10,4');
    });
});
