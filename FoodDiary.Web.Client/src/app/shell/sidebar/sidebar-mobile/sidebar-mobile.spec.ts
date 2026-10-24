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

describe('SidebarMobileComponent', () => {
    beforeEach(() => {
        TestBed.resetTestingModule();
    });

    it('derives open state and report links from selected mobile sheet', () => {
        createComponent('reports');
        const host = document.body;
        const links = host.querySelectorAll('.sidebar-mobile__sheet-link');

        expect(document.querySelector('[role=dialog]')?.getAttribute('aria-label')).toBe('SIDEBAR.REPORTS_AND_GOALS');
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
