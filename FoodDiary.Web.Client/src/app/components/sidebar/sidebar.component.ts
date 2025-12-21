import { Component, DestroyRef, effect, inject, signal } from '@angular/core';
import { TranslateModule } from '@ngx-translate/core';
import { RouterModule } from '@angular/router';
import { AuthService } from '../../services/auth.service';
import { FdUiIconModule } from 'fd-ui-kit/material';
import { MatIconModule } from '@angular/material/icon';
import { UserService } from '../../services/user.service';
import { User } from '../../types/user.data';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { SlicePipe, UpperCasePipe } from '@angular/common';

@Component({
    selector: 'fd-sidebar',
    imports: [
        TranslateModule,
        RouterModule,
        FdUiIconModule,
        MatIconModule,
        SlicePipe,
        UpperCasePipe,
    ],
    templateUrl: './sidebar.component.html',
    styleUrls: ['./sidebar.component.scss']
})
export class SidebarComponent {
    private readonly authService = inject(AuthService);
    private readonly userService = inject(UserService);
    private readonly destroyRef = inject(DestroyRef);

    public isAuthenticated = this.authService.isAuthenticated;
    protected currentUser = signal<User | null>(null);
    protected isFoodTrackingOpen = signal(true);
    protected isBodyTrackingOpen = signal(false);
    protected isUserMenuOpen = signal(false);
    protected isMobileFoodOpen = signal(false);
    protected isMobileBodyOpen = signal(false);
    protected isMobileReportsOpen = signal(false);
    protected isMobileUserOpen = signal(false);

    private readonly userSync = effect(onCleanup => {
        if (!this.isAuthenticated()) {
            this.currentUser.set(null);
            return;
        }

        const subscription = this.userService
            .getInfo()
            .pipe(takeUntilDestroyed(this.destroyRef))
            .subscribe(user => this.currentUser.set(user));

        onCleanup(() => subscription.unsubscribe());
    });

    protected toggleFoodTracking(): void {
        const next = !this.isFoodTrackingOpen();
        this.isFoodTrackingOpen.set(next);
        if (next) {
            this.isBodyTrackingOpen.set(false);
        }
    }

    protected toggleBodyTracking(): void {
        const next = !this.isBodyTrackingOpen();
        this.isBodyTrackingOpen.set(next);
        if (next) {
            this.isFoodTrackingOpen.set(false);
        }
    }

    protected toggleUserMenu(): void {
        this.isUserMenuOpen.set(!this.isUserMenuOpen());
    }

    protected toggleMobileFood(): void {
        const next = !this.isMobileFoodOpen();
        this.closeMobileMenus();
        this.isMobileFoodOpen.set(next);
    }

    protected toggleMobileBody(): void {
        const next = !this.isMobileBodyOpen();
        this.closeMobileMenus();
        this.isMobileBodyOpen.set(next);
    }

    protected toggleMobileReports(): void {
        const next = !this.isMobileReportsOpen();
        this.closeMobileMenus();
        this.isMobileReportsOpen.set(next);
    }

    protected toggleMobileUser(): void {
        const next = !this.isMobileUserOpen();
        this.closeMobileMenus();
        this.isMobileUserOpen.set(next);
    }

    protected closeMobileMenus(): void {
        this.isMobileFoodOpen.set(false);
        this.isMobileBodyOpen.set(false);
        this.isMobileReportsOpen.set(false);
        this.isMobileUserOpen.set(false);
    }

    protected async logout(): Promise<void> {
        await this.authService.onLogout(true);
        this.isUserMenuOpen.set(false);
        this.closeMobileMenus();
    }
}
