import { DOCUMENT } from '@angular/common';
import { ChangeDetectionStrategy, Component, computed, DestroyRef, effect, inject, signal } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { NavigationCancel, NavigationEnd, NavigationError, Router } from '@angular/router';
import { TranslateService } from '@ngx-translate/core';
import { FdUiDialogService } from 'fd-ui-kit/dialog/fd-ui-dialog.service';
import { FdUiToastService } from 'fd-ui-kit/toast/fd-ui-toast.service';
import { firstValueFrom } from 'rxjs';

import { environment } from '../../../environments/environment';
import {
    UnsavedChangesDialogComponent,
    type UnsavedChangesDialogResult,
} from '../../components/shared/unsaved-changes-dialog/unsaved-changes-dialog.component';
import { ADMIN_LOADING_URL_TTL_MS, SIDEBAR_MOBILE_VIEWPORT_QUERY } from '../../config/runtime-ui.tokens';
import { DashboardService } from '../../features/dashboard/api/dashboard.service';
import { AuthService } from '../../services/auth.service';
import { NotificationService } from '../../services/notification.service';
import { UnsavedChangesService } from '../../services/unsaved-changes.service';
import { UserService } from '../../shared/api/user.service';
import { SidebarDesktopComponent } from './sidebar-desktop/sidebar-desktop.component';
import type {
    DesktopSectionId,
    MobileSheetId,
    SidebarActionItem,
    SidebarDirectRouteRequest,
    SidebarNavItem,
    SidebarRouteItem,
} from './sidebar-lib/sidebar.models';
import { BODY_TRACKING_ITEMS, DESKTOP_BOTTOM_ITEMS, FOOD_TRACKING_ITEMS } from './sidebar-lib/sidebar-navigation.config';
import {
    buildPrimarySidebarNavItems,
    calculateSidebarDailyProgressPercent,
    isSidebarActionItem,
    isSidebarRouteActive,
    isSidebarRouteItem,
    normalizeSidebarPath,
} from './sidebar-lib/sidebar-view.utils';
import { SidebarMobileComponent } from './sidebar-mobile/sidebar-mobile.component';

@Component({
    selector: 'fd-sidebar',
    imports: [SidebarDesktopComponent, SidebarMobileComponent],
    templateUrl: './sidebar.component.html',
    styleUrls: ['./sidebar.component.scss'],
    changeDetection: ChangeDetectionStrategy.OnPush,
})
export class SidebarComponent {
    private readonly document = inject(DOCUMENT);
    private readonly destroyRef = inject(DestroyRef);
    private readonly authService = inject(AuthService);
    private readonly translateService = inject(TranslateService);
    private readonly userService = inject(UserService);
    private readonly dialogService = inject(FdUiDialogService);
    private readonly toastService = inject(FdUiToastService);
    private readonly unsavedChangesService = inject(UnsavedChangesService);
    private readonly dashboardService = inject(DashboardService);
    private readonly notificationService = inject(NotificationService);
    private readonly router = inject(Router);
    private readonly mobileViewportQuery = inject(SIDEBAR_MOBILE_VIEWPORT_QUERY);
    private readonly adminLoadingUrlTtlMs = inject(ADMIN_LOADING_URL_TTL_MS);

    public isAuthenticated = this.authService.isAuthenticated;
    public isPremium = this.authService.isPremium;
    public isDietologist = this.authService.isDietologist;
    public isAdmin = this.authService.isAdmin;
    public unreadNotificationCount = this.notificationService.unreadCount;
    protected readonly primaryNavItems = computed<SidebarNavItem[]>(() =>
        buildPrimarySidebarNavItems(this.isDietologist(), this.isAdmin()),
    );
    protected readonly primaryRouteItems = computed<SidebarRouteItem[]>(() => this.primaryNavItems().filter(isSidebarRouteItem));
    protected readonly primaryActionItems = computed<SidebarActionItem[]>(() => this.primaryNavItems().filter(isSidebarActionItem));
    protected readonly foodTrackingItems = FOOD_TRACKING_ITEMS;
    protected readonly bodyTrackingItems = BODY_TRACKING_ITEMS;
    protected readonly desktopBottomItems = DESKTOP_BOTTOM_ITEMS;
    protected readonly currentUser = this.userService.user;
    protected readonly brandStatusKey = computed(() => {
        if (this.isAdmin()) {
            return 'SIDEBAR.STATUS_ADMIN';
        }

        if (this.isDietologist()) {
            return 'SIDEBAR.STATUS_DIETOLOGIST';
        }

        return 'SIDEBAR.STATUS_USER';
    });
    protected readonly userPlanLabelKey = computed(() => (this.isPremium() ? 'SIDEBAR.PLAN_PRO' : 'SIDEBAR.PLAN_FREE'));
    protected readonly openDesktopSection = signal<DesktopSectionId>('food');
    protected readonly isUserMenuOpen = signal(false);
    protected readonly mobileSheet = signal<MobileSheetId>(null);
    protected readonly isFoodTrackingOpen = computed(() => this.openDesktopSection() === 'food');
    protected readonly isBodyTrackingOpen = computed(() => this.openDesktopSection() === 'body');
    protected readonly isMobileSheetOpen = computed(() => this.mobileSheet() !== null);
    protected readonly isMobileViewport = signal(this.getIsMobileViewport());
    protected readonly currentPath = signal(this.getCurrentPath());
    protected readonly isDashboardRoute = computed(() => {
        const path = this.currentPath();
        return path === '/' || path === '/dashboard';
    });
    protected readonly isMobileProgressVisible = computed(
        () => this.isAuthenticated() && this.isMobileViewport() && !this.isDashboardRoute(),
    );
    protected readonly pendingRoute = signal<string | null>(null);
    protected readonly dailyConsumedKcal = signal(0);
    protected readonly dailyGoalKcal = signal(0);
    protected readonly dailyConsumedKcalRounded = computed(() => Math.round(this.dailyConsumedKcal()));
    protected readonly dailyGoalKcalRounded = computed(() => Math.round(this.dailyGoalKcal()));
    protected readonly dailyProgressPercent = computed(() =>
        calculateSidebarDailyProgressPercent(this.dailyConsumedKcal(), this.dailyGoalKcal()),
    );
    private lastUserMenuTrigger: HTMLElement | null = null;
    private lastMobileSheetTrigger: HTMLElement | null = null;

