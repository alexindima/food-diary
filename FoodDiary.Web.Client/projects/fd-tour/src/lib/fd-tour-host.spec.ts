import { DOCUMENT } from '@angular/common';
import { afterEveryRender, Component, PLATFORM_ID, signal } from '@angular/core';
import { type ComponentFixture, TestBed, TestComponentRenderer } from '@angular/core/testing';
import { afterEach, beforeEach, describe, expect, it, onTestFinished, vi } from 'vitest';

import { FdTourService } from './fd-tour.service';
import type { FdTourPlacement } from './fd-tour.types';
import { FdTourHostComponent } from './fd-tour-host';
import { FdTourTargetDirective } from './fd-tour-target.directive';

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

describe('Tour space around a mobile menu item', () => {
    it('limits the measured popover to the available space below the highlighted row', async () => {
        setViewport({ width: 320, height: 640 });
        const target = document.createElement('button');
        target.id = 'long-menu-item';
        document.body.append(target);
        const targetLeft = 20;
        const targetTop = 254;
        const targetWidth = 240;
        const targetHeight = 66;
        const targetRect = new DOMRect(targetLeft, targetTop, targetWidth, targetHeight);
        vi.spyOn(target, 'getBoundingClientRect').mockReturnValue(targetRect);
        const fixture = TestBed.createComponent(FdTourHostComponent);
        const service = TestBed.inject(FdTourService);
        try {
            service.start({ id: 'mobile-space', version: 1, steps: [{ id: 'row', target: '#long-menu-item', title: 'Row' }] });
            fixture.detectChanges();
            const popover = requireElement(fixture.nativeElement as HTMLElement, '.fd-tour__popover');
            vi.spyOn(popover, 'getBoundingClientRect').mockReturnValue(DOMRect.fromRect({ width: 288, height: 305 }));
            await vi.waitFor(() => {
                fixture.detectChanges();
                expect(popover.style.top).toBe('342px');
            });
            expect(popover.style.maxHeight).toBe('282px');
            expect(Number.parseFloat(popover.style.top)).toBeGreaterThan(targetRect.bottom);
        } finally {
            service.close();
            fixture.destroy();
            target.remove();
        }
    });
});

@Component({
    imports: [FdTourHostComponent, FdTourTargetDirective],
    template: `
        <button type="button" [fdTourTarget]="firstTarget()">Dashboard</button>
        <button type="button" fdTourTarget="meal">Meal</button>
        <fd-tour-host />
    `,
})
class TourHost {
    public readonly firstTarget = signal('dashboard');
}

type TourFixture = {
    fixture: ComponentFixture<TourHost>;
    host: HTMLElement;
    tour: FdTourService;
};

function createTourFixture(): TourFixture {
    const fixture = TestBed.createComponent(TourHost);
    const host = fixture.nativeElement as HTMLElement;
    const tour = TestBed.inject(FdTourService);
    fixture.detectChanges();
    onTestFinished(() => {
        tour.close();
        tour.resetCompletion('navigation');
        vi.restoreAllMocks();
    });
    return { fixture, host, tour };
}

function requireElement(host: ParentNode, selector: string): HTMLElement {
    const element = host.querySelector<HTMLElement>(selector);
    if (element === null) {
        throw new Error(`Expected tour element ${selector}.`);
    }
    return element;
}

function startNavigationTour({ fixture, tour }: TourFixture): void {
    expect(
        tour.start({
            id: 'navigation',
            version: 1,
            labels: { previous: 'Earlier', next: 'Continue', finish: 'Finish', skip: 'Dismiss tour', close: 'Close tour' },
            steps: [
                {
                    id: 'dashboard',
                    target: '[data-tour-id="dashboard"]',
                    title: 'Dashboard',
                    description: 'Review your nutrition summary.',
                },
                { id: 'meal', target: '[data-tour-id="meal"]', title: 'Meals' },
            ],
        }),
    ).toBe(true);
    fixture.detectChanges();
}

function pressTourKey(fixture: ComponentFixture<TourHost>, key: string): KeyboardEvent {
    const event = new KeyboardEvent('keydown', { key, bubbles: true, cancelable: true });
    document.dispatchEvent(event);
    fixture.detectChanges();
    return event;
}

