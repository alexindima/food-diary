import { describe, expect, it } from 'vitest';

import type { FastingSession } from '../models/fasting.data';
import {
    buildFastingTimerCardComputedState,
    formatFastingDuration,
    getCyclicPhaseProgressLabel,
    getFastingOccurrenceLabel,
    getFastingProtocolBaseLabel,
} from './fasting-timer-card-state';

const MS_PER_HOUR = 3_600_000;
const MS_PER_MINUTE = 60_000;
const MS_PER_SECOND = 1_000;
const MS_999 = 999;
const THREE_SECONDS_MS = 3_000;
const PROGRESS_12_5 = 12.5;
const PROGRESS_25 = 25;
const PROGRESS_FULL = 100;
const STAGE_TOTAL = 3;
const HOURS_3 = 3;
const HOURS_13 = 13;
const HOURS_16 = 16;
const HOURS_18 = 18;
const HOURS_25 = 25;
const HOURS_30 = 30;
const HOURS_72 = 72;
const MINUTES_30 = 30;
const CYCLIC_EAT_ELAPSED_MS = MINUTES_30 * MS_PER_MINUTE;

const translate = (key: string, params?: Record<string, unknown>): string =>
    params !== undefined ? `${key}:${JSON.stringify(params)}` : key;
const hours = (value: number): number => value * MS_PER_HOUR;

describe('buildFastingTimerCardComputedState', () => {
    registerIdleTests();
    registerIntermittentWindowTests();
    registerIntermittentProtocolTests();
    registerExtendedTests();
    registerCyclicTests();
});

function registerIdleTests(): void {
    describe('idle sessions', () => {
        it('builds idle state without a session', () => {
            const state = buildFastingTimerCardComputedState({
                session: null,
                elapsedMs: 0,
                translate,
            });

            expect(state).toEqual({
                progressPercent: 0,
                elapsedFormatted: '00:00:00',
                remainingFormatted: '00:00:00',
                remainingLabelKey: 'FASTING.REMAINING',
                isOvertime: false,
                showStageProgress: true,
                stateLabel: null,
                detailLabel: null,
                metaLabel: null,
                ringColor: null,
                stage: null,
                nextStageFormatted: null,
            });
        });
    });
}

function registerIntermittentWindowTests(): void {
    describe('intermittent window sessions', () => {
        it('builds intermittent fasting-window state from cycle elapsed time', () => {
            const state = buildFastingTimerCardComputedState({
                session: createSession({ planType: 'Intermittent', protocol: 'F16_8', initialPlannedDurationHours: HOURS_16 }),
                elapsedMs: hours(2),
                translate,
            });

            expect(state.progressPercent).toBe(PROGRESS_12_5);
            expect(state.elapsedFormatted).toBe('02:00:00');
            expect(state.remainingFormatted).toBe('14:00:00');
            expect(state.remainingLabelKey).toBe('FASTING.UNTIL_EATING_WINDOW');
            expect(state.isOvertime).toBe(false);
            expect(state.showStageProgress).toBe(true);
            expect(state.stateLabel).toBe('FASTING.FASTING_WINDOW');
            expect(state.detailLabel).toBe('FASTING.PROTOCOL_16_8');
            expect(state.metaLabel).toBe('FASTING.DAY_LABEL:{"day":1}');
            expect(state.stage?.index).toBe(1);
            expect(state.stage?.total).toBe(STAGE_TOTAL);
            expect(state.nextStageFormatted).toBe('02:00:00');
        });

        it('builds intermittent eating-window state without fasting stage progress', () => {
            const state = buildFastingTimerCardComputedState({
                session: createSession({ planType: 'Intermittent', protocol: 'F16_8', initialPlannedDurationHours: HOURS_16 }),
                elapsedMs: hours(HOURS_18),
                translate,
            });

            expect(state.progressPercent).toBe(PROGRESS_25);
            expect(state.elapsedFormatted).toBe('02:00:00');
            expect(state.remainingFormatted).toBe('06:00:00');
            expect(state.remainingLabelKey).toBe('FASTING.NEXT_FAST');
            expect(state.isOvertime).toBe(false);
            expect(state.showStageProgress).toBe(false);
            expect(state.stateLabel).toBe('FASTING.EATING_WINDOW');
            expect(state.ringColor).toBe('var(--fd-color-green-500)');
            expect(state.stage).toEqual({
                index: 1,
                total: 1,
                titleKey: 'FASTING.EATING_WINDOW',
                descriptionKey: 'FASTING.EATING_WINDOW_DESCRIPTION',
                color: 'var(--fd-color-green-500)',
                glowColor: 'color-mix(in srgb, var(--fd-color-green-500) 18%, transparent)',
                nextTitleKey: null,
                nextInMs: null,
            });
        });

        it('wraps intermittent sessions into the next cycle day', () => {
            const state = buildFastingTimerCardComputedState({
                session: createSession({ planType: 'Intermittent', protocol: 'F18_6', initialPlannedDurationHours: HOURS_18 }),
                elapsedMs: hours(HOURS_25),
                translate,
            });

            expect(state.progressPercent).toBeCloseTo((1 / HOURS_18) * PROGRESS_FULL);
            expect(state.elapsedFormatted).toBe('01:00:00');
            expect(state.remainingFormatted).toBe('17:00:00');
            expect(state.remainingLabelKey).toBe('FASTING.UNTIL_EATING_WINDOW');
            expect(state.metaLabel).toBe('FASTING.DAY_LABEL:{"day":2}');
            expect(state.detailLabel).toBe('FASTING.PROTOCOL_18_6');
        });

        it('builds completed intermittent sessions with fallback elapsed progress instead of cycle-window state', () => {
            const state = buildFastingTimerCardComputedState({
                session: createSession({ endedAtUtc: '2026-04-12T22:00:00Z', isCompleted: true, status: 'Completed' }),
                elapsedMs: hours(HOURS_16),
                translate,
            });

            expect(state.progressPercent).toBe(PROGRESS_FULL);
            expect(state.elapsedFormatted).toBe('16:00:00');
            expect(state.remainingFormatted).toBe('00:00:00');
            expect(state.remainingLabelKey).toBe('FASTING.REMAINING');
            expect(state.stateLabel).toBe('FASTING.FASTING_WINDOW');
            expect(state.detailLabel).toBe('FASTING.PROTOCOL_16_8');
            expect(state.metaLabel).toBeNull();
        });
    });
}

