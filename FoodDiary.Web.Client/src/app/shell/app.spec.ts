import { DOCUMENT, ViewportScroller } from '@angular/common';
import { ChangeDetectionStrategy, Component, DestroyRef, inject, NO_ERRORS_SCHEMA, type Signal, signal } from '@angular/core';
import { type ComponentFixture, TestBed } from '@angular/core/testing';
import {
    ActivatedRoute,
    NavigationEnd,
    provideRouter,
    type Route,
    RouteConfigLoadEnd,
    RouteConfigLoadStart,
    Router,
    RouterOutlet,
    Scroll,
} from '@angular/router';
import { TranslatePipe } from '@ngx-translate/core';
import { FdUiDialogService } from 'fd-ui-kit/dialog/fd-ui-dialog.service';
import { Subject } from 'rxjs';
import { afterEach, beforeEach, describe, expect, it, vi } from 'vitest';

import { waitForAsyncTasksAsync } from '../../testing/async-testing';
import { provideTranslateTesting } from '../../testing/translate-testing.module';
import { AuthService } from '../services/auth.service';
import { GlobalLoadingService } from '../services/global-loading.service';
import { RouteLoadingService } from '../services/route-loading.service';
import { SeoService } from '../services/seo.service';
import { SessionEventsService } from '../shared/auth/session-events.service';
import { editorUnsavedChangesGuard } from '../shared/forms/editor-unsaved-changes.guard';
import { LocalizationService } from '../shared/i18n/localization.service';
import { NotificationRealtimeService } from '../shared/notifications/notification-realtime.service';
import { PushNotificationService } from '../shared/notifications/push-notification.service';
import { PushNotificationReadSyncService } from '../shared/notifications/push-notification-read-sync.service';
import { BrowserWindowService } from '../shared/platform/browser-window.service';
import { PwaInstallPromotionService } from '../shared/service-worker/pwa-install-promotion.service';
import { ThemeService } from '../shared/theme/theme.service';
import { AppComponent } from './app';

const NAVIGATION_ID = 1;
const SHELL_TEST_TIMEOUT_MS = 15000;
const disposePrivateEditor = vi.fn();
const createLatePrivateEditor = vi.fn();
const disposeRootDialog = vi.fn();
const saveRootDialog = vi.fn();
let allowPrivateEditorDeparture = false;

afterEach(() => {
    vi.unstubAllGlobals();
});

@Component({ template: 'Private Alice draft', changeDetection: ChangeDetectionStrategy.OnPush })
class SessionPrivateEditor {
    public constructor() {
        inject(DestroyRef).onDestroy(disposePrivateEditor);
    }

    public async canLeaveAsync(): Promise<boolean> {
        return new Promise<boolean>(resolve => {
            resolve(allowPrivateEditorDeparture);
        });
    }
}

@Component({ template: 'Late private draft', changeDetection: ChangeDetectionStrategy.OnPush })
class SessionLatePrivateEditor {
    public constructor() {
        createLatePrivateEditor();
    }
}

@Component({ template: 'Public sign in', changeDetection: ChangeDetectionStrategy.OnPush })
class SessionPublicLogin {}

@Component({
    template: '<p>Private Alice overlay draft</p><button type="button" (click)="save()">Save draft</button>',
    changeDetection: ChangeDetectionStrategy.OnPush,
})
class SessionRootDialog {
    public constructor() {
        inject(DestroyRef).onDestroy(disposeRootDialog);
    }

    protected save(): void {
        saveRootDialog();
    }
}

