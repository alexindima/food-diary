import { type ComponentFixture, TestBed } from '@angular/core/testing';
import { provideRouter } from '@angular/router';
import { describe, expect, it, vi } from 'vitest';

import { provideTranslateTesting } from '../../../../../testing/translate-testing.module';
import {
    CYCLE_FACTOR_TYPE_HORMONAL_CONTRACEPTION,
    CYCLE_TRACKING_MODE_NO_PERIOD,
    CYCLE_TRACKING_MODE_POSTPARTUM_LACTATION,
    CYCLE_TRACKING_MODE_PREGNANCY,
    CYCLE_TRACKING_MODE_TRYING_TO_CONCEIVE,
    type CyclePredictions,
    type CycleResponse,
    type MenstrualEpisode,
} from '../../../../shared/models/cycle.data';
import { calendarDate } from '../../../../shared/models/semantics/date-value';
import { entityId } from '../../../../shared/models/semantics/entity-id';
import { CycleSummaryCardComponent } from './cycle-summary-card';

const CYCLE_DAY = 5;
const DAYS_TO_PERIOD = 24;
const RECORDED_CYCLE_DAY = 3;
const EPISODE: MenstrualEpisode = {
    id: entityId<'menstrual-episode'>('episode-1'),
    cycleProfileId: entityId<'cycle-profile'>('cycle-1'),
    startDate: calendarDate('2026-05-01T00:00:00.000Z'),
    endDate: null,
    status: 1,
    excludedFromPredictions: false,
};
const CYCLE: CycleResponse = {
    id: entityId<'cycle-profile'>('cycle-1'),
    userId: entityId<'user'>('user-1'),
    mode: CYCLE_TRACKING_MODE_TRYING_TO_CONCEIVE,
    goal: 0,
    reproductiveState: 0,
    hideFromDashboard: false,
    confidence: 1,
    trackingStartDate: calendarDate('2026-05-01T00:00:00.000Z'),
    averageCycleLength: 28,
    averagePeriodLength: 5,
    lutealLength: 14,
    isRegular: true,
    isOnboardingComplete: true,
    showFertilityEstimates: true,
    discreetNotifications: true,
    bleedingEntries: [],
    symptoms: [],
    factors: [
        {
            id: entityId<'cycle-factor'>('factor-1'),
            cycleProfileId: entityId<'cycle-profile'>('cycle-1'),
            type: CYCLE_FACTOR_TYPE_HORMONAL_CONTRACEPTION,
            startDate: calendarDate('2026-05-02T00:00:00.000Z'),
            endDate: null,
            notes: null,
        },
    ],
    fertilitySignals: [],
    menstrualEpisodes: [EPISODE],
    predictions: null,
};

describe('CycleSummaryCard navigation', () => {
    it('exposes the configured card as a keyboard-accessible link to the cycle journal', async () => {
        const { fixture } = await setupComponentAsync();
        fixture.detectChanges();

        const link = (fixture.nativeElement as HTMLElement).querySelector<HTMLAnchorElement>('a[href="/cycle-tracking"]');
        expect(link, 'Configured cycle card must link to its journal').not.toBeNull();
        expect(link?.getAttribute('aria-label')).toBe('CYCLE_CARD.TITLE');
        expect(link?.tabIndex).toBe(0);
    });

    it.each([{ cycle: null, startDate: null }, { isLoading: true }])(
        'does not cover setup or loading with a journal link: %j',
        async overrides => {
            const { fixture } = await setupComponentAsync(overrides);
            fixture.detectChanges();
            expect((fixture.nativeElement as HTMLElement).querySelector('a[href="/cycle-tracking"]')).toBeNull();
        },
    );
});

