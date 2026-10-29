/* eslint-disable @typescript-eslint/no-magic-numbers -- Pixel coordinates and millisecond timelines are explicit gesture fixtures. */
import { Component, signal } from '@angular/core';
import { type ComponentFixture, TestBed } from '@angular/core/testing';
import { afterEach, beforeEach, describe, expect, it, vi } from 'vitest';

import { provideTranslateTesting } from '../../../../../src/testing/translate-testing.module';
import { FdUiDialogComponent } from './fd-ui-dialog';
import { type FdUiDialogConfig, FdUiDialogService } from './fd-ui-dialog.service';
import type { FdUiDialogRef } from './fd-ui-dialog-ref';

@Component({
    imports: [FdUiDialogComponent],
    template: `
        <fd-ui-dialog title="Protein" [disableClose]="locked()">
            <p class="content">Sources</p>
            <div class="nested" style="overflow-y: auto"><p>Nested sources</p></div>
            <input aria-label="Amount" />
            <button type="button">Source action</button>
        </fd-ui-dialog>
    `,
})
class SwipeDialogHost {
    public readonly locked = signal(false);
}

let compact = true;
let reducedMotion = true;

beforeEach(() => {
    compact = true;
    reducedMotion = true;
    vi.stubGlobal(
        'matchMedia',
        vi.fn((query: string) => ({ matches: query.includes('reduced-motion') ? reducedMotion : compact })),
    );
});

afterEach(() => {
    TestBed.inject(FdUiDialogService).closeAll();
    vi.useRealTimers();
    vi.unstubAllGlobals();
});

type SwipeContext = {
    fixture: ComponentFixture<SwipeDialogHost>;
    service: FdUiDialogService;
    ref: FdUiDialogRef<SwipeDialogHost>;
    root: HTMLElement;
    header: HTMLElement;
    body: HTMLElement;
    closed: ReturnType<typeof vi.fn>;
};

function required<T>(value: T | null | undefined): T {
    if (value === null || value === undefined) {
        throw new Error('Expected dialog fixture element or component.');
    }
    return value;
}

async function openAsync(config: FdUiDialogConfig = {}): Promise<SwipeContext> {
    TestBed.configureTestingModule({ imports: [SwipeDialogHost], providers: [provideTranslateTesting()] });
    const fixture = TestBed.createComponent(SwipeDialogHost);
    fixture.detectChanges();
    const service = TestBed.inject(FdUiDialogService);
    const ref = service.open(SwipeDialogHost, { ...config, autoFocus: false, restoreFocus: false });
    TestBed.tick();
    await fixture.whenStable();
    const container = required(document.getElementById(ref.id));
    const root = required(container.querySelector<HTMLElement>('.fd-ui-dialog'));
    const header = required(root.querySelector<HTMLElement>('.fd-ui-dialog__header'));
    const body = required(root.querySelector<HTMLElement>('.fd-ui-dialog__body'));
    body.style.overflowY = 'auto';
    vi.spyOn(root, 'getBoundingClientRect').mockReturnValue(new DOMRect(0, 200, 390, 600));
    const closed = vi.fn();
    ref.closed.subscribe(closed);
    return { fixture, service, ref, root, header, body, closed };
}

function touch(
    target: HTMLElement,
    type: string,
    [x, y, time]: readonly number[],
    options: { count?: number; cancelable?: boolean } = {},
): Event {
    const { count = 1, cancelable = true } = options;
    const point = { identifier: 1, clientX: x, clientY: y };
    const event = new Event(type, { bubbles: true, cancelable });
    Object.defineProperties(event, {
        touches: { value: type === 'touchend' ? [] : Array.from({ length: count }, (_, index) => ({ ...point, identifier: index + 1 })) },
        changedTouches: { value: [point] },
        timeStamp: { value: time },
    });
    target.dispatchEvent(event);
    return event;
}