describe('AppComponent session-owned route lifetime', () => {
    it('keeps the skip link on the current document, including its query parameters', async () => {
        const { fixture, rootElement, router } = await createSessionShellAsync();
        allowPrivateEditorDeparture = true;
        await router.navigateByUrl('/explore?category=public#previous');
        fixture.detectChanges();
        expect(router.url).toBe('/explore?category=public#previous');

        expect(
            rootElement.querySelector<HTMLAnchorElement>('.fd-skip-nav')?.getAttribute('href'),
            'Skip navigation must preserve the current document instead of resolving against the root base URL',
        ).toBe('/explore?category=public#main-content');
    });

    it.each(['local', 'logout'])(
        'destroys a dirty owner even when %s departure cannot load the public destination',
        async reason => {
            const { fixture, rootElement, authenticated, sessions, router } = await createSessionShellAsync();
            expect(rootElement.textContent).toContain('Private Alice draft');
            const dialog = TestBed.inject(FdUiDialogService).open(SessionRootDialog);
            dialog.acquireDismissalLock();
            fixture.detectChanges();
            const overlayElement = dialog.overlayRef.overlayElement;
            const backdrop = dialog.overlayRef.backdropElement;
            if (backdrop === null) {
                throw new Error('Dialog backdrop is missing');
            }
            backdrop.click();
            expect(overlayElement.isConnected).toBe(true);
            expect(disposeRootDialog).not.toHaveBeenCalled();
            authenticated.set(false);
            if (reason === 'local') {
                sessions.notifyLocalSessionInvalidating();
                sessions.notifyLocalSessionInvalidated();
            } else {
                sessions.notifySessionEnded();
            }
            fixture.detectChanges();
            expect(disposePrivateEditor).toHaveBeenCalledOnce();
            expect(overlayElement.isConnected).toBe(false);
            expect(disposeRootDialog).toHaveBeenCalledOnce();
            expect(saveRootDialog).not.toHaveBeenCalled();

            await sessions.runSessionEndNavigationAsync(async () => {
                await expect(router.navigateByUrl('/login')).rejects.toThrow('Public destination is unavailable');
            });
            fixture.detectChanges();
            expect(rootElement.textContent).not.toContain('Private Alice draft');
            expect(disposePrivateEditor).toHaveBeenCalledOnce();

            await router.navigateByUrl('/explore');
            fixture.detectChanges();
            expect(rootElement.textContent).toContain('Public sign in');
        },
        SHELL_TEST_TIMEOUT_MS,
    );
});

describe('AppComponent pending private navigation lifetime', () => {
    it.each(['dietologist', 'change-password-required'])(
        'keeps a late pre-invalidation %s navigation closed without a session',
        async path => {
            let release = (): void => {};
            let loading = (): void => {};
            const started = new Promise<void>(resolve => {
                loading = resolve;
            });
            const destination = new Promise<typeof SessionLatePrivateEditor>(resolve => {
                release = (): void => {
                    resolve(SessionLatePrivateEditor);
                };
            });
            const { fixture, rootElement, authenticated, sessions, router } = await createSessionShellAsync({
                path,
                loadComponent: async () => {
                    loading();
                    return destination;
                },
                data: { requiresSession: true, ...(path === 'change-password-required' ? { shell: 'public' } : {}) },
            });
            allowPrivateEditorDeparture = true;
            const navigation = router.navigateByUrl(`/${path}`);
            await started;
            authenticated.set(false);
            sessions.notifyLocalSessionInvalidated();
            fixture.detectChanges();
            release();
            await navigation;
            fixture.detectChanges();

            expect(createLatePrivateEditor).not.toHaveBeenCalled();
            expect(disposePrivateEditor).toHaveBeenCalledOnce();
            expect(rootElement.textContent).not.toContain('private draft');
            await expect(router.navigateByUrl('/login')).rejects.toThrow('Public destination is unavailable');
            await router.navigateByUrl('/explore');
            fixture.detectChanges();
            expect(rootElement.textContent).toContain('Public sign in');
        },
        SHELL_TEST_TIMEOUT_MS,
    );
});

describe('AppComponent guest dialog lifetime', () => {
    it('preserves a locked guest dialog across ordinary router navigation', async () => {
        const { fixture, authenticated, router } = await createSessionShellAsync();
        allowPrivateEditorDeparture = true;
        await router.navigateByUrl('/explore');
        authenticated.set(false);
        fixture.detectChanges();
        const dialog = TestBed.inject(FdUiDialogService).open(SessionRootDialog);
        dialog.acquireDismissalLock();
        fixture.detectChanges();
        const overlayElement = dialog.overlayRef.overlayElement;

        await router.navigateByUrl('/explore?section=guest');
        fixture.detectChanges();

        expect(overlayElement.isConnected).toBe(true);
        expect(disposeRootDialog).not.toHaveBeenCalled();
        expect(saveRootDialog).not.toHaveBeenCalled();
        dialog.close();
        expect(disposeRootDialog).toHaveBeenCalledOnce();
    });
});

