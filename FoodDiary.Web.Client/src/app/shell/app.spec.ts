import { DOCUMENT, ViewportScroller } from '@angular/common';
import { ChangeDetectionStrategy, Component, DestroyRef, inject, NO_ERRORS_SCHEMA, type Signal, signal } from '@angular/core';
import { type ComponentFixture, DeferBlockBehavior, DeferBlockState, TestBed } from '@angular/core/testing';
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
import { FdTourHostComponent, FdTourService } from 'fd-tour';
import { FdUiDialogService } from 'fd-ui-kit/dialog/fd-ui-dialog.service';
import { Subject } from 'rxjs';
import { afterEach, beforeEach, describe, expect, it, vi } from 'vitest';

import { waitForAsyncTasksAsync } from '../../testing/async-testing';
import { provideTranslateTesting } from '../../testing/translate-testing.module';
import { MarketingConsentComponent } from '../components/shared/marketing-consent/marketing-consent';
import { PublicSiteFooterComponent } from '../features/public/contracts/public-site-chrome';
import { AuthService } from '../services/auth.service';
import { GlobalLoadingService } from '../services/global-loading.service';
import { RouteLoadingService } from '../services/route-loading.service';
import { SeoService } from '../services/seo.service';
import { AuthRecoveryStateService } from '../shared/auth/auth-recovery-state.service';
import { SessionEventsService } from '../shared/auth/session-events.service';
import { editorUnsavedChangesGuard } from '../shared/forms/editor-unsaved-changes.guard';
import { LocalizationService } from '../shared/i18n/localization.service';
import { MARKETING_ATTRIBUTION_ENABLED } from '../shared/marketing/marketing-attribution-enabled';
import { MarketingConsentService } from '../shared/marketing/marketing-consent.service';
import { NotificationRealtimeService } from '../shared/notifications/notification-realtime.service';
import { PushNotificationService } from '../shared/notifications/push-notification.service';
import { PushNotificationReadSyncService } from '../shared/notifications/push-notification-read-sync.service';
import { BrowserStorageService } from '../shared/platform/browser-storage.service';
import { BrowserWindowService } from '../shared/platform/browser-window.service';
import { PwaInstallPromotionService } from '../shared/service-worker/pwa-install-promotion.service';
import { ThemeService } from '../shared/theme/theme.service';
import { AppComponent } from './app';

const NAVIGATION_ID = 1;
const BROWSER_NOTIFICATION_CLIENTS = [NotificationRealtimeService, PushNotificationService, PushNotificationReadSyncService] as const;
const SHELL_TEST_TIMEOUT_MS = 15000;
const SESSION_SHELL_IMPORTS = [RouterOutlet, TranslatePipe, MarketingConsentComponent, PublicSiteFooterComponent, FdTourHostComponent];
const disposePrivateEditor = vi.fn();
const createLatePrivateEditor = vi.fn();
const disposeRootDialog = vi.fn();
const saveRootDialog = vi.fn();
let allowPrivateEditorDeparture = false;

afterEach(() => {
    vi.unstubAllGlobals();
});

@Component({ template: '<p id="tour-owner">Private Alice draft</p>', changeDetection: ChangeDetectionStrategy.OnPush })
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

@Component({ template: '<p id="tour-owner">Other page tour target</p>', changeDetection: ChangeDetectionStrategy.OnPush })
class SessionSecondTourOwner {}

@Component({ selector: 'fd-shell-public-catalog-test', template: 'Public catalog', changeDetection: ChangeDetectionStrategy.OnPush })
class SessionPublicCatalog {}

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