    public constructor() {
        effect(() => {
            if (!this.isAuthenticated()) {
                this.userService.clearUser();
                return;
            }

            this.syncCurrentUser();
        });

        effect(() => {
            if (!this.isMobileProgressVisible()) {
                this.dailyConsumedKcal.set(0);
                this.dailyGoalKcal.set(0);
                return;
            }

            this.syncDailyProgress();
        });

        effect(() => {
            if (this.isAuthenticated()) {
                this.notificationService.fetchUnreadCount();
                this.notificationService.ensureNotificationsLoaded();
            }
        });

        const mobileMediaQuery = typeof window === 'undefined' ? null : window.matchMedia(this.mobileViewportQuery);
        const updateMobileViewport = (): void => {
            this.isMobileViewport.set(this.getIsMobileViewport());
        };

        mobileMediaQuery?.addEventListener('change', updateMobileViewport);
        this.destroyRef.onDestroy(() => mobileMediaQuery?.removeEventListener('change', updateMobileViewport));

        this.router.events.pipe(takeUntilDestroyed(this.destroyRef)).subscribe(event => {
            if (event instanceof NavigationEnd) {
                this.currentPath.set(this.getCurrentPath(event.urlAfterRedirects));
                this.pendingRoute.set(null);
                return;
            }

            if (event instanceof NavigationCancel || event instanceof NavigationError) {
                this.pendingRoute.set(null);
            }
        });
        this.document.addEventListener('keydown', this.handleDocumentKeydown);
        this.destroyRef.onDestroy(() => {
            this.document.removeEventListener('keydown', this.handleDocumentKeydown);
        });
    }

    protected onPrimaryAction(action: SidebarActionItem['action']): void {
        switch (action) {
            case 'openAdminPanel':
                this.openAdminPanel();
                break;
            case 'openNotifications':
                void this.openNotificationsAsync();
                break;
            case 'logout':
                void this.logoutAsync();
                break;
        }
    }

    protected onMobileAction(action: SidebarActionItem['action']): void {
        this.closeMobileMenus();
        this.onPrimaryAction(action);
    }

    protected onRouteSelected(item: SidebarRouteItem): void {
        if (!this.isRouteActive(item.route, item.exact ?? false)) {
            this.pendingRoute.set(item.route);
        }
    }

    protected onDirectRouteClick(route: string, exact = false): void {
        if (!this.isRouteActive(route, exact)) {
            this.pendingRoute.set(route);
        }
    }

    protected onDirectRouteRequest(request: SidebarDirectRouteRequest): void {
        this.onDirectRouteClick(request.route, request.exact ?? false);
    }

    private syncCurrentUser(): void {
        if (this.currentUser() !== null) {
            return;
        }

        this.userService.getInfoSilently().subscribe();
    }

    private syncDailyProgress(): void {
        this.dashboardService.getSnapshotSilently({ date: new Date(), page: 1, pageSize: 1 }).subscribe(snapshot => {
            this.dailyConsumedKcal.set(snapshot?.statistics.totalCalories ?? 0);
            this.dailyGoalKcal.set(snapshot?.dailyGoal ?? 0);
        });
    }

    private getIsMobileViewport(): boolean {
        if (typeof window === 'undefined') {
            return false;
        }

        return window.matchMedia(this.mobileViewportQuery).matches;
    }

    private getCurrentPath(url = this.router.url): string {
        return normalizeSidebarPath(url);
    }

