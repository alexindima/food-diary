import { ChangeDetectionStrategy, Component, signal, type WritableSignal } from '@angular/core';
import { type ComponentFixture, TestBed } from '@angular/core/testing';
import { provideRouter, Router } from '@angular/router';
import { FdUiDialogService } from 'fd-ui-kit/dialog/fd-ui-dialog.service';
import { type Observable, of, Subject } from 'rxjs';
import { afterEach, describe, expect, it, vi } from 'vitest';

import { provideTranslateTesting } from '../../../testing/translate-testing.module';
import { DashboardService } from '../../features/dashboard/api/dashboard.service';
import { AuthService } from '../../services/auth.service';
import { UnsavedChangesService } from '../../services/unsaved-changes.service';
import { UserService } from '../../shared/api/user.service';
import { decodeNotificationKind } from '../../shared/models/notification-kind';
import { utcInstant } from '../../shared/models/semantics/date-value';
import { entityId } from '../../shared/models/semantics/entity-id';
import type { User } from '../../shared/models/user.data';
import { type NotificationItem, NotificationService } from '../../shared/notifications/notification.service';
import { SidebarComponent } from './sidebar';

@Component({ template: '', changeDetection: ChangeDetectionStrategy.OnPush })
class SidebarRouteFixture {}

const user: User = {
    id: entityId<'user'>('sidebar-account'),
    email: 'sidebar@example.test',
    username: 'Alex',
    hasPassword: true,
    pushNotificationsEnabled: false,
    fastingPushNotificationsEnabled: false,
    socialPushNotificationsEnabled: false,
    fastingCheckInReminderHours: 8,
    fastingCheckInFollowUpReminderHours: 2,
    isActive: true,
    isEmailConfirmed: true,
};

type SidebarIntegrationContext = {
    fixture: ComponentFixture<SidebarComponent>;
    auth: {
        isAdmin: WritableSignal<boolean>;
        isDietologist: WritableSignal<boolean>;
        isPremium: WritableSignal<boolean>;
        onLogoutAsync: ReturnType<typeof vi.fn<() => Promise<void>>>;
    };
    notifications: {
        unreadCount: WritableSignal<number>;
        fetchUnreadCount: ReturnType<typeof vi.fn>;
        ensureNotificationsLoaded: ReturnType<typeof vi.fn>;
        notifications: WritableSignal<NotificationItem[]>;
        markAsRead: ReturnType<typeof vi.fn<NotificationService['markAsRead']>>;
        markAllRead: ReturnType<typeof vi.fn<NotificationService['markAllRead']>>;
    };
    open: ReturnType<typeof vi.fn>;
    router: Router;
};

async function setupSidebarAsync(
    options: { decision?: Subject<boolean>; productDecision?: Subject<boolean>; realDialogs?: boolean; mediaQuery?: MediaQueryList } = {},
): Promise<SidebarIntegrationContext> {
    const auth = {
        isAdmin: signal(false),
        isAuthenticated: signal(true),
        isDietologist: signal(false),
        isPremium: signal(false),
        getUserId: (): string => user.id,
        onLogoutAsync: vi.fn<() => Promise<void>>().mockResolvedValue(undefined),
    };
    const notifications = {
        unreadCount: signal(0),
        fetchUnreadCount: vi.fn(),
        ensureNotificationsLoaded: vi.fn(),
        notifications: signal<NotificationItem[]>([]),
        notificationsLoading: signal(false),
        notificationsLoaded: signal(true),
        markAsRead: vi.fn<NotificationService['markAsRead']>(),
        markAllRead: vi.fn<NotificationService['markAllRead']>(),
    };
    const open = vi.fn().mockReturnValue({
        afterClosed: (): Observable<undefined> => of(undefined),
        close: vi.fn(),
    });
    vi.stubGlobal(
        'matchMedia',
        vi.fn().mockReturnValue(
            options.mediaQuery ?? {
                matches: false,
                addEventListener: vi.fn(),
                removeEventListener: vi.fn(),
            },
        ),
    );
    await TestBed.configureTestingModule({
        imports: [SidebarComponent],
        providers: [
            provideTranslateTesting(),
            provideRouter([
                { path: '', component: SidebarRouteFixture },
                {
                    path: 'meals',
                    component: SidebarRouteFixture,
                    canActivate: [(): boolean | Subject<boolean> => options.decision ?? true],
                },
                {
                    path: 'products',
                    component: SidebarRouteFixture,
                    canActivate: [(): boolean | Subject<boolean> => options.productDecision ?? true],
                },
                { path: 'profile', component: SidebarRouteFixture },
            ]),
            { provide: AuthService, useValue: auth },
            { provide: UserService, useValue: { user: signal(user), clearUser: vi.fn(), getInfoSilently: vi.fn() } },
            { provide: DashboardService, useValue: { getSnapshotSilently: vi.fn(() => of(null)) } },
            { provide: NotificationService, useValue: notifications },
            ...(options.realDialogs === true ? [] : [{ provide: FdUiDialogService, useValue: { open } }]),
        ],
    }).compileComponents();
    const router = TestBed.inject(Router);
    await router.navigateByUrl('/');
    const fixture = TestBed.createComponent(SidebarComponent);
    fixture.detectChanges();
    return { fixture, auth, notifications, open, router };
}