describe('CycleSummaryCardComponent', () => {
    it('calculates cycle day and next period status from inputs', async () => {
        const { component, fixture } = await setupComponentAsync({
            startDate: '2026-05-01',
            referenceDate: '2026-05-05',
            predictions: {
                ovulationFrom: null,
                ovulationTo: null,
                nextPeriodStartFrom: calendarDate('2026-05-29'),
                nextPeriodStartTo: calendarDate('2026-05-31'),
                pmsWindowStart: null,
                pmsWindowEnd: null,
                confidence: 'Moderate',
                rationale: 'Based on recent bleeding entries.',
            },
        });

        fixture.detectChanges();

        expect(component['hasCycle']()).toBe(true);
        expect(component['cycleDay']()).toBe(CYCLE_DAY);
        expect(component['statusKey']()).toBe('CYCLE_CARD.NEXT_PERIOD_IN');
        expect(component['statusDays']()).toBe(DAYS_TO_PERIOD);
        expect(component['modeKey']()).toBe('CYCLE_TRACKING.MODE_TRYING_TO_CONCEIVE');
        expect(component['confidence']()).toBe('Moderate');
        expect(component['metaPills']()).toContainEqual({ id: 'confidence', labelKey: 'CYCLE_CARD.CONFIDENCE_MODERATE' });
        expect(component['activeFactorPills']()).toContainEqual({
            id: 'factor-factor-1',
            labelKey: 'CYCLE_TRACKING.FACTOR_HORMONAL_CONTRACEPTION',
        });
        expect(getText(fixture)).toContain('CYCLE_TRACKING.MODE_TRYING_TO_CONCEIVE');
        expect(getText(fixture)).toContain('CYCLE_CARD.CONFIDENCE');
        expect(getText(fixture)).toContain('CYCLE_TRACKING.FACTOR_HORMONAL_CONTRACEPTION');
    });

    it('emits setup action when cycle data is missing', async () => {
        const { component } = await setupComponentAsync({ cycle: null, startDate: null, predictions: null });
        const setupSpy = vi.fn();
        component['setupAction'].subscribe(setupSpy);

        expect(component['hasCycle']()).toBe(false);
        component['onSetup']();

        expect(setupSpy).toHaveBeenCalledOnce();
    });

    it('counts from the recorded period instead of the observation start at local midnight', async () => {
        const { component, fixture } = await setupComponentAsync({
            cycle: {
                ...CYCLE,
                trackingStartDate: calendarDate('2026-08-01'),
                menstrualEpisodes: [{ ...EPISODE, startDate: calendarDate('2026-10-16T00:00:00Z') }],
            },
            startDate: '2026-08-01',
            referenceDate: new Date('2026-10-18T00:00:00'),
        });

        fixture.detectChanges();

        expect(component['cycleDay']()).toBe(RECORDED_CYCLE_DAY);
        expect(component['hasCycle']()).toBe(true);
        expect((fixture.nativeElement as HTMLElement).querySelector<HTMLElement>('.cycle-summary-card__day')?.hidden).toBe(false);
    });

    it('keeps a configured journal without episodes without inventing a numeric cycle day', async () => {
        const { component, fixture } = await setupComponentAsync({ cycle: { ...CYCLE, menstrualEpisodes: [] } });

        fixture.detectChanges();

        expect(component['cycleDay']()).toBeNull();
        expect(component['hasCycle']()).toBe(true);
        expect((fixture.nativeElement as HTMLElement).querySelector<HTMLElement>('.cycle-summary-card__day')?.hidden).toBe(true);
        expect(getText(fixture)).toContain('CYCLE_TRACKING.MODE_TRYING_TO_CONCEIVE');
        expect(getText(fixture)).not.toContain('CYCLE_CARD.NO_DATA_TITLE');
    });

    it.each([CYCLE_TRACKING_MODE_PREGNANCY, CYCLE_TRACKING_MODE_POSTPARTUM_LACTATION, CYCLE_TRACKING_MODE_NO_PERIOD] as const)(
        'keeps metadata without a numeric menstrual day for mode %s',
        async mode => {
            const { component, fixture } = await setupComponentAsync({ cycle: { ...CYCLE, mode } });

            fixture.detectChanges();

            expect(component['cycleDay']()).toBeNull();
            expect(component['hasCycle']()).toBe(true);
            expect((fixture.nativeElement as HTMLElement).querySelector<HTMLElement>('.cycle-summary-card__day')?.hidden).toBe(true);
        },
    );
});

async function setupComponentAsync(
    overrides: Partial<{
        cycle: CycleResponse | null;
        startDate: string | null;
        predictions: CyclePredictions | null;
        referenceDate: Date | string | null;
        isLoading: boolean;
    }> = {},
): Promise<{
    component: CycleSummaryCardComponent;
    fixture: ComponentFixture<CycleSummaryCardComponent>;
}> {
    await TestBed.resetTestingModule()
        .configureTestingModule({
            imports: [CycleSummaryCardComponent],
            providers: [provideTranslateTesting(), provideRouter([])],
        })
        .compileComponents();

    const fixture = TestBed.createComponent(CycleSummaryCardComponent);
    fixture.componentRef.setInput('cycle', 'cycle' in overrides ? overrides.cycle : CYCLE);
    fixture.componentRef.setInput('startDate', 'startDate' in overrides ? overrides.startDate : '2026-05-01');
    fixture.componentRef.setInput('predictions', 'predictions' in overrides ? overrides.predictions : null);
    fixture.componentRef.setInput('referenceDate', 'referenceDate' in overrides ? overrides.referenceDate : '2026-05-05');
    fixture.componentRef.setInput('isLoading', overrides.isLoading ?? false);

    return {
        component: fixture.componentInstance,
        fixture,
    };
}

function getText(fixture: ComponentFixture<CycleSummaryCardComponent>): string {
    return (fixture.nativeElement as HTMLElement).textContent;
}
