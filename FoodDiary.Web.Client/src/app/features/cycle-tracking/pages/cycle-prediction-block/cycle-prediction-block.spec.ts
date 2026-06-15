import { type ComponentFixture, TestBed } from '@angular/core/testing';
import { beforeEach, describe, expect, it } from 'vitest';

import { provideTranslateTesting } from '../../../../../testing/translate-testing.module';
import { CyclePredictionBlockComponent } from './cycle-prediction-block';

let fixture: ComponentFixture<CyclePredictionBlockComponent>;

beforeEach(() => {
    TestBed.configureTestingModule({
        imports: [CyclePredictionBlockComponent],
        providers: [provideTranslateTesting()],
    });

    fixture = TestBed.createComponent(CyclePredictionBlockComponent);
});

describe('CyclePredictionBlockComponent', () => {
    it('renders prediction ranges when available', () => {
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
            hasPredictionRanges: true,
            limitedReasonKey: null,
        });
        fixture.detectChanges();

        expect(getText()).toContain('CYCLE_TRACKING.PRED_NEXT');
        expect(getText()).toContain('Apr 29 - May 1');
        expect(getText()).toContain('Apr 15 - Apr 16');
        expect(getText()).toContain('Apr 23 - Apr 28');
    });

    it('renders limited state when ranges are unavailable', () => {
        fixture.componentRef.setInput('prediction', {
            prediction: {
                nextPeriodStartFrom: null,
                nextPeriodStartTo: null,
                ovulationFrom: null,
                ovulationTo: null,
                pmsWindowStart: null,
                pmsWindowEnd: null,
                confidence: 'Low',
                rationale: 'Predictions are limited by the active tracking mode.',
            },
            nextPeriodRangeLabel: '',
            ovulationRangeLabel: '',
            pmsRangeLabel: '',
            confidenceLabel: 'Low',
            hasPredictionRanges: false,
            limitedReasonKey: 'CYCLE_TRACKING.PREDICTIONS_LIMITED',
        });
        fixture.detectChanges();

        expect(getText()).toContain('CYCLE_TRACKING.PREDICTIONS_LIMITED');
        expect(getText()).toContain('CYCLE_TRACKING.PREDICTIONS_LIMITED_BODY');
        expect(getText()).not.toContain('CYCLE_TRACKING.PRED_NEXT');
    });
});

function getText(): string {
    return (fixture.nativeElement as HTMLElement).textContent;
}