function desktopElement(fixture: ComponentFixture<SidebarComponent>, selector: string): HTMLElement {
    const host = fixture.nativeElement as HTMLElement;
    const element = host.querySelector<HTMLElement>(`fd-sidebar-desktop ${selector}`);
    if (element === null) {
        throw new Error(`Expected the desktop sidebar control ${selector}`);
    }
    return element;
}

afterEach(() => {
    vi.restoreAllMocks();
    vi.unstubAllGlobals();
});

describe('rendered sidebar navigation and account status', () => {
    it('stops showing navigation as pending when the target route refuses the transition', async () => {
        const allow = new Subject<boolean>();
        const { fixture, router } = await setupSidebarAsync({ decision: allow });
        const target = desktopElement(fixture, 'a[href="/meals"]');

        target.click();
        fixture.detectChanges();
        expect(target.classList).toContain('is-pending');

        await vi.waitFor(() => {
            expect(allow.observed).toBe(true);
        });
        allow.next(false);
        await vi.waitFor(() => {
            fixture.detectChanges();
            expect(target.classList).not.toContain('is-pending');
        });
        expect(router.url).toBe('/');
    });

    it('keeps the latest link pending while an earlier navigation is superseded', async () => {
        const mealsDecision = new Subject<boolean>();
        const productsDecision = new Subject<boolean>();
        const { fixture, router } = await setupSidebarAsync({ decision: mealsDecision, productDecision: productsDecision });
        const meals = desktopElement(fixture, 'a[href="/meals"]');
        const products = desktopElement(fixture, 'a[href="/products"]');

        meals.click();
        await vi.waitFor(() => {
            fixture.detectChanges();
            expect(mealsDecision.observed).toBe(true);
            expect(meals.classList).toContain('is-pending');
        });

        products.click();
        await vi.waitFor(() => {
            expect(productsDecision.observed).toBe(true);
        });
        fixture.detectChanges();
        expect(mealsDecision.observed).toBe(false);
        expect(meals.classList).not.toContain('is-pending');
        expect(products.classList).toContain('is-pending');

        productsDecision.next(true);
        await vi.waitFor(() => {
            fixture.detectChanges();
            expect(router.url).toBe('/products');
            expect(products.classList).not.toContain('is-pending');
        });
    });

    it('shows role and plan updates from the signed-in account', async () => {
        const { fixture, auth, notifications } = await setupSidebarAsync();
        const role = desktopElement(fixture, '.fd-ui-sidebar__brand-subtitle');
        const plan = desktopElement(fixture, '.sidebar__user-plan');

        expect(role.textContent).toBe('SIDEBAR.STATUS_USER');
        expect(plan.textContent).toBe('SIDEBAR.PLAN_FREE');
        expect(notifications.fetchUnreadCount).toHaveBeenCalledOnce();
        expect(notifications.ensureNotificationsLoaded).toHaveBeenCalledOnce();

        auth.isDietologist.set(true);
        auth.isPremium.set(true);
        fixture.detectChanges();
        expect(role.textContent).toBe('SIDEBAR.STATUS_DIETOLOGIST');
        expect(plan.textContent).toBe('SIDEBAR.PLAN_PRO');

        auth.isAdmin.set(true);
        notifications.unreadCount.set(1);
        fixture.detectChanges();
        expect(role.textContent).toBe('SIDEBAR.STATUS_ADMIN');
        expect(desktopElement(fixture, '.fd-ui-sidebar__brand-notifications-dot').getAttribute('aria-hidden')).toBe('true');
    });
});

