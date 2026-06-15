import { type ComponentFixture, TestBed } from '@angular/core/testing';
import { describe, expect, it, vi } from 'vitest';

import { provideTranslateTesting } from '../../../../../testing/translate-testing.module';
import {
    CYCLE_FACTOR_TYPE_HORMONAL_CONTRACEPTION,
    CYCLE_TRACKING_MODE_TRYING_TO_CONCEIVE,
    type CyclePredictions,
    type CycleResponse,
} from '../../../cycle-tracking/models/cycle.data';
import { CycleSummaryCardComponent } from './cycle-summary-card';

const CYCLE_DAY = 5;
const DAYS_TO_PERIOD = 24;
const CYCLE: CycleResponse = {
    id: 'cycle-1',
    userId: 'user-1',
    mode: CYCLE_TRACKING_MODE_TRYING_TO_CONCEIVE,
    confidence: 1,
    trackingStartDate: '2026-05-01T00:00:00.000Z',
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
            id: 'factor-1',
            cycleProfileId: 'cycle-1',
            type: CYCLE_FACTOR_TYPE_HORMONAL_CONTRACEPTION,
            startDate: '2026-05-02T00:00:00.000Z',
            endDate: null,
            notes: null,
        },
    ],
    fertilitySignals: [],
    predictions: null,
};

describe('CycleSummaryCardComponent', () => {
    it('calculates cycle day and next period status from inputs', async () => {
        const { component, fixture } = await setupComponentAsync({
            startDate: '2026-05-01',
            referenceDate: '2026-05-05',
            predictions: {
                ovulationFrom: null,
                ovulationTo: null,
                nextPeriodStartFrom: '2026-05-29',
                nextPeriodStartTo: '2026-05-31',
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
            providers: [provideTranslateTesting()],
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
