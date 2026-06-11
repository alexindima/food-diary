import { describe, expect, it } from 'vitest';

import type { BleedingEntry, CycleResponse, CycleSymptomEntry, FertilitySignal } from '../../models/cycle.data';
import {
    BLEEDING_TYPE_BLEEDING,
    CYCLE_FACTOR_TYPE_HORMONAL_CONTRACEPTION,
    CYCLE_FLOW_MEDIUM,
    CYCLE_TRACKING_MODE_TRYING_TO_CONCEIVE,
    OVULATION_TEST_RESULT_POSITIVE,
} from '../../models/cycle.data';
import { DEFAULT_DAY_ACCENT_COLOR, PERIOD_DAY_ACCENT_COLOR } from './cycle-tracking-page.config';
import { buildCycleCurrentView, buildCycleDayItems, buildCyclePredictionView } from './cycle-tracking-page.mapper';

const CYCLE: CycleResponse = {
    id: 'cycle-1',
    userId: 'user-1',
    mode: CYCLE_TRACKING_MODE_TRYING_TO_CONCEIVE,
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
    factors: [
        {
            id: 'factor-1',
            cycleProfileId: 'cycle-1',
            type: CYCLE_FACTOR_TYPE_HORMONAL_CONTRACEPTION,
            startDate: '2026-04-02T00:00:00.000Z',
            endDate: null,
            notes: null,
        },
    ],
    fertilitySignals: [],
    predictions: null,
};

const BLEEDING_ENTRY: BleedingEntry = {
    id: 'bleeding-1',
    cycleProfileId: 'cycle-1',
    date: '2026-04-02T00:00:00.000Z',
    type: BLEEDING_TYPE_BLEEDING,
    flow: CYCLE_FLOW_MEDIUM,
    painImpact: 5,
    notes: 'note',
};

const SYMPTOM_ENTRY: CycleSymptomEntry = {
    id: 'symptom-1',
    cycleProfileId: 'cycle-1',
    date: '2026-04-03T00:00:00.000Z',
    category: 0,
    intensity: 4,
    tags: [],
    note: null,
};

const FERTILITY_SIGNAL: FertilitySignal = {
    id: 'signal-1',
    cycleProfileId: 'cycle-1',
    date: '2026-04-03T00:00:00.000Z',
    basalBodyTemperatureCelsius: 36.62,
    ovulationTestResult: OVULATION_TEST_RESULT_POSITIVE,
    cervicalFluid: 'egg white',
    hadSex: true,
    notes: 'signal note',
};

describe('cycle tracking page mapper', () => {
    it('builds current cycle view with formatted start date', () => {
        const view = buildCycleCurrentView(CYCLE, 'en-US');

        expect(view?.cycle).toBe(CYCLE);
        expect(view?.trackingStartDateLabel).toBe('Apr 1, 2026');
        expect(view?.summaryItems).toContainEqual(
            expect.objectContaining({
                labelKey: 'CYCLE_TRACKING.MODE',
                valueKey: 'CYCLE_TRACKING.MODE_TRYING_TO_CONCEIVE',
            }),
        );
        expect(view?.summaryItems).toContainEqual(
            expect.objectContaining({
                labelKey: 'CYCLE_TRACKING.REGULARITY',
                valueKey: 'CYCLE_TRACKING.REGULARITY_REGULAR',
            }),
        );
        expect(view?.activeFactorItems).toEqual([
            {
                labelKey: 'CYCLE_TRACKING.FACTOR_HORMONAL_CONTRACEPTION',
                startDateLabel: 'Apr 2',
            },
        ]);
    });

    it('returns null when there is no current cycle', () => {
        expect(buildCycleCurrentView(null, 'en-US')).toBeNull();
    });

    it('builds prediction labels using UTC dates', () => {
        const view = buildCyclePredictionView(
            {
                nextPeriodStartFrom: '2026-04-29T23:00:00.000Z',
                nextPeriodStartTo: '2026-05-01T00:00:00.000Z',
                ovulationFrom: '2026-04-15T00:00:00.000Z',
                ovulationTo: '2026-04-16T00:00:00.000Z',
                pmsWindowStart: null,
                pmsWindowEnd: null,
                confidence: 'Moderate',
                rationale: 'Based on recent bleeding entries.',
            },
            'en-US',
        );

        expect(view).toEqual({
            prediction: {
                nextPeriodStartFrom: '2026-04-29T23:00:00.000Z',
                nextPeriodStartTo: '2026-05-01T00:00:00.000Z',
                ovulationFrom: '2026-04-15T00:00:00.000Z',
                ovulationTo: '2026-04-16T00:00:00.000Z',
                pmsWindowStart: null,
                pmsWindowEnd: null,
                confidence: 'Moderate',
                rationale: 'Based on recent bleeding entries.',
            },
            nextPeriodRangeLabel: 'Apr 29 - May 1',
            ovulationRangeLabel: 'Apr 15 - Apr 16',
            pmsRangeLabel: '',
            confidenceLabel: 'Moderate',
        });
    });

    it('preserves invalid date values for diagnostics', () => {
        const view = buildCyclePredictionView(
            {
                nextPeriodStartFrom: 'not-a-date',
                nextPeriodStartTo: null,
                ovulationFrom: '',
                ovulationTo: null,
                pmsWindowStart: undefined,
                pmsWindowEnd: undefined,
                confidence: 'Low',
                rationale: '',
            },
            'en-US',
        );

        expect(view?.nextPeriodRangeLabel).toBe('not-a-date');
        expect(view?.ovulationRangeLabel).toBe('');
        expect(view?.pmsRangeLabel).toBe('');
    });
});

describe('cycle tracking day item mapper', () => {
    it('builds day item styling and badges', () => {
        const items = buildCycleDayItems([BLEEDING_ENTRY], [SYMPTOM_ENTRY], [FERTILITY_SIGNAL], 'en-US');

        const bleedingItem = items.find(item => item.dateLabel === 'Apr 2, 2026');
        const symptomItem = items.find(item => item.dateLabel === 'Apr 3, 2026');

        expect(bleedingItem).toMatchObject({
            dateLabel: 'Apr 2, 2026',
            accentColor: PERIOD_DAY_ACCENT_COLOR,
            badgeLabelKey: 'CYCLE_TRACKING.BADGE_PERIOD',
        });
        expect(symptomItem).toMatchObject({
            accentColor: DEFAULT_DAY_ACCENT_COLOR,
            badgeLabelKey: 'CYCLE_TRACKING.BADGE_TRACKED',
            notes: 'signal note',
        });
        expect(symptomItem?.fertilitySignalItems).toContainEqual({
            textKey: 'CYCLE_TRACKING.BBT_SUMMARY',
            params: { value: '36.62' },
        });
        expect(symptomItem?.fertilitySignalItems).toContainEqual({
            textKey: 'CYCLE_TRACKING.OVULATION_TEST_POSITIVE_SUMMARY',
        });
    });
});