describe('AppComponent shell behavior', () => {
    beforeEach(() => {
        TestBed.resetTestingModule();
    });

    it(
        'mirrors lazy route loading events into RouteLoadingService',
        async () => {
            const { routeLoadingService, routerEvents } = await createComponentAsync();
            const route = { path: 'dashboard' } satisfies Route;

            routerEvents.next(new RouteConfigLoadStart(route));
            routerEvents.next(new RouteConfigLoadEnd(route));

            expect(routeLoadingService.beginLoad).toHaveBeenCalledOnce();
            expect(routeLoadingService.endLoad).toHaveBeenCalledOnce();
        },
        SHELL_TEST_TIMEOUT_MS,
    );

    it(
        'prepares route localization, theme and SEO after navigation',
        async () => {
            const { localizationService, router, routerEvents, seoService, themeService } = await createComponentAsync();

            routerEvents.next(new NavigationEnd(NAVIGATION_ID, '/dashboard', '/dashboard'));
            await waitForAsyncTasksAsync();
            await waitForAsyncTasksAsync();

            expect(themeService.applyThemeForRoute).toHaveBeenCalledWith('/dashboard');
            expect(localizationService.loadTranslationsForRouteAsync).toHaveBeenCalledWith('/dashboard');
            expect(seoService.update).toHaveBeenCalledWith({
                titleKey: 'SEO.DASHBOARD.TITLE',
                descriptionKey: 'SEO.DASHBOARD.DESCRIPTION',
                path: router.url,
            });
        },
        SHELL_TEST_TIMEOUT_MS,
    );

    it(
        'uses compact mobile navigation spacing only for dashboard routes',
        async () => {
            const { component, routerEvents } = await createComponentAsync();
            const compactNavigation = (component as unknown as { usesCompactMobileNavigation: Signal<boolean> })
                .usesCompactMobileNavigation;

            expect(compactNavigation()).toBe(true);

            routerEvents.next(new NavigationEnd(NAVIGATION_ID, '/products', '/products'));

            expect(compactNavigation()).toBe(false);

            routerEvents.next(new NavigationEnd(NAVIGATION_ID, '/dashboard?date=2026-06-11', '/dashboard?date=2026-06-11'));

            expect(compactNavigation()).toBe(true);
        },
        SHELL_TEST_TIMEOUT_MS,
    );

    it(
        'hides the authenticated shell on public routes for signed-in users',
        async () => {
            const { component } = await createComponentAsync({ isAuthenticated: true, shell: 'public' });
            const usesAuthenticatedShell = (component as unknown as { usesAuthenticatedShell: Signal<boolean> }).usesAuthenticatedShell;

            expect(usesAuthenticatedShell()).toBe(false);
        },
        SHELL_TEST_TIMEOUT_MS,
    );

    it(
        'shows the authenticated shell on workspace routes for signed-in users',
        async () => {
            const { component } = await createComponentAsync({ isAuthenticated: true, shell: 'authenticated' });
            const usesAuthenticatedShell = (component as unknown as { usesAuthenticatedShell: Signal<boolean> }).usesAuthenticatedShell;

            expect(usesAuthenticatedShell()).toBe(true);
        },
        SHELL_TEST_TIMEOUT_MS,
    );

    it(
        'reveals the app after the initial route resolves',
        async () => {
            const { routerEvents } = await createComponentAsync();
            const documentRef = TestBed.inject(DOCUMENT);
            documentRef.documentElement.classList.add('fd-session-route-pending');

            routerEvents.next(new NavigationEnd(NAVIGATION_ID, '/', '/dashboard'));

            expect(documentRef.documentElement.classList.contains('fd-session-route-pending')).toBe(false);
        },
        SHELL_TEST_TIMEOUT_MS,
    );
});

