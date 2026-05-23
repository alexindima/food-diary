import { ChangeDetectionStrategy, Component, computed, inject, input, output } from '@angular/core';
import { toSignal } from '@angular/core/rxjs-interop';
import { TranslatePipe, TranslateService } from '@ngx-translate/core';
import { type FdUiSidebarActionItem, FdUiSidebarComponent, type FdUiSidebarRouteItem, type FdUiSidebarSection } from 'fd-ui-kit';
import { startWith } from 'rxjs';

import type { User } from '../../../shared/models/user.data';
import type { SidebarActionId, SidebarActionItem, SidebarDirectRouteRequest, SidebarRouteItem } from '../sidebar-lib/sidebar.models';
import { SidebarUserMenuComponent } from '../sidebar-user-menu/sidebar-user-menu.component';

@Component({
    selector: 'fd-sidebar-desktop',
    imports: [FdUiSidebarComponent, SidebarUserMenuComponent, TranslatePipe],
    templateUrl: './sidebar-desktop.component.html',
    styleUrl: '../sidebar.component.scss',
    changeDetection: ChangeDetectionStrategy.OnPush,
})
export class SidebarDesktopComponent {
    private readonly translateService = inject(TranslateService);
    private readonly languageChange = toSignal(this.translateService.onLangChange.pipe(startWith(null)), { initialValue: null });

    public readonly brandStatusKey = input.required<string>();
    public readonly unreadNotificationCount = input.required<number>();
    public readonly primaryRouteItems = input.required<SidebarRouteItem[]>();
    public readonly primaryActionItems = input.required<SidebarActionItem[]>();
    public readonly pendingRoute = input.required<string | null>();
    public readonly foodTrackingItems = input.required<SidebarRouteItem[]>();
    public readonly bodyTrackingItems = input.required<SidebarRouteItem[]>();
    public readonly desktopBottomItems = input.required<SidebarRouteItem[]>();
    public readonly isFoodTrackingOpen = input.required<boolean>();
    public readonly isBodyTrackingOpen = input.required<boolean>();
    public readonly currentUser = input.required<User | null>();
    public readonly userPlanLabelKey = input.required<string>();
    public readonly isUserMenuOpen = input.required<boolean>();

    public readonly notificationsOpen = output();
    public readonly routeSelected = output<SidebarRouteItem>();
    public readonly primaryAction = output<SidebarActionId>();
    public readonly foodTrackingToggle = output();
    public readonly bodyTrackingToggle = output();
    public readonly userMenuToggle = output<HTMLElement>();
    public readonly directRouteClick = output<SidebarDirectRouteRequest>();
    public readonly logout = output();

    protected readonly sidebarSections = computed<FdUiSidebarSection[]>(() => [
        {
            id: 'primary',
            items: [...this.translateRouteItems(this.primaryRouteItems()), ...this.translateActionItems(this.primaryActionItems())],
        },
        {
            id: 'food',
            title: this.translateKey('SIDEBAR.FOOD_TRACKING'),
            items: this.translateRouteItems(this.foodTrackingItems()),
            expanded: this.isFoodTrackingOpen(),
            collapsible: true,
            secondary: true,
        },
        {
            id: 'body',
            title: this.translateKey('SIDEBAR.BODY_TRACKING'),
            items: this.translateRouteItems(this.bodyTrackingItems()),
            expanded: this.isBodyTrackingOpen(),
            collapsible: true,
            secondary: true,
        },
    ]);

    protected readonly sidebarBottomSections = computed<FdUiSidebarSection[]>(() => [
        {
            id: 'bottom',
            items: this.translateRouteItems(this.desktopBottomItems()),
            secondary: true,
        },
    ]);

    protected onSidebarSectionToggle(sectionId: string): void {
        if (sectionId === 'food') {
            this.foodTrackingToggle.emit();
            return;
        }

        if (sectionId === 'body') {
            this.bodyTrackingToggle.emit();
        }
    }

    protected onSidebarRouteSelected(item: FdUiSidebarRouteItem): void {
        const routeItem = this.findRouteItem(item.id);

        if (routeItem !== undefined) {
            this.routeSelected.emit(routeItem);
        }
    }

    protected onSidebarActionSelected(item: FdUiSidebarActionItem): void {
        const actionItem = this.primaryActionItems().find(primaryItem => primaryItem.id === item.id);

        if (actionItem !== undefined) {
            this.primaryAction.emit(actionItem.action);
        }
    }

    private translateRouteItems(items: SidebarRouteItem[]): FdUiSidebarRouteItem[] {
        return items.map(item => ({
            id: item.id,
            icon: item.icon,
            label: this.translateKey(item.labelKey),
            route: item.route,
            exact: item.exact,
        }));
    }

    private translateActionItems(items: SidebarActionItem[]): FdUiSidebarActionItem[] {
        return items.map(item => ({
            id: item.id,
            icon: item.icon,
            label: this.translateKey(item.labelKey),
            action: item.action,
            badge: item.badge,
            tone: item.variant === 'danger' || item.action === 'openAdminPanel' ? 'danger' : 'default',
        }));
    }

    private translateKey(key: string): string {
        this.languageChange();

        return this.translateService.instant(key);
    }

    private findRouteItem(id: string): SidebarRouteItem | undefined {
        return [...this.primaryRouteItems(), ...this.foodTrackingItems(), ...this.bodyTrackingItems(), ...this.desktopBottomItems()].find(
            item => item.id === id,
        );
    }
}
