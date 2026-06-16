import { ChangeDetectionStrategy, Component, computed, DestroyRef, inject, Injector, signal } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { ActivatedRoute, NavigationEnd, RouteConfigLoadEnd, RouteConfigLoadStart, Router, RouterOutlet } from '@angular/router';
import { FdTourHostComponent } from 'fd-tour';
import { FdUiToastHostComponent, FdUiTopLoaderComponent } from 'fd-ui-kit';
import { filter, from, mergeMap } from 'rxjs';

import { QuickConsumptionDrawerComponent } from '../features/meals/components/quick-consumption-drawer/quick-consumption-drawer';
import { AuthService } from '../services/auth.service';
import { GlobalLoadingService } from '../services/global-loading.service';
import { RouteLoadingService } from '../services/route-loading.service';
import { type SeoData, SeoService } from '../services/seo.service';
import { LocalizationService } from '../shared/i18n/localization.service';
import { NotificationRealtimeService } from '../shared/notifications/notification-realtime.service';
import { PushNotificationService } from '../shared/notifications/push-notification.service';
import { ThemeService } from '../shared/theme/theme.service';
import { parseRouteSeoData } from './app-lib/app-seo-data.utils';
import { SidebarComponent } from './sidebar/sidebar';

@Component({
    selector: 'fd-root',
    imports: [
        RouterOutlet,
        SidebarComponent,
        QuickConsumptionDrawerComponent,
        FdUiToastHostComponent,
        FdUiTopLoaderComponent,
        FdTourHostComponent,
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

    protected isAuthenticated = this.authService.isAuthenticated;
    protected isImpersonating = this.authService.isImpersonating;
    protected impersonationReason = this.authService.impersonationReason;
    protected readonly isTopLoaderVisible = computed(() => this.globalLoadingService.isVisible() || this.routeLoadingService.isVisible());
    protected readonly currentPath = signal(this.getCurrentPath());
    protected readonly usesCompactMobileNavigation = computed(() => {
        const path = this.currentPath();
        return path === '/' || path === '/dashboard';
    });

    public constructor() {
        if (typeof window !== 'undefined') {
            this.injector.get(NotificationRealtimeService);
            this.injector.get(PushNotificationService);
        }

        this.router.events
            .pipe(
                filter((event): event is NavigationEnd => event instanceof NavigationEnd),
                takeUntilDestroyed(this.destroyRef),
            )
            .subscribe(event => {
                this.currentPath.set(this.getCurrentPath(event.urlAfterRedirects));
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
}
