import { signal } from '@angular/core';
import { TestBed } from '@angular/core/testing';
import { ActivatedRoute, NavigationEnd, type Route, RouteConfigLoadEnd, RouteConfigLoadStart, Router } from '@angular/router';
import { Subject } from 'rxjs';
import { beforeEach, describe, expect, it, vi } from 'vitest';

import { AuthService } from '../services/auth.service';
import { GlobalLoadingService } from '../services/global-loading.service';
import { LocalizationService } from '../services/localization.service';
import { NotificationRealtimeService } from '../services/notification-realtime.service';
import { PushNotificationService } from '../services/push-notification.service';
import { RouteLoadingService } from '../services/route-loading.service';
import { SeoService } from '../services/seo.service';
import { ThemeService } from '../services/theme.service';
import { AppComponent } from './app.component';

const NAVIGATION_ID = 1;

describe('AppComponent shell behavior', () => {
    beforeEach(() => {
        TestBed.resetTestingModule();
    });

    it('mirrors lazy route loading events into RouteLoadingService', async () => {
        const { routeLoadingService, routerEvents } = await createComponentAsync();
        const route = { path: 'dashboard' } satisfies Route;

        routerEvents.next(new RouteConfigLoadStart(route));
        routerEvents.next(new RouteConfigLoadEnd(route));

        expect(routeLoadingService.beginLoad).toHaveBeenCalledOnce();
        expect(routeLoadingService.endLoad).toHaveBeenCalledOnce();
    });

    it('prepares route localization, theme and SEO after navigation', async () => {
        const { localizationService, router, routerEvents, seoService, themeService } = await createComponentAsync();

        routerEvents.next(new NavigationEnd(NAVIGATION_ID, '/dashboard', '/dashboard'));
        await Promise.resolve();
        await Promise.resolve();

        expect(themeService.applyThemeForRoute).toHaveBeenCalledWith('/dashboard');
        expect(localizationService.loadTranslationsForRouteAsync).toHaveBeenCalledWith('/dashboard');
        expect(seoService.update).toHaveBeenCalledWith({
            titleKey: 'SEO.DASHBOARD.TITLE',
            descriptionKey: 'SEO.DASHBOARD.DESCRIPTION',
            path: router.url,
        });
    });
});

async function createComponentAsync(): Promise<{
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
    const localizationService = { loadTranslationsForRouteAsync: vi.fn().mockResolvedValue(undefined) };
    const seoService = { update: vi.fn() };
    const themeService = { applyThemeForRoute: vi.fn() };

    TestBed.configureTestingModule({
        imports: [AppComponent],
        providers: [
            {
                provide: AuthService,
                useValue: {
                    isAuthenticated: signal(false),
                    isImpersonating: signal(false),
                    impersonationReason: signal<string | null>(null),
                    onLogoutAsync: vi.fn().mockResolvedValue(undefined),
                },
            },
            { provide: Router, useValue: router },
            {
                provide: ActivatedRoute,
                useValue: createActivatedRouteStub(),
            },
            { provide: SeoService, useValue: seoService },
            { provide: LocalizationService, useValue: localizationService },
            { provide: GlobalLoadingService, useValue: { isVisible: signal(false) } },
            { provide: RouteLoadingService, useValue: routeLoadingService },
            { provide: ThemeService, useValue: themeService },
            { provide: NotificationRealtimeService, useValue: {} },
            { provide: PushNotificationService, useValue: {} },
        ],
    });
    TestBed.overrideComponent(AppComponent, {
        set: {
            imports: [],
            template: '',
        },
    });

    await TestBed.compileComponents();
    TestBed.createComponent(AppComponent).detectChanges();

    return { localizationService, routeLoadingService, router, routerEvents, seoService, themeService };
}

function createActivatedRouteStub(): ActivatedRoute {
    const route = {
        firstChild: null,
        pathFromRoot: [
            {
                snapshot: {
                    data: {
                        seo: {
                            titleKey: 'SEO.DASHBOARD.TITLE',
                            descriptionKey: 'SEO.DASHBOARD.DESCRIPTION',
                        },
                    },
                },
            },
        ],
    };

    return route as unknown as ActivatedRoute;
}
