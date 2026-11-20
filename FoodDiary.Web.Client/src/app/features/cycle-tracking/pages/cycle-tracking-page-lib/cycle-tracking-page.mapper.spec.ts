import { describe, expect, it } from 'vitest';

import { formatDateInputValue } from '../../../../shared/lib/local-date.utils';
import type {
    BleedingEntry,
    CycleNutritionSummary,
    CycleResponse,
    CycleSymptomEntry,
    FertilitySignal,
} from '../../../../shared/models/cycle.data';
import {
    BLEEDING_TYPE_BLEEDING,
    BLEEDING_TYPE_SPOTTING,
    CYCLE_FACTOR_TYPE_HORMONAL_CONTRACEPTION,
    CYCLE_FACTOR_TYPE_LACTATION,
    CYCLE_FACTOR_TYPE_NO_PERIOD,
    CYCLE_FACTOR_TYPE_NON_HORMONAL_CONTRACEPTION,
    CYCLE_FACTOR_TYPE_PERIMENOPAUSE,
    CYCLE_FACTOR_TYPE_POSTPARTUM,
    CYCLE_FACTOR_TYPE_PREGNANCY,
    CYCLE_FLOW_HEAVY,
    CYCLE_FLOW_LIGHT,
    CYCLE_FLOW_MEDIUM,
    CYCLE_FLOW_NONE,
    CYCLE_SYMPTOM_CATEGORY_MOOD,
    CYCLE_SYMPTOM_CATEGORY_OTHER,
    CYCLE_TRACKING_MODE_NO_PERIOD,
    CYCLE_TRACKING_MODE_PERIMENOPAUSE,
    CYCLE_TRACKING_MODE_PERIOD_TRACKING,
    CYCLE_TRACKING_MODE_POSTPARTUM_LACTATION,
    CYCLE_TRACKING_MODE_PREGNANCY,
    CYCLE_TRACKING_MODE_TRYING_TO_CONCEIVE,
    OVULATION_TEST_RESULT_NEGATIVE,
    OVULATION_TEST_RESULT_POSITIVE,
} from '../../../../shared/models/cycle.data';
import { calendarDate } from '../../../../shared/models/semantics/date-value';
import { entityId } from '../../../../shared/models/semantics/entity-id';
import { DEFAULT_DAY_ACCENT_COLOR, PERIOD_DAY_ACCENT_COLOR } from './cycle-tracking-page.config';
import {
    buildCycleCurrentView,
    buildCycleDayItems,
    buildCycleFactorItems,
    buildCycleNutritionSummaryView,
    buildCycleObservationsView,
    buildCycleOverviewView,
    buildCyclePredictionView,
} from './cycle-tracking-page.mapper';

