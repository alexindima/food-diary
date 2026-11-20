import type { ComponentFixture } from '@angular/core/testing';
import { TestBed } from '@angular/core/testing';
import { provideRouter } from '@angular/router';
import { afterEach, beforeEach, describe, expect, it, type MockInstance, onTestFinished, vi } from 'vitest';

import { provideTranslateTesting } from '../../../../testing/translate-testing.module';
import { MOBILE_REPORT_ITEMS } from '../sidebar-lib/sidebar-navigation.config';
import { SidebarMobileComponent } from './sidebar-mobile';

const UNREAD_NOTIFICATION_COUNT = 3;
const MOBILE_NAV_HEIGHT = 56;
const RESIZED_MOBILE_NAV_HEIGHT = 72;

beforeEach(() => {
    TestBed.resetTestingModule();
});

describe('SidebarMobileComponent', () => {
    it('derives open state and report links from selected mobile sheet', () => {
        createComponent('reports');
        const host = document.body;
        const links = host.querySelectorAll('.sidebar-mobile__sheet-link');

        expect(getDialogLabel()).toBe('SIDEBAR.REPORTS_AND_GOALS');
        expect(links.length).toBe(MOBILE_REPORT_ITEMS.length);
        expect(host.querySelectorAll('button.is-open').length).toBe(1);
    });

    it('emits the clicked mobile section trigger', () => {
        const fixture = createComponent(null);
        const component = fixture.componentInstance;
        const foodToggle = vi.fn();
        component['foodToggle'].subscribe(foodToggle);

        const host = document.body;
        const foodButton = host.querySelector('button[aria-haspopup="dialog"]');
        foodButton?.dispatchEvent(new MouseEvent('click', { bubbles: true }));

        expect(foodToggle).toHaveBeenCalledWith(foodButton);
    });
});

describe('SidebarMobileComponent modal navigation', () => {
    it('keeps the bottom navigation inside the active modal so it remains interactive', () => {
        createComponent('user');
        const dialog = document.querySelector('[role="dialog"]');
        const nav = document.querySelector('.sidebar-mobile');

        expect(dialog?.contains(nav), 'Open mobile navigation must be inside the modal interaction boundary').toBe(true);
        expect(nav?.closest('[inert]')).toBeNull();
        expect(document.querySelectorAll('.sidebar-mobile')).toHaveLength(1);
    });

    it('switches sections within the same modal and restores focus after Escape', async () => {
        // Make the mobile navigation visible in jsdom's desktop-sized fixture.
        const mobileLayout = document.createElement('style');
        mobileLayout.textContent = '.sidebar-mobile { display: grid !important; }';
        document.head.append(mobileLayout);
        onTestFinished(() => {
            mobileLayout.remove();
        });
        const fixture = createComponent(null);
        await renderSidebarAsync(fixture);
        const component = fixture.componentInstance;
        component.userToggle.subscribe(() => {
            fixture.componentRef.setInput('mobileSheet', 'user');
        });
        component.bodyToggle.subscribe(() => {
            fixture.componentRef.setInput('mobileSheet', 'body');
        });
        component.closeMenus.subscribe(() => {
            fixture.componentRef.setInput('mobileSheet', null);
        });

        const opener = requiredElement('button[data-mobile-sheet="user"]');
        opener.focus();
        expect(document.activeElement).toBe(opener);
        opener.click();
        await renderSidebarAsync(fixture);
        const dialog = requiredElement('[role="dialog"]');
        const bodyButton = requiredElement('[role="dialog"] button[data-mobile-sheet="body"]');
        bodyButton.focus();
        bodyButton.click();
        await renderSidebarAsync(fixture);

        expect(document.querySelector('[role="dialog"]')).toBe(dialog);
        expect(getDialogLabel()).toBe('SIDEBAR.BODY_TRACKING');
        expect(bodyButton.getAttribute('aria-expanded')).toBe('true');
        expect(document.querySelectorAll('.sidebar-mobile')).toHaveLength(1);
        expect(document.activeElement).toBe(bodyButton);

        bodyButton.dispatchEvent(new KeyboardEvent('keydown', { key: 'Escape', keyCode: 27, bubbles: true }));
        await renderSidebarAsync(fixture);

        expect(document.querySelector('[role="dialog"]')).toBeNull();
        const restoredButton = document.querySelector('[data-mobile-sheet="body"]');
        expect(restoredButton?.closest('[inert]')).toBeNull();
        expect(document.activeElement).toBe(restoredButton);
        expect(document.querySelectorAll('.sidebar-mobile')).toHaveLength(1);
    });

    it('keeps Today available while a sheet is open and returns the navigation after closing', () => {
        const fixture = createComponent('food');
        const requested = vi.fn();
        fixture.componentInstance.directRouteClick.subscribe(requested);
        fixture.componentInstance.closeMenus.subscribe(() => {
            fixture.componentRef.setInput('mobileSheet', null);
        });

        document.querySelector<HTMLAnchorElement>('.sidebar-mobile__item')?.click();
        fixture.detectChanges();

        expect(requested).toHaveBeenCalledWith({ route: '/', exact: true });
        expect(document.querySelector('[role="dialog"]')).toBeNull();
        expect(document.querySelectorAll('.sidebar-mobile')).toHaveLength(1);
    });
});

