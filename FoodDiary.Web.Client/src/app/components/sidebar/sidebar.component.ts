import { Component, DestroyRef, computed, effect, inject, signal } from '@angular/core';
import { TranslateModule } from '@ngx-translate/core';
import { RouterModule } from '@angular/router';
import { AuthService } from '../../services/auth.service';
import { FdUiIconModule } from 'fd-ui-kit/material';
import { MatIconModule } from '@angular/material/icon';
import { UserService } from '../../services/user.service';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { SlicePipe, UpperCasePipe } from '@angular/common';
import { FdUiDialogService } from 'fd-ui-kit/dialog/fd-ui-dialog.service';
import { UnsavedChangesService } from '../../services/unsaved-changes.service';
import { UnsavedChangesDialogComponent, UnsavedChangesDialogResult } from '../shared/unsaved-changes-dialog/unsaved-changes-dialog.component';
import { firstValueFrom } from 'rxjs';
import { DashboardService } from '../../services/dashboard.service';

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
    protected readonly Math = Math;
    private readonly authService = inject(AuthService);
    private readonly userService = inject(UserService);
    private readonly destroyRef = inject(DestroyRef);
    private readonly dialogService = inject(FdUiDialogService);
    private readonly unsavedChangesService = inject(UnsavedChangesService);
    private readonly dashboardService = inject(DashboardService);

    public isAuthenticated = this.authService.isAuthenticated;
    public isPremium = this.authService.isPremium;
    protected readonly currentUser = this.userService.user;
    protected isFoodTrackingOpen = signal(true);
    protected isBodyTrackingOpen = signal(false);
    protected isUserMenuOpen = signal(false);
    protected isMobileFoodOpen = signal(false);
    protected isMobileBodyOpen = signal(false);
    protected isMobileReportsOpen = signal(false);
    protected isMobileUserOpen = signal(false);
    protected readonly isMobileSheetOpen = computed(() =>
        this.isMobileFoodOpen() || this.isMobileBodyOpen() || this.isMobileReportsOpen() || this.isMobileUserOpen(),
    );
    protected readonly dailyConsumedKcal = signal(0);
    protected readonly dailyGoalKcal = signal(0);
    protected readonly dailyProgressPercent = computed(() => {
        const goal = this.dailyGoalKcal();
        if (goal <= 0) {
            return 0;
        }

        return Math.max(0, Math.min((this.dailyConsumedKcal() / goal) * 100, 100));
    });

    private readonly userSync = effect(onCleanup => {
        if (!this.isAuthenticated()) {
            this.userService.clearUser();
            return;
        }

        const subscription = this.userService
            .getInfo()
            .pipe(takeUntilDestroyed(this.destroyRef))
            .subscribe();

        onCleanup(() => subscription.unsubscribe());
    });

    private readonly progressSync = effect(onCleanup => {
        if (!this.isAuthenticated()) {
            this.dailyConsumedKcal.set(0);
            this.dailyGoalKcal.set(0);
            return;
        }

        const subscription = this.dashboardService
            .getSnapshot(new Date(), 1, 1)
            .subscribe(snapshot => {
                this.dailyConsumedKcal.set(snapshot?.statistics?.totalCalories ?? 0);
                this.dailyGoalKcal.set(snapshot?.dailyGoal ?? 0);
            });

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
        const canLogout = await this.confirmUnsavedChanges();
        if (!canLogout) {
            return;
        }

        await this.authService.onLogout(true);
        this.isUserMenuOpen.set(false);
        this.closeMobileMenus();
    }

    private async confirmUnsavedChanges(): Promise<boolean> {
        const handler = this.unsavedChangesService.getHandler();
        if (!handler || !handler.hasChanges()) {
            return true;
        }

        const result = await firstValueFrom(
            this.dialogService
                .open<UnsavedChangesDialogComponent, null, UnsavedChangesDialogResult>(UnsavedChangesDialogComponent, {
                    size: 'sm',
                })
                .afterClosed(),
        );

        if (result === 'save') {
            await Promise.resolve(handler.save());
            return true;
        }

        if (result === 'discard') {
            handler.discard();
            return true;
        }

        return false;
    }
}
