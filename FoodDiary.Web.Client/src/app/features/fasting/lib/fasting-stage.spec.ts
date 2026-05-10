import { describe, expect, it } from 'vitest';

import { resolveFastingStage } from './fasting-stage';

const hours = (value: number): number => value * 3_600_000;

describe('resolveFastingStage', () => {
    it.each([
        {
            elapsedHours: 0,
            plannedHours: 16,
            expectedIndex: 1,
            expectedTotal: 3,
            expectedTitleKey: 'FASTING.STAGES.EARLY.TITLE',
            expectedNextTitleKey: 'FASTING.STAGES.TRANSITION.TITLE',
            expectedNextIn: '04:00:00',
        },
        {
            elapsedHours: 4,
            plannedHours: 16,
            expectedIndex: 2,
            expectedTotal: 3,
            expectedTitleKey: 'FASTING.STAGES.TRANSITION.TITLE',
            expectedNextTitleKey: 'FASTING.STAGES.STORED_ENERGY.TITLE',
            expectedNextIn: '08:00:00',
        },
        {
            elapsedHours: 12,
            plannedHours: 16,
            expectedIndex: 3,
            expectedTotal: 3,
            expectedTitleKey: 'FASTING.STAGES.STORED_ENERGY.TITLE',
            expectedNextTitleKey: null,
            expectedNextIn: null,
        },
        {
            elapsedHours: 16,
            plannedHours: 24,
            expectedIndex: 4,
            expectedTotal: 4,
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
        const stage = resolveFastingStage(hours(10), 12);

        expect(stage.index).toBe(2);
        expect(stage.total).toBe(2);
        expect(stage.titleKey).toBe('FASTING.STAGES.TRANSITION.TITLE');
        expect(stage.nextTitleKey).toBeNull();
    });

    it('falls back to the first stage for zero-length plans', () => {
        const stage = resolveFastingStage(hours(3), 0);

        expect(stage.index).toBe(1);
        expect(stage.total).toBe(1);
        expect(stage.titleKey).toBe('FASTING.STAGES.EARLY.TITLE');
        expect(stage.nextTitleKey).toBeNull();
        expect(stage.nextInMs).toBeNull();
    });

    it('clamps negative elapsed time to the first stage', () => {
        const stage = resolveFastingStage(-hours(2), 16);

        expect(stage.index).toBe(1);
        expect(stage.titleKey).toBe('FASTING.STAGES.EARLY.TITLE');
        expect(stage.nextInMs).toBe(hours(4));
    });
});

function formatMs(ms: number): string {
    const totalSeconds = Math.floor(ms / 1000);
    const hoursPart = Math.floor(totalSeconds / 3600);
    const minutesPart = Math.floor((totalSeconds % 3600) / 60);
    const secondsPart = totalSeconds % 60;

    return `${String(hoursPart).padStart(2, '0')}:${String(minutesPart).padStart(2, '0')}:${String(secondsPart).padStart(2, '0')}`;
}
