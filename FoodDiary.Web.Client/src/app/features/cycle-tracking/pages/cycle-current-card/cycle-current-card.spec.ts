import { type ComponentFixture, TestBed } from '@angular/core/testing';
import { TranslateModule } from '@ngx-translate/core';
import { beforeEach, describe, expect, it } from 'vitest';

import type { CycleResponse } from '../../models/cycle.data';
import { CycleCurrentCardComponent } from './cycle-current-card';

const CYCLE: CycleResponse = {
    id: 'cycle-1',
    userId: 'user-1',
    mode: 0,
    confidence: 1,
    trackingStartDate: '2026-04-01T00:00:00.000Z',
    averageCycleLength: 28,
    averagePeriodLength: 5,
    lutealLength: 14,
    isRegular: true,
    isOnboardingComplete: true,
    showFertilityEstimates: true,
    discreetNotifications: true,
    bleedingEntries: [],
    symptoms: [],
    factors: [],
    fertilitySignals: [],
    predictions: null,
};

let fixture: ComponentFixture<CycleCurrentCardComponent>;

beforeEach(() => {
    TestBed.configureTestingModule({
        imports: [CycleCurrentCardComponent, TranslateModule.forRoot()],
    });

    fixture = TestBed.createComponent(CycleCurrentCardComponent);
});

describe('CycleCurrentCardComponent', () => {
    it('shows loading state', () => {
        fixture.componentRef.setInput('isLoading', true);
        fixture.detectChanges();

        expect(getText()).toContain('CYCLE_TRACKING.LOADING');
    });

    it('derives empty title from missing current cycle', () => {
        fixture.componentRef.setInput('isLoading', false);
        fixture.componentRef.setInput('current', null);
        fixture.detectChanges();

        expect(fixture.componentInstance['titleKey']()).toBe('CYCLE_TRACKING.NO_CYCLE');
        expect(getText()).toContain('CYCLE_TRACKING.NO_CYCLE_COPY');
    });

    it('renders current cycle and prediction blocks', () => {
        fixture.componentRef.setInput('isLoading', false);
        fixture.componentRef.setInput('current', {
            cycle: CYCLE,
            trackingStartDateLabel: 'Apr 1, 2026',
            summaryItems: [
                {
                    labelKey: 'CYCLE_TRACKING.STARTED',
                    valueKey: 'CYCLE_TRACKING.STARTED_SUMMARY',
                    params: { value: 'Apr 1, 2026' },
                    accentColor: 'var(--fd-color-purple-500)',
                },
                {
                    labelKey: 'CYCLE_TRACKING.MODE',
                    valueKey: 'CYCLE_TRACKING.MODE_PERIOD_TRACKING',
                    accentColor: 'var(--fd-color-sky-500)',
                },
            ],
            activeFactorItems: [
                {
                    labelKey: 'CYCLE_TRACKING.FACTOR_HORMONAL_CONTRACEPTION',
                    startDateLabel: 'Apr 2',
                },
            ],
        });
        fixture.componentRef.setInput('prediction', {
            prediction: {
                nextPeriodStartFrom: '2026-04-29',
                nextPeriodStartTo: '2026-05-01',
                ovulationFrom: '2026-04-15',
                ovulationTo: '2026-04-16',
                pmsWindowStart: '2026-04-23',
                pmsWindowEnd: '2026-04-28',
                confidence: 'Moderate',
                rationale: 'Based on recent bleeding entries.',
            },
            nextPeriodRangeLabel: 'Apr 29 - May 1',
            ovulationRangeLabel: 'Apr 15 - Apr 16',
            pmsRangeLabel: 'Apr 23 - Apr 28',
            confidenceLabel: 'Moderate',
        });
        fixture.detectChanges();

        expect(fixture.componentInstance['titleKey']()).toBe('CYCLE_TRACKING.CURRENT_CYCLE');
        expect(getText()).toContain('CYCLE_TRACKING.STARTED_SUMMARY');
        expect(getText()).toContain('CYCLE_TRACKING.MODE_PERIOD_TRACKING');
        expect(getText()).toContain('CYCLE_TRACKING.FACTOR_HORMONAL_CONTRACEPTION');
        expect(getText()).toContain('Apr 29 - May 1');
        expect(getText()).toContain('Apr 15 - Apr 16');
        expect(getText()).toContain('Apr 23 - Apr 28');
    });
});

function getText(): string {
    return (fixture.nativeElement as HTMLElement).textContent;
}