describe('SidebarMobileComponent sheet geometry', () => {
    it('follows the rendered navigation into the modal, responds to a resize, and stops observing it on teardown', async () => {
        const bounds = mockNavigationHeight();
        const observations = recordNavigationObservations();
        const fixture = createComponent(null);
        await renderSidebarAsync(fixture);
        const closedNav = requiredElement('.sidebar-mobile');
        const closedObservation = observationFor(closedNav, observations);
        fixture.componentInstance.foodToggle.subscribe(() => {
            fixture.componentRef.setInput('mobileSheet', 'food');
        });

        requiredElement('button[data-mobile-sheet="food"]').click();
        await renderSidebarAsync(fixture);

        const openNav = requiredElement('[role="dialog"] .sidebar-mobile');
        expect(openNav).not.toBe(closedNav);
        expect(closedNav.isConnected).toBe(false);
        expect(closedObservation.disconnect).toHaveBeenCalledOnce();
        const openObservation = observationFor(openNav, observations);
        expectSheetOffset(MOBILE_NAV_HEIGHT);

        bounds.mockReturnValue(new DOMRect(0, 0, 0, RESIZED_MOBILE_NAV_HEIGHT));
        openObservation.notify();
        await renderSidebarAsync(fixture);
        expectSheetOffset(RESIZED_MOBILE_NAV_HEIGHT);
        expect(openObservation.disconnect).not.toHaveBeenCalled();

        fixture.destroy();
        expect(openObservation.disconnect).toHaveBeenCalledOnce();
        expect(openNav.isConnected).toBe(false);
        expect(document.querySelector('[role="dialog"]')).toBeNull();
    });

    it('positions an open sheet from the initial navigation measurement when resize observation is unavailable', async () => {
        vi.stubGlobal('ResizeObserver', undefined);
        mockNavigationHeight();
        const fixture = createComponent('food');
        await renderSidebarAsync(fixture);

        expectSheetOffset(MOBILE_NAV_HEIGHT);
        expect(getDialogLabel()).toBe('SIDEBAR.FOOD_TRACKING');
        expect(requiredElement('button[data-mobile-sheet="food"]').getAttribute('aria-expanded')).toBe('true');
    });
});

describe('SidebarMobileComponent accessibility and actions', () => {
    it('provides accessible names when visual labels are hidden on narrow screens', () => {
        createComponent(null);
        const host = document.body;
        const items = [...host.querySelectorAll<HTMLElement>('.sidebar-mobile__item')];

        expect(items.map(item => item.getAttribute('aria-label'))).toEqual([
            'SIDEBAR.DASHBOARD',
            'SIDEBAR.FOOD',
            'SIDEBAR.BODY',
            'SIDEBAR.REPORTS',
            'SIDEBAR.USER',
        ]);
    });

    it('hides admin panel action from non-admin users', () => {
        createComponent('user', false);
        const host = document.body;

        expect(host.querySelector('.sidebar-mobile__sheet-link--admin')).toBeNull();
    });

    it('shows admin panel action for admin users', () => {
        createComponent('user', true);
        const host = document.body;

        expect(host.querySelector('.sidebar-mobile__sheet-link--admin')).not.toBeNull();
    });

    it('does not render notification badge when there are no unread notifications', () => {
        createComponent('user', false, 0);
        const host = document.body;

        expect(host.querySelector('.sidebar-mobile__notifications-badge')).toBeNull();
    });

    it('renders notification badge when there are unread notifications', () => {
        createComponent('user', false, UNREAD_NOTIFICATION_COUNT);
        const host = document.body;

        const badge = host.querySelector('.sidebar-mobile__notifications-badge');
        if (badge === null) {
            throw new Error('Expected notification badge to render.');
        }
        expect(badge.textContent.trim()).toBe(String(UNREAD_NOTIFICATION_COUNT));
    });
});

