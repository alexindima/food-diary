import { PERCENT_MULTIPLIER } from '../../../shared/lib/nutrition.constants';
import { MS_PER_HOUR } from '../../../shared/lib/time.constants';
import type { FastingSession } from '../models/fasting.data';
import { EMPTY_FASTING_DURATION_HOURS, MIN_FASTING_HOURS } from './fasting.constants';

const DURATION_ROUNDING_FACTOR = 10;

export function calculateFastingElapsedMs(session: FastingSession | null, now: Date): number {
    if (session === null) {
        return 0;
    }

    const start = new Date(session.startedAtUtc).getTime();
    const end = session.endedAtUtc !== null ? new Date(session.endedAtUtc).getTime() : now.getTime();
    if (Number.isNaN(start) || Number.isNaN(end)) {
        return 0;
    }

    return Math.max(0, end - start);
}

export function calculateFastingProgressPercent(elapsedMs: number, totalMs: number): number {
    if (totalMs <= 0) {
        return 0;
    }

    return Math.min((Math.max(0, elapsedMs) / totalMs) * PERCENT_MULTIPLIER, PERCENT_MULTIPLIER);
}

export function calculateMaxReducibleFastingHours(session: FastingSession | null, elapsedMs: number, totalMs: number): number {
    if (session?.endedAtUtc !== null) {
        return EMPTY_FASTING_DURATION_HOURS;
    }

    const maxByRemainingTime = Math.floor(Math.max(0, totalMs - elapsedMs) / MS_PER_HOUR);
    const maxByMinimumDuration = Math.max(EMPTY_FASTING_DURATION_HOURS, session.plannedDurationHours - MIN_FASTING_HOURS);
    return Math.min(maxByRemainingTime, maxByMinimumDuration);
}

export function getFastingSessionDurationHours(session: FastingSession): number {
    if (session.endedAtUtc === null) {
        return 0;
    }

    const elapsedMs = calculateFastingElapsedMs(session, new Date(session.endedAtUtc));
    return Math.round((elapsedMs / MS_PER_HOUR) * DURATION_ROUNDING_FACTOR) / DURATION_ROUNDING_FACTOR;
}
