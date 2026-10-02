import { formatDateInputValue } from '../../../shared/lib/local-date.utils';
import type { CycleFactor } from '../models/cycle.data';
import { toCycleDateKey } from './cycle-tracking.mapper';

export type CycleFactorStatus = {
    statusLabelKey: string;
    isActive: boolean;
    canEndToday: boolean;
};

export function getCycleFactorStatus(
    factor: Pick<CycleFactor, 'startDate' | 'endDate'>,
    today = formatDateInputValue(new Date()),
): CycleFactorStatus {
    const startDate = toCycleDateKey(factor.startDate);
    const endDate = factor.endDate === null || factor.endDate === undefined ? null : toCycleDateKey(factor.endDate);
    if (startDate > today) {
        return { statusLabelKey: 'CYCLE_TRACKING.FACTOR_PLANNED', isActive: false, canEndToday: false };
    }
    if (endDate !== null && endDate < today) {
        return { statusLabelKey: 'CYCLE_TRACKING.FACTOR_ENDED', isActive: false, canEndToday: false };
    }
    if (endDate === today) {
        return { statusLabelKey: 'CYCLE_TRACKING.FACTOR_ENDS_TODAY', isActive: true, canEndToday: false };
    }
    return { statusLabelKey: 'CYCLE_TRACKING.FACTOR_ACTIVE', isActive: true, canEndToday: true };
}
