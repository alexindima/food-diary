import { describe, expect, it, vi } from 'vitest';

import { scrollElementIntoView } from './scroll-element-into-view';

describe('scrollElementIntoView', () => {
    it.each([
        { device: 'embedded browser without media queries', hasMatchMedia: false, behavior: 'smooth' },
        { device: 'browser with reduced motion enabled', hasMatchMedia: true, behavior: 'instant' },
    ])('uses $behavior scrolling in a $device', ({ hasMatchMedia, behavior }) => {
        const scroll = vi.fn<HTMLElement['scrollIntoView']>();
        const queryMedia = vi.fn().mockReturnValue({ matches: true });
        const heading = document.createElement('h2');
        Object.defineProperty(heading, 'scrollIntoView', { value: scroll });
        vi.stubGlobal('matchMedia', hasMatchMedia ? queryMedia : undefined);

        try {
            scrollElementIntoView(heading);

            expect(scroll).toHaveBeenCalledExactlyOnceWith({ behavior, block: 'start' });
            if (hasMatchMedia) {
                expect(queryMedia).toHaveBeenCalledExactlyOnceWith('(prefers-reduced-motion: reduce)');
            }
        } finally {
            vi.unstubAllGlobals();
        }
    });
});
