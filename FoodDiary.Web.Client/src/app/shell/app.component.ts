import { ChangeDetectionStrategy, Component, computed, DestroyRef, inject, Injector } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { ActivatedRoute, NavigationEnd, RouteConfigLoadEnd, RouteConfigLoadStart, Router, RouterOutlet } from '@angular/router';
import { FdUiToastHostComponent, FdUiTopLoaderComponent } from 'fd-ui-kit';
import { filter, from, mergeMap } from 'rxjs';

import { QuickConsumptionDrawerComponent } from '../features/meals/components/quick-consumption-drawer/quick-consumption-drawer.component';
import { AuthService } from '../services/auth.service';
import { GlobalLoadingService } from '../services/global-loading.service';
import { LocalizationService } from '../services/localization.service';
import { NotificationRealtimeService } from '../services/notification-realtime.service';
import { PushNotificationService } from '../services/push-notification.service';
import { RouteLoadingService } from '../services/route-loading.service';
import { type SeoData, SeoService } from '../services/seo.service';
import { ThemeService } from '../services/theme.service';
import { SidebarComponent } from './sidebar/sidebar.component';

@Component({
    selector: 'fd-root',
    imports: [RouterOutlet, SidebarComponent, QuickConsumptionDrawerComponent, FdUiToastHostComponent, FdUiTopLoaderComponent],
    templateUrl: './app.component.html',
    styleUrl: './app.component.scss',
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

    public isAuthenticated = this.authService.isAuthenticated;
    public isImpersonating = this.authService.isImpersonating;
    public impersonationReason = this.authService.impersonationReason;
    public readonly isTopLoaderVisible = computed(() => this.globalLoadingService.isVisible() || this.routeLoadingService.isVisible());

    public constructor() {
        if (typeof window !== 'undefined') {
            this.injector.get(NotificationRealtimeService);
            this.injector.get(PushNotificationService);
        }

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
        while (route.firstChild) {
            route = route.firstChild;
        }
        return route;
    }

    private getMergedSeoData(route: ActivatedRoute): SeoData {
        const deepestRoute = this.getDeepestRoute(route);

        return deepestRoute.pathFromRoot.reduce<SeoData>((seo, routePart) => {
            const routeSeo = routePart.snapshot.data['seo'] as SeoData | undefined;
            return routeSeo ? { ...seo, ...routeSeo } : seo;
        }, {});
    }

    private async prepareRouteAsync(url: string): Promise<void> {
        this.themeService.applyThemeForRoute(url);
        await this.localizationService.loadTranslationsForRouteAsync(url);
    }

    public stopImpersonation(): void {
        void this.authService.onLogoutAsync(false);
    }
}
