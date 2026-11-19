import { Component, signal } from '@angular/core';
import { type ComponentFixture, TestBed } from '@angular/core/testing';
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
    it('limits the popover to the available space below the highlighted row', () => {
        const widthDescriptor = Object.getOwnPropertyDescriptor(window, 'innerWidth');
        const heightDescriptor = Object.getOwnPropertyDescriptor(window, 'innerHeight');
        Object.defineProperty(window, 'innerWidth', { configurable: true, value: 320 });
        Object.defineProperty(window, 'innerHeight', { configurable: true, value: 640 });
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
            fixture.componentInstance['popoverSize'].set({ width: 288, height: 305 });
            fixture.detectChanges();
            const popover = (fixture.nativeElement as HTMLElement).querySelector<HTMLElement>('.fd-tour__popover');
            expect(popover?.style.maxHeight).toBe('282px');
            expect(Number.parseFloat(popover?.style.top ?? '0')).toBeGreaterThan(targetRect.bottom);
        } finally {
            service.close();
            fixture.destroy();
            target.remove();
            if (widthDescriptor !== undefined) {
                Object.defineProperty(window, 'innerWidth', widthDescriptor);
            }
            if (heightDescriptor !== undefined) {
                Object.defineProperty(window, 'innerHeight', heightDescriptor);
            }
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
        tour.start(
            {
                id: 'navigation',
                version: 1,
                labels: { previous: 'Earlier', next: 'Continue', finish: 'Finish', skip: 'Dismiss tour', close: 'Close tour' },
                steps: [
                    { id: 'dashboard', target: '[data-tour-id="dashboard"]', title: 'Dashboard' },
                    { id: 'meal', target: '[data-tour-id="meal"]', title: 'Meals' },
                ],
            },
            { force: true },
        ),
    ).toBe(true);
    fixture.detectChanges();
}

function pressTourKey(fixture: ComponentFixture<TourHost>, key: string): KeyboardEvent {
    const event = new KeyboardEvent('keydown', { key, bubbles: true, cancelable: true });
    document.dispatchEvent(event);
    fixture.detectChanges();
    return event;
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
        expect(pressTourKey(view.fixture, 'ArrowRight').defaultPrevented).toBe(true);
        expect(dialog.getAttribute('aria-label')).toBe('Meals');
        expect(progress.textContent.trim()).toBe('2 / 2');
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
});
