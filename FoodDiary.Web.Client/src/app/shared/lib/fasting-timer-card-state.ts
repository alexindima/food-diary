import type { FastingSession } from '../models/fasting.data';
import { type FastingStagePresentation, resolveFastingStage } from './fasting-stage';
import { PERCENT_MULTIPLIER } from './nutrition.constants';
import { HOURS_PER_DAY, MINUTES_PER_HOUR, MS_PER_HOUR, MS_PER_SECOND, SECONDS_PER_HOUR, SECONDS_PER_MINUTE } from './time.constants';

const TIME_PAD_LENGTH = 2;

export type FastingTimerCardComputedState = {
    progressPercent: number;
    elapsedFormatted: string;
    remainingFormatted: string;
    remainingLabelKey: string;
    isOvertime: boolean;
    showStageProgress: boolean;
    ringColor: string | null;
    stage: FastingStagePresentation | null;
    nextStageFormatted: string | null;
};

export type FastingTimerCardComputedStateInput = {
    session: FastingSession | null;
    elapsedMs: number;
};

export function buildFastingTimerCardComputedState(input: FastingTimerCardComputedStateInput): FastingTimerCardComputedState {
    const { session, elapsedMs } = input;
    const baseStage = session !== null ? resolveFastingStage(elapsedMs, session.plannedDurationHours) : null;
    const totalMs = Math.max(0, (session?.plannedDurationHours ?? 0) * MS_PER_HOUR);
    const remainingMs = Math.max(0, totalMs - elapsedMs);
    const fallback = buildFastingFallbackState({ elapsedMs, baseStage, totalMs, remainingMs });

    if (!shouldUseIntermittentCycleState(session)) {
        return fallback;
    }

    return buildIntermittentCycleState({ session, elapsedMs });
}

type FastingFallbackStateInput = {
    elapsedMs: number;
    baseStage: FastingStagePresentation | null;
    totalMs: number;
    remainingMs: number;
};

function buildFastingFallbackState(input: FastingFallbackStateInput): FastingTimerCardComputedState {
    const { elapsedMs, baseStage, totalMs, remainingMs } = input;
    return {
        progressPercent: totalMs > 0 ? Math.min((elapsedMs / totalMs) * PERCENT_MULTIPLIER, PERCENT_MULTIPLIER) : 0,
        elapsedFormatted: formatFastingDuration(elapsedMs),
        remainingFormatted: formatFastingDuration(remainingMs),
        remainingLabelKey: 'FASTING.REMAINING',
        isOvertime: totalMs > 0 && elapsedMs > totalMs,
        showStageProgress: true,
        ringColor: baseStage?.color ?? null,
        stage: baseStage,
        nextStageFormatted: formatNextStageDuration(baseStage),
    };
}

function formatNextStageDuration(stage: FastingStagePresentation | null): string | null {
    return stage?.nextInMs !== null && stage?.nextInMs !== undefined ? formatFastingDuration(stage.nextInMs) : null;
}

function shouldUseIntermittentCycleState(session: FastingSession | null): session is FastingSession {
    return session?.planType === 'Intermittent' && session.endedAtUtc === null;
}

function buildIntermittentCycleState(
    input: FastingTimerCardComputedStateInput & { session: FastingSession },
): FastingTimerCardComputedState {
    const { session, elapsedMs } = input;
    const fastHours = Math.max(1, session.initialPlannedDurationHours);
    const eatingHours = Math.max(1, HOURS_PER_DAY - fastHours);
    const cycleLengthMs = (fastHours + eatingHours) * MS_PER_HOUR;
    const cycleDay = Math.floor(elapsedMs / cycleLengthMs) + 1;
    const cycleElapsedMs = elapsedMs % cycleLengthMs;
    const fastWindowMs = fastHours * MS_PER_HOUR;
    const eatingWindowMs = eatingHours * MS_PER_HOUR;

    return cycleElapsedMs < fastWindowMs
        ? buildIntermittentFastWindowState({ elapsedMs: cycleElapsedMs, fastWindowMs, fastHours })
        : buildIntermittentEatingWindowState({ elapsedMs: cycleElapsedMs - fastWindowMs, cycleDay, eatingWindowMs });
}

function buildIntermittentFastWindowState(input: {
    elapsedMs: number;
    fastWindowMs: number;
    fastHours: number;
}): FastingTimerCardComputedState {
    const { elapsedMs, fastWindowMs, fastHours } = input;
    const stage = resolveFastingStage(elapsedMs, fastHours);
    return {
        progressPercent: Math.min((elapsedMs / fastWindowMs) * PERCENT_MULTIPLIER, PERCENT_MULTIPLIER),
        elapsedFormatted: formatFastingDuration(elapsedMs),
        remainingFormatted: formatFastingDuration(Math.max(0, fastWindowMs - elapsedMs)),
        remainingLabelKey: 'FASTING.UNTIL_EATING_WINDOW',
        isOvertime: false,
        showStageProgress: true,
        ringColor: stage.color,
        stage,
        nextStageFormatted: stage.nextInMs !== null ? formatFastingDuration(stage.nextInMs) : null,
    };
}

function buildIntermittentEatingWindowState(input: {
    elapsedMs: number;
    cycleDay: number;
    eatingWindowMs: number;
}): FastingTimerCardComputedState {
    const { elapsedMs, cycleDay, eatingWindowMs } = input;
    return {
        progressPercent: Math.min((elapsedMs / eatingWindowMs) * PERCENT_MULTIPLIER, PERCENT_MULTIPLIER),
        elapsedFormatted: formatFastingDuration(elapsedMs),
        remainingFormatted: formatFastingDuration(Math.max(0, eatingWindowMs - elapsedMs)),
        remainingLabelKey: 'FASTING.NEXT_FAST',
        isOvertime: false,
        showStageProgress: false,
        ringColor: 'var(--fd-color-green-500)',
        stage: {
            index: cycleDay,
            total: cycleDay,
            titleKey: 'FASTING.EATING_WINDOW',
            descriptionKey: 'FASTING.EATING_WINDOW_DESCRIPTION',
            color: 'var(--fd-color-green-500)',
            glowColor: 'color-mix(in srgb, var(--fd-color-green-500) 18%, transparent)',
            nextTitleKey: null,
            nextInMs: null,
        },
        nextStageFormatted: null,
    };
}

export function formatFastingDuration(ms: number): string {
    const totalSeconds = Math.floor(ms / MS_PER_SECOND);
    const hours = Math.floor(totalSeconds / SECONDS_PER_HOUR);
    const minutes = Math.floor((totalSeconds % SECONDS_PER_HOUR) / MINUTES_PER_HOUR);
    const seconds = totalSeconds % SECONDS_PER_MINUTE;
    return `${String(hours).padStart(TIME_PAD_LENGTH, '0')}:${String(minutes).padStart(TIME_PAD_LENGTH, '0')}:${String(seconds).padStart(TIME_PAD_LENGTH, '0')}`;
}
