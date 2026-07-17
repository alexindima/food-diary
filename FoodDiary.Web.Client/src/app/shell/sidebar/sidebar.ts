import { DOCUMENT } from '@angular/common';
import { ChangeDetectionStrategy, Component, computed, DestroyRef, effect, inject, signal } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { NavigationCancel, NavigationEnd, NavigationError, Router } from '@angular/router';
import { TranslateService } from '@ngx-translate/core';
import { FdUiConfirmDialogComponent } from 'fd-ui-kit/dialog/fd-ui-confirm-dialog';
import { FdUiDialogService } from 'fd-ui-kit/dialog/fd-ui-dialog.service';
import { firstValueFrom } from 'rxjs';

import {
    UnsavedChangesDialogComponent,
    type UnsavedChangesDialogResult,
} from '../../components/shared/unsaved-changes-dialog/unsaved-changes-dialog';
import { SIDEBAR_MOBILE_VIEWPORT_QUERY } from '../../config/runtime-ui.tokens';
import { AuthService } from '../../services/auth.service';
import { UnsavedChangesService } from '../../services/unsaved-changes.service';
import { NotificationService } from '../../shared/notifications/notification.service';
import { BrowserWindowService } from '../../shared/platform/browser-window.service';
import { AdminPanelLauncherService } from './admin-panel-launcher.service';
import { SidebarFacade } from './sidebar.facade';
import { SidebarDesktopComponent } from './sidebar-desktop/sidebar-desktop';
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
import { SidebarMobileComponent } from './sidebar-mobile/sidebar-mobile';

@Component({
    selector: 'fd-sidebar',
    imports: [SidebarDesktopComponent, SidebarMobileComponent],
    templateUrl: './sidebar.html',
    styleUrls: ['./sidebar.scss'],
    changeDetection: ChangeDetectionStrategy.OnPush,
})
export class SidebarComponent {
    private readonly document = inject(DOCUMENT);
    private readonly browserWindow = inject(BrowserWindowService);
    private readonly destroyRef = inject(DestroyRef);
    private readonly authService = inject(AuthService);
    private readonly adminPanelLauncher = inject(AdminPanelLauncherService);
    private readonly translateService = inject(TranslateService);
    private readonly sidebarFacade = inject(SidebarFacade);
    private readonly dialogService = inject(FdUiDialogService);
    private readonly unsavedChangesService = inject(UnsavedChangesService);
    private readonly notificationService = inject(NotificationService);
    private readonly router = inject(Router);
    private readonly mobileViewportQuery = inject(SIDEBAR_MOBILE_VIEWPORT_QUERY);

    protected isAuthenticated = this.authService.isAuthenticated;
    protected isPremium = this.authService.isPremium;
    protected isDietologist = this.authService.isDietologist;
    protected isAdmin = this.authService.isAdmin;
    protected unreadNotificationCount = this.notificationService.unreadCount;
    protected readonly primaryNavItems = computed<SidebarNavItem[]>(() =>
        buildPrimarySidebarNavItems(this.isDietologist(), this.isAdmin()),
    );
    protected readonly primaryRouteItems = computed<SidebarRouteItem[]>(() => this.primaryNavItems().filter(isSidebarRouteItem));
    protected readonly primaryActionItems = computed<SidebarActionItem[]>(() => this.primaryNavItems().filter(isSidebarActionItem));
    protected readonly foodTrackingItems = FOOD_TRACKING_ITEMS;
    protected readonly bodyTrackingItems = BODY_TRACKING_ITEMS;
    protected readonly desktopBottomItems = DESKTOP_BOTTOM_ITEMS;
    protected readonly currentUser = this.sidebarFacade.currentUser;
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
    protected readonly dailyConsumedKcal = this.sidebarFacade.dailyConsumedKcal;
    protected readonly dailyGoalKcal = this.sidebarFacade.dailyGoalKcal;
    protected readonly dailyConsumedKcalRounded = computed(() => Math.round(this.dailyConsumedKcal()));
    protected readonly dailyGoalKcalRounded = computed(() => Math.round(this.dailyGoalKcal()));
    protected readonly dailyProgressPercent = computed(() =>
        calculateSidebarDailyProgressPercent(this.dailyConsumedKcal(), this.dailyGoalKcal()),
    );
    private lastUserMenuTrigger: HTMLElement | null = null;
    private lastMobileSheetTrigger: HTMLElement | null = null;
    private mobileSheetScrollTop = 0;