it('keeps the native main region and skip link on the active signed-out route', async () => {
    stubMatchMedia();
    TestBed.configureTestingModule({
        imports: [AppComponent],
        providers: [
            provideTranslateTesting(),
            provideRouter([
                { path: 'mobile/login', component: SessionPublicLogin, data: { shell: 'public' } },
                { path: 'explore', component: SessionPublicCatalog, data: { shell: 'public' } },
            ]),
            {
                provide: AuthService,
                useValue: { isAuthenticated: signal(false), isImpersonating: signal(false), impersonationReason: signal(null) },
            },
            { provide: AuthRecoveryStateService, useValue: { issue: signal(null) } },
            { provide: SeoService, useValue: { update: vi.fn() } },
            { provide: LocalizationService, useValue: { loadTranslationsForRouteAsync: vi.fn().mockResolvedValue(undefined) } },
            { provide: GlobalLoadingService, useValue: { isVisible: signal(false) } },
            { provide: RouteLoadingService, useValue: { isVisible: signal(false), beginLoad: vi.fn(), endLoad: vi.fn() } },
            { provide: ThemeService, useValue: { applyThemeForRoute: vi.fn() } },
            { provide: ViewportScroller, useValue: { scrollToPosition: vi.fn(), setHistoryScrollRestoration: vi.fn() } },
            { provide: BrowserWindowService, useValue: createBrowserWindowStub(false, false) },
            { provide: PwaInstallPromotionService, useValue: { setActive: vi.fn(), shouldShowBanner: signal(false) } },
        ],
    });
    const router = TestBed.inject(Router);
    await router.navigateByUrl('/mobile/login?returnUrl=%2Fdashboard#form');
    const fixture = TestBed.createComponent(AppComponent);
    fixture.detectChanges();
    const host = fixture.nativeElement as HTMLElement;
    const main = host.querySelector('main#main-content');

    expect(main?.querySelector('[role="region"]')?.textContent).toContain('Public sign in');
    expect(host.querySelector('a.fd-skip-nav')?.getAttribute('href')).toBe('/mobile/login?returnUrl=%2Fdashboard#main-content');
    expect(host.querySelector('fd-sidebar')).toBeNull();
    expect(host.querySelector('fd-quick-meal-drawer')).toBeNull();

    await router.navigateByUrl('/explore?category=public#preview');
    await fixture.whenStable();
    fixture.detectChanges();

    expect(main?.querySelector('[role="region"]')?.textContent).toContain('Public catalog');
    expect(main?.textContent).not.toContain('Public sign in');
    expect(host.querySelector('a.fd-skip-nav')?.getAttribute('href')).toBe('/explore?category=public#main-content');
});

it(
    'keeps the tour on the same page and closes it after a successful departure',
    async () => {
        const { fixture, rootElement, router } = await createSessionShellAsync();
        await vi.waitFor(() => {
            fixture.detectChanges();
            expect(rootElement.querySelector('fd-tour-host')).not.toBeNull();
        });
        const tour = TestBed.inject(FdTourService);
        const definition = {
            id: 'editor-navigation',
            version: 1,
            steps: [{ id: 'start', target: '#tour-owner', title: 'Editor help' }],
        };
        expect(tour.start(definition, { force: true })).toBe(true);

        expect(await router.navigateByUrl('/editor?filter=test#details')).toBe(true);
        fixture.detectChanges();
        expect(tour.activeTour()).toBe(definition);
        expect(await router.navigateByUrl('/explore')).toBe(false);
        expect(tour.activeTour()).toBe(definition);

        allowPrivateEditorDeparture = true;
        expect(await router.navigateByUrl('/explore')).toBe(true);
        fixture.detectChanges();
        expect(tour.snapshot()).toBeNull();
    },
    SHELL_TEST_TIMEOUT_MS,
);

it.each([
    { page: 'the same page', url: '/editor?filter=test#details', samePage: true },
    { page: 'a different page', url: '/explore', samePage: false },
    { page: 'a different page with the same target selector', url: '/another-tour', samePage: false },
])(
    'resolves a tour started before the host mounts after navigating to $page',
    async ({ url, samePage }) => {
        const { fixture, rootElement, router } = await createSessionShellAsync(
            { path: 'another-tour', component: SessionSecondTourOwner },
            false,
            { deferBlockBehavior: DeferBlockBehavior.Manual },
        );
        expect(rootElement.querySelector('fd-tour-host')).toBeNull();
        expect(await router.navigateByUrl('/editor?filter=initial#intro')).toBe(true);
        fixture.detectChanges();
        const tour = TestBed.inject(FdTourService);
        const definition = {
            id: 'editor-deferred-navigation',
            version: 1,
            steps: [{ id: 'start', target: '#tour-owner', title: 'Editor help' }],
        };
        expect(tour.start(definition, { force: true })).toBe(true);
        const target = tour.target();
        expect(target?.isConnected).toBe(true);

        allowPrivateEditorDeparture = true;
        expect(await router.navigateByUrl(url)).toBe(true);
        fixture.detectChanges();
        expect(rootElement.querySelector('fd-tour-host')).toBeNull();
        expect(target?.isConnected).toBe(samePage);
        if (url === '/another-tour') {
            expect(rootElement.querySelector('#tour-owner')?.textContent).toBe('Other page tour target');
        }

        for (const block of await fixture.getDeferBlocks()) {
            await block.render(DeferBlockState.Complete);
        }
        fixture.detectChanges();

        expect(rootElement.querySelector('fd-tour-host')).not.toBeNull();
        expect(tour.activeTour()).toBe(samePage ? definition : null);
    },
    SHELL_TEST_TIMEOUT_MS,
);

