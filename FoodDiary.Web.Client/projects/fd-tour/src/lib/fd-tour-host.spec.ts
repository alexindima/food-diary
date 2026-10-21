import { TestBed } from '@angular/core/testing';
import { afterEach, beforeEach, describe, expect, it, vi } from 'vitest';

import { FdTourService } from './fd-tour.service';
import { FdTourHostComponent } from './fd-tour-host';

describe('FdTourHostComponent keyboard focus', () => {
    let trigger: HTMLButtonElement;

    beforeEach(() => {
        trigger = document.createElement('button');
        trigger.id = 'tour-focus-target';
        document.body.append(trigger);
        TestBed.configureTestingModule({ imports: [FdTourHostComponent] });
    });

    afterEach(() => {
        trigger.remove();
    });

    it('contains Tab navigation and restores the trigger after closing', async () => {
        const fixture = TestBed.createComponent(FdTourHostComponent);
        fixture.detectChanges();
        trigger.focus();
        const tour = TestBed.inject(FdTourService);
        tour.start(
            { id: 'focus-test', version: 1, steps: [{ id: 'first', target: '#tour-focus-target', title: 'First' }] },
            { force: true },
        );
        fixture.detectChanges();
        const host = fixture.nativeElement as HTMLElement;
        document.body.append(host);
        const buttons = host.querySelectorAll<HTMLButtonElement>('[role="dialog"] button:not(:disabled)');
        const first = buttons[0];
        const last = [...buttons].at(-1);
        expect(first).toBeDefined();
        expect(last).toBeDefined();

        await vi.waitFor(() => {
            fixture.detectChanges();
            expect(host.querySelector('.fd-tour__popover--visible')).not.toBeNull();
        });
        expect(document.activeElement).toBe(first);
        first.focus();
        document.dispatchEvent(new KeyboardEvent('keydown', { key: 'Tab', shiftKey: true, cancelable: true }));
        expect(document.activeElement).toBe(last);
        last?.focus();
        document.dispatchEvent(new KeyboardEvent('keydown', { key: 'Tab', cancelable: true }));
        expect(document.activeElement).toBe(first);

        document.dispatchEvent(new KeyboardEvent('keydown', { key: 'Escape', cancelable: true }));
        fixture.detectChanges();
        expect(host.querySelector('[role="dialog"]')).toBeNull();
        expect(document.activeElement).toBe(trigger);
        fixture.destroy();
        host.remove();
    });
});

describe('FdTourHostComponent responsive highlight', () => {
    it('highlights the visible fallback instead of the hidden original', () => {
        TestBed.configureTestingModule({ imports: [FdTourHostComponent] });
        const targetHost = document.createElement('div');
        targetHost.innerHTML = '<button class="hidden-target" hidden>Hidden</button><button class="visible-target">More actions</button>';
        document.body.append(targetHost);
        const fallback = targetHost.querySelector<HTMLElement>('.visible-target');
        if (fallback === null) {
            throw new Error('Expected a responsive tour target');
        }
        const targetPosition = 100;
        const targetSize = 40;
        const fallbackRect = new DOMRect(targetPosition, targetPosition, targetSize, targetSize);
        vi.spyOn(fallback, 'getBoundingClientRect').mockReturnValue(fallbackRect);
        const fixture = TestBed.createComponent(FdTourHostComponent);
        const tour = TestBed.inject(FdTourService);
        try {
            tour.start(
                {
                    id: 'highlight-responsive',
                    version: 1,
                    steps: [{ id: 'help', target: '.hidden-target, .visible-target', title: 'Help' }],
                },
                { force: true },
            );
            fixture.detectChanges();
            const highlight = (fixture.nativeElement as HTMLElement).querySelector<HTMLElement>('.fd-tour__highlight');
            expect(highlight?.style.left).toBe('92px');
            expect(highlight?.style.top).toBe('92px');
            expect(highlight?.style.width).toBe('56px');
        } finally {
            tour.close();
            fixture.destroy();
            targetHost.remove();
        }
    });
});
