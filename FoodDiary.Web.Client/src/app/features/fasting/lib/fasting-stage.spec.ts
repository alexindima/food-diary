import { describe, expect, it } from 'vitest';

import { resolveFastingStage } from './fasting-stage';

const hours = (value: number): number => value * 3_600_000;

describe('resolveFastingStage', () => {
    it.each([
        [0, 16, 1, 3, 'FASTING.STAGES.EARLY.TITLE', 'FASTING.STAGES.TRANSITION.TITLE', '04:00:00'],
        [4, 16, 2, 3, 'FASTING.STAGES.TRANSITION.TITLE', 'FASTING.STAGES.STORED_ENERGY.TITLE', '08:00:00'],
        [12, 16, 3, 3, 'FASTING.STAGES.STORED_ENERGY.TITLE', null, null],
        [16, 24, 4, 4, 'FASTING.STAGES.DEEP.TITLE', null, null],
    ] as const)(
        'resolves %i elapsed hours in a %i hour plan',
        (elapsedHours, plannedHours, expectedIndex, expectedTotal, expectedTitleKey, expectedNextTitleKey, expectedNextIn) => {
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
