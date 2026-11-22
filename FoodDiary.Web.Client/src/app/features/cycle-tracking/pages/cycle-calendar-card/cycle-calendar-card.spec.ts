import { type ComponentFixture, TestBed } from '@angular/core/testing';
import { afterEach, beforeEach, describe, expect, it, vi } from 'vitest';

import { provideTranslateTesting } from '../../../../../testing/translate-testing.module';
import { type CycleResponse, MENSTRUAL_EPISODE_STATUS_CONFIRMED } from '../../../../shared/models/cycle.data';
import { calendarDate } from '../../../../shared/models/semantics/date-value';
import { entityId } from '../../../../shared/models/semantics/entity-id';
import { CycleCalendarCardComponent } from './cycle-calendar-card';

const CYCLE: CycleResponse = {
    id: entityId<'cycle-profile'>('cycle-1'),
    userId: entityId<'user'>('user-1'),
    mode: 0,
    goal: 0,
    reproductiveState: 0,
    hideFromDashboard: false,
    confidence: 2,
    trackingStartDate: calendarDate('2025-03-01T00:00:00Z'),
    averageCycleLength: 28,
    averagePeriodLength: 5,
    lutealLength: 14,
    isRegular: true,
    isOnboardingComplete: true,
    showFertilityEstimates: true,
    discreetNotifications: false,
    bleedingEntries: [
        {
            id: entityId<'bleeding-entry'>('bleeding-1'),
            cycleProfileId: entityId<'cycle-profile'>('cycle-1'),
            date: calendarDate('2025-03-15T00:00:00Z'),
            type: 0,
            flow: 2,
        },
    ],
    symptoms: [],
    factors: [],
    fertilitySignals: [],
    menstrualEpisodes: [
        {
            id: entityId<'menstrual-episode'>('episode-1'),
            cycleProfileId: entityId<'cycle-profile'>('cycle-1'),
            startDate: calendarDate('2025-03-14T00:00:00Z'),
            endDate: calendarDate('2025-03-16T00:00:00Z'),
            status: 1,
            excludedFromPredictions: false,
        },
    ],
    predictions: {
        nextPeriodStartFrom: calendarDate('2025-04-11T00:00:00Z'),
        nextPeriodStartTo: calendarDate('2025-04-13T00:00:00Z'),
        confidence: 'medium',
        rationale: 'test',
    },
};

describe('CycleCalendarCardComponent', () => {
    let component: CycleCalendarCardComponent;
    let fixture: ComponentFixture<CycleCalendarCardComponent>;

    beforeEach(async () => {
        vi.useFakeTimers({ toFake: ['Date'] });
        vi.setSystemTime(new Date('2025-04-12T12:00:00'));
        await TestBed.configureTestingModule({
            imports: [CycleCalendarCardComponent],
            providers: [provideTranslateTesting()],
        }).compileComponents();

        fixture = TestBed.createComponent(CycleCalendarCardComponent);
        component = fixture.componentInstance;
        fixture.componentRef.setInput('cycle', CYCLE);
        fixture.componentRef.setInput('locale', 'en');
        fixture.detectChanges();
    });

    afterEach(() => vi.useRealTimers());

    it('maps logged, confirmed and predicted dates to calendar markers', () => {
        const markers = component['markers']();

        expect(markers).toContainEqual(expect.objectContaining({ date: '2025-03-15', tone: 'danger' }));
        expect(markers).toContainEqual(expect.objectContaining({ date: '2025-03-14', tone: 'brand' }));
        expect(markers).toContainEqual(expect.objectContaining({ date: '2025-03-16', tone: 'brand' }));
        expect(markers).toContainEqual(expect.objectContaining({ date: '2025-04-11', tone: 'warning' }));
        expect(markers).toContainEqual(expect.objectContaining({ date: '2025-04-13', tone: 'warning' }));
    });

    it('emits a local calendar date from the rendered calendar', () => {
        const selected = vi.fn<(date: string) => void>();
        component.dateSelected.subscribe(selected);
        const day = calendarDay(fixture, '2025-04-11');

        day.click();
        fixture.detectChanges();

        expect(day.getAttribute('aria-selected')).toBe('true');

        day.click();
        expect(selected.mock.calls).toEqual([['2025-04-11'], ['2025-04-11']]);
    });

    it('keeps logged days in the calendar when an older profile has no separate episode list', () => {
        fixture.componentRef.setInput('cycle', { ...CYCLE, menstrualEpisodes: undefined, predictions: undefined });
        fixture.detectChanges();
        const host = fixture.nativeElement as HTMLElement;
        const previousMonth = host.querySelector<HTMLButtonElement>('button[aria-label="CALENDAR.PREVIOUS_MONTH"]');
        expect(previousMonth).not.toBeNull();
        previousMonth?.click();
        fixture.detectChanges();

        const day = calendarDay(fixture, '2025-03-15');
        expect(day.getAttribute('aria-label')).toContain('CYCLE_TRACKING.CALENDAR_LOGGED_BLEEDING');
        expect(day.getAttribute('aria-label')).not.toContain('CYCLE_TRACKING.CALENDAR_CONFIRMED_PERIOD');
        expect(day.disabled).toBe(false);
    });

    it('marks only the start of an open episode and a one-date prediction, where future logging stays disabled', () => {
        const periodStart = '2025-04-02';
        const predictedStart = '2025-04-26';
        fixture.componentRef.setInput('cycle', {
            ...CYCLE,
            menstrualEpisodes: [
                {
                    id: entityId<'menstrual-episode'>('open-episode'),
                    cycleProfileId: CYCLE.id,
                    startDate: calendarDate(periodStart),
                    status: MENSTRUAL_EPISODE_STATUS_CONFIRMED,
                    excludedFromPredictions: false,
                },
            ],
            predictions: { nextPeriodStartFrom: calendarDate(predictedStart), confidence: 'medium', rationale: 'Observed cycles' },
        });
        fixture.detectChanges();
        const selected = vi.fn<(date: string) => void>();
        component.dateSelected.subscribe(selected);

        const confirmed = 'CYCLE_TRACKING.CALENDAR_CONFIRMED_PERIOD';
        const predicted = 'CYCLE_TRACKING.CALENDAR_PREDICTED_PERIOD';
        expect(calendarDay(fixture, periodStart).getAttribute('aria-label')).toContain(confirmed);
        expect(calendarDay(fixture, '2025-04-03').getAttribute('aria-label')).not.toContain(confirmed);
        expect(calendarDay(fixture, '2025-04-25').getAttribute('aria-label')).not.toContain(predicted);
        expect(calendarDay(fixture, '2025-04-27').getAttribute('aria-label')).not.toContain(predicted);
        const futureDay = calendarDay(fixture, predictedStart);
        expect(futureDay.getAttribute('aria-label')).toContain(predicted);
        expect(futureDay.disabled).toBe(true);
        futureDay.click();
        expect(selected).not.toHaveBeenCalled();
    });
});

function calendarDay(fixture: ComponentFixture<CycleCalendarCardComponent>, date: string): HTMLButtonElement {
    const host = fixture.nativeElement as HTMLElement;
    const day = host.querySelector<HTMLButtonElement>(`button[role="gridcell"][data-date="${date}"]`);
    if (day === null) {
        throw new Error(`Expected the calendar day ${date}`);
    }
    return day;
}
