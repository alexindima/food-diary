import { describe, expect, it } from 'vitest';

import type { CycleDay, CycleResponse } from '../../models/cycle.data';
import { DEFAULT_DAY_ACCENT_COLOR, PERIOD_DAY_ACCENT_COLOR } from './cycle-tracking-page.config';
import { buildCycleCurrentView, buildCycleDayItems, buildCyclePredictionView } from './cycle-tracking-page.mapper';

const CYCLE: CycleResponse = {
    id: 'cycle-1',
    userId: 'user-1',
    startDate: '2026-04-01T00:00:00.000Z',
    averageLength: 28,
    lutealLength: 14,
    days: [],
    predictions: null,
};

const PERIOD_DAY: CycleDay = {
    id: 'day-1',
    cycleId: 'cycle-1',
    date: '2026-04-02T00:00:00.000Z',
    isPeriod: true,
    symptoms: {
        pain: 5,
        mood: 3,
        edema: 1,
        headache: 2,
        energy: 4,
        sleepQuality: 6,
        libido: 2,
    },
    notes: 'note',
};

describe('cycle tracking page mapper', () => {
    it('builds current cycle view with formatted start date', () => {
        const view = buildCycleCurrentView(CYCLE, 'en-US');

        expect(view).toEqual({
            cycle: CYCLE,
            startDateLabel: 'Apr 1, 2026',
        });
    });

    it('returns null when there is no current cycle', () => {
        expect(buildCycleCurrentView(null, 'en-US')).toBeNull();
    });

    it('builds prediction labels using UTC dates', () => {
        const view = buildCyclePredictionView(
            {
                nextPeriodStart: '2026-04-29T23:00:00.000Z',
                ovulationDate: '2026-04-15T00:00:00.000Z',
                pmsStart: null,
            },
            'en-US',
        );

        expect(view).toEqual({
            prediction: {
                nextPeriodStart: '2026-04-29T23:00:00.000Z',
                ovulationDate: '2026-04-15T00:00:00.000Z',
                pmsStart: null,
            },
            nextPeriodStartLabel: 'Apr 29',
            ovulationDateLabel: 'Apr 15',
            pmsStartLabel: '',
        });
    });

    it('preserves invalid date values for diagnostics', () => {
        const view = buildCyclePredictionView(
            {
                nextPeriodStart: 'not-a-date',
                ovulationDate: '',
                pmsStart: undefined,
            },
            'en-US',
        );

        expect(view?.nextPeriodStartLabel).toBe('not-a-date');
        expect(view?.ovulationDateLabel).toBe('');
        expect(view?.pmsStartLabel).toBe('');
    });

    it('builds day item styling and badges', () => {
        const nonPeriodDay = { ...PERIOD_DAY, id: 'day-2', isPeriod: false };

        const items = buildCycleDayItems([PERIOD_DAY, nonPeriodDay], 'en-US');

        expect(items[0]).toMatchObject({
            dateLabel: 'Apr 2, 2026',
            accentColor: PERIOD_DAY_ACCENT_COLOR,
            badgeLabelKey: 'CYCLE_TRACKING.BADGE_PERIOD',
        });
        expect(items[1]).toMatchObject({
            accentColor: DEFAULT_DAY_ACCENT_COLOR,
            badgeLabelKey: 'CYCLE_TRACKING.BADGE_FOLLICULAR',
        });
    });
});