function setViewport({ width, height }: { width: number; height: number }): void {
    const innerWidth = vi.spyOn(window, 'innerWidth', 'get').mockReturnValue(width);
    const innerHeight = vi.spyOn(window, 'innerHeight', 'get').mockReturnValue(height);
    onTestFinished(() => {
        innerWidth.mockRestore();
        innerHeight.mockRestore();
    });
}

describe('FdTourHostComponent navigation', () => {
    it('navigates between steps using arrow keys and leaves keyboard events alone when closed', () => {
        const view = createTourFixture();
        expect(pressTourKey(view.fixture, 'ArrowRight').defaultPrevented).toBe(false);
        startNavigationTour(view);
        const dialog = requireElement(view.host, '[role="dialog"]');
        const progress = requireElement(dialog, '.fd-tour__progress');

        expect(dialog.getAttribute('aria-label')).toBe('Dashboard');
        expect(progress.textContent.trim()).toBe('1 / 2');
        expect(requireElement(dialog, '.fd-tour__description').textContent.trim()).toBe('Review your nutrition summary.');
        expect(pressTourKey(view.fixture, 'ArrowRight').defaultPrevented).toBe(true);
        expect(dialog.getAttribute('aria-label')).toBe('Meals');
        expect(progress.textContent.trim()).toBe('2 / 2');
        expect(dialog.querySelector('.fd-tour__description')).toBeNull();
        expect(pressTourKey(view.fixture, 'ArrowLeft').defaultPrevented).toBe(true);
        expect(dialog.getAttribute('aria-label')).toBe('Dashboard');
    });

    it('updates button labels and state throughout the tour and completes from the final step', () => {
        const view = createTourFixture();
        startNavigationTour(view);
        const dialog = requireElement(view.host, '[role="dialog"]');
        const next = requireElement(dialog, '.fd-tour__button--primary');
        const previous = requireElement(dialog, '.fd-tour__button--secondary');
        expect(previous.hasAttribute('disabled')).toBe(true);
        expect(next.textContent.trim()).toBe('Continue');

        next.click();
        view.fixture.detectChanges();
        expect(previous.hasAttribute('disabled')).toBe(false);
        expect(previous.textContent.trim()).toBe('Earlier');
        expect(next.textContent.trim()).toBe('Finish');

        previous.click();
        view.fixture.detectChanges();
        expect(dialog.getAttribute('aria-label')).toBe('Dashboard');

        next.click();
        view.fixture.detectChanges();
        next.click();
        view.fixture.detectChanges();
        expect(view.host.querySelector('[role="dialog"]')).toBeNull();
        expect(view.tour.snapshot()).toBeNull();
    });

    it('dismisses through the named close control and lets the user reopen the tour', () => {
        const view = createTourFixture();
        startNavigationTour(view);
        const close = requireElement(view.host, '.fd-tour__close');
        expect(close.getAttribute('aria-label')).toBe('Close tour');

        close.click();
        view.fixture.detectChanges();
        expect(view.host.querySelector('[role="dialog"]')).toBeNull();

        startNavigationTour(view);
        expect(requireElement(view.host, '[role="dialog"]').getAttribute('aria-label')).toBe('Dashboard');
    });
});

