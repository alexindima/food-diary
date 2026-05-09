import { describe, expect, it } from 'vitest';

import { type FastingSession } from '../models/fasting.data';
import { buildFastingTimerCardComputedState } from './fasting-timer-card-state';

const translate = (key: string, params?: Record<string, unknown>): string => (params ? `${key}:${JSON.stringify(params)}` : key);

describe('buildFastingTimerCardComputedState', () => {
    it('builds intermittent fasting-window state from cycle elapsed time', () => {
        const state = buildFastingTimerCardComputedState({
            session: createSession({ planType: 'Intermittent', protocol: 'F16_8', initialPlannedDurationHours: 16 }),
            elapsedMs: 2 * 3_600_000,
            translate,
        });

        expect(state.progressPercent).toBe(12.5);
        expect(state.elapsedFormatted).toBe('02:00:00');
        expect(state.remainingFormatted).toBe('14:00:00');
        expect(state.remainingLabelKey).toBe('FASTING.UNTIL_EATING_WINDOW');
        expect(state.isOvertime).toBe(false);
        expect(state.showStageProgress).toBe(true);
        expect(state.stateLabel).toBe('FASTING.FASTING_WINDOW');
        expect(state.detailLabel).toBe('FASTING.PROTOCOL_16_8');
        expect(state.metaLabel).toBe('FASTING.DAY_LABEL:{"day":1}');
    });

    it('builds intermittent eating-window state without fasting stage progress', () => {
        const state = buildFastingTimerCardComputedState({
            session: createSession({ planType: 'Intermittent', protocol: 'F16_8', initialPlannedDurationHours: 16 }),
            elapsedMs: 18 * 3_600_000,
            translate,
        });

        expect(state.progressPercent).toBe(25);
        expect(state.elapsedFormatted).toBe('02:00:00');
        expect(state.remainingFormatted).toBe('06:00:00');
        expect(state.remainingLabelKey).toBe('FASTING.NEXT_FAST');
        expect(state.isOvertime).toBe(false);
        expect(state.showStageProgress).toBe(false);
        expect(state.stateLabel).toBe('FASTING.EATING_WINDOW');
        expect(state.ringColor).toBe('var(--fd-color-green-500)');
    });

    it('derives timer state for cyclic sessions', () => {
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
            elapsedMs: 30 * 60_000,
            translate,
        });

        expect(state.progressPercent).toBe(12.5);
        expect(state.elapsedFormatted).toBe('00:30:00');
        expect(state.remainingFormatted).toBe('03:30:00');
        expect(state.remainingLabelKey).toBe('FASTING.REMAINING');
        expect(state.stateLabel).toBe('FASTING.EAT_DAY');
        expect(state.detailLabel).toBe('2:1 (20:4)');
        expect(state.metaLabel).toBe('FASTING.CYCLIC_EAT_PHASE_DAY_PROGRESS:{"current":1,"total":1}');
    });
});

function createSession(overrides: Partial<FastingSession> = {}): FastingSession {
    return {
        id: 'session-1',
        startedAtUtc: '2026-04-12T06:00:00Z',
        endedAtUtc: null,
        initialPlannedDurationHours: 16,
        addedDurationHours: 0,
        plannedDurationHours: 16,
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