describe('AppComponent update departure checks', () => {
    it('allows an update when no editor is active', async () => {
        const { component } = await createComponentAsync();
        const shell = component as unknown as { confirmAppReloadAsync: () => Promise<boolean> };
        expect(await shell.confirmAppReloadAsync()).toBe(true);
    });
});

describe('AppComponent sidebar behavior', () => {
    beforeEach(() => {
        TestBed.resetTestingModule();
    });

    it(
        'toggles the transient desktop sidebar state',
        async () => {
            const { component } = await createComponentAsync();
            const harness = component as unknown as { isSidebarCollapsed: Signal<boolean>; toggleSidebar: () => void };

            expect(harness.isSidebarCollapsed()).toBe(false);

            harness.toggleSidebar();
            expect(harness.isSidebarCollapsed()).toBe(true);

            harness.toggleSidebar();
            expect(harness.isSidebarCollapsed()).toBe(false);
        },
        SHELL_TEST_TIMEOUT_MS,
    );
});

describe('AppComponent public navigation', () => {
    it.each([false, true])('scrolls both viewports to the top, respecting reduced motion=%s', async reducedMotion => {
        const { fixture, rootElement, routerEvents, viewportScroll, viewportScroller } = await createComponentAsync({ reducedMotion });
        const navigation = new NavigationEnd(NAVIGATION_ID, '/ru/food-diary', '/ru/food-diary');
        routerEvents.next(navigation);
        fixture.detectChanges();
        routerEvents.next(new Scroll(navigation, null, null));

        const behavior = reducedMotion ? 'instant' : 'smooth';
        expect(viewportScroll).toHaveBeenCalledExactlyOnceWith({ top: 0, left: 0, behavior });
        expect(viewportScroller.scrollToPosition).toHaveBeenCalledExactlyOnceWith([0, 0], { behavior });
        expect(rootElement.querySelector('.fd-root')?.classList.contains('fd-root--public-site')).toBe(true);
    });

    it('keeps the scroll position when only the public query or fragment changes', async () => {
        const { fixture, routerEvents, viewportScroll, viewportScroller } = await createComponentAsync();
        routerEvents.next(new NavigationEnd(NAVIGATION_ID, '/pricing', '/pricing'));
        fixture.detectChanges();
        const navigation = new NavigationEnd(NAVIGATION_ID + 1, '/pricing?auth=login#details', '/pricing?auth=login#details');
        routerEvents.next(navigation);
        routerEvents.next(new Scroll(navigation, null, 'details'));

        expect(viewportScroll).not.toHaveBeenCalled();
        expect(viewportScroller.scrollToPosition).not.toHaveBeenCalled();
    });

    it('resets the workspace scroll container after navigation', async () => {
        const { fixture, rootElement, routerEvents, viewportScroll, viewportScroller } = await createComponentAsync();
        const navigation = new NavigationEnd(NAVIGATION_ID, '/products', '/products');
        routerEvents.next(navigation);
        fixture.detectChanges();
        routerEvents.next(new Scroll(navigation, null, null));

        expect(viewportScroll).toHaveBeenCalledExactlyOnceWith({ top: 0, left: 0, behavior: 'instant' });
        expect(viewportScroller.scrollToPosition).toHaveBeenCalledExactlyOnceWith([0, 0], { behavior: 'instant' });
        expect(rootElement.querySelector('.fd-root')?.classList.contains('fd-root--public-site')).toBe(false);
    });

    it('honors navigation that explicitly owns its scroll position', async () => {
        const { fixture, routerEvents, viewportScroll, viewportScroller } = await createComponentAsync();
        const navigation = new NavigationEnd(NAVIGATION_ID, '/pricing', '/pricing');
        routerEvents.next(navigation);
        fixture.detectChanges();
        routerEvents.next(new Scroll(navigation, null, null, 'manual'));

        expect(viewportScroll).not.toHaveBeenCalled();
        expect(viewportScroller.scrollToPosition).not.toHaveBeenCalled();
    });
});

function stubMatchMedia(): void {
    vi.stubGlobal(
        'matchMedia',
        vi.fn((query: string): MediaQueryList => {
            const mediaQuery = {
                matches: false,
                media: query,
                onchange: null,
                addListener: vi.fn(),
                removeListener: vi.fn(),
                addEventListener: vi.fn(),
                removeEventListener: vi.fn(),
                dispatchEvent: vi.fn(() => true),
            };
            return mediaQuery;
        }),
    );
}