function registerIntermittentProtocolTests(): void {
    describe('intermittent protocol labels', () => {
        it('formats custom intermittent protocols as fast-to-eating ratios', () => {
            const state = buildFastingTimerCardComputedState({
                session: createSession({ protocol: 'CustomIntermittent', initialPlannedDurationHours: 22, plannedDurationHours: 22 }),
                elapsedMs: hours(1),
                translate,
            });

            expect(state.detailLabel).toBe('22:2');
        });

        it('clamps custom intermittent ratio labels to a valid 24-hour split', () => {
            expect(
                getFastingProtocolBaseLabel(translate, createSession({ protocol: 'CustomIntermittent', initialPlannedDurationHours: 0 })),
            ).toBe('1:23');
            expect(
                getFastingProtocolBaseLabel(translate, createSession({ protocol: 'CustomIntermittent', initialPlannedDurationHours: 48 })),
            ).toBe('23:1');
        });
    });
}

function registerExtendedTests(): void {
    describe('extended sessions', () => {
        it('marks extended sessions as overtime after the planned duration', () => {
            const state = buildFastingTimerCardComputedState({
                session: createSession({
                    planType: 'Extended',
                    protocol: 'F24_0',
                    initialPlannedDurationHours: 24,
                    plannedDurationHours: 24,
                    occurrenceKind: 'FastDay',
                }),
                elapsedMs: hours(HOURS_30),
                translate,
            });

            expect(state.progressPercent).toBe(PROGRESS_FULL);
            expect(state.elapsedFormatted).toBe('30:00:00');
            expect(state.remainingFormatted).toBe('00:00:00');
            expect(state.isOvertime).toBe(true);
            expect(state.stateLabel).toBe('FASTING.FAST_DAY');
            expect(state.detailLabel).toBe('FASTING.PROTOCOL_24_0');
            expect(state.stage?.titleKey).toBe('FASTING.STAGES.DEEP.TITLE');
            expect(state.nextStageFormatted).toBeNull();
        });

        it('formats custom extended protocols with added duration', () => {
            const state = buildFastingTimerCardComputedState({
                session: createSession({
                    planType: 'Extended',
                    protocol: 'Custom',
                    initialPlannedDurationHours: HOURS_30,
                    addedDurationHours: 6,
                    plannedDurationHours: 36,
                }),
                elapsedMs: hours(HOURS_3),
                translate,
            });

            expect(state.detailLabel).toBe('30 FASTING.HOURS (+6 FASTING.HOURS)');
        });

        it('falls back to hours formatting for unknown protocol ids', () => {
            const state = buildFastingTimerCardComputedState({
                session: createSession({
                    planType: 'Extended',
                    protocol: 'Legacy48',
                    initialPlannedDurationHours: 48,
                    addedDurationHours: 0,
                    plannedDurationHours: 48,
                }),
                elapsedMs: hours(1),
                translate,
            });

            expect(state.detailLabel).toBe('48 FASTING.HOURS');
        });
    });
}

