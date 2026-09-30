import { TestBed } from '@angular/core/testing';
import { describe, expect, it } from 'vitest';

import { provideTranslateTesting } from '../../../../../testing/translate-testing.module';
import { StatisticsDietStabilityCardComponent } from './statistics-diet-stability-card';

/* eslint-disable @typescript-eslint/no-magic-numbers -- Compact fixture values keep the stability states readable. */

describe('StatisticsDietStabilityCardComponent', () => {
    it.each([false, true])('shows metrics only when the period has enough recorded days: %s', async enoughData => {
        await TestBed.configureTestingModule({
            imports: [StatisticsDietStabilityCardComponent],
            providers: [provideTranslateTesting()],
        }).compileComponents();
        const fixture = TestBed.createComponent(StatisticsDietStabilityCardComponent);
        fixture.componentRef.setInput('data', {
            stableCount: 1,
            totalCount: 3,
            averageDeviationPercent: 18,
            longestLoggingStreak: 2,
            usesDailyIntervals: true,
            hasGoal: true,
            days: [
                { label: '3 Aug', status: 'stable' },
                { label: '4 Aug', status: 'deviation' },
                { label: '5 Aug', status: enoughData ? 'stable' : 'missing' },
            ],
        });
        fixture.detectChanges();
        const root = fixture.nativeElement as HTMLElement;

        expect(root.querySelectorAll('.statistics-diet-stability-card__day-marker')).toHaveLength(enoughData ? 3 : 0);
        if (enoughData) {
            expect(root.textContent).toContain('18%');
            expect(root.textContent).toContain('3 Aug');
        } else {
            expect(root.textContent).not.toContain('18%');
            expect(root.textContent).toContain('STATISTICS.DASHBOARD.STABILITY.INSUFFICIENT_DATA');
        }
    });
});
