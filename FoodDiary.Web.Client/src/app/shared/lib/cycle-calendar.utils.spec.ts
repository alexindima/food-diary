import { describe, expect, it } from 'vitest';

import {
    CYCLE_TRACKING_MODE_NO_PERIOD,
    CYCLE_TRACKING_MODE_PERIMENOPAUSE,
    CYCLE_TRACKING_MODE_PERIOD_TRACKING,
    CYCLE_TRACKING_MODE_POSTPARTUM_LACTATION,
    CYCLE_TRACKING_MODE_PREGNANCY,
    CYCLE_TRACKING_MODE_TRYING_TO_CONCEIVE,
    type CycleResponse,
    type MenstrualEpisode,
} from '../models/cycle.data';
import { calendarDate } from '../models/semantics/date-value';
import { entityId } from '../models/semantics/entity-id';
import { calculateCycleDayNumber, resolveCycleStartDateKey } from './cycle-calendar.utils';

const THIRD_DAY = 3;
const AVERAGE_CYCLE_LENGTH = 28;

describe('cycle calendar day', () => {
    it.each([CYCLE_TRACKING_MODE_PERIOD_TRACKING, CYCLE_TRACKING_MODE_TRYING_TO_CONCEIVE, CYCLE_TRACKING_MODE_PERIMENOPAUSE] as const)(
        'uses the latest non-future episode in period mode %s',
        mode => {
            const episodes = [episode('2026-10-16T00:00:00Z'), episode('2026-09-18'), episode('2026-10-20')];
            expect(resolveCycleStartDateKey({ mode, menstrualEpisodes: episodes }, '2026-10-18')).toBe('2026-10-16');
            expect(episodes.map(item => item.startDate)).toEqual(['2026-10-16T00:00:00Z', '2026-09-18', '2026-10-20']);
        },
    );

    it.each([CYCLE_TRACKING_MODE_PREGNANCY, CYCLE_TRACKING_MODE_POSTPARTUM_LACTATION, CYCLE_TRACKING_MODE_NO_PERIOD] as const)(
        'does not infer a menstrual day in non-period mode %s',
        mode => {
            expect(resolveCycleStartDateKey({ mode, menstrualEpisodes: [episode('2026-10-16')] }, '2026-10-18')).toBeNull();
        },
    );

    it('resolves the episode at the selected historical date and includes its start day', () => {
        const cycle: Pick<CycleResponse, 'mode' | 'menstrualEpisodes'> = {
            mode: CYCLE_TRACKING_MODE_PERIOD_TRACKING,
            menstrualEpisodes: [episode('2026-10-16'), episode('2026-09-18')],
        };
        expect(resolveCycleStartDateKey(cycle, '2026-10-15')).toBe('2026-09-18');
        expect(resolveCycleStartDateKey(cycle, '2026-10-16')).toBe('2026-10-16');
        expect(resolveCycleStartDateKey(cycle, '2026-09-17')).toBeNull();
    });

    it('does not invent a period start when episodes are missing or future only', () => {
        expect(resolveCycleStartDateKey({ mode: CYCLE_TRACKING_MODE_PERIOD_TRACKING }, '2026-10-18')).toBeNull();
        expect(
            resolveCycleStartDateKey(
                { mode: CYCLE_TRACKING_MODE_PERIOD_TRACKING, menstrualEpisodes: [episode('2026-10-19')] },
                '2026-10-18',
            ),
        ).toBeNull();
    });

    it.each([
        ['2026-10-16', '2026-10-18', THIRD_DAY],
        ['2026-10-16', '2026-10-16', 1],
        ['2026-12-31', '2027-01-01', 2],
        ['2028-02-28', '2028-03-01', THIRD_DAY],
        ['2026-03-07', '2026-03-09', THIRD_DAY],
        ['2026-10-16', '2026-11-13', 1],
        ['2026-10-16', '2026-10-15', AVERAGE_CYCLE_LENGTH],
    ])('preserves calendar-day and existing average-cycle wrapping from %s to %s', (start, reference, expected) => {
        expect(calculateCycleDayNumber(start, reference, AVERAGE_CYCLE_LENGTH)).toBe(expected);
    });

    it('keeps an unknown or invalid calendar day absent', () => {
        expect(calculateCycleDayNumber(null, '2026-10-18', AVERAGE_CYCLE_LENGTH)).toBeNull();
        expect(calculateCycleDayNumber('invalid', '2026-10-18', AVERAGE_CYCLE_LENGTH)).toBeNull();
        expect(calculateCycleDayNumber('2026-10-16', 'invalid', AVERAGE_CYCLE_LENGTH)).toBeNull();
    });
});

function episode(start: string): MenstrualEpisode {
    return {
        id: entityId<'menstrual-episode'>(`episode-${start}`),
        cycleProfileId: entityId<'cycle-profile'>('cycle-calendar-test'),
        startDate: calendarDate(start),
        endDate: null,
        status: 1,
        excludedFromPredictions: false,
    };
}
