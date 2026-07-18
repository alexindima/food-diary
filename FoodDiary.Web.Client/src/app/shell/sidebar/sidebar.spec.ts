import { signal, type WritableSignal } from '@angular/core';
import { TestBed } from '@angular/core/testing';
import { NavigationEnd, Router } from '@angular/router';
import { TranslateService } from '@ngx-translate/core';
import { FdUiDialogService } from 'fd-ui-kit/dialog/fd-ui-dialog.service';
import { FdUiToastService } from 'fd-ui-kit/toast/fd-ui-toast.service';
import { Observable, of, Subject } from 'rxjs';
import { afterEach, beforeEach, describe, expect, it, vi } from 'vitest';

import { environment } from '../../../environments/environment';
import { ADMIN_LOADING_URL_TTL_MS, SIDEBAR_MOBILE_VIEWPORT_QUERY } from '../../config/runtime-ui.tokens';
import { DashboardService } from '../../features/dashboard/api/dashboard.service';
import { AuthService } from '../../services/auth.service';
import { UnsavedChangesService } from '../../services/unsaved-changes.service';
import { UserService } from '../../shared/api/user.service';
import { NotificationService } from '../../shared/notifications/notification.service';
import { SidebarComponent } from './sidebar';
import type { DesktopSectionId, MobileSheetId, SidebarActionItem, SidebarRouteItem } from './sidebar-lib/sidebar.models';

const NAVIGATION_ID = 1;
const ADMIN_LOADING_TTL_MS = 100;
const LOCKED_SCROLL_Y = 240;
const ADMIN_SSO_CODE = 'admin-sso-code';
const ADMIN_LOADING_URL = 'blob:admin-loading';

type SidebarHarness = {
    closeMobileMenus: () => void;
    logoutAsync: () => Promise<void>;
    mobileSheet: WritableSignal<MobileSheetId>;
    onDirectRouteClick: (route: string, exact?: boolean) => void;
    onMobileAction: (action: SidebarActionItem['action']) => void;
    onRouteSelected: (item: SidebarRouteItem) => void;
    openAdminPanel: () => void;
    openDesktopSection: WritableSignal<DesktopSectionId>;
    pendingRoute: WritableSignal<string | null>;
    toggleBodyTracking: () => void;
    toggleFoodTracking: () => void;
    toggleMobileFood: (trigger?: HTMLElement) => void;
    toggleUserMenu: (trigger?: HTMLElement) => void;
};

describe('SidebarComponent behavior', () => {
    beforeEach(setupSidebarTestEnvironment);
    afterEach(cleanupSidebarTestEnvironment);

    it('sets pending route for inactive navigation and clears it after navigation end', () => {
        const { component, router, routerEvents } = createComponent();
        const harness = component as unknown as SidebarHarness;

        router.url = '/products';
        harness.onRouteSelected({ id: 'meals', icon: 'restaurant', labelKey: 'SIDEBAR.FOOD_DIARY', route: '/meals' });

        expect(harness.pendingRoute()).toBe('/meals');

        router.url = '/meals';
        routerEvents.next(new NavigationEnd(NAVIGATION_ID, '/meals', '/meals'));

        expect(harness.pendingRoute()).toBeNull();
    });

    it('does not set pending route for active direct navigation', () => {
        const { component, router } = createComponent();
        const harness = component as unknown as SidebarHarness;
        router.url = '/meals';

        harness.onDirectRouteClick('/meals', true);

        expect(harness.pendingRoute()).toBeNull();
    });

    it('toggles desktop food and body sections', () => {
        const { component } = createComponent();
        const harness = component as unknown as SidebarHarness;

        expect(harness.openDesktopSection()).toBe('food');

        harness.toggleFoodTracking();
        expect(harness.openDesktopSection()).toBeNull();

        harness.toggleBodyTracking();
        expect(harness.openDesktopSection()).toBe('body');
    });

    it('closes mobile menus before forwarding mobile actions', () => {
        const { component } = createComponent();
        const harness = component as unknown as SidebarHarness;

        harness.toggleMobileFood(document.createElement('button'));
        harness.onMobileAction('openAdminPanel');

        expect(harness.mobileSheet()).toBeNull();
    });
});

describe('SidebarComponent mobile behavior', () => {
    beforeEach(setupSidebarTestEnvironment);
    afterEach(cleanupSidebarTestEnvironment);

    it('closes mobile sheet on Escape and restores trigger focus', () => {
        const { component } = createComponent();
        const harness = component as unknown as SidebarHarness;
        const trigger = document.createElement('button');
        const focusSpy = vi.spyOn(trigger, 'focus');

        harness.toggleMobileFood(trigger);
        document.dispatchEvent(new KeyboardEvent('keydown', { key: 'Escape' }));

        expect(harness.mobileSheet()).toBeNull();
        expect(focusSpy).toHaveBeenCalledOnce();
    });

    it('blocks page scroll without shifting content while a mobile sheet is open', () => {
        const { component } = createComponent({ isMobileViewport: true });
        const harness = component as unknown as SidebarHarness;
        const scrollTo = vi.spyOn(window, 'scrollTo').mockImplementation(() => {});
        Object.defineProperty(window, 'innerWidth', { configurable: true, value: 1200 });
        Object.defineProperty(window, 'scrollY', { configurable: true, value: LOCKED_SCROLL_Y });
        Object.defineProperty(document.documentElement, 'clientWidth', { configurable: true, value: 1183 });

        harness.toggleMobileFood(document.createElement('button'));
        TestBed.tick();

        expectScrollLockApplied();

        harness.closeMobileMenus();
        TestBed.tick();

        expectScrollLockReleased();
        expect(scrollTo).toHaveBeenCalledWith(0, LOCKED_SCROLL_Y);
    });
});

