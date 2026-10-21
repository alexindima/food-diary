import { TestBed } from '@angular/core/testing';
import { TranslateService } from '@ngx-translate/core';
import { FdTourService } from 'fd-tour';
import { describe, expect, it, vi } from 'vitest';

import { LocalizedTourDefinitionService } from './localized-tour-definition.service';

describe('LocalizedTourDefinitionService responsive header targets', () => {
    it.each([false, true])('targets the visible header control when overflow is %s', overflow => {
        TestBed.configureTestingModule({ providers: [{ provide: TranslateService, useValue: { instant: (key: string): string => key } }] });
        const host = document.createElement('fd-header-actions-overflow');
        host.innerHTML =
            '<div><button data-tour-id="page-help">Help</button></div><button class="fd-header-actions-overflow__trigger">More</button>';
        document.body.append(host);
        const original = host.querySelector<HTMLElement>('[data-tour-id]');
        const fallback = host.querySelector<HTMLElement>('.fd-header-actions-overflow__trigger');
        if (original === null || fallback === null) {
            throw new Error('Expected responsive header targets');
        }
        const parent = original.parentElement;
        if (parent === null) {
            throw new Error('Expected the original header actions container');
        }
        parent.style.visibility = overflow ? 'hidden' : 'visible';
        fallback.style.display = overflow ? 'block' : 'none';
        const originalScroll = vi.fn();
        const fallbackScroll = vi.fn();
        Object.defineProperty(original, 'scrollIntoView', { configurable: true, value: originalScroll });
        Object.defineProperty(fallback, 'scrollIntoView', { configurable: true, value: fallbackScroll });
        const tour = TestBed.inject(FdTourService);
        try {
            const definition = TestBed.inject(LocalizedTourDefinitionService).build({
                id: 'responsive-page',
                translationRoot: 'PAGE.TOUR',
                steps: [{ id: 'help', target: 'page-help', titleKey: 'HELP', descriptionKey: 'HELP_TEXT', placement: 'bottom' }],
            });
            expect(tour.start(definition, { force: true })).toBe(true);
            expect(originalScroll).toHaveBeenCalledTimes(overflow ? 0 : 1);
            expect(fallbackScroll).toHaveBeenCalledTimes(overflow ? 1 : 0);
        } finally {
            tour.close();
            host.remove();
        }
    });
});
