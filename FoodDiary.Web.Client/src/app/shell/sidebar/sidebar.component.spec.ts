import { signal, type WritableSignal } from '@angular/core';
import { TestBed } from '@angular/core/testing';
import { NavigationEnd, Router } from '@angular/router';
import { TranslateService } from '@ngx-translate/core';
import { FdUiDialogService } from 'fd-ui-kit/dialog/fd-ui-dialog.service';
import { FdUiToastService } from 'fd-ui-kit/toast/fd-ui-toast.service';
import { type Observable, of, Subject } from 'rxjs';
import { afterEach, beforeEach, describe, expect, it, vi } from 'vitest';

import { ADMIN_LOADING_URL_TTL_MS, SIDEBAR_MOBILE_VIEWPORT_QUERY } from '../../config/runtime-ui.tokens';
import { DashboardService } from '../../features/dashboard/api/dashboard.service';
import { AuthService } from '../../services/auth.service';
import { NotificationService } from '../../services/notification.service';
import { UnsavedChangesService } from '../../services/unsaved-changes.service';
import { UserService } from '../../shared/api/user.service';
import { SidebarComponent } from './sidebar.component';
import type { MobileSheetId, SidebarRouteItem } from './sidebar-lib/sidebar.models';

const NAVIGATION_ID = 1;
const ADMIN_LOADING_TTL_MS = 100;

type SidebarHarness = {
    logoutAsync: () => Promise<void>;
    mobileSheet: WritableSignal<MobileSheetId>;
    onRouteSelected: (item: SidebarRouteItem) => void;
    pendingRoute: WritableSignal<string | null>;
    toggleMobileFood: (trigger?: HTMLElement) => void;
    toggleUserMenu: (trigger?: HTMLElement) => void;
};

describe('SidebarComponent behavior', () => {
    beforeEach(() => {
        TestBed.resetTestingModule();
        Object.defineProperty(window, 'matchMedia', {
            configurable: true,
            value: vi.fn(() => ({
                matches: false,
                addEventListener: vi.fn(),
                removeEventListener: vi.fn(),
            })),
        });
    });

    afterEach(() => {
        vi.restoreAllMocks();
    });

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

    it('does not logout when unsaved changes confirmation is cancelled', async () => {
        const { authService, component, dialogService } = createComponent();
        const harness = component as unknown as SidebarHarness;
        dialogService.open.mockReturnValue({ afterClosed: () => of(null) });

        await harness.logoutAsync();

        expect(authService.onLogoutAsync).not.toHaveBeenCalled();
    });

    it('discards unsaved changes before logout when confirmed', async () => {
        const { authService, component, dialogService, unsavedHandler } = createComponent();
        const harness = component as unknown as SidebarHarness;
        dialogService.open.mockReturnValue({ afterClosed: () => of('discard') });

        await harness.logoutAsync();

        expect(unsavedHandler.discard).toHaveBeenCalledOnce();
        expect(authService.onLogoutAsync).toHaveBeenCalledWith(true);
    });
});

function createComponent(): {
    authService: { onLogoutAsync: ReturnType<typeof vi.fn> };
    component: SidebarComponent;
    dialogService: { open: ReturnType<typeof vi.fn> };
    router: { events: Subject<unknown>; url: string };
    routerEvents: Subject<unknown>;
    unsavedHandler: { discard: ReturnType<typeof vi.fn>; hasChanges: ReturnType<typeof vi.fn>; save: ReturnType<typeof vi.fn> };
} {
    const routerEvents = new Subject<unknown>();
    const router = { events: routerEvents, url: '/' };
    const authService = createAuthServiceMock();
    const dialogService = createDialogServiceMock();
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
            { provide: FdUiToastService, useValue: { error: vi.fn() } },
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

    return { authService, component: fixture.componentInstance, dialogService, router, routerEvents, unsavedHandler };
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
        onLogoutAsync: vi.fn().mockResolvedValue(undefined),
        startAdminSso: vi.fn(),
    };
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
