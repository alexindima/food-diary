import { DOCUMENT } from '@angular/common';
import { Component, DestroyRef, ElementRef, computed, effect, inject, signal, viewChild } from '@angular/core';
import { TranslateService } from '@ngx-translate/core';
import { TranslateModule } from '@ngx-translate/core';
import { NavigationCancel, NavigationEnd, NavigationError, Router, RouterModule } from '@angular/router';
import { FdUiButtonComponent, FdUiHintDirective, FdUiIconComponent } from 'fd-ui-kit';
import { AuthService } from '../../services/auth.service';
import { NotificationService } from '../../services/notification.service';
import { FdUiToastService } from 'fd-ui-kit/toast/fd-ui-toast.service';
import { UserService } from '../../shared/api/user.service';
import { SlicePipe, UpperCasePipe } from '@angular/common';
import { FdUiDialogService } from 'fd-ui-kit/dialog/fd-ui-dialog.service';
import { UnsavedChangesService } from '../../services/unsaved-changes.service';
import {
    UnsavedChangesDialogComponent,
    UnsavedChangesDialogResult,
} from '../../components/shared/unsaved-changes-dialog/unsaved-changes-dialog.component';
import { firstValueFrom } from 'rxjs';
import { environment } from '../../../environments/environment';
import { DashboardService } from '../../features/dashboard/api/dashboard.service';
import { SidebarActionLinksComponent } from './sidebar-action-links.component';
import { SidebarRouteLinksComponent } from './sidebar-route-links.component';
import { SidebarActionItem, SidebarNavItem, SidebarRouteItem } from './sidebar.models';

const FOOD_TRACKING_ITEMS: SidebarRouteItem[] = [
    { id: 'meals', icon: 'restaurant_menu', labelKey: 'SIDEBAR.FOOD_DIARY', route: '/meals' },
    { id: 'products', icon: 'inventory_2', labelKey: 'SIDEBAR.PRODUCTS', route: '/products' },
    { id: 'recipes', icon: 'restaurant', labelKey: 'SIDEBAR.RECIPES', route: '/recipes' },
    { id: 'meal-plans', icon: 'restaurant_menu', labelKey: 'SIDEBAR.MEAL_PLANS', route: '/meal-plans' },
    { id: 'shopping-lists', icon: 'shopping_cart', labelKey: 'SIDEBAR.SHOPPING_LIST', route: '/shopping-lists' },
    { id: 'fasting', icon: 'timer', labelKey: 'SIDEBAR.FASTING', route: '/fasting' },
];

const BODY_TRACKING_ITEMS: SidebarRouteItem[] = [
    { id: 'weight-history', icon: 'monitor_weight', labelKey: 'SIDEBAR.WEIGHT', route: '/weight-history' },
    { id: 'waist-history', icon: 'straighten', labelKey: 'SIDEBAR.WAIST', route: '/waist-history' },
    { id: 'cycle-tracking', icon: 'favorite', labelKey: 'SIDEBAR.CYCLE', route: '/cycle-tracking' },
];

const DESKTOP_BOTTOM_ITEMS: SidebarRouteItem[] = [
    { id: 'statistics', icon: 'bar_chart', labelKey: 'SIDEBAR.REPORTS', route: '/statistics' },
    { id: 'goals', icon: 'flag', labelKey: 'SIDEBAR.GOALS', route: '/goals' },
    { id: 'gamification', icon: 'emoji_events', labelKey: 'SIDEBAR.ACHIEVEMENTS', route: '/gamification' },
    { id: 'lessons', icon: 'school', labelKey: 'SIDEBAR.LESSONS', route: '/lessons' },
    { id: 'weekly-check-in', icon: 'assessment', labelKey: 'SIDEBAR.WEEKLY_CHECK_IN', route: '/weekly-check-in' },
];