describe('FdTourHostComponent presentation', () => {
    it('recomputes highlight geometry following both scroll and resize', () => {
        const view = createTourFixture();
        const initialBounds = { x: 240, y: 300, width: 70, height: 40 };
        const scrolledBounds = { ...initialBounds, y: 200 };
        const resizedBounds = { ...scrolledBounds, x: 20, width: 100 };
        const target = requireElement(view.host, '[data-tour-id="dashboard"]');
        const bounds = vi.spyOn(target, 'getBoundingClientRect').mockReturnValue(DOMRect.fromRect(initialBounds));
        startNavigationTour(view);
        const highlight = requireElement(view.host, '.fd-tour__highlight');
        expect(highlight.style.top).toBe('292px');
        expect(highlight.style.width).toBe('86px');

        bounds.mockReturnValue(DOMRect.fromRect(scrolledBounds));
        document.dispatchEvent(new Event('scroll'));
        view.fixture.detectChanges();
        expect(highlight.style.top).toBe('192px');

        bounds.mockReturnValue(DOMRect.fromRect(resizedBounds));
        window.dispatchEvent(new Event('resize'));
        view.fixture.detectChanges();
        expect(highlight.style.left).toBe('12px');
        expect(highlight.style.width).toBe('116px');
    });

    it('reveals the current step after switching steps while the first is waiting for scroll to settle', async () => {
        const view = createTourFixture();
        startNavigationTour(view);
        document.dispatchEvent(new Event('scroll'));
        view.fixture.detectChanges();
        expect(view.host.querySelector('.fd-tour__popover--visible')).toBeNull();
        pressTourKey(view.fixture, 'ArrowRight');
        expect(view.host.querySelector('.fd-tour__popover--visible')).toBeNull();
        expect(pressTourKey(view.fixture, 'Tab').defaultPrevented).toBe(true);

        await vi.waitFor(() => {
            view.fixture.detectChanges();
            expect(view.host.querySelector('.fd-tour__popover--visible')?.getAttribute('aria-label')).toBe('Meals');
        });
        const skip = requireElement(view.host, '.fd-tour__button--ghost');
        expect(skip.textContent.trim()).toBe('Dismiss tour');
        skip.click();
        view.fixture.detectChanges();
        expect(view.host.querySelector('[role="dialog"]')).toBeNull();
    });

    it('uses the updated target attribute to resolve a dynamically identified control', () => {
        const { fixture, host, tour } = createTourFixture();
        fixture.componentInstance.firstTarget.set('renamed-dashboard');
        fixture.detectChanges();
        const target = requireElement(host, '[data-tour-id="renamed-dashboard"]');
        expect(target.textContent).toBe('Dashboard');
        expect(
            tour.start({
                id: 'renamed-target',
                version: 1,
                steps: [{ id: 'dashboard', target: '[data-tour-id="renamed-dashboard"]', title: 'Dashboard' }],
            }),
        ).toBe(true);
        fixture.detectChanges();
        expect(tour.target()).toBe(target);

        requireElement(host, '.fd-tour__backdrop').click();
        fixture.detectChanges();
        expect(host.querySelector('[role="dialog"]')).toBeNull();
    });
});

describe('FdTourHostComponent desktop layout', () => {
    const startPositionedTour = (placement: FdTourPlacement, bounds: DOMRect): HTMLElement => {
        const { fixture, host, tour } = createTourFixture();
        const target = requireElement(host, '[data-tour-id="dashboard"]');
        vi.spyOn(target, 'getBoundingClientRect').mockReturnValue(bounds);
        tour.start({
            id: 'positioned-tour',
            version: 1,
            steps: [{ id: 'dashboard', target: '[data-tour-id="dashboard"]', title: 'Dashboard', placement }],
        });
        fixture.detectChanges();
        return requireElement(host, '[role="dialog"]');
    };

    it.each<{ placement: FdTourPlacement; left: string; top: string }>([
        { placement: 'top', left: '350px', top: '78px' },
        { placement: 'right', left: '582px', top: '312px' },
        { placement: 'left', left: '118px', top: '312px' },
        { placement: 'bottom', left: '350px', top: '382px' },
    ])('uses the requested $placement placement relative to the padded highlight', ({ placement, left, top }) => {
        const bounds = DOMRect.fromRect({ x: 500, y: 320, width: 60, height: 40 });
        const popover = startPositionedTour(placement, bounds);

        expect(popover.style.left).toBe(left);
        expect(popover.style.top).toBe(top);
    });

    it('places a top-preferring popover below a target near the upper desktop edge', () => {
        const bounds = DOMRect.fromRect({ x: 30, y: 24, width: 80, height: 40 });
        const popover = startPositionedTour('top', bounds);

        expect(Number.parseFloat(popover.style.top)).toBeGreaterThan(bounds.bottom);
        expect(popover.style.left).toBe('16px');
    });

    it.each([
        { placement: 'auto', top: '124px' },
        { placement: 'top', top: '16px' },
    ] as const)('keeps the $placement popover inside a crowded desktop viewport', ({ placement, top }) => {
        setViewport({ width: 720, height: 360 });
        const bounds = DOMRect.fromRect({ x: 20, y: 20, width: 680, height: 320 });
        const popover = startPositionedTour(placement, bounds);

        expect(popover.style.top).toBe(top);
        expect(popover.style.left).toBe('180px');
        expect(popover.style.width).toBe('360px');
    });
});