async function createSessionShellAsync(lateRoute?: Route): Promise<{
    fixture: ComponentFixture<AppComponent>;
    rootElement: HTMLElement;
    authenticated: ReturnType<typeof signal<boolean>>;
    sessions: SessionEventsService;
    router: Router;
}> {
    TestBed.resetTestingModule();
    disposePrivateEditor.mockClear();
    createLatePrivateEditor.mockClear();
    disposeRootDialog.mockClear();
    saveRootDialog.mockClear();
    allowPrivateEditorDeparture = false;
    stubMatchMedia();
    const authenticated = signal(true);
    TestBed.configureTestingModule({
        imports: [AppComponent],
        providers: [
            provideTranslateTesting(),
            provideRouter([
                {
                    path: 'editor',
                    component: SessionPrivateEditor,
                    data: { requiresSession: true },
                    canDeactivate: [editorUnsavedChangesGuard],
                },
                {
                    path: 'login',
                    data: { shell: 'public' },
                    loadComponent: async (): Promise<typeof SessionPublicLogin> =>
                        new Promise((_resolve, reject) => {
                            reject(new Error('Public destination is unavailable'));
                        }),
                },
                { path: 'explore', component: SessionPublicLogin },
                ...(lateRoute === undefined ? [] : [lateRoute]),
            ]),
            {
                provide: AuthService,
                useValue: {
                    isAuthenticated: authenticated,
                    isImpersonating: signal(false),
                    impersonationReason: signal<string | null>(null),
                    onLogoutAsync: vi.fn(),
                },
            },
            { provide: SeoService, useValue: { update: vi.fn() } },
            { provide: LocalizationService, useValue: { loadTranslationsForRouteAsync: vi.fn().mockResolvedValue(undefined) } },
            { provide: GlobalLoadingService, useValue: { isVisible: signal(false) } },
            { provide: RouteLoadingService, useValue: { isVisible: signal(false), beginLoad: vi.fn(), endLoad: vi.fn() } },
            { provide: ThemeService, useValue: { applyThemeForRoute: vi.fn() } },
            { provide: ViewportScroller, useValue: { scrollToPosition: vi.fn(), setHistoryScrollRestoration: vi.fn() } },
            { provide: BrowserWindowService, useValue: createBrowserWindowStub(false) },
            { provide: NotificationRealtimeService, useValue: {} },
            { provide: PushNotificationService, useValue: {} },
            { provide: PushNotificationReadSyncService, useValue: {} },
            { provide: PwaInstallPromotionService, useValue: { setActive: vi.fn(), shouldShowBanner: signal(false) } },
        ],
    });
    TestBed.overrideComponent(AppComponent, {
        set: { imports: [RouterOutlet, TranslatePipe], schemas: [NO_ERRORS_SCHEMA] },
    });
    await TestBed.compileComponents();
    const fixture = TestBed.createComponent(AppComponent);
    fixture.detectChanges();
    const router = TestBed.inject(Router);
    await router.navigateByUrl('/editor');
    fixture.detectChanges();
    const rootElement: unknown = fixture.nativeElement;
    if (!(rootElement instanceof HTMLElement)) {
        throw new Error('Shell root element is missing');
    }
    return {
        fixture,
        rootElement,
        authenticated,
        sessions: TestBed.inject(SessionEventsService),
        router,
    };
}

