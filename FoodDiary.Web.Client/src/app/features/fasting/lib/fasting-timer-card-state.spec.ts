import { describe, expect, it } from 'vitest';

import { buildFastingTimerCardComputedState, formatFastingDuration } from '../../../shared/lib/fasting-timer-card-state';
import type { FastingSession } from '../../../shared/models/fasting.data';
import { utcInstant } from '../../../shared/models/semantics/date-value';
import { entityId } from '../../../shared/models/semantics/entity-id';

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
const HOURS_24 = 24;
const HOURS_25 = 25;
const HOURS_30 = 30;
const HOURS_36 = 36;
const HOURS_72 = 72;
const MINUTES_30 = 30;
const CYCLIC_EAT_ELAPSED_MS = MINUTES_30 * MS_PER_MINUTE;

const hours = (value: number): number => value * MS_PER_HOUR;

describe('buildFastingTimerCardComputedState', () => {
    registerIdleTests();
    registerIntermittentWindowTests();
    registerIntermittentProtocolTests();
    registerExtendedTests();
    registerExtendedBoundaryTests();
    registerIntermittentBoundaryTests();
    registerCyclicTests();
});

function registerIdleTests(): void {
    describe('idle sessions', () => {
        it('builds idle state without a session', () => {
            const state = buildFastingTimerCardComputedState({
                session: null,
                elapsedMs: 0,
            });

            expect(state).toEqual({
                progressPercent: 0,
                elapsedFormatted: '00:00:00',
                remainingFormatted: '00:00:00',
                remainingLabelKey: 'FASTING.REMAINING',
                isOvertime: false,
                showStageProgress: true,
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
                session: createSession({ planType: 'Intermittent', protocol: 'Fast16Eat8', initialPlannedDurationHours: HOURS_16 }),
                elapsedMs: hours(2),
            });

            expect(state.progressPercent).toBe(PROGRESS_12_5);
            expect(state.elapsedFormatted).toBe('02:00:00');
            expect(state.remainingFormatted).toBe('14:00:00');
            expect(state.remainingLabelKey).toBe('FASTING.UNTIL_EATING_WINDOW');
            expect(state.isOvertime).toBe(false);
            expect(state.showStageProgress).toBe(true);
            expect(state.stage?.index).toBe(1);
            expect(state.stage?.total).toBe(STAGE_TOTAL);
            expect(state.nextStageFormatted).toBe('02:00:00');
        });

        it('builds intermittent eating-window state without fasting stage progress', () => {
            const state = buildFastingTimerCardComputedState({
                session: createSession({ planType: 'Intermittent', protocol: 'Fast16Eat8', initialPlannedDurationHours: HOURS_16 }),
                elapsedMs: hours(HOURS_18),
            });

            expect(state.progressPercent).toBe(PROGRESS_25);
            expect(state.elapsedFormatted).toBe('02:00:00');
            expect(state.remainingFormatted).toBe('06:00:00');
            expect(state.remainingLabelKey).toBe('FASTING.NEXT_FAST');
            expect(state.isOvertime).toBe(false);
            expect(state.showStageProgress).toBe(false);
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
                session: createSession({ planType: 'Intermittent', protocol: 'Fast18Eat6', initialPlannedDurationHours: HOURS_18 }),
                elapsedMs: hours(HOURS_25),
            });

            expect(state.progressPercent).toBeCloseTo((1 / HOURS_18) * PROGRESS_FULL);
            expect(state.elapsedFormatted).toBe('01:00:00');
            expect(state.remainingFormatted).toBe('17:00:00');
            expect(state.remainingLabelKey).toBe('FASTING.UNTIL_EATING_WINDOW');
        });

        it('builds completed intermittent sessions with fallback elapsed progress instead of cycle-window state', () => {
            const state = buildFastingTimerCardComputedState({
                session: createSession({ endedAtUtc: utcInstant('2026-04-12T22:00:00Z'), isCompleted: true, status: 'Completed' }),
                elapsedMs: hours(HOURS_16),
            });

            expect(state.progressPercent).toBe(PROGRESS_FULL);
            expect(state.elapsedFormatted).toBe('16:00:00');
            expect(state.remainingFormatted).toBe('00:00:00');
            expect(state.remainingLabelKey).toBe('FASTING.REMAINING');
        });
    });
}

function registerIntermittentProtocolTests(): void {
    it('keeps the second eating period of a custom intermittent fast one hour from its next fast', () => {
        const fastHours = 22;
        const elapsedToday = 23;
        const state = buildFastingTimerCardComputedState({
            session: createSession({
                protocol: 'CustomIntermittent',
                initialPlannedDurationHours: fastHours,
                plannedDurationHours: fastHours,
            }),
            elapsedMs: hours(HOURS_24 + elapsedToday),
        });

        expect(state.elapsedFormatted).toBe('01:00:00');
        expect(state.remainingFormatted).toBe('01:00:00');
        expect(state.remainingLabelKey).toBe('FASTING.NEXT_FAST');
        expect(state.stage).toMatchObject({ index: 2, total: 2, titleKey: 'FASTING.EATING_WINDOW' });
    });
}