function registerCyclicTests(): void {
    describe('cyclic sessions', () => {
        it('derives timer state for cyclic eating-day sessions', () => {
            const state = buildFastingTimerCardComputedState({
                session: createSession({
                    planType: 'Cyclic',
                    protocol: 'Cyclic',
                    occurrenceKind: 'EatDay',
                    cyclicFastDays: 2,
                    cyclicEatDays: 1,
                    cyclicEatDayFastHours: 20,
                    cyclicEatDayEatingWindowHours: 4,
                    cyclicPhaseDayNumber: 1,
                    cyclicPhaseDayTotal: 1,
                    plannedDurationHours: 4,
                }),
                elapsedMs: CYCLIC_EAT_ELAPSED_MS,
                translate,
            });

            expect(state.progressPercent).toBe(PROGRESS_12_5);
            expect(state.elapsedFormatted).toBe('00:30:00');
            expect(state.remainingFormatted).toBe('03:30:00');
            expect(state.remainingLabelKey).toBe('FASTING.REMAINING');
            expect(state.stateLabel).toBe('FASTING.EAT_DAY');
            expect(state.detailLabel).toBe('2:1 (20:4)');
            expect(state.metaLabel).toBe('FASTING.CYCLIC_EAT_PHASE_DAY_PROGRESS:{"current":1,"total":1}');
        });

        it('derives timer state for cyclic fast-day sessions with stage progress in the meta line', () => {
            const state = buildFastingTimerCardComputedState({
                session: createSession({
                    planType: 'Cyclic',
                    protocol: 'Cyclic',
                    occurrenceKind: 'FastDay',
                    cyclicFastDays: STAGE_TOTAL,
                    cyclicEatDays: 1,
                    cyclicEatDayFastHours: HOURS_16,
                    cyclicEatDayEatingWindowHours: 8,
                    cyclicPhaseDayNumber: 2,
                    cyclicPhaseDayTotal: STAGE_TOTAL,
                    plannedDurationHours: HOURS_72,
                }),
                elapsedMs: hours(HOURS_13),
                translate,
            });

            expect(state.progressPercent).toBeCloseTo((HOURS_13 / HOURS_72) * PROGRESS_FULL);
            expect(state.stateLabel).toBe('FASTING.FAST_DAY');
            expect(state.detailLabel).toBe('3:1 (16:8)');
            expect(state.metaLabel).toBe('FASTING.CYCLIC_FAST_PHASE_STAGE_PROGRESS:{"current":2,"total":3,"stage":3,"stageTotal":4}');
            expect(state.stage?.titleKey).toBe('FASTING.STAGES.STORED_ENERGY.TITLE');
            expect(state.nextStageFormatted).toBe('03:00:00');
        });

        it('uses occurrence label as cyclic meta fallback when day totals are missing', () => {
            const state = buildFastingTimerCardComputedState({
                session: createSession({
                    planType: 'Cyclic',
                    protocol: 'Cyclic',
                    occurrenceKind: 'FastDay',
                    cyclicFastDays: null,
                    cyclicEatDays: null,
                    cyclicPhaseDayNumber: null,
                    cyclicPhaseDayTotal: null,
                }),
                elapsedMs: hours(1),
                translate,
            });

            expect(state.detailLabel).toBe('1:1 (16:8)');
            expect(state.metaLabel).toBe('FASTING.FAST_DAY');
        });
    });
}

describe('getFastingOccurrenceLabel', () => {
    it.each([
        ['FastDay', 'FASTING.FAST_DAY'],
        ['EatDay', 'FASTING.EAT_DAY'],
        ['FastingWindow', 'FASTING.FASTING_WINDOW'],
        ['EatingWindow', 'FASTING.EATING_WINDOW'],
        [null, null],
        [undefined, null],
    ] as const)('maps %s to %s', (kind, expected) => {
        expect(getFastingOccurrenceLabel(translate, kind)).toBe(expected);
    });
});

describe('getCyclicPhaseProgressLabel', () => {
    it('builds fast-day progress without stage details when no stage is supplied', () => {
        expect(
            getCyclicPhaseProgressLabel(
                translate,
                createSession({
                    planType: 'Cyclic',
                    occurrenceKind: 'FastDay',
                    cyclicPhaseDayNumber: 1,
                    cyclicPhaseDayTotal: 2,
                }),
                null,
            ),
        ).toBe('FASTING.CYCLIC_FAST_PHASE_DAY_PROGRESS:{"current":1,"total":2}');
    });
});

describe('formatFastingDuration', () => {
    it.each([
        [0, '00:00:00'],
        [MS_999, '00:00:00'],
        [MS_PER_SECOND, '00:00:01'],
        [MS_PER_MINUTE + MS_PER_SECOND, '00:01:01'],
        [hours(HOURS_25) + 2 * MS_PER_MINUTE + THREE_SECONDS_MS, '25:02:03'],
    ] as const)('formats %i ms as %s', (ms, expected) => {
        expect(formatFastingDuration(ms)).toBe(expected);
    });
});

function createSession(overrides: Partial<FastingSession> = {}): FastingSession {
    return {
        id: 'session-1',
        startedAtUtc: '2026-04-12T06:00:00Z',
        endedAtUtc: null,
        initialPlannedDurationHours: HOURS_16,
        addedDurationHours: 0,
        plannedDurationHours: HOURS_16,
        protocol: 'F16_8',
        planType: 'Intermittent',
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
        ...overrides,
    };
}