    protected toggleFoodTracking(): void {
        this.toggleDesktopSection('food');
    }

    protected toggleBodyTracking(): void {
        this.toggleDesktopSection('body');
    }

    protected toggleUserMenu(trigger?: HTMLElement): void {
        if (trigger !== undefined) {
            this.lastUserMenuTrigger = trigger;
        }

        if (this.isUserMenuOpen()) {
            this.closeUserMenu();
            return;
        }

        this.isUserMenuOpen.set(true);
    }

    protected async openNotificationsAsync(): Promise<void> {
        const { NotificationsDialogComponent } =
            await import('../../components/shared/notifications-dialog/notifications-dialog.component');
        this.dialogService.open(NotificationsDialogComponent, {
            preset: 'list',
        });
    }

    protected openAdminPanel(): void {
        const adminAppUrl = environment.adminAppUrl;
        if (!this.isAdmin() || adminAppUrl === undefined || adminAppUrl.length === 0) {
            return;
        }

        const loadingUrl = URL.createObjectURL(
            new Blob(
                [
                    `
                        <!doctype html>
                        <html lang="en">
                            <head>
                                <meta charset="utf-8">
                                <title>Opening admin panel...</title>
                                <style>
                                    body {
                                        margin: 0;
                                        min-height: 100vh;
                                        display: grid;
                                        place-items: center;
                                        background: #0f172a;
                                        color: #e2e8f0;
                                        font: 600 16px/1.5 Inter, Arial, sans-serif;
                                    }
                                </style>
                            </head>
                            <body>Opening admin panel...</body>
                        </html>
                    `,
                ],
                { type: 'text/html' },
            ),
        );
        const adminWindow = window.open(loadingUrl, '_blank');
        window.setTimeout(() => {
            URL.revokeObjectURL(loadingUrl);
        }, this.adminLoadingUrlTtlMs);

        this.authService.startAdminSso().subscribe({
            next: response => {
                const url = new URL('/', adminAppUrl);
                url.searchParams.set('code', response.code);
                adminWindow?.location.assign(url.toString());
            },
            error: () => {
                adminWindow?.close();
                this.toastService.error(this.translateService.instant('USER_MANAGE.ADMIN_SSO_ERROR'));
            },
        });
    }

    protected toggleMobileFood(trigger?: HTMLElement): void {
        this.toggleMobileSheet('food', trigger);
    }

    protected toggleMobileBody(trigger?: HTMLElement): void {
        this.toggleMobileSheet('body', trigger);
    }

    protected toggleMobileReports(trigger?: HTMLElement): void {
        this.toggleMobileSheet('reports', trigger);
    }

    protected toggleMobileUser(trigger?: HTMLElement): void {
        this.toggleMobileSheet('user', trigger);
    }

    protected closeMobileMenus(): void {
        const focusTarget = this.lastMobileSheetTrigger;
        this.mobileSheet.set(null);
        this.lastMobileSheetTrigger = null;
        focusTarget?.focus();
    }

    protected async logoutAsync(): Promise<void> {
        const canLogout = await this.confirmUnsavedChangesAsync();
        if (!canLogout) {
            return;
        }

        await this.authService.onLogoutAsync(true);
        this.closeUserMenu();
        this.closeMobileMenus();
    }

    private async confirmUnsavedChangesAsync(): Promise<boolean> {
        const handler = this.unsavedChangesService.getHandler();
        if (handler?.hasChanges() !== true) {
            return true;
        }

        const result = await firstValueFrom(
            this.dialogService
                .open<UnsavedChangesDialogComponent, null, UnsavedChangesDialogResult>(UnsavedChangesDialogComponent, {
                    preset: 'confirm',
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

    private toggleDesktopSection(section: Exclude<DesktopSectionId, null>): void {
        this.openDesktopSection.update(current => (current === section ? null : section));
    }

    private toggleMobileSheet(sheet: Exclude<MobileSheetId, null>, trigger?: HTMLElement): void {
        if (trigger !== undefined) {
            this.lastMobileSheetTrigger = trigger;
        }

        if (this.mobileSheet() === sheet) {
            this.closeMobileMenus();
            return;
        }

        this.mobileSheet.set(sheet);
    }

    private isRouteActive(route: string, exact: boolean): boolean {
        return isSidebarRouteActive(this.router.url, route, exact);
    }

    private readonly handleDocumentKeydown = (event: KeyboardEvent): void => {
        if (event.key !== 'Escape') {
            return;
        }

        if (this.isMobileSheetOpen()) {
            event.preventDefault();
            this.closeMobileMenus();
            return;
        }

        if (this.isUserMenuOpen()) {
            event.preventDefault();
            this.closeUserMenu();
        }
    };

    private closeUserMenu(): void {
        this.isUserMenuOpen.set(false);
        this.lastUserMenuTrigger?.focus();
    }
}
