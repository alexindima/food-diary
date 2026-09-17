import { HOURS_PER_DAY, MS_PER_HOUR } from '../../../../shared/lib/time.constants';
import { MAX_CYCLIC_DAYS, MAX_INTERMITTENT_FAST_HOURS } from '../../../fasting/lib/fasting.constants';
import type { FastingSession } from '../../../fasting/models/fasting.data';

const FULL_PERCENT = 100;
export type DashboardFastingTimeline = {
    intermittent: boolean;
    eating: boolean;
    fastHours: number;
    eatHours: number;
    position: number;
    boundary: number;
    start: Date | null;
    boundaryDate: Date | null;
    end: Date | null;
    current: Date | null;
};
export type DashboardFastingCycleDay = { day: number; labelKey: string; current: boolean; complete: boolean };

export function buildDashboardFastingTimeline(session: FastingSession | null, elapsedMs: number): DashboardFastingTimeline {
    if (session === null) {
        return {
            intermittent: false,
            eating: false,
            fastHours: 0,
            eatHours: 0,
            position: 0,
            boundary: 0,
            start: null,
            boundaryDate: null,
            end: null,
            current: null,
        };
    }
    const duration = Math.max(1, session.plannedDurationHours);
    const start = new Date(session.startedAtUtc).getTime();
    const intermittent = session.planType === 'Intermittent';
    const fastHours = Math.min(MAX_INTERMITTENT_FAST_HOURS, Math.max(1, session.initialPlannedDurationHours));
    const cycleMs = HOURS_PER_DAY * MS_PER_HOUR;
    const repeats = intermittent && session.endedAtUtc === null;
    const cycleElapsed = repeats ? elapsedMs % cycleMs : elapsedMs;
    const cycleStart = repeats ? start + Math.floor(elapsedMs / cycleMs) * cycleMs : start;
    const totalMs = (intermittent ? HOURS_PER_DAY : duration) * MS_PER_HOUR;
    return {
        intermittent,
        eating: isEatingPhase(session, repeats, cycleElapsed, fastHours),
        fastHours,
        eatHours: HOURS_PER_DAY - fastHours,
        position: Math.min(FULL_PERCENT, Math.max(0, (cycleElapsed / totalMs) * FULL_PERCENT)),
        boundary: (fastHours / HOURS_PER_DAY) * FULL_PERCENT,
        start: validDate(cycleStart),
        boundaryDate: validDate(cycleStart + fastHours * MS_PER_HOUR),
        end: validDate(cycleStart + totalMs),
        current: validDate(cycleStart + cycleElapsed),
    };
}
function validDate(ms: number): Date | null {
    return Number.isFinite(ms) ? new Date(ms) : null;
}
function isEatingPhase(session: FastingSession, repeats: boolean, elapsed: number, fastHours: number): boolean {
    return (
        session.occurrenceKind === 'EatDay' || session.occurrenceKind === 'EatingWindow' || (repeats && elapsed >= fastHours * MS_PER_HOUR)
    );
}
export function buildDashboardFastingCycle(session: FastingSession | null): DashboardFastingCycleDay[] {
    if (session?.planType !== 'Cyclic') {
        return [];
    }
    const fastDays = Math.min(MAX_CYCLIC_DAYS, Math.max(1, session.cyclicFastDays ?? 1));
    const eatDays = Math.min(MAX_CYCLIC_DAYS, Math.max(1, session.cyclicEatDays ?? 1));
    const isEating = session.occurrenceKind === 'EatDay' || session.occurrenceKind === 'EatingWindow';
    const phaseDay = Math.max(1, session.cyclicPhaseDayNumber ?? 1);
    const current = isEating ? fastDays + Math.min(eatDays, phaseDay) : Math.min(fastDays, phaseDay);
    return Array.from({ length: fastDays + eatDays }, (_, index) => ({
        day: index + 1,
        labelKey: index < fastDays ? 'FASTING.FAST_DAY' : 'FASTING.EAT_DAY',
        current: index + 1 === current,
        complete: index + 1 < current,
    }));
}