describe('FdTourHostComponent mobile layout', () => {
    it.each([
        { description: 'space above the target', height: 640, targetY: 500, targetHeight: 52, top: '258px', maxHeight: '462px' },
        { description: 'a short viewport', height: 300, targetY: 90, targetHeight: 120, top: '64px', maxHeight: '268px' },
    ])('preserves viewport gutters with $description', ({ height, targetY, targetHeight, top, maxHeight }) => {
        setViewport({ width: 320, height });
        const view = createTourFixture();
        const target = requireElement(view.host, '[data-tour-id="dashboard"]');
        vi.spyOn(target, 'getBoundingClientRect').mockReturnValue(
            DOMRect.fromRect({ x: 32, y: targetY, width: 256, height: targetHeight }),
        );
        startNavigationTour(view);

        const dialog = requireElement(view.host, '[role="dialog"]');
        expect(dialog.style.top).toBe(top);
        expect(dialog.style.left).toBe('16px');
        expect(dialog.style.width).toBe('288px');
        expect(dialog.style.maxHeight).toBe(maxHeight);
    });
});

async function waitForVisibleDialogAsync(view: TourFixture): Promise<HTMLElement> {
    await vi.waitFor(() => {
        view.fixture.detectChanges();
        expect(view.host.querySelector('.fd-tour__popover--visible')).not.toBeNull();
    });
    return requireElement(view.host, '[role="dialog"]');
}

describe('FdTourHostComponent focus boundaries', () => {
    it('lets unrelated keyboard input propagate and restores focus inside the dialog on Tab', async () => {
        const view = createTourFixture();
        startNavigationTour(view);
        const dialog = await waitForVisibleDialogAsync(view);
        const outside = requireElement(view.host, '[data-tour-id="meal"]');
        const keydown = vi.fn();
        outside.addEventListener('keydown', keydown);
        outside.focus();
        outside.dispatchEvent(new KeyboardEvent('keydown', { key: 'a', bubbles: true, cancelable: true }));
        expect(keydown).toHaveBeenCalledOnce();

        outside.dispatchEvent(new KeyboardEvent('keydown', { key: 'Tab', bubbles: true, cancelable: true }));
        expect(keydown).toHaveBeenCalledOnce();
        expect(document.activeElement).toBe(requireElement(dialog, '.fd-tour__close'));
    });

    it('prevents Tab from leaving the tour when all of its controls are disabled by a containing fieldset', async () => {
        const view = createTourFixture();
        const fieldset = document.createElement('fieldset');
        const overlayHost = requireElement(view.host, 'fd-tour-host');
        overlayHost.replaceWith(fieldset);
        fieldset.append(overlayHost);
        startNavigationTour(view);
        const dialog = await waitForVisibleDialogAsync(view);
        fieldset.disabled = true;
        const outside = requireElement(view.host, '[data-tour-id="dashboard"]');
        outside.focus();
        expect(dialog.querySelector('button:not(:disabled)')).toBeNull();

        expect(pressTourKey(view.fixture, 'Tab').defaultPrevented).toBe(true);
        expect(document.activeElement).toBe(outside);
    });

    it('does not attempt to restore focus to a non-HTML trigger', async () => {
        const view = createTourFixture();
        const trigger = document.createElementNS('http://www.w3.org/2000/svg', 'svg');
        trigger.setAttribute('tabindex', '0');
        view.host.prepend(trigger);
        trigger.focus();
        expect(document.activeElement).toBe(trigger);
        startNavigationTour(view);
        const dialog = await waitForVisibleDialogAsync(view);
        expect(document.activeElement).toBe(requireElement(dialog, '.fd-tour__close'));

        pressTourKey(view.fixture, 'Escape');
        expect(view.host.querySelector('[role="dialog"]')).toBeNull();
        expect(document.activeElement).not.toBe(trigger);
    });

    it('keeps focus on the opener when the tour closes during its first visible render', async () => {
        const view = createTourFixture();
        const opener = requireElement(view.host, '[data-tour-id="dashboard"]');
        opener.focus();
        startNavigationTour(view);
        const focus = vi.spyOn(requireElement(view.host, '.fd-tour__close'), 'focus');
        const closeOnReveal = TestBed.runInInjectionContext(() =>
            afterEveryRender({
                write: () => {
                    if (view.host.querySelector('.fd-tour__popover--visible') !== null) {
                        view.tour.close();
                    }
                },
            }),
        );
        onTestFinished(() => {
            closeOnReveal.destroy();
        });

        await vi.waitFor(() => {
            view.fixture.detectChanges();
            expect(view.host.querySelector('[role="dialog"]')).toBeNull();
        });
        expect(focus).not.toHaveBeenCalled();
        expect(document.activeElement).toBe(opener);
    });
});

