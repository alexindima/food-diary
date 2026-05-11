import { ChangeDetectionStrategy, Component, input, output } from '@angular/core';
import { TranslatePipe } from '@ngx-translate/core';
import { FdUiButtonComponent, FdUiHintDirective, FdUiIconComponent } from 'fd-ui-kit';

import type { User } from '../../shared/models/user.data';
import type { SidebarActionId, SidebarActionItem, SidebarDirectRouteRequest, SidebarRouteItem } from './sidebar.models';
import { SidebarRouteLinksComponent } from './sidebar-route-links.component';
import { SidebarUserMenuComponent } from './sidebar-user-menu.component';

@Component({
    selector: 'fd-sidebar-desktop',
    imports: [
        FdUiButtonComponent,
        FdUiHintDirective,
        FdUiIconComponent,
        SidebarRouteLinksComponent,
        SidebarUserMenuComponent,
        TranslatePipe,
    ],
    templateUrl: './sidebar-desktop.component.html',
    styleUrl: './sidebar.component.scss',
    changeDetection: ChangeDetectionStrategy.OnPush,
})
export class SidebarDesktopComponent {
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

    public readonly notificationsOpen = output<void>();
    public readonly routeSelected = output<SidebarRouteItem>();
    public readonly primaryAction = output<SidebarActionId>();
    public readonly foodTrackingToggle = output<void>();
    public readonly bodyTrackingToggle = output<void>();
    public readonly userMenuToggle = output<HTMLElement>();
    public readonly directRouteClick = output<SidebarDirectRouteRequest>();
    public readonly logout = output<void>();
}