async function createComponentAsync(
    options: { isAuthenticated?: boolean; shell?: 'authenticated' | 'public'; reducedMotion?: boolean } = {},
): Promise<{
    fixture: ComponentFixture<AppComponent>;
    rootElement: HTMLElement;
    viewportScroll: ReturnType<typeof vi.fn>;
    viewportScroller: { scrollToPosition: ReturnType<typeof vi.fn>; setHistoryScrollRestoration: ReturnType<typeof vi.fn> };
    component: AppComponent;
    localizationService: { loadTranslationsForRouteAsync: ReturnType<typeof vi.fn> };
    routeLoadingService: { beginLoad: ReturnType<typeof vi.fn>; endLoad: ReturnType<typeof vi.fn> };
    router: { events: Subject<unknown>; url: string };
    routerEvents: Subject<unknown>;
    seoService: { update: ReturnType<typeof vi.fn> };
    themeService: { applyThemeForRoute: ReturnType<typeof vi.fn> };
}> {
    const routerEvents = new Subject<unknown>();
    const router = { events: routerEvents, url: '/dashboard' };
    const routeLoadingService = { beginLoad: vi.fn(), endLoad: vi.fn() };
    const localizationService = { loadTranslationsForRouteAsync: vi.fn().mockResolvedValue(void 0) };
    const seoService = { update: vi.fn() };
    const themeService = { applyThemeForRoute: vi.fn() };
    const viewportScroller = { scrollToPosition: vi.fn(), setHistoryScrollRestoration: vi.fn() };

    TestBed.configureTestingModule({
        imports: [AppComponent],
        providers: [
            {
                provide: AuthService,
                useValue: {
                    isAuthenticated: signal(options.isAuthenticated ?? false),
                    isImpersonating: signal(false),
                    impersonationReason: signal<string | null>(null),
                    onLogoutAsync: vi.fn().mockResolvedValue(void 0),
                },
            },
            { provide: Router, useValue: router },
            {
                provide: ActivatedRoute,
                useValue: createActivatedRouteStub(options.shell ?? 'authenticated'),
            },
            { provide: SeoService, useValue: seoService },
            { provide: LocalizationService, useValue: localizationService },
            { provide: GlobalLoadingService, useValue: { isVisible: signal(false) } },
            { provide: RouteLoadingService, useValue: routeLoadingService },
            { provide: ThemeService, useValue: themeService },
            { provide: ViewportScroller, useValue: viewportScroller },
            { provide: BrowserWindowService, useValue: createBrowserWindowStub(options.reducedMotion ?? false) },
            { provide: NotificationRealtimeService, useValue: {} },
            { provide: PushNotificationService, useValue: {} },
            { provide: PushNotificationReadSyncService, useValue: {} },
        ],
    });
    TestBed.overrideComponent(AppComponent, {
        set: {
            imports: [],
            template:
                '<div class="fd-root" [class.fd-root--public-site]="usesPublicSiteChrome()"><div #pageViewport class="fd-root__viewport"></div></div>',
        },
    });

    await TestBed.compileComponents();
    const fixture = TestBed.createComponent(AppComponent);
    fixture.detectChanges();
    const rootElement = fixture.nativeElement as HTMLElement;
    const viewportScroll = attachViewportScrollSpy(rootElement);

    return {
        fixture,
        rootElement,
        viewportScroll,
        viewportScroller,
        component: fixture.componentInstance,
        localizationService,
        routeLoadingService,
        router,
        routerEvents,
        seoService,
        themeService,
    };
}

function attachViewportScrollSpy(rootElement: HTMLElement): ReturnType<typeof vi.fn> {
    const viewportScroll = vi.fn();
    const viewportElement = rootElement.querySelector<HTMLElement>('.fd-root__viewport');
    if (viewportElement === null) {
        throw new Error('Shell viewport is missing');
    }
    viewportElement.scrollTo = viewportScroll;
    return viewportScroll;
}

function createBrowserWindowStub(reducedMotion: boolean): {
    isAvailable: () => boolean;
    matchMedia: () => Pick<MediaQueryList, 'matches'>;
} {
    return {
        isAvailable: (): boolean => true,
        matchMedia: (): Pick<MediaQueryList, 'matches'> => ({ matches: reducedMotion }),
    };
}

function createActivatedRouteStub(shell: 'authenticated' | 'public'): ActivatedRoute {
    const data = {
        shell,
        seo: {
            titleKey: 'SEO.DASHBOARD.TITLE',
            descriptionKey: 'SEO.DASHBOARD.DESCRIPTION',
        },
    };
    const route = {
        firstChild: null,
        snapshot: { data },
        pathFromRoot: [
            {
                snapshot: { data },
            },
        ],
    };

    return route as unknown as ActivatedRoute;
}
