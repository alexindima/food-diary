import { describe, expect, it } from 'vitest';

import type { BleedingEntry, CycleNutritionSummary, CycleResponse, CycleSymptomEntry, FertilitySignal } from '../../models/cycle.data';
import {
    BLEEDING_TYPE_BLEEDING,
    CYCLE_FACTOR_TYPE_HORMONAL_CONTRACEPTION,
    CYCLE_FLOW_HEAVY,
    CYCLE_FLOW_MEDIUM,
    CYCLE_TRACKING_MODE_TRYING_TO_CONCEIVE,
    OVULATION_TEST_RESULT_POSITIVE,
} from '../../models/cycle.data';
import { DEFAULT_DAY_ACCENT_COLOR, PERIOD_DAY_ACCENT_COLOR } from './cycle-tracking-page.config';
import {
    buildCycleCurrentView,
    buildCycleDayItems,
    buildCycleFactorItems,
    buildCycleNutritionSummaryView,
    buildCyclePredictionView,
} from './cycle-tracking-page.mapper';

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
        {
            id: 'factor-2',
            cycleProfileId: 'cycle-1',
            type: CYCLE_FACTOR_TYPE_HORMONAL_CONTRACEPTION,
            startDate: '2026-03-02T00:00:00.000Z',
            endDate: '2026-03-10T00:00:00.000Z',
            notes: null,
        },
    ],
    fertilitySignals: [],
    predictions: null,
};
const PROLONGED_BLEEDING_DAYS = 8;
const CARE_PROMPT_YEAR = 2026;
const APRIL_MONTH_INDEX = 3;
const LAST_PROLONGED_BLEEDING_INDEX = PROLONGED_BLEEDING_DAYS - 1;
const SEVERE_PAIN_VALUE = 8;
const NUTRITION_LOGGED_CYCLE_DAYS = 4;
const NUTRITION_SUMMARY: CycleNutritionSummary = {
    dateFrom: '2026-04-01T00:00:00.000Z',
    dateTo: '2026-04-30T23:59:59.999Z',
    loggedCycleDays: NUTRITION_LOGGED_CYCLE_DAYS,
    daysWithMeals: 3,
    bleedingDays: 2,
    averageCaloriesOnBleedingDays: 2100.25,
    averageCaloriesOnNonBleedingCycleDays: 1800,
    averageFiberOnBleedingDays: 18.5,
    averageFiberOnNonBleedingCycleDays: 28,
    averagePainImpactOnDaysWithMeals: 6.25,
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
                id: 'factor-1',
                labelKey: 'CYCLE_TRACKING.FACTOR_HORMONAL_CONTRACEPTION',
                startDateLabel: 'Apr 2',
            },
        ]);
    });

    it('returns null when there is no current cycle', () => {
        expect(buildCycleCurrentView(null, 'en-US')).toBeNull();
    });
});

describe('cycle tracking prediction mapper', () => {
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
            hasPredictionRanges: true,
            limitedReasonKey: null,
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

    it('marks prediction as limited when backend returns no date ranges', () => {
        const view = buildCyclePredictionView(
            {
                nextPeriodStartFrom: null,
                nextPeriodStartTo: null,
                ovulationFrom: null,
                ovulationTo: null,
                pmsWindowStart: null,
                pmsWindowEnd: null,
                confidence: 'Low',
                rationale: 'Predictions are limited by the active tracking mode.',
            },
            'en-US',
        );

        expect(view?.hasPredictionRanges).toBe(false);
        expect(view?.limitedReasonKey).toBe('CYCLE_TRACKING.PREDICTIONS_LIMITED');
    });
});

describe('cycle tracking nutrition summary mapper', () => {
    it('formats nutrition summary values', () => {
        const view = buildCycleNutritionSummaryView(NUTRITION_SUMMARY, 'en-US');

        expect(view?.summary.loggedCycleDays).toBe(NUTRITION_LOGGED_CYCLE_DAYS);
        expect(view?.bleedingCaloriesLabel).toBe('2,100.3');
        expect(view?.nonBleedingCaloriesLabel).toBe('1,800');
        expect(view?.bleedingFiberLabel).toBe('18.5');
        expect(view?.nonBleedingFiberLabel).toBe('28');
        expect(view?.painImpactLabel).toBe('6.3');
    });

    it('returns null without nutrition summary', () => {
        expect(buildCycleNutritionSummaryView(null, 'en-US')).toBeNull();
    });
});

describe('cycle tracking factor mapper', () => {
    it('builds factor list items with date ranges and status labels', () => {
        const view = buildCycleFactorItems(CYCLE.factors, 'en-US');

        expect(view).toEqual([
            {
                id: 'factor-1',
                labelKey: 'CYCLE_TRACKING.FACTOR_HORMONAL_CONTRACEPTION',
                dateRangeLabel: 'Apr 2',
                statusLabelKey: 'CYCLE_TRACKING.FACTOR_ACTIVE',
                isActive: true,
            },
            {
                id: 'factor-2',
                labelKey: 'CYCLE_TRACKING.FACTOR_HORMONAL_CONTRACEPTION',
                dateRangeLabel: 'Mar 2 - Mar 10',
                statusLabelKey: 'CYCLE_TRACKING.FACTOR_ENDED',
                isActive: false,
            },
        ]);
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

    it('builds care prompts for severe pain, heavy flow, and prolonged bleeding', () => {
        const bleedingEntries = Array.from({ length: PROLONGED_BLEEDING_DAYS }, (_, index): BleedingEntry => {
            const date = new Date(Date.UTC(CARE_PROMPT_YEAR, APRIL_MONTH_INDEX, index + 1)).toISOString();
            return {
                id: `bleeding-${index + 1}`,
                cycleProfileId: 'cycle-1',
                date,
                type: BLEEDING_TYPE_BLEEDING,
                flow: index === LAST_PROLONGED_BLEEDING_INDEX ? CYCLE_FLOW_HEAVY : CYCLE_FLOW_MEDIUM,
                painImpact: index === LAST_PROLONGED_BLEEDING_INDEX ? SEVERE_PAIN_VALUE : null,
                notes: null,
            };
        });

        const items = buildCycleDayItems(bleedingEntries, [], [], 'en-US');
        const latestItem = items.find(item => item.dateLabel === 'Apr 8, 2026');

        expect(latestItem?.carePromptItems).toEqual([
            { id: 'severe-pain', textKey: 'CYCLE_TRACKING.CARE_SEVERE_PAIN' },
            { id: 'heavy-flow', textKey: 'CYCLE_TRACKING.CARE_HEAVY_FLOW' },
            { id: 'prolonged-bleeding', textKey: 'CYCLE_TRACKING.CARE_PROLONGED_BLEEDING' },
        ]);
    });
});
