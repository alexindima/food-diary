import { signal } from '@angular/core';
import { describe, expect, it } from 'vitest';

import { createImageFailureState } from './image-failure-state';

describe('image failure state', () => {
    it('retains failed URLs through unrelated updates with the same image sources', () => {
        const owner = signal({ image: '/cover.png', title: 'Before' });
        const failures = createImageFailureState(() => [owner().image]);
        failures.markFailed('/cover.png');
        owner.set({ image: '/cover.png', title: 'After' });
        expect(failures.unavailable().has('/cover.png')).toBe(true);
    });

    it('resets for replaced sources and ignores late failures from the previous image', () => {
        const source = signal('/before.png');
        const failures = createImageFailureState(() => [source()]);
        failures.markFailed('/before.png');
        source.set('/after.png');
        failures.markFailed('/before.png');
        expect(failures.unavailable().size).toBe(0);
        failures.markFailed('/after.png');
        expect(failures.unavailable().has('/after.png')).toBe(true);
        source.set('/before.png');
        expect(failures.unavailable().size).toBe(0);
    });

    it('handles collages and absent sources without retaining unknown URLs', () => {
        const failures = createImageFailureState(() => [null, undefined, '/one.png', '/two.png']);
        failures.markFailed(undefined);
        failures.markFailed('/unknown.png');
        failures.markFailed('/two.png');
        expect([...failures.unavailable()]).toEqual(['/two.png']);
    });
});
