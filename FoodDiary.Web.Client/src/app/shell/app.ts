import { DOCUMENT, ViewportScroller } from '@angular/common';
import {
    ChangeDetectionStrategy,
    Component,
    computed,
    DestroyRef,
    effect,
    type ElementRef,
    inject,
    Injector,
    signal,
    untracked,
    viewChild,
} from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import {
    ActivatedRoute,
    ChildrenOutletContexts,
    NavigationEnd,
    RouteConfigLoadEnd,
    RouteConfigLoadStart,
    Router,
    RouterOutlet,
    Scroll,
} from '@angular/router';
import { TranslatePipe } from '@ngx-translate/core';
import { FdTourHostComponent } from 'fd-tour';
import { FdUiToastHostComponent, FdUiTopLoaderComponent } from 'fd-ui-kit';
import { FdUiDialogService } from 'fd-ui-kit/dialog/fd-ui-dialog.service';
import { filter, from, mergeMap } from 'rxjs';

import { AppUpdateNoticeComponent } from '../components/shared/app-update-notice/app-update-notice';
import { PwaInstallBannerComponent } from '../components/shared/pwa-install-banner/pwa-install-banner';
import { isPublicMarketingPath } from '../config/public-site-paths';
import { QuickMealDrawerComponent } from '../features/meals/contracts/quick-meal-drawer';
import { PublicSiteFooterComponent, PublicSiteHeaderComponent } from '../features/public/contracts/public-site-chrome';
import { AuthService } from '../services/auth.service';
import { GlobalLoadingService } from '../services/global-loading.service';
import { RouteLoadingService } from '../services/route-loading.service';
import { type SeoData, SeoService } from '../services/seo.service';
import { SessionEventsService } from '../shared/auth/session-events.service';
import { LocalizationService } from '../shared/i18n/localization.service';
import { NotificationRealtimeService } from '../shared/notifications/notification-realtime.service';
import { PushNotificationService } from '../shared/notifications/push-notification.service';
import { BrowserWindowService } from '../shared/platform/browser-window.service';
import { PwaInstallPromotionService } from '../shared/service-worker/pwa-install-promotion.service';
import { ThemeService } from '../shared/theme/theme.service';
import { confirmRoutedEditorsReloadAsync } from './app-lib/app-reload.utils';
import { parseRouteSeoData } from './app-lib/app-seo-data.utils';
import { SidebarComponent } from './sidebar/sidebar';

const SESSION_ROUTE_PENDING_CLASS = 'fd-session-route-pending';
type AppShellMode = 'authenticated' | 'public';

@Component({
    selector: 'fd-root',
    imports: [
        RouterOutlet,
        SidebarComponent,
        QuickMealDrawerComponent,
        FdUiToastHostComponent,
        FdUiTopLoaderComponent,
        FdTourHostComponent,
        TranslatePipe,
        PublicSiteHeaderComponent,
        PublicSiteFooterComponent,
        AppUpdateNoticeComponent,
        PwaInstallBannerComponent,
    ],
    templateUrl: './app.html',
    styleUrl: './app.scss',
    changeDetection: ChangeDetectionStrategy.OnPush,
})
export class AppComponent {
    private readonly authService = inject(AuthService);
    private readonly router = inject(Router);
    private readonly activatedRoute = inject(ActivatedRoute);
    private readonly seoService = inject(SeoService);
    private readonly localizationService = inject(LocalizationService);
    private readonly destroyRef = inject(DestroyRef);
    private readonly injector = inject(Injector);
    private readonly globalLoadingService = inject(GlobalLoadingService);
    private readonly routeLoadingService = inject(RouteLoadingService);
    private readonly themeService = inject(ThemeService);
    private readonly document = inject(DOCUMENT);
    private readonly browserWindow = inject(BrowserWindowService);
    private readonly viewportScroller = inject(ViewportScroller);
    private readonly pageViewport = viewChild<ElementRef<HTMLElement>>('pageViewport');
    private readonly outletContexts = inject(ChildrenOutletContexts);
    private readonly installPromotion = inject(PwaInstallPromotionService);
    private readonly sessionEvents = inject(SessionEventsService);
    private readonly dialogService = inject(FdUiDialogService);
    private publicScrollNavigationId: number | null = null;

    protected isAuthenticated = this.authService.isAuthenticated;
    protected isImpersonating = this.authService.isImpersonating;
    protected impersonationReason = this.authService.impersonationReason;
    protected readonly shellMode = signal<AppShellMode>(this.getShellMode());
    protected readonly routeContentActive = signal(true);
    protected readonly usesAuthenticatedShell = computed(() => this.isAuthenticated() && this.shellMode() === 'authenticated');
    protected readonly isTopLoaderVisible = computed(() => this.globalLoadingService.isVisible() || this.routeLoadingService.isVisible());
    protected readonly currentPath = signal(this.getCurrentPath());
    protected readonly usesPublicSiteChrome = computed(() => isPublicMarketingPath(this.currentPath()));
    protected readonly isSidebarCollapsed = signal(false);
    protected readonly usesCompactMobileNavigation = computed(() => {
        const path = this.currentPath();
        return path === '/' || path === '/dashboard';
    });
    protected readonly confirmAppReloadAsync = async (): Promise<boolean> => {
        return confirmRoutedEditorsReloadAsync(this.outletContexts);
    };
    protected readonly showInstallPromotion = computed(
        () =>
            this.usesAuthenticatedShell() &&
            this.usesCompactMobileNavigation() &&
            !this.isImpersonating() &&
            this.installPromotion.shouldShowBanner(),
    );

