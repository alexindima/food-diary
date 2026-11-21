import { describe, expect, it, onTestFinished, vi } from 'vitest';

import { createClientId } from './client-id.utils';

describe('createClientId', () => {
    it('prefixes generated ids', () => {
        expect(createClientId('temp')).toMatch(/^temp-/);
    });

    it('produces distinct temporary IDs when a browser does not expose randomUUID', () => {
        const firstSample = 0.5;
        const secondSample = 0.25;
        const random = vi.spyOn(Math, 'random').mockReturnValueOnce(firstSample).mockReturnValueOnce(secondSample);
        vi.stubGlobal('crypto', {});
        onTestFinished(() => {
            random.mockRestore();
            vi.unstubAllGlobals();
        });

        const first = createClientId('temp');
        const second = createClientId('temp');

        expect(first).toMatch(/^temp-[a-z0-9]+$/);
        expect(second).toMatch(/^temp-[a-z0-9]+$/);
        expect(second).not.toBe(first);
    });
});