function mockNativePopover(): Array<{ action: 'show' | 'hide'; element: HTMLElement }> {
    const prototype = HTMLElement.prototype;
    const calls: Array<{ action: 'show' | 'hide'; element: HTMLElement }> = [];
    const show = Object.getOwnPropertyDescriptor(prototype, 'showPopover');
    const hide = Object.getOwnPropertyDescriptor(prototype, 'hidePopover');
    Object.defineProperties(prototype, {
        showPopover: {
            configurable: true,
            value(this: HTMLElement) {
                calls.push({ action: 'show', element: this });
            },
        },
        hidePopover: {
            configurable: true,
            value(this: HTMLElement) {
                calls.push({ action: 'hide', element: this });
            },
        },
    });
    onTestFinished(() => {
        for (const [name, descriptor] of [
            ['showPopover', show],
            ['hidePopover', hide],
        ] as const) {
            if (descriptor === undefined) {
                Reflect.deleteProperty(prototype, name);
            } else {
                Object.defineProperty(prototype, name, descriptor);
            }
        }
    });
    return calls;
}

describe('FdTourHostComponent browser boundary', () => {
    it('promotes the overlay back to the top layer each time a new target is presented', () => {
        const calls = mockNativePopover();
        const view = createTourFixture();
        startNavigationTour(view);
        const overlay = requireElement(view.host, '[popover="manual"]');
        expect(overlay.isConnected).toBe(true);
        expect(calls).toEqual([
            { action: 'hide', element: overlay },
            { action: 'show', element: overlay },
        ]);

        pressTourKey(view.fixture, 'ArrowRight');
        expect(requireElement(view.host, '[role="dialog"]').getAttribute('aria-label')).toBe('Meals');
        expect(calls.slice(2)).toEqual([
            { action: 'hide', element: overlay },
            { action: 'show', element: overlay },
        ]);
    });

    it('shows the controls immediately for a document without a browsing context', () => {
        const detachedDocument = document.implementation.createHTMLDocument('Detached tour');
        TestBed.configureTestingModule({
            providers: [
                { provide: DOCUMENT, useValue: detachedDocument },
                {
                    provide: TestComponentRenderer,
                    useValue: {
                        insertRootElement(rootElementId: string): void {
                            const root = detachedDocument.createElement('div');
                            root.id = rootElementId;
                            detachedDocument.body.append(root);
                        },
                    },
                },
            ],
        });
        const view = createTourFixture();
        onTestFinished(() => {
            view.host.remove();
        });
        startNavigationTour(view);

        const popover = requireElement(view.host, '.fd-tour__popover--visible');
        expect(popover.getAttribute('aria-label')).toBe('Dashboard');
        expect(popover.style.left).toBe('16px');
        expect(popover.style.top).toBe('16px');
        expect(popover.style.width).toBe('360px');
    });
});

describe('FdTourHostComponent idle and server rendering', () => {
    it('initializes without animation frame support when there is no active tour', () => {
        vi.stubGlobal('requestAnimationFrame', undefined);
        onTestFinished(() => {
            vi.unstubAllGlobals();
        });
        const view = createTourFixture();

        expect(view.tour.snapshot()).toBeNull();
        expect(view.host.querySelector('[role="dialog"]')).toBeNull();
        expect(pressTourKey(view.fixture, 'Tab').defaultPrevented).toBe(false);
    });

    it('leaves the server render empty and avoids registering browser handlers', () => {
        TestBed.overrideProvider(PLATFORM_ID, { useValue: 'server' });
        const documentListeners = vi.spyOn(document, 'addEventListener');
        const windowListeners = vi.spyOn(window, 'addEventListener');
        const view = createTourFixture();

        expect(
            view.tour.start({
                id: 'server',
                version: 1,
                steps: [{ id: 'dashboard', target: '[data-tour-id="dashboard"]', title: 'Dashboard' }],
            }),
        ).toBe(false);
        expect(view.host.querySelector('[role="dialog"]')).toBeNull();
        expect(documentListeners.mock.calls.some(([event]) => event === 'keydown')).toBe(false);
        expect(documentListeners.mock.calls.some(([event]) => event === 'scroll')).toBe(false);
        expect(windowListeners.mock.calls.some(([event]) => event === 'resize')).toBe(false);
    });
});