type NavigationObservation = {
    target: Element;
    disconnect: ReturnType<typeof vi.fn>;
    notify: () => void;
};

function recordNavigationObservations(): NavigationObservation[] {
    const observations: NavigationObservation[] = [];
    class SidebarResizeObserver implements ResizeObserver {
        public readonly disconnect = vi.fn();

        public constructor(private readonly callback: ResizeObserverCallback) {}

        public observe(target: Element): void {
            if (target.matches('.sidebar-mobile')) {
                observations.push({
                    target,
                    disconnect: this.disconnect,
                    notify: () => {
                        this.callback([], this);
                    },
                });
            }
        }

        public unobserve(): void {}
    }
    vi.stubGlobal('ResizeObserver', SidebarResizeObserver);
    return observations;
}

function observationFor(target: Element, observations: NavigationObservation[]): NavigationObservation {
    const observation = observations.find(entry => entry.target === target);
    if (observation === undefined) {
        throw new Error('Expected the rendered navigation to be observed');
    }
    return observation;
}

function mockNavigationHeight(): MockInstance<HTMLElement['getBoundingClientRect']> {
    const bounds = vi.spyOn(HTMLElement.prototype, 'getBoundingClientRect').mockReturnValue(new DOMRect(0, 0, 0, MOBILE_NAV_HEIGHT));
    onTestFinished(() => {
        bounds.mockRestore();
    });
    return bounds;
}

async function renderSidebarAsync(fixture: ComponentFixture<SidebarMobileComponent>): Promise<void> {
    fixture.detectChanges();
    TestBed.tick();
    await fixture.whenStable();
    fixture.detectChanges();
}

function expectSheetOffset(height: number): void {
    expect(requiredElement('.sidebar-mobile__sheet').style.getPropertyValue('--sidebar-mobile-sheet-offset')).toBe(`${height}px`);
    expect(requiredElement('.sidebar-mobile__backdrop').style.bottom).toBe(`${height}px`);
}

function requiredElement(selector: string): HTMLElement {
    const element = document.querySelector<HTMLElement>(selector);
    if (element === null) {
        throw new Error(`Expected the rendered sidebar element ${selector}`);
    }
    return element;
}

function getDialogLabel(): string | undefined {
    const dialog = document.querySelector('[role="dialog"]');
    const labelId = dialog?.getAttribute('aria-labelledby');
    return labelId !== null && labelId !== undefined
        ? document.getElementById(labelId)?.textContent.trim()
        : (dialog?.getAttribute('aria-label') ?? undefined);
}

function createComponent(
    mobileSheet: 'food' | 'body' | 'reports' | 'user' | null,
    isAdmin = false,
    unreadNotificationCount = 0,
): ComponentFixture<SidebarMobileComponent> {
    TestBed.configureTestingModule({
        imports: [SidebarMobileComponent],
        providers: [provideTranslateTesting(), provideRouter([])],
    });

    const fixture = TestBed.createComponent(SidebarMobileComponent);
    fixture.componentRef.setInput('pendingRoute', null);
    fixture.componentRef.setInput('unreadNotificationCount', unreadNotificationCount);
    fixture.componentRef.setInput('mobileSheet', mobileSheet);
    fixture.componentRef.setInput('isAdmin', isAdmin);
    fixture.detectChanges();

    return fixture;
}

beforeEach(() =>
    vi.stubGlobal(
        'matchMedia',
        vi.fn(() => ({ matches: false })),
    ),
);
afterEach(() => vi.unstubAllGlobals());