describe('rendered sidebar menu and notification overlay', () => {
    it('closes the desktop account menu when its trigger is selected again', async () => {
        const { fixture } = await setupSidebarAsync();
        const trigger = desktopElement(fixture, '.sidebar__user');
        trigger.click();
        fixture.detectChanges();
        expect(trigger.getAttribute('aria-expanded')).toBe('true');
        expect(desktopElement(fixture, '[role="menu"]')).toBeTruthy();

        trigger.click();
        fixture.detectChanges();

        expect(trigger.getAttribute('aria-expanded')).toBe('false');
        expect((fixture.nativeElement as HTMLElement).querySelector('fd-sidebar-desktop [role="menu"]')).toBeNull();
        expect(document.activeElement).toBe(trigger);
    });

    it('dismisses the account menu on Escape and restores focus to its trigger', async () => {
        const { fixture } = await setupSidebarAsync();
        const trigger = desktopElement(fixture, '.sidebar__user');
        trigger.click();
        fixture.detectChanges();
        await fixture.whenStable();
        expect(trigger.getAttribute('aria-expanded')).toBe('true');
        expect(desktopElement(fixture, '[role="menu"]')).toBeTruthy();

        const handled = new KeyboardEvent('keydown', { key: 'Escape', cancelable: true, bubbles: true });
        handled.preventDefault();
        document.dispatchEvent(handled);
        fixture.detectChanges();
        expect(trigger.getAttribute('aria-expanded')).toBe('true');

        const escape = new KeyboardEvent('keydown', { key: 'Escape', cancelable: true, bubbles: true });
        document.dispatchEvent(escape);
        fixture.detectChanges();

        expect(escape.defaultPrevented).toBe(true);
        expect(trigger.getAttribute('aria-expanded')).toBe('false');
        expect(document.activeElement).toBe(trigger);
    });

    it('closes the account menu before requesting collapse', async () => {
        const { fixture } = await setupSidebarAsync();
        const toggle = vi.fn();
        fixture.componentInstance.collapsedToggle.subscribe(toggle);
        const trigger = desktopElement(fixture, '.sidebar__user');
        trigger.click();
        fixture.detectChanges();
        expect(trigger.getAttribute('aria-expanded')).toBe('true');

        desktopElement(fixture, 'button[aria-label="SIDEBAR.COLLAPSE"]').click();
        fixture.detectChanges();

        expect(trigger.getAttribute('aria-expanded')).toBe('false');
        expect(toggle).toHaveBeenCalledOnce();
    });

    it('opens notifications while mounted', async () => {
        const { fixture, open } = await setupSidebarAsync();
        desktopElement(fixture, 'button[aria-label="NOTIFICATIONS.TITLE"]').click();
        await vi.dynamicImportSettled();
        expect(open).toHaveBeenCalledExactlyOnceWith(expect.any(Function), { preset: 'list' });
    });

    it('does not open notifications after the sidebar is destroyed during the lazy load', async () => {
        const { fixture, open } = await setupSidebarAsync();
        desktopElement(fixture, 'button[aria-label="NOTIFICATIONS.TITLE"]').click();
        fixture.destroy();
        await vi.dynamicImportSettled();
        expect(open).not.toHaveBeenCalled();
    });
});

