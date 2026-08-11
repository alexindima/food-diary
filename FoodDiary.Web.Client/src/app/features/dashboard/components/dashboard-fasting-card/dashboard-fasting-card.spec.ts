import { type ComponentFixture, TestBed } from '@angular/core/testing';
import { beforeEach, describe, expect, it, vi } from 'vitest';

import { provideTranslateTesting } from '../../../../../testing/translate-testing.module';
import { LocalizationService } from '../../../../shared/i18n/localization.service';
import { HOURS_PER_DAY } from '../../../../shared/lib/time.constants';
import type { FastingSession } from '../../../fasting/models/fasting.data';
import { DashboardFastingCardComponent } from './dashboard-fasting-card';

describe('DashboardFastingCardComponent', () => {
    let fixture: ComponentFixture<DashboardFastingCardComponent>;

    beforeEach(() => {
        vi.useFakeTimers();
        vi.setSystemTime(new Date('2026-04-12T12:00:00Z'));
        TestBed.configureTestingModule({
            imports: [DashboardFastingCardComponent],
            providers: [provideTranslateTesting(), { provide: LocalizationService, useValue: { getCurrentLanguage: (): string => 'en' } }],
        });
        fixture = TestBed.createComponent(DashboardFastingCardComponent);
        fixture.componentRef.setInput('session', createSession());
        fixture.detectChanges();
    });

    it('renders live progress with the shared accessible ring', () => {
        const host = fixture.nativeElement as HTMLElement;
        const ring = host.querySelector<HTMLElement>('.fd-ui-progress-ring');
        const value = host.querySelector<SVGCircleElement>('.fd-ui-progress-ring__value');

        expect(ring?.getAttribute('role')).toBe('progressbar');
        expect(value?.getAttribute('stroke-dasharray')).toBe('50 100');
        expect(host.textContent).toContain('12:00:00');
    });

    it('passes the current fasting stage color to the progress ring', () => {
        const ringHost = (fixture.nativeElement as HTMLElement).querySelector<HTMLElement>('fd-ui-progress-ring');

        expect(ringHost?.style.getPropertyValue('--fd-progress-ring-color')).not.toBe('');
    });
});

function createSession(): FastingSession {
    return {
        id: 'session-1',
        startedAtUtc: '2026-04-12T00:00:00Z',
        endedAtUtc: null,
        initialPlannedDurationHours: HOURS_PER_DAY,
        addedDurationHours: 0,
        plannedDurationHours: HOURS_PER_DAY,
        protocol: 'Fast24',
        planType: 'Extended',
        occurrenceKind: 'FastingWindow',
        cyclicFastDays: null,
        cyclicEatDays: null,
        cyclicEatDayFastHours: null,
        cyclicEatDayEatingWindowHours: null,
        cyclicPhaseDayNumber: null,
        cyclicPhaseDayTotal: null,
        isCompleted: false,
        status: 'Active',
        notes: null,
        checkInAtUtc: null,
        hungerLevel: null,
        energyLevel: null,
        moodLevel: null,
        symptoms: [],
        checkInNotes: null,
        checkIns: [],
    };
}