    public constructor() {
        effect(() => {
            this.sidebarFacade.syncCurrentUser(this.isAuthenticated());
        });

        effect(() => {
            this.sidebarFacade.dailyProgressInvalidationVersion();
            if (!this.isMobileProgressVisible()) {
                this.dailyConsumedKcal.set(0);
                this.dailyGoalKcal.set(0);
                return;
            }

            this.sidebarFacade.syncDailyProgress();
        });

        effect(() => {
            if (this.isAuthenticated()) {
                this.notificationService.fetchUnreadCount();
                this.notificationService.ensureNotificationsLoaded();
            }
        });

        effect(onCleanup => {
            if (!this.isMobileViewport() || !this.isMobileSheetOpen()) {
                return;
            }

            this.lockMobileSheetScroll();
            onCleanup(() => {
                this.unlockMobileSheetScroll();
            });
        });

        const mobileMediaQuery = this.browserWindow.matchMedia(this.mobileViewportQuery);
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
        if (this.browserWindow.isAvailable()) {
            this.document.addEventListener('keydown', this.handleDocumentKeydown);
            this.destroyRef.onDestroy(() => {
                this.document.removeEventListener('keydown', this.handleDocumentKeydown);
            });
        }
    }

    protected onPrimaryAction(action: SidebarActionItem['action']): void {
        switch (action) {
            case 'openAdminPanel': {
                this.openAdminPanel();
                break;
            }
            case 'openNotifications': {
                void this.openNotificationsAsync();
                break;
            }
            case 'logout': {
                void this.logoutAsync();
                break;
            }
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

    private getIsMobileViewport(): boolean {
        return this.browserWindow.matchMedia(this.mobileViewportQuery)?.matches ?? false;
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
        const { NotificationsDialogComponent } = await import('../../components/shared/notifications-dialog/notifications-dialog');
        this.dialogService.open(NotificationsDialogComponent, {
            preset: 'list',
        });
    }

    protected openAdminPanel(): void {
        this.adminPanelLauncher.open(this.isAdmin());
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
        const confirmed = await this.confirmLogoutAsync();
        if (!confirmed) {
            return;
        }

        const canLogout = await this.confirmUnsavedChangesAsync();
        if (!canLogout) {
            return;
        }

        await this.authService.onLogoutAsync(true);
        this.closeUserMenu();
        this.closeMobileMenus();
    }

    private async confirmLogoutAsync(): Promise<boolean> {
        const result = await firstValueFrom(
            this.dialogService
                .open(FdUiConfirmDialogComponent, {
                    preset: 'confirm',
                    data: {
                        title: this.translateService.instant('HEADER.LOGOUT_CONFIRM_TITLE'),
                        message: this.translateService.instant('HEADER.LOGOUT_CONFIRM_MESSAGE'),
                        confirmLabel: this.translateService.instant('HEADER.LOGOUT_CONFIRM_ACTION'),
                        cancelLabel: this.translateService.instant('COMMON.CANCEL'),
                        danger: true,
                    },
                })
                .afterClosed(),
        );

        return result === true;
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
            const saveResult = await handler.save();
            return saveResult !== false;
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

    private lockMobileSheetScroll(): void {
        if (!this.browserWindow.isAvailable()) {
            return;
        }

        const scrollbarWidth = Math.max(0, this.browserWindow.getViewportWidth() - this.document.documentElement.clientWidth);
        this.mobileSheetScrollTop = this.browserWindow.getScrollY();
        this.document.body.style.insetBlockStart = `-${this.mobileSheetScrollTop}px`;
        this.document.body.style.paddingInlineEnd = `${scrollbarWidth}px`;
        this.document.body.classList.add('fd-scroll-lock');
    }

    private unlockMobileSheetScroll(): void {
        this.document.body.classList.remove('fd-scroll-lock');
        this.document.body.style.removeProperty('inset-block-start');
        this.document.body.style.removeProperty('padding-inline-end');

        if (!this.browserWindow.isAvailable()) {
            return;
        }

        this.browserWindow.scrollTo(0, this.mobileSheetScrollTop);
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
