import { FASTING_PROTOCOLS, type FastingOccurrenceKind, type FastingSession } from '../models/fasting.data';
import { type FastingStagePresentation, resolveFastingStage } from './fasting-stage';

type TranslateFn = (key: string, params?: Record<string, unknown>) => string;

export interface FastingTimerCardComputedState {
    progressPercent: number;
    elapsedFormatted: string;
    remainingFormatted: string;
    remainingLabelKey: string;
    isOvertime: boolean;
    showStageProgress: boolean;
    stateLabel: string | null;
    detailLabel: string | null;
    metaLabel: string | null;
    ringColor: string | null;
    stage: FastingStagePresentation | null;
    nextStageFormatted: string | null;
}

export interface FastingTimerCardComputedStateInput {
    session: FastingSession | null;
    elapsedMs: number;
    translate: TranslateFn;
}

export function buildFastingTimerCardComputedState(input: FastingTimerCardComputedStateInput): FastingTimerCardComputedState {
    const { session, elapsedMs, translate } = input;
    const baseStage = session ? resolveFastingStage(elapsedMs, session.plannedDurationHours) : null;
    const totalMs = Math.max(0, (session?.plannedDurationHours ?? 0) * 3_600_000);
    const remainingMs = Math.max(0, totalMs - elapsedMs);
    const fallback: FastingTimerCardComputedState = {
        progressPercent: totalMs > 0 ? Math.min((elapsedMs / totalMs) * 100, 100) : 0,
        elapsedFormatted: formatFastingDuration(elapsedMs),
        remainingFormatted: formatFastingDuration(remainingMs),
        remainingLabelKey: 'FASTING.REMAINING',
        isOvertime: totalMs > 0 && elapsedMs > totalMs,
        showStageProgress: true,
        stateLabel: getFastingOccurrenceLabel(translate, session?.occurrenceKind),
        detailLabel: session ? getFastingProtocolBaseLabel(translate, session) : null,
        metaLabel: session?.planType === 'Cyclic' ? getCyclicPhaseProgressLabel(translate, session, baseStage) : null,
        ringColor: baseStage?.color ?? null,
        stage: baseStage,
        nextStageFormatted: baseStage?.nextInMs ? formatFastingDuration(baseStage.nextInMs) : null,
    };

    if (session?.planType !== 'Intermittent' || session.endedAtUtc) {
        return fallback;
    }

    const fastHours = Math.max(1, session.initialPlannedDurationHours || session.plannedDurationHours);
    const eatingHours = Math.max(1, 24 - fastHours);
    const cycleLengthMs = (fastHours + eatingHours) * 3_600_000;
    const cycleDay = Math.floor(elapsedMs / cycleLengthMs) + 1;
    const cycleElapsedMs = elapsedMs % cycleLengthMs;
    const fastWindowMs = fastHours * 3_600_000;
    const eatingWindowMs = eatingHours * 3_600_000;

    if (cycleElapsedMs < fastWindowMs) {
        const stage = resolveFastingStage(cycleElapsedMs, fastHours);
        return {
            progressPercent: Math.min((cycleElapsedMs / fastWindowMs) * 100, 100),
            elapsedFormatted: formatFastingDuration(cycleElapsedMs),
            remainingFormatted: formatFastingDuration(Math.max(0, fastWindowMs - cycleElapsedMs)),
            remainingLabelKey: 'FASTING.UNTIL_EATING_WINDOW',
            isOvertime: false,
            showStageProgress: true,
            stateLabel: translate('FASTING.FASTING_WINDOW'),
            detailLabel: getFastingProtocolBaseLabel(translate, session),
            metaLabel: getFastingCycleLabel(translate, cycleDay),
            ringColor: stage.color,
            stage,
            nextStageFormatted: stage.nextInMs ? formatFastingDuration(stage.nextInMs) : null,
        };
    }

    const eatingElapsedMs = cycleElapsedMs - fastWindowMs;
    return {
        progressPercent: Math.min((eatingElapsedMs / eatingWindowMs) * 100, 100),
        elapsedFormatted: formatFastingDuration(eatingElapsedMs),
        remainingFormatted: formatFastingDuration(Math.max(0, eatingWindowMs - eatingElapsedMs)),
        remainingLabelKey: 'FASTING.NEXT_FAST',
        isOvertime: false,
        showStageProgress: false,
        stateLabel: translate('FASTING.EATING_WINDOW'),
        detailLabel: getFastingProtocolBaseLabel(translate, session),
        metaLabel: getFastingCycleLabel(translate, cycleDay),
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

export function getFastingOccurrenceLabel(translate: TranslateFn, kind?: FastingOccurrenceKind | null): string | null {
    switch (kind) {
        case 'FastDay':
            return translate('FASTING.FAST_DAY');
        case 'EatDay':
            return translate('FASTING.EAT_DAY');
        case 'FastingWindow':
            return translate('FASTING.FASTING_WINDOW');
        case 'EatingWindow':
            return translate('FASTING.EATING_WINDOW');
        default:
            return null;
    }
}

export function getFastingProtocolBaseLabel(translate: TranslateFn, session: FastingSession): string {
    if (session.planType === 'Cyclic') {
        const cycleLabel = session.cyclicFastDays && session.cyclicEatDays ? `${session.cyclicFastDays}:${session.cyclicEatDays}` : '1:1';
        const eatWindowHours = session.cyclicEatDayEatingWindowHours ?? 8;
        const eatFastHours = session.cyclicEatDayFastHours ?? 16;
        return `${cycleLabel} (${eatFastHours}:${eatWindowHours})`;
    }

    const option = FASTING_PROTOCOLS.find(item => item.value === session.protocol);
    const hoursLabel = translate('FASTING.HOURS');

    if (!option) {
        return formatFastingHours(session.initialPlannedDurationHours, session.addedDurationHours, hoursLabel);
    }

    if (option.value === 'CustomIntermittent') {
        return getIntermittentRatioLabel(session.initialPlannedDurationHours);
    }

    const baseLabel = option.value === 'Custom' ? `${session.initialPlannedDurationHours} ${hoursLabel}` : translate(option.labelKey);

    return session.addedDurationHours > 0 ? `${baseLabel} (+${session.addedDurationHours} ${hoursLabel})` : baseLabel;
}

export function getCyclicPhaseProgressLabel(
    translate: TranslateFn,
    session: FastingSession,
    stage: FastingStagePresentation | null = null,
): string | null {
    const dayNumber = session.cyclicPhaseDayNumber;
    const dayTotal = session.cyclicPhaseDayTotal;
    if (!dayNumber || !dayTotal) {
        return getFastingOccurrenceLabel(translate, session.occurrenceKind);
    }

    if (session.occurrenceKind === 'EatDay') {
        return translate('FASTING.CYCLIC_EAT_PHASE_DAY_PROGRESS', { current: dayNumber, total: dayTotal });
    }

    if (stage) {
        return translate('FASTING.CYCLIC_FAST_PHASE_STAGE_PROGRESS', {
            current: dayNumber,
            total: dayTotal,
            stage: stage.index,
            stageTotal: stage.total,
        });
    }

    return translate('FASTING.CYCLIC_FAST_PHASE_DAY_PROGRESS', { current: dayNumber, total: dayTotal });
}

export function getFastingCycleLabel(translate: TranslateFn, cycleDay: number | null): string | null {
    return cycleDay ? translate('FASTING.DAY_LABEL', { day: cycleDay }) : null;
}

export function formatFastingDuration(ms: number): string {
    const totalSeconds = Math.floor(ms / 1000);
    const hours = Math.floor(totalSeconds / 3600);
    const minutes = Math.floor((totalSeconds % 3600) / 60);
    const seconds = totalSeconds % 60;
    return `${String(hours).padStart(2, '0')}:${String(minutes).padStart(2, '0')}:${String(seconds).padStart(2, '0')}`;
}

function formatFastingHours(baseHours: number, addedHours: number, hoursLabel: string): string {
    return addedHours > 0 ? `${baseHours} ${hoursLabel} (+${addedHours} ${hoursLabel})` : `${baseHours} ${hoursLabel}`;
}

function getIntermittentRatioLabel(fastHours: number): string {
    const normalizedFastHours = Math.max(1, Math.min(23, fastHours));
    const eatingWindowHours = Math.max(1, 24 - normalizedFastHours);
    return `${normalizedFastHours}:${eatingWindowHours}`;
}