describe('SidebarComponent logout behavior', () => {
    beforeEach(setupSidebarTestEnvironment);
    afterEach(cleanupSidebarTestEnvironment);

    it('does not logout when unsaved changes confirmation is cancelled', async () => {
        const { authService, component, dialogService } = createComponent();
        const harness = component as unknown as SidebarHarness;
        dialogService.open.mockReturnValueOnce({ afterClosed: () => of(true) }).mockReturnValueOnce({ afterClosed: () => of(null) });

        await harness.logoutAsync();

        expect(authService.onLogoutAsync).not.toHaveBeenCalled();
    });

    it('does not logout when logout confirmation is cancelled', async () => {
        const { authService, component, dialogService, unsavedHandler } = createComponent();
        const harness = component as unknown as SidebarHarness;
        dialogService.open.mockReturnValue({ afterClosed: () => of(false) });

        await harness.logoutAsync();

        expect(unsavedHandler.hasChanges).not.toHaveBeenCalled();
        expect(authService.onLogoutAsync).not.toHaveBeenCalled();
    });

    it('discards unsaved changes before logout when confirmed', async () => {
        const { authService, component, dialogService, unsavedHandler } = createComponent();
        const harness = component as unknown as SidebarHarness;
        dialogService.open.mockReturnValueOnce({ afterClosed: () => of(true) }).mockReturnValueOnce({ afterClosed: () => of('discard') });

        await harness.logoutAsync();

        expect(unsavedHandler.discard).toHaveBeenCalledOnce();
        expect(authService.onLogoutAsync).toHaveBeenCalledWith(true);
    });

    it('saves unsaved changes before logout when requested', async () => {
        const { authService, component, dialogService, unsavedHandler } = createComponent();
        const harness = component as unknown as SidebarHarness;
        dialogService.open.mockReturnValueOnce({ afterClosed: () => of(true) }).mockReturnValueOnce({ afterClosed: () => of('save') });

        await harness.logoutAsync();

        expect(unsavedHandler.save).toHaveBeenCalledOnce();
        expect(authService.onLogoutAsync).toHaveBeenCalledWith(true);
    });
});

describe('SidebarComponent admin behavior', () => {
    beforeEach(setupSidebarTestEnvironment);
    afterEach(cleanupSidebarTestEnvironment);

    it('opens admin panel with SSO code for admin users', () => {
        const { authService, component } = createComponent();
        const harness = component as unknown as SidebarHarness;
        const adminWindow = createAdminWindowMock();
        authService.isAdmin.set(true);
        authService.startAdminSso.mockReturnValueOnce(of({ code: ADMIN_SSO_CODE }));
        mockAdminWindow(adminWindow);

        harness.openAdminPanel();

        expect(window.open).toHaveBeenCalledWith(ADMIN_LOADING_URL, '_blank');
        expect(adminWindow.location.assign).toHaveBeenCalledWith(`http://localhost:4300/#code=${ADMIN_SSO_CODE}`);
    });

    it('closes admin loading window and shows toast when SSO fails', () => {
        const { authService, component, toastService } = createComponent();
        const harness = component as unknown as SidebarHarness;
        const adminWindow = createAdminWindowMock();
        authService.isAdmin.set(true);
        authService.startAdminSso.mockReturnValueOnce(
            new Observable(subscriber => {
                subscriber.error(new Error('sso failed'));
            }),
        );
        mockAdminWindow(adminWindow);

        harness.openAdminPanel();

        expect(adminWindow.close).toHaveBeenCalledOnce();
        expect(toastService.error).toHaveBeenCalledWith('USER_MANAGE.ADMIN_SSO_ERROR');
    });
});

function setupSidebarTestEnvironment(): void {
    TestBed.resetTestingModule();
    Object.defineProperty(window, 'matchMedia', {
        configurable: true,
        value: vi.fn(() => ({
            matches: false,
            addEventListener: vi.fn(),
            removeEventListener: vi.fn(),
        })),
    });
}

function cleanupSidebarTestEnvironment(): void {
    document.body.classList.remove('fd-scroll-lock');
    document.body.style.removeProperty('inset-block-start');
    document.body.style.removeProperty('padding-inline-end');
    vi.restoreAllMocks();
}

