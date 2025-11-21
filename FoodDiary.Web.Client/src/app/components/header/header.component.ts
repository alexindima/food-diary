
import { Component, DestroyRef, inject, OnInit } from '@angular/core';
import { TranslateModule } from '@ngx-translate/core';
import { NavigationEnd, Router, RouterModule } from '@angular/router';
import { NavigationService } from '../../services/navigation.service';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { filter } from 'rxjs';
import { AuthService } from '../../services/auth.service';
import { FdUiIconModule } from 'fd-ui-kit/material';
import {
    FdUiMenuComponent,
    FdUiMenuTriggerDirective,
    FdUiMenuItemComponent,
    FdUiMenuDividerComponent,
} from 'fd-ui-kit/menu';

@Component({
    selector: 'fd-header',
    imports: [
    TranslateModule,
    RouterModule,
    FdUiIconModule,
    FdUiMenuComponent,
    FdUiMenuTriggerDirective,
    FdUiMenuItemComponent,
    FdUiMenuDividerComponent
],
    templateUrl: './header.component.html',
    styleUrls: ['./header.component.scss']
})
export class HeaderComponent implements OnInit {
    private readonly router = inject(Router);
    private readonly navigationService = inject(NavigationService);
    private readonly authService = inject(AuthService);
    private readonly destroyRef = inject(DestroyRef);

    public isAuthenticated = this.authService.isAuthenticated;

    protected userSectionActive = false;
    public ngOnInit(): void {
        this.router.events
            .pipe(
                takeUntilDestroyed(this.destroyRef),
                filter(event => event instanceof NavigationEnd),
            )
            .subscribe(() => {
                this.userSectionActive = this.isUserSectionRoute(this.router.url);
            });
    }

    protected async goToProfile(): Promise<void> {
        await this.navigationService.navigateToProfile();
    }

    protected async goToWeightHistory(): Promise<void> {
        await this.navigationService.navigateToWeightHistory();
    }

    protected async goToWaistHistory(): Promise<void> {
        await this.navigationService.navigateToWaistHistory();
    }

    protected async goToCycleTracking(): Promise<void> {
        await this.navigationService.navigateToCycleTracking();
    }

    protected async logout(): Promise<void> {
        await this.authService.onLogout();
    }

    private isUserSectionRoute(url: string): boolean {
        return (
            url.startsWith('/profile') ||
            url.startsWith('/weight-history') ||
            url.startsWith('/waist-history')
        );
    }
}