function drag(target: HTMLElement, distance = 200, duration = 500): Event {
    touch(target, 'touchstart', [100, 200, 0]);
    const move = touch(target, 'touchmove', [100, 200 + distance, duration]);
    touch(target, 'touchend', [100, 200 + distance, duration + 10]);
    return move;
}

describe('Mobile dialog swipe dismissal', () => {
    it('follows a downward drag and closes at one quarter of the sheet height', async () => {
        const { header, root, closed } = await openAsync();
        touch(header, 'touchstart', [100, 200, 0]);
        const move = touch(header, 'touchmove', [100, 360, 500]);
        expect(move.defaultPrevented).toBe(true);
        expect(root.style.translate).toBe('0 160px');
        touch(header, 'touchend', [100, 360, 510]);
        expect(closed).toHaveBeenCalledOnce();
    });

    it('returns a short slow drag to rest', async () => {
        const { header, root, closed } = await openAsync();
        drag(header, 60);
        expect(root.style.translate).toBe('');
        expect(closed).not.toHaveBeenCalled();
    });

    it('accepts an intentional fast flick but not a tap or a held short drag', async () => {
        const { header, closed } = await openAsync();
        drag(header, 6, 5);
        expect(closed).not.toHaveBeenCalled();
        touch(header, 'touchstart', [100, 200, 0]);
        touch(header, 'touchmove', [100, 260, 50]);
        touch(header, 'touchend', [100, 260, 500]);
        expect(closed).not.toHaveBeenCalled();
        drag(header, 60, 50);
        expect(closed).toHaveBeenCalledOnce();
    });

    it('animates the exit before destroying the dialog', async () => {
        const { header, root, closed } = await openAsync();
        reducedMotion = false;
        vi.useFakeTimers();
        drag(header);
        expect(root.style.translate).toBe('0 600px');
        expect(closed).not.toHaveBeenCalled();
        vi.advanceTimersByTime(180);
        expect(closed).toHaveBeenCalledOnce();
    });

    it('allows a fresh downward drag from content already at its scroll start', async () => {
        const { body, closed } = await openAsync();
        expect(drag(body).defaultPrevented).toBe(true);
        expect(closed).toHaveBeenCalledOnce();
    });
});

describe('Dialog scroll and touch ownership', () => {
    it('leaves a scrolled list in control even when it reaches the start in the same gesture', async () => {
        const { body, root, closed } = await openAsync();
        body.scrollTop = 100;
        touch(body, 'touchstart', [100, 200, 0]);
        body.scrollTop = 0;
        const move = touch(body, 'touchmove', [100, 400, 500]);
        touch(body, 'touchend', [100, 400, 510]);
        expect(move.defaultPrevented).toBe(false);
        expect(root.style.translate).toBe('');
        expect(closed).not.toHaveBeenCalled();
    });

    it('respects nested scrollers while the header remains draggable', async () => {
        const { root, header, closed } = await openAsync();
        const nested = required(root.querySelector<HTMLElement>('.nested'));
        nested.scrollTop = 100;
        expect(drag(nested).defaultPrevented).toBe(false);
        expect(closed).not.toHaveBeenCalled();
        drag(header);
        expect(closed).toHaveBeenCalledOnce();
    });

    it.each([
        [160, 200],
        [100, 140],
    ])('keeps horizontal/upward gestures native (%i, %i)', async (x, y) => {
        const { body, closed } = await openAsync();
        touch(body, 'touchstart', [100, 200, 0]);
        expect(touch(body, 'touchmove', [x, y, 50]).defaultPrevented).toBe(false);
        touch(body, 'touchmove', [100, 400, 500]);
        touch(body, 'touchend', [100, 400, 510]);
        expect(closed).not.toHaveBeenCalled();
    });

    it.each(['input', 'button'])('preserves %s interaction', async selector => {
        const { root, closed } = await openAsync();
        expect(drag(required(root.querySelector<HTMLElement>(selector))).defaultPrevented).toBe(false);
        expect(closed).not.toHaveBeenCalled();
    });

    it.each(['touchcancel', 'multitouch', 'uncancelable'])('returns to rest on %s', async reason => {
        const { header, root, closed } = await openAsync();
        touch(header, 'touchstart', [100, 200, 0]);
        touch(header, 'touchmove', [100, 250, 300]);
        touch(header, reason === 'touchcancel' ? 'touchcancel' : 'touchmove', [100, 400, 500], {
            count: reason === 'multitouch' ? 2 : 1,
            cancelable: reason !== 'uncancelable',
        });
        touch(header, 'touchend', [100, 400, 510]);
        expect(root.style.translate).toBe('');
        expect(closed).not.toHaveBeenCalled();
    });
});