describe('Sidebar navigation across viewport changes', () => {
    it('closes the desktop account menu when entering the mobile viewport', async () => {
        const viewport = Object.assign(new EventTarget(), { matches: false });
        const { fixture } = await setupSidebarAsync({ mediaQuery: viewport as unknown as MediaQueryList });
        const trigger = desktopElement(fixture, '.sidebar__user');
        trigger.click();
        fixture.detectChanges();
        expect(trigger.getAttribute('aria-expanded')).toBe('true');

        viewport.matches = true;
        viewport.dispatchEvent(new Event('change'));
        fixture.detectChanges();
        expect(trigger.getAttribute('aria-expanded')).toBe('false');

        viewport.matches = false;
        viewport.dispatchEvent(new Event('change'));
        fixture.detectChanges();
        expect((fixture.nativeElement as HTMLElement).querySelector('fd-sidebar-desktop [role="menu"]')).toBeNull();
    });

    it('keeps the active grouped link selected without showing a pending navigation on click', async () => {
        const { fixture, router } = await setupSidebarAsync();
        await router.navigateByUrl('/meals');
        fixture.detectChanges();
        const diary = desktopElement(fixture, 'a[href="/meals"]');
        await vi.waitFor(() => {
            expect(diary.classList).toContain('is-active');
        });

        diary.click();
        fixture.detectChanges();
        expect(diary.classList).not.toContain('is-pending');
        expect(diary.classList).toContain('is-active');
        expect(router.url).toBe('/meals');
    });
});

describe('Notifications from the desktop sidebar', () => {
    it('follows the notification link after a failed acknowledgment and keeps the unread dot until a successful bulk read', async () => {
        const { fixture, notifications, router } = await setupSidebarAsync({ realDialogs: true });
        const target = '/profile?intent=set-password';
        const item: NotificationItem = {
            id: entityId<'notification'>('sidebar-inbox-notice'),
            type: decodeNotificationKind('PasswordSetupSuggested'),
            title: 'Set a backup password',
            body: null,
            targetUrl: target,
            referenceId: null,
            isRead: false,
            createdAtUtc: utcInstant('2026-05-18T12:00:00Z'),
        };
        const acknowledge = new Subject<void>();
        notifications.markAsRead.mockReturnValueOnce(acknowledge);
        notifications.notifications.set([item]);
        notifications.unreadCount.set(1);
        fixture.detectChanges();
        const bell = desktopElement(fixture, 'button[aria-label="NOTIFICATIONS.TITLE"]');
        const unreadDot = '.fd-ui-sidebar__brand-notifications-dot';
        expect(bell.querySelector(unreadDot)).not.toBeNull();

        const inbox = await showSidebarInboxAsync(fixture, bell);
        const card = inbox.querySelector<HTMLButtonElement>('button[aria-label="Set a backup password"]');
        if (card === null) {
            throw new Error('Expected the sidebar notice to show in the inbox');
        }
        card.click();
        expect(notifications.markAsRead).toHaveBeenCalledExactlyOnceWith(item.id);
        expect(router.url).toBe('/');
        acknowledge.error(new Error('Acknowledgment unavailable'));
        await vi.waitFor(() => {
            fixture.detectChanges();
            expect(router.url).toBe(target);
            expect(document.querySelector('fd-notifications-dialog')).toBeNull();
        });
        expect(bell.querySelector(unreadDot)).not.toBeNull();

        const reopened = await showSidebarInboxAsync(fixture, bell);
        const bulk = new Subject<void>();
        notifications.markAllRead.mockReturnValueOnce(bulk);
        const markAll = reopened.querySelector<HTMLButtonElement>('.notifications-dialog__footer button');
        if (markAll === null) {
            throw new Error('Expected the inbox bulk acknowledgment action');
        }
        markAll.click();
        expect(notifications.markAllRead).toHaveBeenCalledOnce();
        expect(bell.querySelector(unreadDot)).not.toBeNull();

        notifications.notifications.set([{ ...item, isRead: true }]);
        notifications.unreadCount.set(0);
        bulk.next();
        bulk.complete();
        fixture.detectChanges();
        expect(bell.querySelector(unreadDot)).toBeNull();
        expect(reopened.querySelector('.notifications-dialog__item.is-read')).not.toBeNull();
        TestBed.inject(FdUiDialogService).closeAll();
    });
});

