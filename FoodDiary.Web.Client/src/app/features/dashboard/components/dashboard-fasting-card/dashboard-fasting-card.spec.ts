import { type ComponentFixture, TestBed } from '@angular/core/testing';
import { afterEach, beforeEach, describe, expect, it, vi } from 'vitest';

import { provideTranslateTesting } from '../../../../../testing/translate-testing.module';
import { LocalizationService } from '../../../../shared/i18n/localization.service';
import { HOURS_PER_DAY, MS_PER_HOUR, MS_PER_SECOND } from '../../../../shared/lib/time.constants';
import type { FastingSession } from '../../../fasting/models/fasting.data';
import { DashboardFastingCardComponent } from './dashboard-fasting-card';
import { buildDashboardFastingCycle, buildDashboardFastingTimeline } from './dashboard-fasting-timeline';

const CYCLE_LENGTH = 3;
const TWELVE_HOURS = 12;
const EIGHTEEN_HOURS = 18;
const THIRTY_HOURS = 30;
const TWO_DAYS = 48;
const QUARTER = 25;
const HALF = 50;
const THREE_QUARTERS = 75;
const FULL = 100;

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
    afterEach(() => {
        fixture.destroy();
        vi.useRealTimers();
    });
    it('renders live accessible progress and advances the elapsed timer', () => {
        const host = fixture.nativeElement as HTMLElement;
        expect(host.querySelector('[role="progressbar"]')?.getAttribute('aria-valuenow')).toBe('50');
        expect(host.textContent).toContain('12:00:00');
        vi.advanceTimersByTime(MS_PER_SECOND);
        fixture.detectChanges();
        expect(host.textContent).toContain('12:00:01');
    });
    it('freezes completed sessions at their recorded end', () => {
        fixture.componentRef.setInput('session', { ...createSession(), endedAtUtc: '2026-04-12T06:00:00Z' });
        fixture.detectChanges();
        vi.advanceTimersByTime(MS_PER_SECOND);
        fixture.detectChanges();
        expect((fixture.nativeElement as HTMLElement).textContent).toContain('06:00:00');
    });
    it('does not describe fasting stages during a cyclic eating window', () => {
        fixture.componentRef.setInput('session', {
            ...createSession(),
            planType: 'Cyclic',
            occurrenceKind: 'EatDay',
            cyclicFastDays: 1,
            cyclicEatDays: 2,
            cyclicPhaseDayNumber: 1,
        });
        fixture.detectChanges();
        const host = fixture.nativeElement as HTMLElement;
        expect(host.querySelector('.dashboard-fasting-card__stage')?.textContent).toContain('FASTING.EATING_WINDOW');
        expect(host.querySelectorAll('.dashboard-fasting-card__days li')).toHaveLength(CYCLE_LENGTH);
        expect(host.querySelectorAll('[aria-current="step"]')).toHaveLength(1);
    });
    it('preserves the fasting segment through eating and resets only at the next cycle', () => {
        verifyPhaseTransitions(fixture);
    });
    it('handles a missing session without invalid dates or progress', () => {
        fixture.componentRef.setInput('session', null);
        fixture.detectChanges();
        expect((fixture.nativeElement as HTMLElement).textContent).not.toMatch(/NaN|Invalid Date/u);
    });
});

describe('dashboard fasting timeline', () => {
    it('keeps the 24-hour position distinct from current-phase progress', () => {
        const session = {
            ...createSession(),
            planType: 'Intermittent' as const,
            initialPlannedDurationHours: 16,
            plannedDurationHours: 16,
        };
        const fasting = buildDashboardFastingTimeline(session, TWELVE_HOURS * MS_PER_HOUR);
        expect(fasting.position).toBe(HALF);
        expect(fasting.eating).toBe(false);
        const eating = buildDashboardFastingTimeline(session, EIGHTEEN_HOURS * MS_PER_HOUR);
        expect(eating.position).toBe(THREE_QUARTERS);
        expect(eating.eating).toBe(true);
        const nextDay = buildDashboardFastingTimeline(session, THIRTY_HOURS * MS_PER_HOUR);
        expect(nextDay.position).toBe(QUARTER);
        expect(nextDay.start?.toISOString()).toBe('2026-04-13T00:00:00.000Z');
    });
    it('caps overtime at the end instead of wrapping an extended fast', () => {
        expect(buildDashboardFastingTimeline(createSession(), TWO_DAYS * MS_PER_HOUR).position).toBe(FULL);
    });
    it('follows server fast: eat day ordering for both phases', () => {
        const session = { ...createSession(), planType: 'Cyclic' as const, cyclicFastDays: 2, cyclicEatDays: 1, cyclicPhaseDayNumber: 2 };
        expect(buildDashboardFastingCycle(session).find(day => day.current)?.day).toBe(2);
        expect(
            buildDashboardFastingCycle({ ...session, occurrenceKind: 'EatDay', cyclicPhaseDayNumber: 1 }).find(day => day.current)?.day,
        ).toBe(CYCLE_LENGTH);
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

function verifyPhaseTransitions(fixture: ComponentFixture<DashboardFastingCardComponent>): void {
    fixture.componentRef.setInput('session', {
        ...createSession(),
        planType: 'Intermittent',
        initialPlannedDurationHours: 16,
        plannedDurationHours: 16,
    });
    fixture.detectChanges();
    const host = fixture.nativeElement as HTMLElement;
    const fast = (): number => Number.parseFloat(host.querySelector<HTMLElement>('.dashboard-fasting-card__fill')?.style.width ?? '0');
    const eat = (): number =>
        Number.parseFloat(host.querySelector<HTMLElement>('.dashboard-fasting-card__eating-fill')?.style.width ?? '0');
    const moveTo = (iso: string): void => {
        vi.setSystemTime(new Date(new Date(iso).getTime() - MS_PER_SECOND));
        vi.advanceTimersByTime(MS_PER_SECOND);
        fixture.detectChanges();
    };
    moveTo('2026-04-12T15:59:59Z');
    const before = fast();
    expect(eat()).toBe(0);
    moveTo('2026-04-12T16:00:00Z');
    const boundary = fast();
    expect(boundary).toBeGreaterThan(before);
    expect(eat()).toBe(0);
    expect(host.querySelector('.dashboard-fasting-card__stage')?.textContent).toContain('FASTING.EATING_WINDOW');
    moveTo('2026-04-12T18:00:00Z');
    expect(fast()).toBe(boundary);
    expect(eat()).toBeGreaterThan(0);
    expect(host.querySelector('.dashboard-fasting-card__elapsed')?.textContent).toBe('02:00:00');
    moveTo('2026-04-13T00:00:00Z');
    expect(fast()).toBe(0);
    expect(eat()).toBe(0);
    expect(host.querySelector('.dashboard-fasting-card__elapsed')?.textContent).toBe('00:00:00');
    expect(host.querySelector('.dashboard-fasting-card__stage')?.textContent).not.toContain('FASTING.EATING_WINDOW');
}