const MOBILE_REPORT_ITEMS: SidebarRouteItem[] = [
    { id: 'statistics', icon: 'bar_chart', labelKey: 'SIDEBAR.REPORTS', route: '/statistics' },
    { id: 'goals', icon: 'flag', labelKey: 'SIDEBAR.GOALS', route: '/goals' },
    { id: 'gamification', icon: 'emoji_events', labelKey: 'SIDEBAR.ACHIEVEMENTS', route: '/gamification' },
    { id: 'weekly-check-in', icon: 'assessment', labelKey: 'SIDEBAR.WEEKLY_CHECK_IN', route: '/weekly-check-in' },
];

const MOBILE_USER_ROUTE_ITEMS: SidebarRouteItem[] = [{ id: 'profile', icon: 'settings', labelKey: 'HEADER.PROFILE', route: '/profile' }];

type DesktopSectionId = 'food' | 'body' | null;
type MobileSheetId = 'food' | 'body' | 'reports' | 'user' | null;

@Component({
    selector: 'fd-sidebar',
    imports: [
        TranslateModule,
        RouterModule,
        FdUiHintDirective,
        FdUiButtonComponent,
        FdUiIconComponent,
        SidebarActionLinksComponent,
        SidebarRouteLinksComponent,
        SlicePipe,
        UpperCasePipe,
    ],
    templateUrl: './sidebar.component.html',
    styleUrls: ['./sidebar.component.scss'],
})
export class SidebarComponent {
    protected readonly Math = Math;
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