describe('FdTourHostComponent independent tour scope', () => {
    it('renders and closes its local tour while a root tour remains active', async () => {
        await TestBed.configureTestingModule({ imports: [FdTourHostComponent] })
            .overrideComponent(FdTourHostComponent, { add: { providers: [FdTourService] } })
            .compileComponents();
        const fixture = TestBed.createComponent(FdTourHostComponent);
        const host = fixture.nativeElement as HTMLElement;
        const rootTour = TestBed.inject(FdTourService);
        const localTour = fixture.debugElement.injector.get(FdTourService);
        const target = document.createElement('button');
        target.id = 'scoped-tour-target';
        document.body.append(target);
        onTestFinished(() => {
            target.remove();
            rootTour.close();
        });
        expect(localTour).not.toBe(rootTour);

        const step = { id: 'meal', target: '#scoped-tour-target', title: 'Meals tour' };
        expect(rootTour.start({ id: 'root-tour', version: 1, steps: [step] })).toBe(true);
        fixture.detectChanges();
        expect(host.querySelector('[role="dialog"]')).toBeNull();

        expect(
            localTour.start({
                id: 'local',
                version: 1,
                steps: [{ ...step, title: 'Local meals tour' }],
            }),
        ).toBe(true);
        fixture.detectChanges();
        const dialog = requireElement(host, '[role="dialog"]');
        expect(dialog.getAttribute('aria-label')).toBe('Local meals tour');

        requireElement(dialog, '.fd-tour__close').click();
        fixture.detectChanges();
        expect(host.querySelector('[role="dialog"]')).toBeNull();
        expect(localTour.snapshot()).toBeNull();
        expect(rootTour.activeStep()?.id).toBe('meal');
    });
});

describe('FdTourHostComponent animation cancellation', () => {
    beforeEach(() => {
        vi.useFakeTimers({ toFake: ['requestAnimationFrame', 'cancelAnimationFrame', 'setTimeout', 'clearTimeout'] });
    });

    afterEach(() => {
        vi.useRealTimers();
    });

    it('does not reveal the next step from an animation frame scheduled by its predecessor', async () => {
        const view = createTourFixture();
        const scrollIdleMs = 140;
        startNavigationTour(view);

        await vi.advanceTimersByTimeAsync(scrollIdleMs);
        vi.advanceTimersToNextFrame();
        pressTourKey(view.fixture, 'ArrowRight');
        vi.advanceTimersToNextFrame();
        view.fixture.detectChanges();
        const intermediate = requireElement(view.host, '[role="dialog"]');
        expect(intermediate.getAttribute('aria-label')).toBe('Meals');
        expect(intermediate.classList.contains('fd-tour__popover--visible')).toBe(false);

        await vi.advanceTimersByTimeAsync(scrollIdleMs);
        vi.advanceTimersToNextFrame();
        vi.advanceTimersToNextFrame();
        view.fixture.detectChanges();
        expect(requireElement(view.host, '.fd-tour__popover--visible').getAttribute('aria-label')).toBe('Meals');
    });

    it('reveals the tour when repeated scrolling prevents the idle timeout', async () => {
        const view = createTourFixture();
        const scrollIntervalMs = 100;
        const revealDeadlineMs = 700;
        startNavigationTour(view);

        for (let elapsed = scrollIntervalMs; elapsed < revealDeadlineMs; elapsed += scrollIntervalMs) {
            await vi.advanceTimersByTimeAsync(scrollIntervalMs);
            document.dispatchEvent(new Event('scroll'));
            view.fixture.detectChanges();
            expect(view.host.querySelector('.fd-tour__popover--visible')).toBeNull();
        }
        await vi.advanceTimersByTimeAsync(scrollIntervalMs);
        vi.advanceTimersToNextFrame();
        vi.advanceTimersToNextFrame();
        view.fixture.detectChanges();
        expect(requireElement(view.host, '.fd-tour__popover--visible').getAttribute('aria-label')).toBe('Dashboard');
    });
});