    public constructor() {
        this.sessionEvents.localSessionInvalidating$.pipe(takeUntilDestroyed(this.destroyRef)).subscribe(() => {
            this.routeContentActive.set(false);
            this.dialogService.closeAll();
        });
        this.sessionEvents.sessionEnded$.pipe(takeUntilDestroyed(this.destroyRef)).subscribe(() => {
            this.routeContentActive.set(false);
            this.dialogService.closeAll();
        });
        effect(() => {
            const active = this.usesAuthenticatedShell() && !this.isImpersonating();
            untracked(() => {
                this.installPromotion.setActive(active);
            });
        });
        this.viewportScroller.setHistoryScrollRestoration('manual');
        if (this.browserWindow.isAvailable()) {
            this.injector.get(NotificationRealtimeService);
            this.injector.get(PushNotificationService);
        }

        this.router.events
            .pipe(
                filter((event): event is NavigationEnd => event instanceof NavigationEnd),
                takeUntilDestroyed(this.destroyRef),
            )
            .subscribe(event => {
                const nextPath = this.getCurrentPath(event.urlAfterRedirects);
                this.publicScrollNavigationId = nextPath !== this.currentPath() && isPublicMarketingPath(nextPath) ? event.id : null;
                this.document.documentElement.classList.remove(SESSION_ROUTE_PENDING_CLASS);
                this.currentPath.set(nextPath);
                this.shellMode.set(this.getShellMode());
                const requiresSession = this.getDeepestRoute(this.activatedRoute).pathFromRoot.some(
                    route => route.snapshot.data['requiresSession'] === true,
                );
                this.routeContentActive.set(!requiresSession || this.isAuthenticated());
            });

        // Router Scroll arrives after the new view renders. Desktop scrolls inside
        // the shell viewport; mobile uses the document viewport.
        this.router.events
            .pipe(
                filter((event): event is Scroll => event instanceof Scroll),
                takeUntilDestroyed(this.destroyRef),
            )
            .subscribe(event => {
                if (event.scrollBehavior === 'manual') {
                    return;
                }
                if (!this.usesPublicSiteChrome()) {
                    this.viewportScroller.scrollToPosition([0, 0], { behavior: 'instant' });
                    return;
                }
                if (event.routerEvent.id !== this.publicScrollNavigationId) {
                    return;
                }
                const behavior = this.browserWindow.matchMedia('(prefers-reduced-motion: reduce)')?.matches === true ? 'instant' : 'smooth';
                this.pageViewport()?.nativeElement.scrollTo({ top: 0, left: 0, behavior });
                this.viewportScroller.scrollToPosition([0, 0], { behavior });
            });

        this.router.events
            .pipe(
                filter((event): event is NavigationEnd => event instanceof NavigationEnd),
                mergeMap(event => from(this.prepareRouteAsync(event.urlAfterRedirects))),
                takeUntilDestroyed(this.destroyRef),
            )
            .subscribe(() => {
                const seo = this.getMergedSeoData(this.activatedRoute);
                this.seoService.update({ ...seo, path: this.router.url });
            });

        this.router.events
            .pipe(
                filter(event => event instanceof RouteConfigLoadStart || event instanceof RouteConfigLoadEnd),
                takeUntilDestroyed(this.destroyRef),
            )
            .subscribe(event => {
                if (event instanceof RouteConfigLoadStart) {
                    this.routeLoadingService.beginLoad();
                    return;
                }

                this.routeLoadingService.endLoad();
            });
    }

    private getDeepestRoute(route: ActivatedRoute): ActivatedRoute {
        while (route.firstChild !== null) {
            route = route.firstChild;
        }
        return route;
    }

    private getMergedSeoData(route: ActivatedRoute): SeoData {
        const deepestRoute = this.getDeepestRoute(route);

        return deepestRoute.pathFromRoot.reduce<SeoData>((seo, routePart) => {
            const routeSeo = parseRouteSeoData(routePart.snapshot.data['seo']);
            return routeSeo === null ? seo : { ...seo, ...routeSeo };
        }, {});
    }

    private getShellMode(): AppShellMode {
        return this.getDeepestRoute(this.activatedRoute).snapshot.data['shell'] === 'public' ? 'public' : 'authenticated';
    }

    private async prepareRouteAsync(url: string): Promise<void> {
        this.themeService.applyThemeForRoute(url);
        await this.localizationService.loadTranslationsForRouteAsync(url);
    }

    private getCurrentPath(url = this.router.url): string {
        const path = url.split(/[?#]/, 1)[0];
        const normalized = path.length > 0 ? path : '/';
        return normalized.endsWith('/') && normalized.length > 1 ? normalized.slice(0, -1) : normalized;
    }

    protected stopImpersonation(): void {
        void this.authService.onLogoutAsync(false);
    }

    protected toggleSidebar(): void {
        this.isSidebarCollapsed.update(isCollapsed => !isCollapsed);
    }
}