    public isAuthenticated = this.authService.isAuthenticated;
    public isPremium = this.authService.isPremium;
    public isDietologist = this.authService.isDietologist;
    public isAdmin = this.authService.isAdmin;
    public unreadNotificationCount = this.notificationService.unreadCount;
    protected readonly primaryNavItems = computed<SidebarNavItem[]>(() => {
        const items: SidebarNavItem[] = [{ id: 'dashboard', icon: 'dashboard', labelKey: 'SIDEBAR.DASHBOARD', route: '/', exact: true }];

        if (this.isDietologist()) {
            items.push({ id: 'dietologist', icon: 'medical_services', labelKey: 'SIDEBAR.MY_CLIENTS', route: '/dietologist' });
        }

        if (this.isAdmin()) {
            items.push({ id: 'admin', icon: 'admin_panel_settings', labelKey: 'SIDEBAR.ADMIN_PANEL', action: 'openAdminPanel' });
        }

        return items;
    });
    protected readonly primaryRouteItems = computed<SidebarRouteItem[]>(() =>
        this.primaryNavItems().filter((item): item is SidebarRouteItem => 'route' in item),
    );
    protected readonly primaryActionItems = computed<SidebarActionItem[]>(() =>
        this.primaryNavItems().filter((item): item is SidebarActionItem => 'action' in item),
    );
    protected readonly foodTrackingItems = FOOD_TRACKING_ITEMS;
    protected readonly bodyTrackingItems = BODY_TRACKING_ITEMS;
    protected readonly desktopBottomItems = DESKTOP_BOTTOM_ITEMS;
    protected readonly mobileReportItems = MOBILE_REPORT_ITEMS;
    protected readonly mobileUserRouteItems = MOBILE_USER_ROUTE_ITEMS;
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
    protected readonly openDesktopSection = signal<DesktopSectionId>('food');
    protected isUserMenuOpen = signal(false);
    protected readonly mobileSheet = signal<MobileSheetId>(null);
    protected readonly isFoodTrackingOpen = computed(() => this.openDesktopSection() === 'food');
    protected readonly isBodyTrackingOpen = computed(() => this.openDesktopSection() === 'body');
    protected readonly isMobileFoodOpen = computed(() => this.mobileSheet() === 'food');
    protected readonly isMobileBodyOpen = computed(() => this.mobileSheet() === 'body');
    protected readonly isMobileReportsOpen = computed(() => this.mobileSheet() === 'reports');
    protected readonly isMobileUserOpen = computed(() => this.mobileSheet() === 'user');
    protected readonly isMobileSheetOpen = computed(() => this.mobileSheet() !== null);
    protected readonly pendingRoute = signal<string | null>(null);
    protected readonly activeMobileSheetLabelKey = computed(() => {
        switch (this.mobileSheet()) {
            case 'food':
                return 'SIDEBAR.FOOD_TRACKING';
            case 'body':
                return 'SIDEBAR.BODY_TRACKING';
            case 'reports':
                return 'SIDEBAR.REPORTS_AND_GOALS';
            case 'user':
                return 'SIDEBAR.USER_MENU';
            default:
                return '';
        }
    });
    protected readonly activeMobileSheetRouteItems = computed<SidebarRouteItem[]>(() => {
        switch (this.mobileSheet()) {
            case 'food':
                return this.foodTrackingItems;
            case 'body':
                return this.bodyTrackingItems;
            case 'reports':
                return this.mobileReportItems;
            case 'user':
                return this.mobileUserRouteItems;
            default:
                return [];
        }
    });
    protected readonly mobileUserActionItems = computed<SidebarActionItem[]>(() => {
        const items: SidebarActionItem[] = [
            {
                id: 'mobile-notifications',
                icon: 'notifications',
                labelKey: 'NOTIFICATIONS.TITLE',
                action: 'openNotifications',
                variant: 'secondary',
                fill: 'outline',
                className: 'sidebar-mobile__sheet-action',
                badge: this.unreadNotificationCount() || undefined,
            },
            {
                id: 'mobile-logout',
                icon: 'logout',
                labelKey: 'HEADER.LOGOUT',
                action: 'logout',
                variant: 'danger',
                fill: 'outline',
                className: 'sidebar-mobile__sheet-logout',
            },
        ];

        if (this.isAdmin()) {
            items.splice(1, 0, {
                id: 'mobile-admin',
                icon: 'admin_panel_settings',
                labelKey: 'SIDEBAR.ADMIN_PANEL',
                action: 'openAdminPanel',
                variant: 'danger',
                fill: 'outline',
                className: 'sidebar-mobile__sheet-action',
            });
        }

        return items;
    });
    protected readonly activeMobileSheetActionItems = computed<SidebarActionItem[]>(() => {
        if (this.mobileSheet() === 'user') {
            return this.mobileUserActionItems();
        }

        return [];
    });
    protected readonly dailyConsumedKcal = signal(0);
    protected readonly dailyGoalKcal = signal(0);
    protected readonly dailyProgressPercent = computed(() => {
        const goal = this.dailyGoalKcal();
        if (goal <= 0) {
            return 0;
        }

        return Math.max(0, Math.min((this.dailyConsumedKcal() / goal) * 100, 100));
    });
    private readonly userMenuRef = viewChild<ElementRef<HTMLElement>>('userMenu');
    private readonly mobileSheetRef = viewChild<ElementRef<HTMLElement>>('mobileSheet');
    private lastUserMenuTrigger: HTMLElement | null = null;
    private lastMobileSheetTrigger: HTMLElement | null = null;

    private readonly userSync = effect(() => {
        if (!this.isAuthenticated()) {
            this.userService.clearUser();
            return;
        }

        this.syncCurrentUser();
    });

    private readonly progressSync = effect(() => {
        if (!this.isAuthenticated()) {
            this.dailyConsumedKcal.set(0);
            this.dailyGoalKcal.set(0);
            return;
        }

        this.syncDailyProgress();
    });

    private readonly notificationSync = effect(() => {
        if (this.isAuthenticated()) {
            this.notificationService.fetchUnreadCount();
            this.notificationService.ensureNotificationsLoaded();
        }
    });
    private readonly userMenuFocusSync = effect(() => {
        if (!this.isUserMenuOpen()) {
            return;
        }

        queueMicrotask(() => {
            this.focusFirstInteractive(this.userMenuRef()?.nativeElement);
        });
    });
    private readonly mobileSheetFocusSync = effect(() => {
        if (!this.isMobileSheetOpen()) {
            return;
        }

        queueMicrotask(() => {
            this.focusFirstInteractive(this.mobileSheetRef()?.nativeElement);
        });
    });

