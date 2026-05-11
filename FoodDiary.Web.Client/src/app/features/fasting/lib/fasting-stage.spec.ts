import { describe, expect, it } from 'vitest';

import { resolveFastingStage } from './fasting-stage';

const MS_PER_HOUR = 3_600_000;
const MS_PER_SECOND = 1000;
const SECONDS_PER_HOUR = 3600;
const MINUTES_PER_HOUR = 60;
const SECONDS_PER_MINUTE = 60;
const PAD_LENGTH = 2;
const HOURS_3 = 3;
const HOURS_4 = 4;
const HOURS_10 = 10;
const HOURS_12 = 12;
const HOURS_16 = 16;
const HOURS_24 = 24;
const TOTAL_EXTENDED_STAGES = 4;

const hours = (value: number): number => value * MS_PER_HOUR;

describe('resolveFastingStage', () => {
    it.each([
        {
            elapsedHours: 0,
            plannedHours: HOURS_16,
            expectedIndex: 1,
            expectedTotal: HOURS_3,
            expectedTitleKey: 'FASTING.STAGES.EARLY.TITLE',
            expectedNextTitleKey: 'FASTING.STAGES.TRANSITION.TITLE',
            expectedNextIn: '04:00:00',
        },
        {
            elapsedHours: HOURS_4,
            plannedHours: HOURS_16,
            expectedIndex: 2,
            expectedTotal: HOURS_3,
            expectedTitleKey: 'FASTING.STAGES.TRANSITION.TITLE',
            expectedNextTitleKey: 'FASTING.STAGES.STORED_ENERGY.TITLE',
            expectedNextIn: '08:00:00',
        },
        {
            elapsedHours: HOURS_12,
            plannedHours: HOURS_16,
            expectedIndex: HOURS_3,
            expectedTotal: HOURS_3,
            expectedTitleKey: 'FASTING.STAGES.STORED_ENERGY.TITLE',
            expectedNextTitleKey: null,
            expectedNextIn: null,
        },
        {
            elapsedHours: HOURS_16,
            plannedHours: HOURS_24,
            expectedIndex: TOTAL_EXTENDED_STAGES,
            expectedTotal: TOTAL_EXTENDED_STAGES,
            expectedTitleKey: 'FASTING.STAGES.DEEP.TITLE',
            expectedNextTitleKey: null,
            expectedNextIn: null,
        },
    ])(
        'resolves elapsed fasting stage',
        ({ elapsedHours, plannedHours, expectedIndex, expectedTotal, expectedTitleKey, expectedNextTitleKey, expectedNextIn }) => {
            const stage = resolveFastingStage(hours(elapsedHours), plannedHours);

            expect(stage.index).toBe(expectedIndex);
            expect(stage.total).toBe(expectedTotal);
            expect(stage.titleKey).toBe(expectedTitleKey);
            expect(stage.nextTitleKey).toBe(expectedNextTitleKey);
            expect(stage.nextInMs === null ? null : formatMs(stage.nextInMs)).toBe(expectedNextIn);
        },
    );

    it('ignores stage definitions that start after the planned duration', () => {
        const stage = resolveFastingStage(hours(HOURS_10), HOURS_12);

        expect(stage.index).toBe(2);
        expect(stage.total).toBe(2);
        expect(stage.titleKey).toBe('FASTING.STAGES.TRANSITION.TITLE');
        expect(stage.nextTitleKey).toBeNull();
    });

    it('falls back to the first stage for zero-length plans', () => {
        const stage = resolveFastingStage(hours(HOURS_3), 0);

        expect(stage.index).toBe(1);
        expect(stage.total).toBe(1);
        expect(stage.titleKey).toBe('FASTING.STAGES.EARLY.TITLE');
        expect(stage.nextTitleKey).toBeNull();
        expect(stage.nextInMs).toBeNull();
    });

    it('clamps negative elapsed time to the first stage', () => {
        const stage = resolveFastingStage(-hours(2), HOURS_16);

        expect(stage.index).toBe(1);
        expect(stage.titleKey).toBe('FASTING.STAGES.EARLY.TITLE');
        expect(stage.nextInMs).toBe(hours(HOURS_4));
    });
});

function formatMs(ms: number): string {
    const totalSeconds = Math.floor(ms / MS_PER_SECOND);
    const hoursPart = Math.floor(totalSeconds / SECONDS_PER_HOUR);
    const minutesPart = Math.floor((totalSeconds % SECONDS_PER_HOUR) / MINUTES_PER_HOUR);
    const secondsPart = totalSeconds % SECONDS_PER_MINUTE;

    return `${String(hoursPart).padStart(PAD_LENGTH, '0')}:${String(minutesPart).padStart(PAD_LENGTH, '0')}:${String(secondsPart).padStart(PAD_LENGTH, '0')}`;
}
