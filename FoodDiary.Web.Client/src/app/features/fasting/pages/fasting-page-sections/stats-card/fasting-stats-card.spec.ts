import type { ComponentFixture } from '@angular/core/testing';
import { TestBed } from '@angular/core/testing';
import { TranslateService } from '@ngx-translate/core';
import { beforeEach, describe, expect, it } from 'vitest';

import { provideTranslateTesting } from '../../../../../../testing/translate-testing.module';
import type { FastingStats } from '../../../../../shared/models/fasting.data';
import { FastingStatsCardComponent } from './fasting-stats-card';

const TOTAL_COMPLETED = 8;
const CURRENT_STREAK = 3;
const AVERAGE_DURATION_HOURS = 17.5;
const COMPLETION_RATE = 62.5;
const CHECK_IN_RATE = 75;

describe('FastingStatsCardComponent', () => {
    beforeEach(() => {
        TestBed.configureTestingModule({
            imports: [FastingStatsCardComponent],
            providers: [provideTranslateTesting()],
        });
    });

    /* eslint-disable @typescript-eslint/no-magic-numbers -- These counts cover singular and Russian plural boundaries. */
    it.each([
        ['en', 1, 'ONE'],
        ['en', 21, 'OTHER'],
        ['ru', 1, 'ONE'],
        ['ru', 2, 'FEW'],
        ['ru', 11, 'MANY'],
        ['ru', 21, 'ONE'],
    ])('uses %s plural form for %i days', (language, count, category) => {
        TestBed.inject(TranslateService).use(String(language));
        const fixture = createComponent(createStats({ currentStreak: Number(count) }));
        expect(getElement(fixture).textContent).toContain(`FASTING.DAY_UNIT_${category}`);
    });
    /* eslint-enable @typescript-eslint/no-magic-numbers -- Restore the normal rule after boundary cases. */

    it('renders empty state without stats', () => {
        const fixture = createComponent(null);
        const element = getElement(fixture);

        expect(element.textContent).toContain('FASTING.NO_STATS');
        expect(element.querySelector('.fasting__stats')).toBeNull();
    });

    it('renders primary and personal summary stats', () => {
        const fixture = createComponent(createStats());
        const element = getElement(fixture);

        expect(element.textContent).toContain(TOTAL_COMPLETED.toString());
        expect(element.textContent).toContain(CURRENT_STREAK.toString());
        expect(element.textContent).toContain(AVERAGE_DURATION_HOURS.toString());
        expect(element.textContent).toContain(`${COMPLETION_RATE}%`);
        expect(element.textContent).toContain(`${CHECK_IN_RATE}%`);
        expect(element.textContent).toContain('FASTING.CHECK_IN.SYMPTOMS.HEADACHE');
    });

    it('hides personal summary when no personal data is available', () => {
        const fixture = createComponent(
            createStats({
                completionRateLast30Days: 0,
                checkInRateLast30Days: 0,
                lastCheckInAtUtc: null,
                topSymptom: null,
            }),
        );
        const element = getElement(fixture);

        expect(element.textContent).not.toContain('FASTING.PERSONAL_SUMMARY.TITLE');
    });
});

function createComponent(stats: FastingStats | null): ComponentFixture<FastingStatsCardComponent> {
    const fixture = TestBed.createComponent(FastingStatsCardComponent);
    fixture.componentRef.setInput('stats', stats);
    fixture.detectChanges();

    return fixture;
}

function getElement(fixture: ComponentFixture<FastingStatsCardComponent>): HTMLElement {
    return fixture.nativeElement as HTMLElement;
}

function createStats(overrides: Partial<FastingStats> = {}): FastingStats {
    return {
        totalCompleted: TOTAL_COMPLETED,
        currentStreak: CURRENT_STREAK,
        averageDurationHours: AVERAGE_DURATION_HOURS,
        completionRateLast30Days: COMPLETION_RATE,
        checkInRateLast30Days: CHECK_IN_RATE,
        lastCheckInAtUtc: '2026-05-01T12:00:00.000Z',
        topSymptom: 'headache',
        ...overrides,
    };
}
