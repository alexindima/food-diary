import { DOCUMENT, ViewportScroller } from '@angular/common';
import { type Signal, signal } from '@angular/core';
import { type ComponentFixture, TestBed } from '@angular/core/testing';
import { ActivatedRoute, NavigationEnd, type Route, RouteConfigLoadEnd, RouteConfigLoadStart, Router, Scroll } from '@angular/router';
import { Subject } from 'rxjs';
import { beforeEach, describe, expect, it, vi } from 'vitest';

import { waitForAsyncTasksAsync } from '../../testing/async-testing';
import { AuthService } from '../services/auth.service';
import { GlobalLoadingService } from '../services/global-loading.service';
import { RouteLoadingService } from '../services/route-loading.service';
import { SeoService } from '../services/seo.service';
import { LocalizationService } from '../shared/i18n/localization.service';
import { NotificationRealtimeService } from '../shared/notifications/notification-realtime.service';
import { PushNotificationService } from '../shared/notifications/push-notification.service';
import { BrowserWindowService } from '../shared/platform/browser-window.service';
import { ThemeService } from '../shared/theme/theme.service';
import { AppComponent } from './app';

const NAVIGATION_ID = 1;
const SHELL_TEST_TIMEOUT_MS = 15000;

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

    it('preserves immediate document scrolling for workspace routes', async () => {
        const { fixture, rootElement, routerEvents, viewportScroll, viewportScroller } = await createComponentAsync();
        const navigation = new NavigationEnd(NAVIGATION_ID, '/products', '/products');
        routerEvents.next(navigation);
        fixture.detectChanges();
        routerEvents.next(new Scroll(navigation, null, null));

        expect(viewportScroll).not.toHaveBeenCalled();
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