const CYCLE: CycleResponse = {
    id: entityId<'cycle-profile'>('cycle-1'),
    userId: entityId<'user'>('user-1'),
    mode: CYCLE_TRACKING_MODE_TRYING_TO_CONCEIVE,
    goal: 0,
    reproductiveState: 0,
    hideFromDashboard: false,
    confidence: 1,
    trackingStartDate: calendarDate('2026-04-01T00:00:00.000Z'),
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
            startDate: calendarDate('2026-04-02T00:00:00.000Z'),
            endDate: null,
            notes: null,
        },
        {
            id: entityId<'cycle-factor'>('factor-2'),
            cycleProfileId: entityId<'cycle-profile'>('cycle-1'),
            type: CYCLE_FACTOR_TYPE_HORMONAL_CONTRACEPTION,
            startDate: calendarDate('2026-03-02T00:00:00.000Z'),
            endDate: calendarDate('2026-03-10T00:00:00.000Z'),
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
const PAIN_INTENSITY = 2;
const HIGHER_MOOD_INTENSITY = 3;
const NUTRITION_LOGGED_CYCLE_DAYS = 4;
const OVERVIEW_YEAR = 2026;
const OVERVIEW_MONTH_INDEX = 3;
const OVERVIEW_DAY = 16;
const EPISODE_START_DAY = 10;
const EXPECTED_CYCLE_DAY = 7;
const OVERVIEW_DAY_COUNT = 11;
const NUTRITION_SUMMARY: CycleNutritionSummary = {
    dateFrom: calendarDate('2026-04-01T00:00:00.000Z'),
    dateTo: calendarDate('2026-04-30T23:59:59.999Z'),
    loggedCycleDays: NUTRITION_LOGGED_CYCLE_DAYS,
    daysWithMeals: 3,
    bleedingDays: 2,
    averageCaloriesOnBleedingDays: 2100.25,
    averageCaloriesOnNonBleedingCycleDays: 1800,
    averageFiberOnBleedingDays: 18.5,
    averageFiberOnNonBleedingCycleDays: 28,
    averagePainImpactOnDaysWithMeals: 6.25,
    hasEnoughNutritionData: true,
};

const BLEEDING_ENTRY: BleedingEntry = {
    id: entityId<'bleeding-entry'>('bleeding-1'),
    cycleProfileId: entityId<'cycle-profile'>('cycle-1'),
    date: calendarDate('2026-04-02T00:00:00.000Z'),
    type: BLEEDING_TYPE_BLEEDING,
    flow: CYCLE_FLOW_MEDIUM,
    painImpact: 5,
    notes: 'note',
};

const SYMPTOM_ENTRY: CycleSymptomEntry = {
    id: entityId<'cycle-symptom-entry'>('symptom-1'),
    cycleProfileId: entityId<'cycle-profile'>('cycle-1'),
    date: calendarDate('2026-04-03T00:00:00.000Z'),
    category: 0,
    intensity: 4,
    tags: [],
    note: null,
};

const FERTILITY_SIGNAL: FertilitySignal = {
    id: entityId<'fertility-signal'>('signal-1'),
    cycleProfileId: entityId<'cycle-profile'>('cycle-1'),
    date: calendarDate('2026-04-03T00:00:00.000Z'),
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

describe('cycle tracking overview mapper', () => {
    it('keeps the overview absent without a profile', () => {
        expect(buildCycleOverviewView(null, 'en-US')).toBeNull();
    });

    it('marks only the reported local calendar day when a prediction has no ending date', () => {
        const prediction = calendarDate('2026-04-16T00:00:00.000Z');
        const view = buildCycleOverviewView(
            { ...CYCLE, predictions: { nextPeriodStartFrom: prediction, confidence: 'Low', rationale: '' } },
            'en-US',
            new Date('2026-04-15T23:30:00'),
        );

        expect(view?.days.filter(day => day.isPredictedPeriod).map(day => day.dateKey)).toEqual(['2026-04-16']);
    });

    it('marks a notes-only day as tracked and offers to review the existing record', () => {
        const view = buildCycleOverviewView(
            {
                ...CYCLE,
                bleedingEntries: [],
                symptoms: [],
                fertilitySignals: [],
                dayNotes: [{ date: calendarDate('2026-04-16T00:00:00Z'), notes: 'Quiet day' }],
            },
            'en-US',
            new Date(OVERVIEW_YEAR, OVERVIEW_MONTH_INDEX, OVERVIEW_DAY),
        );
        expect(view?.hasTodayEntry).toBe(true);
        expect(view?.days.find(day => day.isToday)?.isTracked).toBe(true);
        expect(view?.days.find(day => day.dateKey === '2026-04-15')?.isTracked).toBe(false);
    });

    it('builds a today-first overview around the current date', () => {
        const view = buildCycleOverviewView(
            {
                ...CYCLE,
                bleedingEntries: [{ ...BLEEDING_ENTRY, date: calendarDate('2026-04-16T00:00:00.000Z') }],
                menstrualEpisodes: [
                    {
                        id: entityId<'menstrual-episode'>('episode-1'),
                        cycleProfileId: CYCLE.id,
                        startDate: calendarDate('2026-04-10T00:00:00.000Z'),
                        endDate: calendarDate('2026-04-14T00:00:00.000Z'),
                        status: 1,
                        excludedFromPredictions: false,
                    },
                ],
                predictions: {
                    confidence: 'Low',
                    rationale: '',
                    nextPeriodStartFrom: calendarDate('2026-04-19T00:00:00.000Z'),
                    nextPeriodStartTo: calendarDate('2026-04-20T00:00:00.000Z'),
                },
            },
            'en-US',
            new Date(OVERVIEW_YEAR, OVERVIEW_MONTH_INDEX, OVERVIEW_DAY),
        );

        expect(view?.todayDateKey).toBe('2026-04-16');
        expect(view?.cycleDayNumber).toBe(EXPECTED_CYCLE_DAY);
        expect(view?.hasTodayEntry).toBe(true);
        expect(view?.days).toHaveLength(OVERVIEW_DAY_COUNT);
        expect(view?.days.find(day => day.isToday)).toEqual(expect.objectContaining({ dateKey: '2026-04-16', isBleeding: true }));
        expect(view?.days.find(day => day.dateKey === '2026-04-19')?.isPredictedPeriod).toBe(true);

        const viewFromEpisodeStart = buildCycleOverviewView(
            {
                ...CYCLE,
                menstrualEpisodes: [
                    {
                        id: entityId<'menstrual-episode'>('episode-1'),
                        cycleProfileId: CYCLE.id,
                        startDate: calendarDate('2026-04-10T00:00:00.000Z'),
                        endDate: calendarDate('2026-04-14T00:00:00.000Z'),
                        status: 1,
                        excludedFromPredictions: false,
                    },
                ],
            },
            'en-US',
            new Date(OVERVIEW_YEAR, OVERVIEW_MONTH_INDEX, EPISODE_START_DAY),
        );
        expect(viewFromEpisodeStart?.days.find(day => day.dateKey === '2026-04-09')?.cycleDayNumber).toBe(CYCLE.averageCycleLength);
    });
});

it('marks a symptom-only day from a saved non-ISO date without marking it as bleeding', () => {
    const symptom = { ...SYMPTOM_ENTRY, date: calendarDate('Thu, 16 Apr 2026 12:30:00 GMT') };
    const view = buildCycleOverviewView({ ...CYCLE, symptoms: [symptom] }, 'en-US', new Date('2026-04-16T12:00:00'));

    expect(view?.hasTodayEntry).toBe(true);
    expect(view?.days.find(day => day.isToday)).toMatchObject({ dateKey: '2026-04-16', isTracked: true, isBleeding: false });
});

it('does not mark any calendar day for a symptom with an unparseable saved date', () => {
    const symptom = { ...SYMPTOM_ENTRY, date: calendarDate('invalid') };
    const view = buildCycleOverviewView({ ...CYCLE, symptoms: [symptom] }, 'en-US', new Date('2026-04-16T12:00:00'));

    expect(view?.hasTodayEntry).toBe(false);
    expect(view?.days.every(day => !day.isTracked)).toBe(true);
});

describe('Cycle profile display', () => {
    it.each([
        [CYCLE_TRACKING_MODE_PERIOD_TRACKING, 'MODE_PERIOD_TRACKING'],
        [CYCLE_TRACKING_MODE_PREGNANCY, 'MODE_PREGNANCY'],
        [CYCLE_TRACKING_MODE_POSTPARTUM_LACTATION, 'MODE_POSTPARTUM_LACTATION'],
        [CYCLE_TRACKING_MODE_PERIMENOPAUSE, 'MODE_PERIMENOPAUSE'],
        [CYCLE_TRACKING_MODE_NO_PERIOD, 'MODE_NO_PERIOD'],
    ] as const)('presents configured tracking mode %s and disabled fertility estimates', (mode, key) => {
        const view = buildCycleCurrentView({ ...CYCLE, mode, isRegular: false, showFertilityEstimates: false }, 'en-US');

        expect(view?.summaryItems).toEqual(
            expect.arrayContaining([
                expect.objectContaining({ labelKey: 'CYCLE_TRACKING.MODE', valueKey: `CYCLE_TRACKING.${key}` }),
                expect.objectContaining({ labelKey: 'CYCLE_TRACKING.REGULARITY', valueKey: 'CYCLE_TRACKING.REGULARITY_IRREGULAR' }),
                expect.objectContaining({ labelKey: 'CYCLE_TRACKING.FERTILITY_ESTIMATES', valueKey: 'CYCLE_TRACKING.DISABLED' }),
            ]),
        );
    });

    it.each([
        [CYCLE_FACTOR_TYPE_PREGNANCY, 'FACTOR_PREGNANCY'],
        [CYCLE_FACTOR_TYPE_LACTATION, 'FACTOR_LACTATION'],
        [CYCLE_FACTOR_TYPE_NON_HORMONAL_CONTRACEPTION, 'FACTOR_NON_HORMONAL_CONTRACEPTION'],
        [CYCLE_FACTOR_TYPE_POSTPARTUM, 'FACTOR_POSTPARTUM'],
        [CYCLE_FACTOR_TYPE_PERIMENOPAUSE, 'FACTOR_PERIMENOPAUSE'],
        [CYCLE_FACTOR_TYPE_NO_PERIOD, 'FACTOR_NO_PERIOD'],
    ] as const)('uses the configured journal label for active factor %s', (type, key) => {
        const factor = {
            id: entityId<'cycle-factor'>('active-factor'),
            cycleProfileId: CYCLE.id,
            type,
            startDate: calendarDate('2026-04-02T00:00:00.000Z'),
        };
        const view = buildCycleCurrentView({ ...CYCLE, factors: [factor] }, 'en-US');

        expect(view?.activeFactorItems).toEqual([{ id: factor.id, labelKey: `CYCLE_TRACKING.${key}`, startDateLabel: 'Apr 2' }]);
    });
});

describe('current cycle factors', () => {
    it('orders ongoing factors newest first and leaves completed factors out of the current profile summary', () => {
        const older = { ...CYCLE.factors[0], id: entityId<'cycle-factor'>('older'), startDate: calendarDate('2026-04-01'), endDate: null };
        const newest = {
            ...CYCLE.factors[0],
            id: entityId<'cycle-factor'>('newest'),
            startDate: calendarDate('2026-04-06'),
            endDate: undefined,
        };
        const ended = CYCLE.factors[1];

        const summary = buildCycleCurrentView({ ...CYCLE, factors: [older, ended, newest] }, 'en-US');

        expect(summary?.activeFactorItems.map(({ id, startDateLabel }) => ({ id, startDateLabel }))).toEqual([
            { id: newest.id, startDateLabel: 'Apr 6' },
            { id: older.id, startDateLabel: 'Apr 1' },
        ]);
    });
});

describe('cycle tracking without menstrual episodes', () => {
    it('does not invent a cycle day from the observation start without a period', () => {
        const view = buildCycleOverviewView({ ...CYCLE, menstrualEpisodes: [] }, 'en-US', new Date('2026-04-16T12:00:00Z'));
        expect(view?.cycleDayNumber).toBeNull();
        expect(view?.days.every(day => day.cycleDayNumber === null)).toBe(true);
    });
});

describe('cycle tracking prediction mapper', () => {
    it('builds prediction labels using UTC dates', () => {
        const view = buildCyclePredictionView(
            {
                nextPeriodStartFrom: calendarDate('2026-04-29T23:00:00.000Z'),
                nextPeriodStartTo: calendarDate('2026-05-01T00:00:00.000Z'),
                ovulationFrom: calendarDate('2026-04-15T00:00:00.000Z'),
                ovulationTo: calendarDate('2026-04-16T00:00:00.000Z'),
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
            confidenceKey: 'CYCLE_TRACKING.CONFIDENCE_MEDIUM',
            dataSufficiencyKey: 'CYCLE_TRACKING.SUFFICIENCY_UNAVAILABLE',
            completedCycleCount: 0,
            usedEpisodeCount: 0,
            excludedEpisodeCount: 0,
            calibrationSampleCount: 0,
            historicalCoveragePercent: null,
            meanAbsoluteErrorDays: null,
            explanationKey: 'CYCLE_TRACKING.PREDICTION_EXPLANATION',
            hasPredictionRanges: true,
            limitedReasonKey: null,
        });
    });

    it('preserves invalid date values for diagnostics', () => {
        const view = buildCyclePredictionView(
            {
                nextPeriodStartFrom: calendarDate('not-a-date'),
                nextPeriodStartTo: null,
                ovulationFrom: calendarDate(''),
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

describe('cycle tracking prediction v2 mapper', () => {
    it.each([
        ['ambiguous_episode_history', 'CYCLE_TRACKING.PREDICTIONS_AMBIGUOUS'],
        ['prediction_paused_by_state', 'CYCLE_TRACKING.PREDICTIONS_PAUSED'],
        ['unknown_future_reason', 'CYCLE_TRACKING.PREDICTIONS_LIMITED'],
    ])('explains %s separately from insufficient history', (reason, key) => {
        const view = buildCyclePredictionView({ confidence: 'Low', rationale: '', reasonCodes: [reason] }, 'ru');
        expect(view?.limitedReasonKey).toBe(key);
    });
    it('shows a learning state when completed cycle history is insufficient', () => {
        const view = buildCyclePredictionView(
            {
                confidence: 'Learning',
                rationale: 'At least three completed cycle intervals are needed.',
                dataSufficiency: 'Insufficient',
                completedCycleCount: 2,
                reasonCodes: ['insufficient_completed_cycles'],
                algorithmVersion: 'period-v2.0',
            },
            'en-US',
        );

        expect(view?.hasPredictionRanges).toBe(false);
        expect(view?.completedCycleCount).toBe(2);
        expect(view?.limitedReasonKey).toBe('CYCLE_TRACKING.PREDICTIONS_LEARNING');
    });
});

describe('cycle tracking nutrition summary mapper', () => {
    it('formats nutrition summary values', () => {
        const view = buildCycleNutritionSummaryView(NUTRITION_SUMMARY, 'en-US');

        expect(view?.summary.loggedCycleDays).toBe(NUTRITION_LOGGED_CYCLE_DAYS);
        expect(view?.hasEnoughData).toBe(true);
        expect(view?.dateFromLabel).toBe('Apr 1, 2026');
        expect(view?.dateToLabel).toBe('Apr 30, 2026');
        expect(view?.bleedingCaloriesLabel).toBe('2,100.3');
        expect(view?.nonBleedingCaloriesLabel).toBe('1,800');
        expect(view?.bleedingFiberLabel).toBe('18.5');
        expect(view?.nonBleedingFiberLabel).toBe('28');
        expect(view?.painImpactLabel).toBe('6.3');
    });

    it('returns null without nutrition summary', () => {
        expect(buildCycleNutritionSummaryView(null, 'en-US')).toBeNull();
    });

    it('hides comparison values when nutrition summary has limited data', () => {
        const view = buildCycleNutritionSummaryView({ ...NUTRITION_SUMMARY, hasEnoughNutritionData: false }, 'en-US');

        expect(view?.hasEnoughData).toBe(false);
        expect(view?.bleedingCaloriesLabel).toBe('\u2014');
        expect(view?.nonBleedingCaloriesLabel).toBe('\u2014');
        expect(view?.painImpactLabel).toBe('\u2014');
    });
});

describe('cycle tracking factor mapper', () => {
    it('shows a future start as planned', () => {
        const now = new Date();
        const tomorrow = formatDateInputValue(new Date(now.getFullYear(), now.getMonth(), now.getDate() + 1));
        const view = buildCycleFactorItems([{ ...CYCLE.factors[0], startDate: calendarDate(tomorrow), endDate: null }], 'en-US');
        expect(view[0].statusLabelKey).toBe('CYCLE_TRACKING.FACTOR_PLANNED');
        expect(view[0].isActive).toBe(false);
    });

    it('keeps a factor active until its future end date', () => {
        const now = new Date();
        const tomorrow = formatDateInputValue(new Date(now.getFullYear(), now.getMonth(), now.getDate() + 1));
        const view = buildCycleFactorItems([{ ...CYCLE.factors[0], endDate: calendarDate(tomorrow) }], 'en-US');
        expect(view[0].statusLabelKey).toBe('CYCLE_TRACKING.FACTOR_ACTIVE');
        expect(view[0].isActive).toBe(true);
    });

    it('shows that a factor ends today', () => {
        const view = buildCycleFactorItems([{ ...CYCLE.factors[0], endDate: calendarDate(formatDateInputValue(new Date())) }], 'en-US');
        expect(view[0].statusLabelKey).toBe('CYCLE_TRACKING.FACTOR_ENDS_TODAY');
    });

    it('builds factor list items with date ranges and status labels', () => {
        const view = buildCycleFactorItems(CYCLE.factors, 'en-US');

        expect(view).toEqual([
            {
                id: 'factor-1',
                labelKey: 'CYCLE_TRACKING.FACTOR_HORMONAL_CONTRACEPTION',
                dateRangeLabel: 'Apr 2',
                statusLabelKey: 'CYCLE_TRACKING.FACTOR_ACTIVE',
                isActive: true,
                canEndToday: true,
            },
            {
                id: 'factor-2',
                labelKey: 'CYCLE_TRACKING.FACTOR_HORMONAL_CONTRACEPTION',
                dateRangeLabel: 'Mar 2 - Mar 10',
                statusLabelKey: 'CYCLE_TRACKING.FACTOR_ENDED',
                isActive: false,
                canEndToday: false,
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
        expect(bleedingItem?.bleedingSummaryItems).toEqual([
            {
                id: BLEEDING_ENTRY.id,
                typeLabelKey: 'CYCLE_TRACKING.BLEEDING_TYPE_BLEEDING',
                flowLabelKey: 'CYCLE_TRACKING.FLOW_MEDIUM',
                painSeverityKey: 'CYCLE_TRACKING.SEVERITY_MODERATE',
            },
        ]);
        expect(symptomItem?.symptomSummaryItems).toEqual([
            {
                id: SYMPTOM_ENTRY.id,
                labelKey: 'CYCLE_TRACKING.SYMPTOM_PAIN',
                severityKey: 'CYCLE_TRACKING.SEVERITY_MODERATE',
            },
        ]);
        expect(symptomItem?.fertilitySignalItems).toContainEqual({
            textKey: 'CYCLE_TRACKING.BBT_SUMMARY',
            params: { value: '36.62' },
        });
        expect(symptomItem?.fertilitySignalItems).toContainEqual({
            textKey: 'CYCLE_TRACKING.OVULATION_TEST_POSITIVE_SUMMARY',
        });
    });

    it('filters zero-intensity symptoms from day history', () => {
        const items = buildCycleDayItems([], [{ ...SYMPTOM_ENTRY, intensity: 0 }], [], 'en-US');

        expect(items).toEqual([]);
    });
});

describe('Cycle journal day display', () => {
    it.each([
        [CYCLE_FLOW_LIGHT, 'CYCLE_TRACKING.FLOW_LIGHT'],
        [CYCLE_FLOW_NONE, null],
    ] as const)('shows spotting independently of the optional flow label %s', (flow, flowLabelKey) => {
        const entry: BleedingEntry = { ...BLEEDING_ENTRY, type: BLEEDING_TYPE_SPOTTING, flow, painImpact: 1 };
        const [item] = buildCycleDayItems([entry], [], [], 'en-US');

        expect(item).toMatchObject({
            accentColor: DEFAULT_DAY_ACCENT_COLOR,
            badgeLabelKey: 'CYCLE_TRACKING.BADGE_SPOTTING',
            bleedingSummaryItems: [
                { typeLabelKey: 'CYCLE_TRACKING.BLEEDING_TYPE_SPOTTING', flowLabelKey, painSeverityKey: 'CYCLE_TRACKING.SEVERITY_MILD' },
            ],
        });
    });

    it('keeps a negative test and labels other symptoms without showing blank optional signals', () => {
        const signal: FertilitySignal = {
            ...FERTILITY_SIGNAL,
            basalBodyTemperatureCelsius: null,
            ovulationTestResult: OVULATION_TEST_RESULT_NEGATIVE,
            cervicalFluid: ' ',
            hadSex: false,
        };
        const [item] = buildCycleDayItems([], [{ ...SYMPTOM_ENTRY, category: CYCLE_SYMPTOM_CATEGORY_OTHER }], [signal], 'en-US');

        expect(item.symptomSummaryItems).toMatchObject([{ labelKey: 'CYCLE_TRACKING.SYMPTOM_OTHER' }]);
        expect(item.fertilitySignalItems).toEqual([{ textKey: 'CYCLE_TRACKING.OVULATION_TEST_NEGATIVE_SUMMARY' }]);
    });
});

describe('cycle observations mapper', () => {
    it.each([
        [HIGHER_MOOD_INTENSITY, ['CYCLE_TRACKING.SYMPTOM_MOOD', 'CYCLE_TRACKING.SYMPTOM_PAIN']],
        [PAIN_INTENSITY, ['CYCLE_TRACKING.SYMPTOM_PAIN', 'CYCLE_TRACKING.SYMPTOM_MOOD']],
    ] as const)('ranks equally frequent symptoms consistently when the recorded mood intensity is %s', (intensity, labels) => {
        const symptoms: CycleSymptomEntry[] = [
            { ...SYMPTOM_ENTRY, id: entityId<'cycle-symptom-entry'>('mood'), category: CYCLE_SYMPTOM_CATEGORY_MOOD, intensity },
            { ...SYMPTOM_ENTRY, intensity: PAIN_INTENSITY },
        ];
        const items = buildCycleDayItems([], symptoms, [], 'en-US');

        expect(items[0].symptomSummaryItems.map(item => item.labelKey)).toEqual(labels);
        expect(buildCycleObservationsView(items).topSymptom).toMatchObject({ labelKey: labels[0], loggedDayCount: 1 });
    });

    it('builds descriptive observations from active journal entries', () => {
        const symptoms: CycleSymptomEntry[] = [
            {
                ...SYMPTOM_ENTRY,
                id: entityId<'cycle-symptom-entry'>('pain-1'),
                date: calendarDate('2026-04-01T00:00:00.000Z'),
                intensity: 4,
            },
            {
                ...SYMPTOM_ENTRY,
                id: entityId<'cycle-symptom-entry'>('pain-2'),
                date: calendarDate('2026-04-02T00:00:00.000Z'),
                intensity: 8,
            },
            {
                ...SYMPTOM_ENTRY,
                id: entityId<'cycle-symptom-entry'>('sleep-1'),
                date: calendarDate('2026-04-03T00:00:00.000Z'),
                category: 3,
                intensity: 5,
            },
            {
                ...SYMPTOM_ENTRY,
                id: entityId<'cycle-symptom-entry'>('ignored-zero'),
                date: calendarDate('2026-04-03T00:00:00.000Z'),
                category: 7,
                intensity: 0,
            },
        ];
        const items = buildCycleDayItems([BLEEDING_ENTRY], symptoms, [], 'en-US');

        expect(buildCycleObservationsView(items)).toEqual({
            hasEnoughData: true,
            trackedDayCount: 3,
            bleedingDayCount: 1,
            activeSymptomRecordCount: 3,
            topSymptom: {
                labelKey: 'CYCLE_TRACKING.SYMPTOM_PAIN',
                loggedDayCount: 2,
                severityKey: 'CYCLE_TRACKING.SEVERITY_MODERATE',
            },
        });
    });
});

describe('cycle period metadata mapper', () => {
    it('marks inferred and confirmed period starts', () => {
        const inferred = buildCycleDayItems([BLEEDING_ENTRY], [], [], {
            locale: 'en-US',
            menstrualEpisodes: [
                {
                    id: entityId<'menstrual-episode'>(''),
                    cycleProfileId: entityId<'cycle-profile'>('cycle-1'),
                    startDate: calendarDate(BLEEDING_ENTRY.date),
                    endDate: calendarDate(BLEEDING_ENTRY.date),
                    status: 0,
                    excludedFromPredictions: false,
                },
            ],
        });
        const confirmed = buildCycleDayItems([BLEEDING_ENTRY], [], [], {
            locale: 'en-US',
            menstrualEpisodes: [
                {
                    id: entityId<'menstrual-episode'>('episode-1'),
                    cycleProfileId: entityId<'cycle-profile'>('cycle-1'),
                    startDate: calendarDate(BLEEDING_ENTRY.date),
                    endDate: calendarDate(BLEEDING_ENTRY.date),
                    status: 1,
                    excludedFromPredictions: false,
                },
            ],
        });

        expect(inferred[0]).toMatchObject({ isPeriodStart: true, isPeriodStartConfirmed: false });
        expect(confirmed[0]).toMatchObject({ isPeriodStart: true, isPeriodStartConfirmed: true });
    });

    it('builds care prompts for severe pain, heavy flow, and prolonged bleeding', () => {
        const bleedingEntries = Array.from({ length: PROLONGED_BLEEDING_DAYS }, (_, index): BleedingEntry => {
            const date = new Date(Date.UTC(CARE_PROMPT_YEAR, APRIL_MONTH_INDEX, index + 1)).toISOString();
            return {
                id: entityId<'bleeding-entry'>(`bleeding-${index + 1}`),
                cycleProfileId: entityId<'cycle-profile'>('cycle-1'),
                date: calendarDate(date),
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

describe('Notes-only day history', () => {
    it('shows the note without fabricating symptoms or bleeding', () => {
        const items = buildCycleDayItems([], [], [], {
            locale: 'ru',
            menstrualEpisodes: [],
            dayNotes: [{ date: calendarDate('2026-04-02'), notes: 'Quiet day' }],
        });
        expect(items).toHaveLength(1);
        expect(items[0].notes).toBe('Quiet day');
        expect(items[0].symptoms).toEqual([]);
        expect(items[0].bleedingEntries).toEqual([]);
    });
});

describe('cycle prediction confidence localization', () => {
    it.each([
        ['Established', 'Low', 'SUFFICIENCY_ESTABLISHED'],
        ['Limited', 'High', 'SUFFICIENCY_LIMITED'],
        ['Insufficient', 'High', 'SUFFICIENCY_INSUFFICIENT'],
        ['Unavailable', 'High', 'SUFFICIENCY_UNAVAILABLE'],
        ['FutureValue', 'High', 'SUFFICIENCY_UNAVAILABLE'],
        [undefined, 'Learning', 'CONFIDENCE_LEARNING'],
        [undefined, 'Low', 'CONFIDENCE_LOW'],
        [undefined, 'Medium', 'CONFIDENCE_MEDIUM'],
        [undefined, 'Moderate', 'CONFIDENCE_MEDIUM'],
        [undefined, 'High', 'CONFIDENCE_HIGH'],
        [undefined, 'FutureValue', 'SUFFICIENCY_UNAVAILABLE'],
    ])('maps sufficiency %s and legacy confidence %s to localized copy', (dataSufficiency, confidence, key) => {
        const view = buildCyclePredictionView(
            {
                nextPeriodStartFrom: null,
                nextPeriodStartTo: null,
                ovulationFrom: null,
                ovulationTo: null,
                pmsWindowStart: null,
                pmsWindowEnd: null,
                dataSufficiency,
                confidence,
                rationale: '',
            },
            'ru-RU',
        );

        expect(view?.confidenceKey).toBe(`CYCLE_TRACKING.${key}`);
    });
});