function registerExtendedTests(): void {
    describe('extended sessions', () => {
        it('marks extended sessions as overtime after the planned duration', () => {
            const state = buildFastingTimerCardComputedState({
                session: createSession({
                    planType: 'Extended',
                    protocol: 'Fast24',
                    initialPlannedDurationHours: 24,
                    plannedDurationHours: 24,
                    occurrenceKind: 'FastDay',
                }),
                elapsedMs: hours(HOURS_30),
            });

            expect(state.progressPercent).toBe(PROGRESS_FULL);
            expect(state.elapsedFormatted).toBe('30:00:00');
            expect(state.remainingFormatted).toBe('00:00:00');
            expect(state.isOvertime).toBe(true);
            expect(state.stage?.titleKey).toBe('FASTING.STAGES.DEEP.TITLE');
            expect(state.nextStageFormatted).toBeNull();
        });

        it('uses the extended planned target after adding time', () => {
            const state = buildFastingTimerCardComputedState({
                session: createSession({
                    planType: 'Extended',
                    protocol: 'Custom',
                    initialPlannedDurationHours: HOURS_30,
                    addedDurationHours: 6,
                    plannedDurationHours: 36,
                }),
                elapsedMs: hours(HOURS_3),
            });

            expect(state.elapsedFormatted).toBe('03:00:00');
            expect(state.remainingFormatted).toBe('33:00:00');
            expect(state.isOvertime).toBe(false);
        });

        it('keeps timer progress for an older protocol identifier', () => {
            const state = buildFastingTimerCardComputedState({
                session: createSession({
                    planType: 'Extended',
                    protocol: 'Legacy48',
                    initialPlannedDurationHours: 48,
                    addedDurationHours: 0,
                    plannedDurationHours: 48,
                }),
                elapsedMs: hours(1),
            });

            expect(state.elapsedFormatted).toBe('01:00:00');
            expect(state.remainingFormatted).toBe('47:00:00');
            expect(state.isOvertime).toBe(false);
        });
    });
}

function registerExtendedBoundaryTests(): void {
    it.each([
        [HOURS_24, -1, '23:59:59', false],
        [HOURS_24, 0, '24:00:00', false],
        [HOURS_24, 1, '24:00:00', true],
        [HOURS_36, -1, '35:59:59', false],
        [HOURS_36, 0, '36:00:00', false],
        [HOURS_36, 1, '36:00:00', true],
    ] as const)('keeps a %ih extended timer bounded at target offset %ims', (targetHours, offsetMs, elapsed, overtime) => {
        const state = buildFastingTimerCardComputedState({
            session: createSession({
                planType: 'Extended',
                protocol: `Fast${targetHours}`,
                initialPlannedDurationHours: targetHours,
                plannedDurationHours: targetHours,
                occurrenceKind: 'FastDay',
            }),
            elapsedMs: hours(targetHours) + offsetMs,
        });

        expect(state.elapsedFormatted).toBe(elapsed);
        expect(state.remainingFormatted).toBe('00:00:00');
        expect(state.isOvertime).toBe(overtime);
        if (offsetMs < 0) {
            expect(state.progressPercent).toBeLessThan(PROGRESS_FULL);
        } else {
            expect(state.progressPercent).toBe(PROGRESS_FULL);
        }
    });
}

function registerIntermittentBoundaryTests(): void {
    it.each([
        [hours(HOURS_16) - 1, 'FASTING.UNTIL_EATING_WINDOW', { elapsed: '15:59:59', remaining: '00:00:00' }],
        [hours(HOURS_16), 'FASTING.NEXT_FAST', { elapsed: '00:00:00', remaining: '08:00:00' }],
        [hours(HOURS_16) + 1, 'FASTING.NEXT_FAST', { elapsed: '00:00:00', remaining: '07:59:59' }],
        [hours(HOURS_24) - 1, 'FASTING.NEXT_FAST', { elapsed: '07:59:59', remaining: '00:00:00' }],
        [hours(HOURS_24), 'FASTING.UNTIL_EATING_WINDOW', { elapsed: '00:00:00', remaining: '16:00:00' }],
        [hours(HOURS_24) + 1, 'FASTING.UNTIL_EATING_WINDOW', { elapsed: '00:00:00', remaining: '15:59:59' }],
    ] as const)('switches the 16:8 timer at elapsed %ims', (elapsedMs, remainingLabelKey, expected) => {
        const state = buildFastingTimerCardComputedState({ session: createSession(), elapsedMs });

        expect(state.remainingLabelKey).toBe(remainingLabelKey);
        expect(state.elapsedFormatted).toBe(expected.elapsed);
        expect(state.remainingFormatted).toBe(expected.remaining);
        expect(state.isOvertime).toBe(false);
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
            });

            expect(state.progressPercent).toBe(PROGRESS_12_5);
            expect(state.elapsedFormatted).toBe('00:30:00');
            expect(state.remainingFormatted).toBe('03:30:00');
            expect(state.remainingLabelKey).toBe('FASTING.REMAINING');
        });

        it('derives timer stage and remaining duration for cyclic fast-day sessions', () => {
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
            });

            expect(state.progressPercent).toBeCloseTo((HOURS_13 / HOURS_72) * PROGRESS_FULL);
            expect(state.remainingFormatted).toBe('59:00:00');
            expect(state.stage?.titleKey).toBe('FASTING.STAGES.STORED_ENERGY.TITLE');
            expect(state.nextStageFormatted).toBe('03:00:00');
        });
    });
}

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
        id: entityId<'fasting-session'>('session-1'),
        startedAtUtc: utcInstant('2026-04-12T06:00:00Z'),
        endedAtUtc: null,
        initialPlannedDurationHours: HOURS_16,
        addedDurationHours: 0,
        plannedDurationHours: HOURS_16,
        protocol: 'Fast16Eat8',
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