describe('AppComponent session-owned route lifetime', () => {
    it('mounts the marketing notice only on public marketing pages', async () => {
        const { fixture, rootElement, router } = await createSessionShellAsync({ path: '', component: SessionPublicLogin });
        expect(rootElement.querySelector('fd-marketing-consent'), 'The diary must have no marketing consent block').toBeNull();
        allowPrivateEditorDeparture = true;
        await router.navigateByUrl('/');
        fixture.detectChanges();
        expect(rootElement.querySelector('fd-marketing-consent')).not.toBeNull();
        await router.navigateByUrl('/explore');
        fixture.detectChanges();
        expect(rootElement.querySelector('fd-marketing-consent')).toBeNull();
    });

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

describe('AppComponent platform and support session controls', () => {
    it('keeps browser-only notification clients dormant when the server shell renders a workspace route', async () => {
        const { rootElement, startNotificationClient } = await createSessionShellAsync(undefined, false, { browserAvailable: false });

        expect(rootElement.textContent).toContain('Private Alice draft');
        expect(startNotificationClient).not.toHaveBeenCalled();
    });

    it('renders the impersonation reason and stops the support session from its banner', async () => {
        const reason = 'Support ticket example';
        const { rootElement, startNotificationClient, endSupportSession, setInstallPromotionActive } = await createSessionShellAsync(
            undefined,
            false,
            { impersonationReason: reason },
        );
        const banner = rootElement.querySelector('.fd-impersonation-banner');
        const endSession = banner?.querySelector('button');
        if (banner === null || !(endSession instanceof HTMLButtonElement)) {
            throw new Error('The support session banner did not render its end-session action');
        }

        expect(banner.getAttribute('role')).toBe('status');
        expect(banner.textContent).toContain(reason);
        expect(startNotificationClient).toHaveBeenCalledTimes(BROWSER_NOTIFICATION_CLIENTS.length);
        expect(setInstallPromotionActive).toHaveBeenLastCalledWith(false);
        endSession.click();
        expect(endSupportSession).toHaveBeenCalledExactlyOnceWith(false);
    });
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
            const { fixture, localizationService, router, routerEvents, seoService, themeService } = await createComponentAsync();

            routerEvents.next(new NavigationEnd(NAVIGATION_ID, '/dashboard', '/dashboard'));
            fixture.detectChanges();
            await waitForAsyncTasksAsync();
            await waitForAsyncTasksAsync();

            expect(themeService.applyThemeForRoute).toHaveBeenCalledWith('/dashboard', false);
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

    it('keeps public scroll position when a slash alias adds query or fragment parameters', async () => {
        const { fixture, routerEvents, viewportScroll, viewportScroller } = await createComponentAsync();
        routerEvents.next(new NavigationEnd(NAVIGATION_ID, '/pricing/', '/pricing/'));
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

describe('Public shell marketing settings focus', () => {
    it('moves focus to main after the first choice and returns it to the footer after updating the choice', async () => {
        const { fixture, rootElement, router } = await createSessionShellAsync({ path: '', component: SessionPublicLogin }, true);
        const storage = TestBed.inject(BrowserStorageService);
        vi.spyOn(storage, 'getJson').mockReturnValue(null);
        vi.spyOn(storage, 'setJson').mockReturnValue(undefined);
        vi.spyOn(storage, 'removeItem').mockReturnValue(undefined);
        allowPrivateEditorDeparture = true;
        await router.navigateByUrl('/');
        fixture.detectChanges();
        await fixture.whenStable();
        fixture.detectChanges();

        const reject = marketingButton(rootElement, 'fd-marketing-consent', 'MARKETING_CONSENT.REJECT_SHORT');
        reject.focus();
        reject.click();
        fixture.detectChanges();
        expect(TestBed.inject(MarketingConsentService).currentDecision()?.allowed).toBe(false);
        expect(rootElement.querySelector('.marketing-consent')).toBeNull();
        expect(document.activeElement?.id).toBe('main-content');

        const launcher = marketingButton(rootElement, 'fd-public-site-footer', 'MARKETING_CONSENT.SETTINGS');
        launcher.focus();
        launcher.click();
        fixture.detectChanges();
        await fixture.whenStable();
        expect(document.activeElement).toBe(rootElement.querySelector('.marketing-consent'));

        const allow = marketingButton(rootElement, 'fd-marketing-consent', 'MARKETING_CONSENT.ALLOW_SHORT');
        allow.focus();
        allow.click();
        fixture.detectChanges();
        expect(TestBed.inject(MarketingConsentService).currentDecision()?.allowed).toBe(true);
        expect(rootElement.querySelector('.marketing-consent')).toBeNull();
        expect(document.activeElement).toBe(launcher);

        launcher.click();
        fixture.detectChanges();
        expect(rootElement.querySelector('.marketing-consent')).not.toBeNull();
        await router.navigateByUrl('/explore');
        fixture.detectChanges();
        expect(rootElement.querySelector('fd-marketing-consent')).toBeNull();
        await router.navigateByUrl('/');
        fixture.detectChanges();
        await fixture.whenStable();
        fixture.detectChanges();
        expect(rootElement.querySelector('.marketing-consent')).toBeNull();
    });
});

function marketingButton(host: HTMLElement, owner: string, label: string): HTMLButtonElement {
    const button = Array.from(host.querySelectorAll<HTMLButtonElement>(`${owner} button`)).find(
        child => child.textContent.trim() === label,
    );
    if (button === undefined) {
        throw new Error(`Expected public shell button ${label}`);
    }
    return button;
}

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

type SessionShellFixture = {
    fixture: ComponentFixture<AppComponent>;
    rootElement: HTMLElement;
    authenticated: ReturnType<typeof signal<boolean>>;
    sessions: SessionEventsService;
    router: Router;
    startNotificationClient: ReturnType<typeof vi.fn>;
    endSupportSession: ReturnType<typeof vi.fn>;
    setInstallPromotionActive: ReturnType<typeof vi.fn>;
};

async function createSessionShellAsync(
    lateRoute?: Route,
    marketingAttributionEnabled = false,
    options: { browserAvailable?: boolean; deferBlockBehavior?: DeferBlockBehavior; impersonationReason?: string } = {},
): Promise<SessionShellFixture> {
    resetSessionShellFixture();
    stubMatchMedia();
    const authenticated = signal(true);
    const startNotificationClient = vi.fn(() => ({}));
    const endSupportSession = vi.fn<AuthService['onLogoutAsync']>().mockResolvedValue(undefined);
    const setInstallPromotionActive = vi.fn<PwaInstallPromotionService['setActive']>();
    TestBed.configureTestingModule({
        deferBlockBehavior: options.deferBlockBehavior,
        imports: [AppComponent],
        providers: [
            provideTranslateTesting(),
            { provide: MARKETING_ATTRIBUTION_ENABLED, useValue: marketingAttributionEnabled },
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
                    isImpersonating: signal(options.impersonationReason !== undefined),
                    impersonationReason: signal(options.impersonationReason ?? null),
                    onLogoutAsync: endSupportSession,
                },
            },
            { provide: SeoService, useValue: { update: vi.fn() } },
            { provide: LocalizationService, useValue: { loadTranslationsForRouteAsync: vi.fn().mockResolvedValue(undefined) } },
            { provide: GlobalLoadingService, useValue: { isVisible: signal(false) } },
            { provide: RouteLoadingService, useValue: { isVisible: signal(false), beginLoad: vi.fn(), endLoad: vi.fn() } },
            { provide: ThemeService, useValue: { applyThemeForRoute: vi.fn() } },
            { provide: ViewportScroller, useValue: { scrollToPosition: vi.fn(), setHistoryScrollRestoration: vi.fn() } },
            { provide: BrowserWindowService, useValue: createBrowserWindowStub(false, options.browserAvailable) },
            ...BROWSER_NOTIFICATION_CLIENTS.map(provide => ({ provide, useFactory: startNotificationClient })),
            { provide: PwaInstallPromotionService, useValue: { setActive: setInstallPromotionActive, shouldShowBanner: signal(false) } },
        ],
    });
    TestBed.overrideComponent(AppComponent, { set: { imports: SESSION_SHELL_IMPORTS, schemas: [NO_ERRORS_SCHEMA] } });
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
        startNotificationClient,
        endSupportSession,
        setInstallPromotionActive,
    };
}

function resetSessionShellFixture(): void {
    TestBed.resetTestingModule();
    disposePrivateEditor.mockClear();
    createLatePrivateEditor.mockClear();
    disposeRootDialog.mockClear();
    saveRootDialog.mockClear();
    allowPrivateEditorDeparture = false;
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

function createBrowserWindowStub(
    reducedMotion: boolean,
    available = true,
): {
    isAvailable: () => boolean;
    matchMedia: () => Pick<MediaQueryList, 'matches'>;
} {
    return {
        isAvailable: (): boolean => available,
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