it('dismisses the notification inbox and releases its pending request when the sidebar is removed', async () => {
    const { fixture, notifications } = await setupSidebarAsync({ realDialogs: true });
    const dialogs = TestBed.inject(FdUiDialogService);
    notifications.notifications.set([
        {
            id: entityId<'notification'>('sidebar-unread-while-leaving'),
            type: decodeNotificationKind('PasswordSetupSuggested'),
            title: 'Set a backup password',
            body: null,
            targetUrl: null,
            referenceId: null,
            isRead: false,
            createdAtUtc: utcInstant('2026-05-18T12:00:00Z'),
        },
    ]);
    const pending = new Subject<void>();
    notifications.markAllRead.mockReturnValueOnce(pending);

    try {
        const bell = desktopElement(fixture, 'button[aria-label="NOTIFICATIONS.TITLE"]');
        const inbox = await showSidebarInboxAsync(fixture, bell);
        const markAll = inbox.querySelector<HTMLButtonElement>('.notifications-dialog__footer button');
        if (markAll === null) {
            throw new Error('Expected the inbox mark-all-read control');
        }
        markAll.click();
        expect(pending.observed).toBe(true);

        fixture.destroy();
        await vi.waitFor(() => {
            expect(document.querySelector('fd-notifications-dialog')).toBeNull();
        });
        expect(pending.observed).toBe(false);
    } finally {
        dialogs.closeAll();
    }
});

describe('Repeated sidebar notification actions', () => {
    it('opens one notification inbox while loading and allows it to be opened again after dismissal', async () => {
        const { fixture } = await setupSidebarAsync({ realDialogs: true });
        const bell = desktopElement(fixture, 'button[aria-label="NOTIFICATIONS.TITLE"]');
        const inboxSelector = 'fd-notifications-dialog';
        const dialogs = TestBed.inject(FdUiDialogService);

        bell.click();
        const inbox = await showSidebarInboxAsync(fixture, bell);
        expect(document.querySelectorAll(inboxSelector)).toHaveLength(1);

        const close = inbox.querySelector<HTMLButtonElement>('.fd-ui-dialog__close-button');
        if (close === null) {
            throw new Error('Expected the rendered notification inbox close action');
        }
        close.click();
        await vi.waitFor(() => {
            fixture.detectChanges();
            expect(document.querySelector(inboxSelector)).toBeNull();
        });
        await showSidebarInboxAsync(fixture, bell);
        expect(document.querySelectorAll(inboxSelector)).toHaveLength(1);
        dialogs.closeAll();
    });
});

async function showSidebarInboxAsync(fixture: ComponentFixture<SidebarComponent>, bell: HTMLElement): Promise<HTMLElement> {
    bell.click();
    await vi.dynamicImportSettled();
    return vi.waitFor(() => {
        fixture.detectChanges();
        const inbox = document.querySelector('fd-notifications-dialog');
        if (!(inbox instanceof HTMLElement)) {
            throw new Error('Expected the rendered notification inbox');
        }
        return inbox;
    });
}

