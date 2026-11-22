import { DOCUMENT } from '@angular/common';
import { ChangeDetectionStrategy, Component, computed, DestroyRef, effect, inject, input, output, signal } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { NavigationCancel, NavigationEnd, NavigationError, Router } from '@angular/router';
import { TranslateService } from '@ngx-translate/core';
import { FdUiConfirmDialogComponent } from 'fd-ui-kit/dialog/fd-ui-confirm-dialog';
import { type FdUiDialogConfig, FdUiDialogService } from 'fd-ui-kit/dialog/fd-ui-dialog.service';
import { finalize, firstValueFrom } from 'rxjs';

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
    isSidebarActionItem,
    isSidebarRouteActive,
    isSidebarRouteItem,
} from './sidebar-lib/sidebar-view.utils';
import { SidebarMobileComponent } from './sidebar-mobile/sidebar-mobile';

const MOBILE_USER_TRIGGER = 'fd-sidebar-mobile nav button[data-mobile-sheet="user"]';

@Component({
    selector: 'fd-sidebar',
    imports: [SidebarDesktopComponent, SidebarMobileComponent],
    templateUrl: './sidebar.html',
    styleUrls: ['./sidebar.scss'],
    changeDetection: ChangeDetectionStrategy.OnPush,
})
export class SidebarComponent {
    public readonly isCollapsed = input(false);
    public readonly collapsedToggle = output();
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
    protected readonly pendingRoute = signal<string | null>(null);
    private lastUserMenuTrigger: HTMLElement | null = null;
    private lastMobileSheetTrigger: HTMLElement | null = null;
    private openingNotifications = false;

    public constructor() {
        effect(() => {
            this.sidebarFacade.syncCurrentUser(this.isAuthenticated());
        });

        effect(() => {
            if (this.isAuthenticated()) {
                this.notificationService.fetchUnreadCount();
                this.notificationService.ensureNotificationsLoaded();
            }
        });

        const mobileMediaQuery = this.browserWindow.matchMedia(this.mobileViewportQuery);
        if (mobileMediaQuery !== null) {
            const updateMobileViewport = (): void => {
                if (mobileMediaQuery.matches) {
                    this.closeUserMenu(false);
                    return;
                }
                this.closeMobileMenus();
            };
            mobileMediaQuery.addEventListener('change', updateMobileViewport);
            this.destroyRef.onDestroy(() => {
                mobileMediaQuery.removeEventListener('change', updateMobileViewport);
            });
        }

        this.router.events.pipe(takeUntilDestroyed(this.destroyRef)).subscribe(event => {
            if (event instanceof NavigationEnd) {
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

    protected onPrimaryAction(action: SidebarActionItem['action'], logoutFocusTarget: FdUiDialogConfig['restoreFocus'] = true): void {
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
                void this.logoutAsync(logoutFocusTarget);
                break;
            }
        }
    }

    protected onMobileAction(action: SidebarActionItem['action']): void {
        this.closeMobileMenus();
        this.onPrimaryAction(action, MOBILE_USER_TRIGGER);
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

    protected toggleFoodTracking(): void {
        this.toggleDesktopSection('food');
    }

    protected toggleCollapsed(): void {
        this.closeUserMenu(false);
        this.collapsedToggle.emit();
    }

    protected toggleBodyTracking(): void {
        this.toggleDesktopSection('body');
    }

    protected toggleUserMenu(trigger: HTMLElement): void {
        this.lastUserMenuTrigger = trigger;

        if (this.isUserMenuOpen()) {
            this.closeUserMenu();
            return;
        }

        this.isUserMenuOpen.set(true);
    }

    protected async openNotificationsAsync(): Promise<void> {
        if (this.openingNotifications) {
            return;
        }

        this.openingNotifications = true;
        try {
            const { NotificationsDialogComponent } = await import('../../components/shared/notifications-dialog/notifications-dialog');
            if (this.destroyRef.destroyed) {
                return;
            }
            const dialog = this.dialogService.open(NotificationsDialogComponent, { preset: 'list' });
            const unregisterClose = this.destroyRef.onDestroy(() => {
                dialog.close();
            });
            dialog.afterClosed().pipe(takeUntilDestroyed(this.destroyRef), finalize(unregisterClose)).subscribe();
        } finally {
            this.openingNotifications = false;
        }
    }

    protected openAdminPanel(): void {
        this.adminPanelLauncher.open();
    }

    protected toggleMobileFood(trigger: HTMLElement): void {
        this.toggleMobileSheet('food', trigger);
    }

    protected toggleMobileBody(trigger: HTMLElement): void {
        this.toggleMobileSheet('body', trigger);
    }

    protected toggleMobileReports(trigger: HTMLElement): void {
        this.toggleMobileSheet('reports', trigger);
    }

    protected toggleMobileUser(trigger: HTMLElement): void {
        this.toggleMobileSheet('user', trigger);
    }

    protected closeMobileMenus(): void {
        const focusTarget = this.lastMobileSheetTrigger;
        this.mobileSheet.set(null);
        this.lastMobileSheetTrigger = null;
        focusTarget?.focus();
    }

    protected async logoutAsync(restoreFocus: FdUiDialogConfig['restoreFocus'] = true): Promise<void> {
        const confirmed = await this.confirmLogoutAsync(restoreFocus);
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

    private async confirmLogoutAsync(restoreFocus: FdUiDialogConfig['restoreFocus']): Promise<boolean> {
        const result = await firstValueFrom(
            this.dialogService
                .open(FdUiConfirmDialogComponent, {
                    preset: 'confirm',
                    restoreFocus,
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

    private toggleMobileSheet(sheet: Exclude<MobileSheetId, null>, trigger: HTMLElement): void {
        this.lastMobileSheetTrigger = trigger;

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
        if (event.key !== 'Escape' || event.defaultPrevented) {
            return;
        }

        if (this.isUserMenuOpen()) {
            event.preventDefault();
            this.closeUserMenu();
        }
    };

    protected closeUserMenu(restoreFocus = true): void {
        this.isUserMenuOpen.set(false);
        if (restoreFocus) {
            this.lastUserMenuTrigger?.focus();
        }
    }
}
