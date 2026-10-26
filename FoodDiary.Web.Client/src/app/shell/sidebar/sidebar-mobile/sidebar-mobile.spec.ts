import type { ComponentFixture } from '@angular/core/testing';
import { TestBed } from '@angular/core/testing';
import { provideRouter } from '@angular/router';
import { afterEach, beforeEach, describe, expect, it, vi } from 'vitest';

import { provideTranslateTesting } from '../../../../testing/translate-testing.module';
import { MOBILE_REPORT_ITEMS } from '../sidebar-lib/sidebar-navigation.config';
import { SidebarMobileComponent } from './sidebar-mobile';

const DAILY_CONSUMED = 850;
const DAILY_GOAL = 2000;
const DAILY_PROGRESS = 42.5;
const UNREAD_NOTIFICATION_COUNT = 3;

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

    it('switches sections within the same modal and closes with Escape', async () => {
        const fixture = createComponent('user');
        TestBed.tick();
        await fixture.whenStable();
        const component = fixture.componentInstance;
        const dialog = document.querySelector('[role="dialog"]');
        const bodyButton = document.querySelector<HTMLButtonElement>('[data-mobile-sheet="body"]');
        component.bodyToggle.subscribe(() => {
            fixture.componentRef.setInput('mobileSheet', 'body');
        });
        component.closeMenus.subscribe(() => {
            fixture.componentRef.setInput('mobileSheet', null);
        });

        bodyButton?.click();
        fixture.detectChanges();
        TestBed.tick();
        await fixture.whenStable();

        expect(document.querySelector('[role="dialog"]')).toBe(dialog);
        expect(getDialogLabel()).toBe('SIDEBAR.BODY_TRACKING');
        expect(bodyButton?.getAttribute('aria-expanded')).toBe('true');
        expect(document.querySelectorAll('.sidebar-mobile')).toHaveLength(1);

        dialog?.dispatchEvent(new KeyboardEvent('keydown', { key: 'Escape', keyCode: 27, bubbles: true }));
        fixture.detectChanges();
        TestBed.tick();
        await fixture.whenStable();

        expect(document.querySelector('[role="dialog"]')).toBeNull();
        const restoredButton = document.querySelector('[data-mobile-sheet="body"]');
        expect(restoredButton?.closest('[inert]')).toBeNull();
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
    fixture.componentRef.setInput('isProgressVisible', true);
    fixture.componentRef.setInput('dailyConsumedKcalRounded', DAILY_CONSUMED);
    fixture.componentRef.setInput('dailyGoalKcalRounded', DAILY_GOAL);
    fixture.componentRef.setInput('dailyProgressPercent', DAILY_PROGRESS);
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