    public constructor() {
        this.router.events.subscribe(event => {
            if (event instanceof NavigationEnd || event instanceof NavigationCancel || event instanceof NavigationError) {
                this.pendingRoute.set(null);
            }
        });
        this.document.addEventListener('keydown', this.handleDocumentKeydown);
        this.destroyRef.onDestroy(() => this.document.removeEventListener('keydown', this.handleDocumentKeydown));
    }

    protected onPrimaryAction(action: SidebarActionItem['action']): void {
        switch (action) {
            case 'openAdminPanel':
                this.openAdminPanel();
                break;
            case 'openNotifications':
                this.openNotifications();
                break;
            case 'logout':
                void this.logout();
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

    private syncCurrentUser(): void {
        this.userService.getInfoSilently().subscribe();
    }

    private syncDailyProgress(): void {
        this.dashboardService.getSnapshotSilently(new Date(), 1, 1).subscribe(snapshot => {
            this.dailyConsumedKcal.set(snapshot?.statistics?.totalCalories ?? 0);
            this.dailyGoalKcal.set(snapshot?.dailyGoal ?? 0);
        });
    }

    protected toggleFoodTracking(): void {
        this.toggleDesktopSection('food');
    }

    protected toggleBodyTracking(): void {
        this.toggleDesktopSection('body');
    }

    protected toggleUserMenu(trigger?: HTMLElement): void {
        if (trigger) {
            this.lastUserMenuTrigger = trigger;
        }

        if (this.isUserMenuOpen()) {
            this.closeUserMenu();
            return;
        }

        this.isUserMenuOpen.set(true);
    }

    protected async openNotifications(): Promise<void> {
        const { NotificationsDialogComponent } =
            await import('../../components/shared/notifications-dialog/notifications-dialog.component');
        this.dialogService.open(NotificationsDialogComponent, {
            size: 'md',
        });
    }

    protected openAdminPanel(): void {
        if (!this.isAdmin() || !environment.adminAppUrl) {
            return;
        }

        const adminWindow = window.open('about:blank', '_blank');
        adminWindow?.document.write(`
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
        `);
        adminWindow?.document.close();

        this.authService.startAdminSso().subscribe({
            next: response => {
                const url = new URL('/', environment.adminAppUrl);
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

    protected async logout(): Promise<void> {
        const canLogout = await this.confirmUnsavedChanges();
        if (!canLogout) {
            return;
        }

        await this.authService.onLogout(true);
        this.closeUserMenu();
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

    private toggleDesktopSection(section: Exclude<DesktopSectionId, null>): void {
        this.openDesktopSection.update(current => (current === section ? null : section));
    }

    private toggleMobileSheet(sheet: Exclude<MobileSheetId, null>, trigger?: HTMLElement): void {
        if (trigger) {
            this.lastMobileSheetTrigger = trigger;
        }

        if (this.mobileSheet() === sheet) {
            this.closeMobileMenus();
            return;
        }

        this.mobileSheet.set(sheet);
    }

    private isRouteActive(route: string, exact: boolean): boolean {
        const currentPath = this.router.url.split('?')[0].split('#')[0] || '/';
        if (exact) {
            return currentPath === route;
        }

        return currentPath === route || currentPath.startsWith(`${route}/`);
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

    private focusFirstInteractive(container?: HTMLElement | null): void {
        if (!container) {
            return;
        }

        const firstInteractive = container.querySelector<HTMLElement>('button:not([disabled]), a[href], [tabindex]:not([tabindex="-1"])');
        firstInteractive?.focus();
    }
}
