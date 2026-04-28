import { Component, DestroyRef, Injector, ViewEncapsulation, computed, inject } from '@angular/core';
import { ActivatedRoute, NavigationEnd, RouteConfigLoadEnd, RouteConfigLoadStart, Router, RouterOutlet } from '@angular/router';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { filter, map, mergeMap } from 'rxjs';

import { AuthService } from '../services/auth.service';
import { SeoService, SeoData } from '../services/seo.service';
import { QuickConsumptionDrawerComponent } from '../features/meals/components/quick-consumption-drawer/quick-consumption-drawer.component';
import { SidebarComponent } from './sidebar/sidebar.component';
import { NotificationRealtimeService } from '../services/notification-realtime.service';
import { PushNotificationService } from '../services/push-notification.service';
import { FdUiToastHostComponent, FdUiTopLoaderComponent } from 'fd-ui-kit';
import { GlobalLoadingService } from '../services/global-loading.service';
import { RouteLoadingService } from '../services/route-loading.service';

@Component({
    selector: 'fd-root',
    imports: [RouterOutlet, SidebarComponent, QuickConsumptionDrawerComponent, FdUiToastHostComponent, FdUiTopLoaderComponent],
    templateUrl: './app.component.html',
    styleUrl: './app.component.scss',
    encapsulation: ViewEncapsulation.None,
})
export class AppComponent {
    private readonly authService = inject(AuthService);
    private readonly router = inject(Router);
    private readonly activatedRoute = inject(ActivatedRoute);
    private readonly seoService = inject(SeoService);
    private readonly destroyRef = inject(DestroyRef);
    private readonly injector = inject(Injector);
    private readonly globalLoadingService = inject(GlobalLoadingService);
    private readonly routeLoadingService = inject(RouteLoadingService);

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
                filter(event => event instanceof NavigationEnd),
                map(() => this.getDeepestRoute(this.activatedRoute)),
                mergeMap(route => route.data.pipe(map(data => ({ data, url: this.router.url })))),
                takeUntilDestroyed(this.destroyRef),
            )
            .subscribe(({ data, url }) => {
                const seo: SeoData = data['seo'] ?? {};
                this.seoService.update({ ...seo, path: url });
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

    public stopImpersonation(): void {
        void this.authService.onLogout(false);
    }
}