describe('Dialog gesture dismissal policies', () => {
    it('cancels an active drag when a pending operation begins', async () => {
        const { header, root, ref, closed } = await openAsync();
        touch(header, 'touchstart', [100, 200, 0]);
        touch(header, 'touchmove', [100, 400, 500]);
        required(ref.componentInstance).locked.set(true);
        TestBed.tick();
        expect(root.style.translate).toBe('');
        touch(header, 'touchend', [100, 400, 510]);
        expect(closed).not.toHaveBeenCalled();
    });

    it('rechecks a ref dismissal lock after the exit animation', async () => {
        const { header, root, ref, closed } = await openAsync();
        reducedMotion = false;
        vi.useFakeTimers();
        drag(header);
        const release = ref.acquireDismissalLock();
        vi.advanceTimersByTime(180);
        expect(root.style.translate).toBe('');
        expect(closed).not.toHaveBeenCalled();
        release();
        drag(header);
        vi.advanceTimersByTime(180);
        expect(closed).toHaveBeenCalledOnce();
    });

    it('honors closePredicate and restores a rejected dismissal', async () => {
        const { header, root, closed } = await openAsync({ closePredicate: () => false });
        drag(header);
        expect(root.style.translate).toBe('');
        expect(closed).not.toHaveBeenCalled();
    });

    it.each([{ disableClose: true }, { preset: 'fullscreen' }, { preset: 'surface' }] satisfies FdUiDialogConfig[])(
        'does not swipe a guarded or custom layout: %j',
        async config => {
            const { header, closed } = await openAsync(config);
            expect(drag(header).defaultPrevented).toBe(false);
            expect(closed).not.toHaveBeenCalled();
        },
    );

    it('does not intercept desktop gestures', async () => {
        compact = false;
        const { header, closed } = await openAsync();
        expect(drag(header).defaultPrevented).toBe(false);
        expect(closed).not.toHaveBeenCalled();
    });

    it('dismisses only the topmost nested dialog', async () => {
        const { fixture, header, service, closed } = await openAsync();
        const nestedRef = service.open(SwipeDialogHost, { autoFocus: false, restoreFocus: false });
        TestBed.tick();
        await fixture.whenStable();
        const nestedHeader = required(required(document.getElementById(nestedRef.id)).querySelector<HTMLElement>('.fd-ui-dialog__header'));
        const nestedClosed = vi.fn();
        nestedRef.closed.subscribe(nestedClosed);
        drag(header);
        expect(closed).not.toHaveBeenCalled();
        drag(nestedHeader);
        expect(nestedClosed).toHaveBeenCalledOnce();
        expect(closed).not.toHaveBeenCalled();
        drag(header);
        expect(closed).toHaveBeenCalledOnce();
    });

    it('cleans up an animation when another close action destroys the dialog', async () => {
        const { header, root, ref, closed } = await openAsync();
        reducedMotion = false;
        vi.useFakeTimers();
        drag(header);
        ref.close();
        vi.advanceTimersByTime(180);
        expect(closed).toHaveBeenCalledOnce();
        expect(root.style.translate).toBe('');
    });
});