describe('rendered sidebar mobile navigation', () => {
    it.each(['body', 'reports'])('toggles the %s sheet through its real navigation button', async sheet => {
        const { fixture } = await setupSidebarAsync({ realDialogs: true });
        const selector = `button[data-mobile-sheet="${sheet}"]`;
        const host = fixture.nativeElement as HTMLElement;
        const trigger = host.querySelector<HTMLButtonElement>(selector);
        if (trigger === null) {
            throw new Error('Expected the mobile navigation control');
        }

        trigger.click();
        fixture.detectChanges();
        const dialog = document.querySelector('[role="dialog"]');
        const current = dialog?.querySelector<HTMLButtonElement>(selector);
        if (current === null || current === undefined) {
            throw new Error('Expected the open mobile sheet to contain its navigation control');
        }
        expect(current.getAttribute('aria-expanded')).toBe('true');

        current.click();
        fixture.detectChanges();

        expect(document.querySelector('[role="dialog"]')).toBeNull();
        expect(host.querySelector(selector)?.getAttribute('aria-expanded')).toBe('false');
    });

    it('closes the user sheet when launching mobile notifications', async () => {
        const { fixture } = await setupSidebarAsync({ realDialogs: true });
        const host = fixture.nativeElement as HTMLElement;
        host.querySelector<HTMLButtonElement>('[data-mobile-sheet="user"]')?.click();
        fixture.detectChanges();
        const notifications = document.querySelector<HTMLButtonElement>('[role="dialog"] .sidebar-mobile__notifications-button');
        if (notifications === null) {
            throw new Error('Expected the mobile notifications control');
        }

        notifications.click();
        fixture.detectChanges();
        await vi.dynamicImportSettled();

        expect(document.querySelector('.sidebar-mobile__sheet')).toBeNull();
        await vi.waitFor(() => {
            TestBed.tick();
            expect(document.querySelector('fd-notifications-dialog')).not.toBeNull();
        });
    });
});

describe('rendered sidebar mobile logout', () => {
    it.each([
        { decision: 'cancelled', label: 'COMMON.CANCEL', logoutCalls: 0 },
        { decision: 'confirmed', label: 'HEADER.LOGOUT_CONFIRM_ACTION', logoutCalls: 1 },
    ])('closes the mobile user sheet and honors $decision logout', async ({ label, logoutCalls }) => {
        const { fixture, auth } = await setupSidebarAsync({ realDialogs: true });
        const host = fixture.nativeElement as HTMLElement;
        const trigger = host.querySelector<HTMLButtonElement>('button[data-mobile-sheet="user"]');
        if (trigger === null) {
            throw new Error('Expected the mobile account navigation control');
        }

        trigger.click();
        fixture.detectChanges();
        const signOut = document.querySelector<HTMLButtonElement>('.sidebar-mobile__sheet-logout');
        if (signOut === null) {
            throw new Error('Expected the mobile logout control');
        }
        signOut.click();
        fixture.detectChanges();

        expect(document.querySelector('.sidebar-mobile__sheet')).toBeNull();
        expect(trigger.getAttribute('aria-expanded')).toBe('false');
        expect(auth.onLogoutAsync).not.toHaveBeenCalled();
        const action = await vi.waitFor(() => {
            TestBed.tick();
            const button = [...document.querySelectorAll<HTMLButtonElement>('fd-ui-confirm-dialog button')].find(
                item => item.textContent.trim() === label,
            );
            if (button === undefined) {
                throw new Error(`Expected logout confirmation action ${label}`);
            }
            return button;
        });
        action.click();
        await fixture.whenStable();

        expect(auth.onLogoutAsync).toHaveBeenCalledTimes(logoutCalls);
        expect(document.querySelector('fd-ui-confirm-dialog')).toBeNull();
    });
});

