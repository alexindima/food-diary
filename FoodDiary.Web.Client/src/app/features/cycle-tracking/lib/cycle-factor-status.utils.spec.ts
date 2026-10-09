import { describe, expect, it } from 'vitest';

import { calendarDate, optionalCalendarDate } from '../../../shared/models/semantics/date-value';
import { getCycleFactorStatus } from './cycle-factor-status.utils';

describe('cycle factor date status', () => {
    it.each([
        ['2026-04-03', null, 'PLANNED', false, false],
        ['2026-04-03', '2026-04-05', 'PLANNED', false, false],
        ['2026-04-02', null, 'ACTIVE', true, true],
        ['2026-04-01', undefined, 'ACTIVE', true, true],
        ['2026-04-01', '2026-04-03', 'ACTIVE', true, true],
        ['2026-04-01', '2026-04-02', 'ENDS_TODAY', true, false],
        ['2026-04-02', '2026-04-02', 'ENDS_TODAY', true, false],
        ['2026-04-01', '2026-04-01', 'ENDED', false, false],
    ] as const)('resolves %s to %s on the current calendar day', (...values) => {
        const [startDate, endDate, label, isActive, canEndToday] = values;
        expect(getCycleFactorStatus({ startDate: calendarDate(startDate), endDate: optionalCalendarDate(endDate) }, '2026-04-02')).toEqual({
            statusLabelKey: `CYCLE_TRACKING.FACTOR_${label}`,
            isActive,
            canEndToday,
        });
    });

    it('compares calendar dates without shifting timestamp offsets', () => {
        expect(
            getCycleFactorStatus(
                { startDate: calendarDate('2026-04-02T00:00:00+14:00'), endDate: calendarDate('2026-04-02T23:00:00-12:00') },
                '2026-04-02',
            ),
        ).toEqual({ statusLabelKey: 'CYCLE_TRACKING.FACTOR_ENDS_TODAY', isActive: true, canEndToday: false });
    });
});
