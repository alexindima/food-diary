import { TestBed } from '@angular/core/testing';
import { describe, expect, it } from 'vitest';

import { provideTranslateTesting } from '../../../../../testing/translate-testing.module';
import { StatisticsDietStabilityCardComponent } from './statistics-diet-stability-card';

/* eslint-disable @typescript-eslint/no-magic-numbers -- Compact fixture values keep the stability states readable. */

describe('StatisticsDietStabilityCardComponent', () => {
    it('renders summary metrics, statuses, and missing data', async () => {
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
                { label: '5 Aug', status: 'missing' },
            ],
        });
        fixture.detectChanges();
        const root = fixture.nativeElement as HTMLElement;

        expect(root.querySelectorAll('.statistics-diet-stability-card__day-marker')).toHaveLength(3);
        expect(root.textContent).toContain('18%');
        expect(root.textContent).toContain('3 Aug');
    });
});
