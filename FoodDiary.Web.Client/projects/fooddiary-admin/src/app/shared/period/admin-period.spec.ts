import { convertToParamMap } from '@angular/router';
import { describe, expect, it } from 'vitest';

import { adminExclusiveDatePeriod, adminPeriod, adminUtcPeriod } from './admin-period';

describe('Admin calendar period', () => {
    const now = new Date('2026-09-08T10:00:00Z');
    it('adapts inclusive calendar dates to the existing AI exclusive-date API', () => {
        expect(adminExclusiveDatePeriod({ from: '2026-09-08', to: '2026-09-08' }, now)).toEqual({ from: '2026-09-08', to: '2026-09-09' });
        expect(adminExclusiveDatePeriod({}, now)).toEqual({ from: '1970-01-01', to: '2026-09-09' });
    });
    it('rejects normalized impossible dates and reversed or future ranges', () => {
        for (const [from, to] of [
            ['2026-02-30', '2026-03-01'],
            ['2026-09-08', '2026-09-07'],
            ['2026-09-08', '2026-09-09'],
        ]) {
            expect(adminPeriod(convertToParamMap({ period: 'custom', from, to }), 'all', now)).toBeNull();
        }
    });
    it('uses seven calendar days including today', () => {
        expect(adminPeriod(convertToParamMap({ period: '7d' }), 'all', now)).toEqual({ from: '2026-09-02', to: '2026-09-08' });
    });
    it('includes the final selected day through an exclusive UTC midnight boundary', () => {
        expect(adminUtcPeriod({ from: '2026-03-28', to: '2026-03-29' })).toEqual({
            fromUtc: '2026-03-28T00:00:00Z',
            toUtc: '2026-03-30T00:00:00.000Z',
        });
    });
    it('does not impose a date filter for all-time records', () => {
        expect(adminPeriod(convertToParamMap({ period: 'all' }), 'all', now)).toEqual({});
    });
});