describe('sidebar links opened outside the current tab', () => {
    it('keeps the desktop account menu open for browser-modified clicks and still follows a plain click', async () => {
        const { fixture, router } = await setupSidebarAsync();
        const account = desktopElement(fixture, '.sidebar__user');
        account.click();
        fixture.detectChanges();
        const profile = sidebarLink(fixture.nativeElement as HTMLElement, 'fd-sidebar-desktop a[href="/profile"]');

        for (const modifiers of [{ ctrlKey: true }, { metaKey: true }, { shiftKey: true }, { altKey: true }]) {
            activateWithoutCurrentTabNavigation(profile, modifiers);
            fixture.detectChanges();
            expect(account.getAttribute('aria-expanded')).toBe('true');
            expect(profile.classList).not.toContain('is-pending');
            expect(router.url).toBe('/');
        }

        profile.click();
        await vi.waitFor(() => {
            fixture.detectChanges();
            expect(router.url).toBe('/profile');
            expect(account.getAttribute('aria-expanded')).toBe('false');
        });
    });

    it.each([
        { sheet: 'reports', initialUrl: '/profile', targetUrl: '/', modifiers: { metaKey: true } },
        { sheet: 'user', initialUrl: '/', targetUrl: '/profile', modifiers: { ctrlKey: true } },
        { sheet: 'food', initialUrl: '/', targetUrl: '/meals', modifiers: { shiftKey: true } },
    ])(
        'keeps the mobile $sheet sheet available when opening $targetUrl outside this tab',
        async ({ sheet, initialUrl, targetUrl, modifiers }) => {
            const { fixture, router } = await setupSidebarAsync({ realDialogs: true });
            await router.navigateByUrl(initialUrl);
            fixture.detectChanges();
            const sidebar = fixture.nativeElement as HTMLElement;
            const sheetToggle = sidebar.querySelector<HTMLButtonElement>(`button[data-mobile-sheet="${sheet}"]`);
            if (sheetToggle === null) {
                throw new Error('Expected a mobile sheet toggle');
            }
            sheetToggle.click();
            fixture.detectChanges();

            const dialog = document.querySelector('[role="dialog"]');
            if (dialog === null) {
                throw new Error('Expected the mobile sidebar sheet');
            }
            const link = sidebarLink(dialog, `a[href="${targetUrl}"]`);
            activateWithoutCurrentTabNavigation(link, modifiers);
            fixture.detectChanges();

            expect(document.querySelector('[role="dialog"]')).toBe(dialog);
            expect(link.classList).not.toContain('is-pending');
            expect(router.url).toBe(initialUrl);

            link.click();
            await vi.waitFor(() => {
                fixture.detectChanges();
                expect(router.url).toBe(targetUrl);
                expect(document.querySelector('[role="dialog"]')).toBeNull();
            });
        },
    );
});

function activateWithoutCurrentTabNavigation(link: HTMLAnchorElement, modifiers: MouseEventInit): void {
    const click = new MouseEvent('click', { ...modifiers, bubbles: true, cancelable: true, button: 0 });
    let nativeNavigationAvailable = false;
    const preventJsdomNavigation = (event: MouseEvent): void => {
        nativeNavigationAvailable = !event.defaultPrevented;
        event.preventDefault();
    };
    document.addEventListener('click', preventJsdomNavigation, { once: true });
    link.dispatchEvent(click);
    document.removeEventListener('click', preventJsdomNavigation);
    expect(nativeNavigationAvailable).toBe(true);
}

function sidebarLink(host: ParentNode, selector: string): HTMLAnchorElement {
    const link = host.querySelector(selector);
    if (!(link instanceof HTMLAnchorElement)) {
        throw new Error(`Expected the sidebar link ${selector}`);
    }
    return link;
}

describe('rendered sidebar logout', () => {
    it('logs out after one confirmation when the current page is clean', async () => {
        const { fixture, open, auth } = await setupSidebarAsync();
        open.mockReturnValueOnce({ afterClosed: () => of(true) });
        clickLogout(fixture);
        await vi.waitFor(() => {
            expect(auth.onLogoutAsync).toHaveBeenCalledExactlyOnceWith(true);
        });
        expect(open).toHaveBeenCalledOnce();
    });

    it('preserves the current session and menu when saving the preceding editor fails', async () => {
        const { fixture, open, auth } = await setupSidebarAsync();
        const save = vi.fn<() => Promise<boolean>>().mockResolvedValue(false);
        TestBed.inject(UnsavedChangesService).register({ hasChanges: () => true, save, discard: vi.fn() });
        open.mockReturnValueOnce({ afterClosed: () => of(true) }).mockReturnValueOnce({ afterClosed: () => of('save') });

        clickLogout(fixture);
        await vi.waitFor(() => {
            expect(save).toHaveBeenCalledOnce();
        });
        await fixture.whenStable();
        fixture.detectChanges();

        expect(auth.onLogoutAsync).not.toHaveBeenCalled();
        expect(desktopElement(fixture, '.sidebar__user').getAttribute('aria-expanded')).toBe('true');
    });
});

function clickLogout(fixture: ComponentFixture<SidebarComponent>): void {
    desktopElement(fixture, '.sidebar__user').click();
    fixture.detectChanges();
    desktopElement(fixture, 'button[role="menuitem"]').click();
}
