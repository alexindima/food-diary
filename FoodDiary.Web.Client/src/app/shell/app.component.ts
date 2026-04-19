import { Component, DestroyRef, Injector, ViewEncapsulation, inject } from '@angular/core';
import { ActivatedRoute, NavigationEnd, Router, RouterOutlet } from '@angular/router';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { filter, map, mergeMap } from 'rxjs';

import { AuthService } from '../services/auth.service';
import { SeoService, SeoData } from '../services/seo.service';
import { QuickConsumptionDrawerComponent } from '../features/meals/components/quick-consumption-drawer/quick-consumption-drawer.component';
import { SidebarComponent } from './sidebar/sidebar.component';
import { NotificationRealtimeService } from '../services/notification-realtime.service';
import { PushNotificationService } from '../services/push-notification.service';
import { FdUiToastHostComponent } from 'fd-ui-kit';

@Component({
    selector: 'fd-root',
    imports: [RouterOutlet, SidebarComponent, QuickConsumptionDrawerComponent, FdUiToastHostComponent],
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

    public isAuthenticated = this.authService.isAuthenticated;

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
    }

    private getDeepestRoute(route: ActivatedRoute): ActivatedRoute {
        while (route.firstChild) {
            route = route.firstChild;
        }
        return route;
    }
}