function expectScrollLockApplied(): void {
    expect(document.body.classList.contains('fd-scroll-lock')).toBe(true);
    expect(document.body.style.insetBlockStart).toBe(`-${LOCKED_SCROLL_Y}px`);
    expect(document.body.style.paddingInlineEnd).toBe('17px');
}

function expectScrollLockReleased(): void {
    expect(document.body.classList.contains('fd-scroll-lock')).toBe(false);
    expect(document.body.style.insetBlockStart).toBe('');
    expect(document.body.style.paddingInlineEnd).toBe('');
}

function createComponent(options: { isMobileViewport?: boolean } = {}): {
    authService: ReturnType<typeof createAuthServiceMock>;
    component: SidebarComponent;
    dialogService: { open: ReturnType<typeof vi.fn> };
    router: { events: Subject<unknown>; url: string };
    routerEvents: Subject<unknown>;
    toastService: { error: ReturnType<typeof vi.fn> };
    unsavedHandler: { discard: ReturnType<typeof vi.fn>; hasChanges: ReturnType<typeof vi.fn>; save: ReturnType<typeof vi.fn> };
} {
    Object.defineProperty(window, 'matchMedia', {
        configurable: true,
        value: vi.fn(() => ({
            matches: options.isMobileViewport ?? false,
            addEventListener: vi.fn(),
            removeEventListener: vi.fn(),
        })),
    });

    const routerEvents = new Subject<unknown>();
    const router = { events: routerEvents, url: '/' };
    const authService = createAuthServiceMock();
    const dialogService = createDialogServiceMock();
    const toastService = { error: vi.fn() };
    const unsavedHandler = {
        discard: vi.fn(),
        hasChanges: vi.fn(() => true),
        save: vi.fn(),
    };

    TestBed.configureTestingModule({
        imports: [SidebarComponent],
        providers: [
            { provide: AuthService, useValue: authService },
            {
                provide: UserService,
                useValue: {
                    user: signal(null),
                    clearUser: vi.fn(),
                    getInfoSilently: vi.fn(() => of(null)),
                },
            },
            {
                provide: DashboardService,
                useValue: {
                    getSnapshotSilently: vi.fn(() =>
                        of({
                            dailyGoal: 0,
                            statistics: {
                                totalCalories: 0,
                            },
                        }),
                    ),
                },
            },
            {
                provide: NotificationService,
                useValue: {
                    unreadCount: signal(0),
                    fetchUnreadCount: vi.fn(),
                    ensureNotificationsLoaded: vi.fn(),
                },
            },
            { provide: UnsavedChangesService, useValue: { getHandler: vi.fn(() => unsavedHandler) } },
            { provide: FdUiDialogService, useValue: dialogService },
            { provide: FdUiToastService, useValue: toastService },
            { provide: TranslateService, useValue: { instant: (key: string): string => key } },
            { provide: Router, useValue: router },
            { provide: SIDEBAR_MOBILE_VIEWPORT_QUERY, useValue: '(max-width: 767px)' },
            { provide: ADMIN_LOADING_URL_TTL_MS, useValue: ADMIN_LOADING_TTL_MS },
        ],
    });
    TestBed.overrideComponent(SidebarComponent, {
        set: {
            imports: [],
            template: '',
        },
    });

    const fixture = TestBed.createComponent(SidebarComponent);
    fixture.detectChanges();

    return { authService, component: fixture.componentInstance, dialogService, router, routerEvents, toastService, unsavedHandler };
}

function createAuthServiceMock(): {
    isAdmin: WritableSignal<boolean>;
    isAuthenticated: WritableSignal<boolean>;
    isDietologist: WritableSignal<boolean>;
    isPremium: WritableSignal<boolean>;
    onLogoutAsync: ReturnType<typeof vi.fn>;
    startAdminSso: ReturnType<typeof vi.fn>;
} {
    return {
        isAuthenticated: signal(true),
        isPremium: signal(false),
        isDietologist: signal(false),
        isAdmin: signal(false),
        onLogoutAsync: vi.fn().mockResolvedValue(void 0),
        startAdminSso: vi.fn(),
    };
}

function createAdminWindowMock(): {
    close: ReturnType<typeof vi.fn>;
    location: {
        assign: ReturnType<typeof vi.fn>;
    };
} {
    return {
        close: vi.fn(),
        location: {
            assign: vi.fn(),
        },
    };
}

function mockAdminWindow(adminWindow: ReturnType<typeof createAdminWindowMock>): void {
    environment.adminAppUrl = 'http://localhost:4300';
    vi.spyOn(URL, 'createObjectURL').mockReturnValue(ADMIN_LOADING_URL);
    vi.spyOn(URL, 'revokeObjectURL').mockImplementation(() => {});
    vi.spyOn(window, 'open').mockReturnValue(adminWindow as unknown as Window);
}

function createDialogServiceMock(): {
    open: ReturnType<typeof vi.fn>;
} {
    return {
        open: vi.fn((): { afterClosed: () => Observable<null> } => ({
            afterClosed: () => of(null),
        })),
    };
}
