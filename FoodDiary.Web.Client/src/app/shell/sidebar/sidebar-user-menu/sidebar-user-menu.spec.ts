import { Component } from '@angular/core';
import type { ComponentFixture } from '@angular/core/testing';
import { TestBed } from '@angular/core/testing';
import { provideRouter } from '@angular/router';
import { beforeEach, describe, expect, it, vi } from 'vitest';

import { waitForAsyncTasksAsync } from '../../../../testing/async-testing';
import { provideTranslateTesting } from '../../../../testing/translate-testing.module';
import type { User } from '../../../shared/models/user.data';
import { SidebarUserMenuComponent } from './sidebar-user-menu';

@Component({
    template: '',
})
class DummyRouteComponent {}

const user: User = {
    id: 'user-1',
    email: 'user@example.com',
    username: 'Alex',
    hasPassword: true,
    pushNotificationsEnabled: true,
    fastingPushNotificationsEnabled: false,
    socialPushNotificationsEnabled: true,
    fastingCheckInReminderHours: 8,
    fastingCheckInFollowUpReminderHours: 2,
    isActive: true,
    isEmailConfirmed: true,
};

describe('SidebarUserMenuComponent', () => {
    beforeEach(() => {
        TestBed.resetTestingModule();
    });

    it('renders user identity and plan label', () => {
        const fixture = createComponent({ isOpen: false });
        const host = getHost(fixture);
        const avatar = requireElement(host, '.sidebar__avatar');

        expect(host.textContent).toContain('Alex');
        expect(host.textContent).toContain('SIDEBAR.PREMIUM');
        expect(avatar.textContent.trim()).toBe('A');
        expect(host.querySelector('[role="menu"]')).toBeNull();
    });

    it('renders menu links when open and marks pending profile route', () => {
        const fixture = createComponent({ isOpen: true, pendingRoute: '/profile' });
        const host = getHost(fixture);
        const profileLink = requireElement(host, 'a[role="menuitem"]');
        const logoutButton = requireElement(host, 'button[role="menuitem"]');

        expect(host.querySelector('[role="menu"]')).not.toBeNull();
        expect(profileLink.classList).toContain('is-pending');
        expect(profileLink.textContent).toContain('HEADER.PROFILE');
        expect(logoutButton.textContent).toContain('HEADER.LOGOUT');
    });

    it('renders the open collapsed menu as a flyout without clipped identity text', () => {
        const fixture = createComponent({ isOpen: true, isCollapsed: true });
        const host = getHost(fixture);
        const menu = requireElement(host, '[role="menu"]');

        expect(menu.classList).toContain('sidebar__user-menu--flyout');
        expect(host.querySelector('.sidebar__user-meta')).toBeNull();
        expect(host.querySelector('.sidebar__user-arrow')).toBeNull();
        expect(menu.textContent).toContain('HEADER.PROFILE');
        expect(menu.textContent).toContain('HEADER.LOGOUT');
    });

    it('renders the expanded menu as the same desktop flyout', () => {
        const fixture = createComponent({ isOpen: true });

        expect(requireElement(getHost(fixture), '[role="menu"]').classList).toContain('sidebar__user-menu--flyout');
    });

    it('emits the native button element when user toggles the menu', () => {
        const fixture = createComponent({ isOpen: false });
        const toggleSpy = vi.fn();
        fixture.componentInstance.toggleMenu.subscribe(toggleSpy);
        const button = requireElement(getHost(fixture), '.sidebar__user');

        button.click();

        expect(toggleSpy).toHaveBeenCalledWith(button);
    });

    it('emits profile route and logout actions from open menu', async () => {
        const fixture = createComponent({ isOpen: true });
        const directRouteSpy = vi.fn();
        const logoutSpy = vi.fn();
        fixture.componentInstance.directRouteClick.subscribe(directRouteSpy);
        fixture.componentInstance.logout.subscribe(logoutSpy);
        const host = getHost(fixture);

        requireElement(host, 'a[role="menuitem"]').click();
        await fixture.whenStable();
        requireElement(host, 'button[role="menuitem"]').click();

        expect(directRouteSpy).toHaveBeenCalledWith({ route: '/profile' });
        expect(logoutSpy).toHaveBeenCalledOnce();
    });

    it('falls back to email initial when username is empty', () => {
        const fixture = createComponent({ isOpen: false, user: { ...user, username: '' } });
        const host = getHost(fixture);
        const avatar = requireElement(host, '.sidebar__avatar');

        expect(avatar.textContent.trim()).toBe('U');
        expect(host.textContent).toContain('user@example.com');
    });

    it('focuses the first menu action when opened', async () => {
        const focusSpy = vi.spyOn(HTMLElement.prototype, 'focus').mockImplementation(() => {});

        createComponent({ isOpen: true });
        await waitForAsyncTasksAsync();

        expect(focusSpy).toHaveBeenCalled();
        focusSpy.mockRestore();
    });
});

type CreateOptions = {
    isOpen: boolean;
    isCollapsed?: boolean;
    pendingRoute?: string | null;
    user?: User;
};

function createComponent(options: CreateOptions): ComponentFixture<SidebarUserMenuComponent> {
    TestBed.configureTestingModule({
        imports: [SidebarUserMenuComponent],
        providers: [provideTranslateTesting(), provideRouter([{ path: 'profile', component: DummyRouteComponent }])],
    });

    const fixture = TestBed.createComponent(SidebarUserMenuComponent);
    fixture.componentRef.setInput('user', options.user ?? user);
    fixture.componentRef.setInput('userPlanLabelKey', 'SIDEBAR.PREMIUM');
    fixture.componentRef.setInput('isOpen', options.isOpen);
    fixture.componentRef.setInput('pendingRoute', options.pendingRoute ?? null);
    fixture.componentRef.setInput('isCollapsed', options.isCollapsed ?? false);
    fixture.detectChanges();

    return fixture;
}

function getHost(fixture: ComponentFixture<SidebarUserMenuComponent>): HTMLElement {
    return fixture.nativeElement as HTMLElement;
}

function requireElement(host: HTMLElement, selector: string): HTMLElement {
    const element = host.querySelector<HTMLElement>(selector);
    if (element === null) {
        throw new Error(`Expected element ${selector} to exist.`);
    }

    return element;
}
