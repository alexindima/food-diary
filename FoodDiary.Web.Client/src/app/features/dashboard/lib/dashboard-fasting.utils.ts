import { FASTING_PROTOCOLS, type FastingOccurrenceKind, type FastingSession } from '../../fasting/models/fasting.data';

type TranslateFn = (key: string, params?: Record<string, unknown>) => string;

export function getDashboardFastingOccurrenceLabel(translate: TranslateFn, kind?: FastingOccurrenceKind | null): string | null {
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

export function getDashboardFastingProtocolBaseLabel(translate: TranslateFn, session: FastingSession): string {
    if (session.planType === 'Cyclic') {
        const cycleLabel = session.cyclicFastDays && session.cyclicEatDays ? `${session.cyclicFastDays}:${session.cyclicEatDays}` : '1:1';
        const eatWindowHours = session.cyclicEatDayEatingWindowHours ?? 8;
        const eatFastHours = session.cyclicEatDayFastHours ?? 16;
        return `${cycleLabel} (${eatFastHours}:${eatWindowHours})`;
    }

    const option = FASTING_PROTOCOLS.find(item => item.value === session.protocol);
    const hoursLabel = translate('FASTING.HOURS');

    if (!option) {
        return formatDashboardFastingHours(session.initialPlannedDurationHours, session.addedDurationHours, hoursLabel);
    }

    if (option.value === 'CustomIntermittent') {
        return getDashboardIntermittentRatioLabel(session.initialPlannedDurationHours);
    }

    const baseLabel = option.value === 'Custom' ? `${session.initialPlannedDurationHours} ${hoursLabel}` : translate(option.labelKey);

    return session.addedDurationHours > 0 ? `${baseLabel} (+${session.addedDurationHours} ${hoursLabel})` : baseLabel;
}

export function getDashboardCyclicPhaseProgressLabel(translate: TranslateFn, session: FastingSession): string | null {
    const dayNumber = session.cyclicPhaseDayNumber;
    const dayTotal = session.cyclicPhaseDayTotal;
    if (!dayNumber || !dayTotal) {
        return getDashboardFastingOccurrenceLabel(translate, session.occurrenceKind);
    }

    const key = session.occurrenceKind === 'EatDay' ? 'FASTING.CYCLIC_EAT_PHASE_PROGRESS' : 'FASTING.CYCLIC_FAST_PHASE_PROGRESS';
    return translate(key, { current: dayNumber, total: dayTotal });
}

export function getDashboardFastingCycleLabel(translate: TranslateFn, cycleDay: number | null): string | null {
    return cycleDay ? translate('FASTING.DAY_LABEL', { day: cycleDay }) : null;
}

export function formatDashboardFastingDuration(ms: number): string {
    const totalSeconds = Math.floor(ms / 1000);
    const hours = Math.floor(totalSeconds / 3600);
    const minutes = Math.floor((totalSeconds % 3600) / 60);
    const seconds = totalSeconds % 60;
    return `${String(hours).padStart(2, '0')}:${String(minutes).padStart(2, '0')}:${String(seconds).padStart(2, '0')}`;
}

function formatDashboardFastingHours(baseHours: number, addedHours: number, hoursLabel: string): string {
    return addedHours > 0 ? `${baseHours} ${hoursLabel} (+${addedHours} ${hoursLabel})` : `${baseHours} ${hoursLabel}`;
}

function getDashboardIntermittentRatioLabel(fastHours: number): string {
    const normalizedFastHours = Math.max(1, Math.min(23, fastHours));
    const eatingWindowHours = Math.max(1, 24 - normalizedFastHours);
    return `${normalizedFastHours}:${eatingWindowHours}`;
}
