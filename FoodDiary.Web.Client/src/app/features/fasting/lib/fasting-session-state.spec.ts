import { describe, expect, it } from 'vitest';

import { MS_PER_HOUR } from '../../../shared/lib/time.constants';
import type { FastingSession } from '../models/fasting.data';
import {
    calculateFastingElapsedMs,
    calculateFastingProgressPercent,
    calculateMaxReducibleFastingHours,
    getFastingSessionDurationHours,
} from './fasting-session-state';

const HOURS_2 = 2;
const HOURS_4 = 4;
const HOURS_12 = 12;
const HOURS_16 = 16;
const HOURS_20 = 20;
const PROGRESS_25 = 25;
const PROGRESS_FULL = 100;
const ROUNDED_HOURS_2_3 = 2.3;

describe('fasting session state', () => {
    it('calculates active and completed elapsed time and clamps invalid ranges', () => {
        const session = createSession();

        expect(calculateFastingElapsedMs(session, new Date('2026-04-12T10:00:00Z'))).toBe(HOURS_4 * MS_PER_HOUR);
        expect(calculateFastingElapsedMs({ ...session, endedAtUtc: '2026-04-12T08:00:00Z' }, new Date('2026-04-12T12:00:00Z'))).toBe(
            HOURS_2 * MS_PER_HOUR,
        );
        expect(calculateFastingElapsedMs({ ...session, startedAtUtc: 'invalid' }, new Date())).toBe(0);
        expect(calculateFastingElapsedMs(null, new Date())).toBe(0);
    });

    it('calculates bounded progress', () => {
        expect(calculateFastingProgressPercent(HOURS_4 * MS_PER_HOUR, HOURS_16 * MS_PER_HOUR)).toBe(PROGRESS_25);
        expect(calculateFastingProgressPercent(HOURS_20 * MS_PER_HOUR, HOURS_16 * MS_PER_HOUR)).toBe(PROGRESS_FULL);
        expect(calculateFastingProgressPercent(HOURS_4 * MS_PER_HOUR, 0)).toBe(0);
    });

    it('limits reduction by remaining time and minimum duration', () => {
        const session = createSession();

        expect(calculateMaxReducibleFastingHours(session, HOURS_4 * MS_PER_HOUR, HOURS_16 * MS_PER_HOUR)).toBe(HOURS_12);
        expect(calculateMaxReducibleFastingHours(session, HOURS_20 * MS_PER_HOUR, HOURS_16 * MS_PER_HOUR)).toBe(0);
        expect(calculateMaxReducibleFastingHours({ ...session, endedAtUtc: session.startedAtUtc }, 0, 0)).toBe(0);
        expect(calculateMaxReducibleFastingHours(null, 0, 0)).toBe(0);
    });

    it('reports rounded completed duration and rejects active or invalid sessions', () => {
        expect(getFastingSessionDurationHours({ ...createSession(), endedAtUtc: '2026-04-12T08:15:00Z' })).toBe(ROUNDED_HOURS_2_3);
        expect(getFastingSessionDurationHours(createSession())).toBe(0);
        expect(getFastingSessionDurationHours({ ...createSession(), endedAtUtc: 'invalid' })).toBe(0);
    });
});

function createSession(): FastingSession {
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
    };
}
