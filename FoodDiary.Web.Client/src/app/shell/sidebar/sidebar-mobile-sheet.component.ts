import { ChangeDetectionStrategy, Component, input, output } from '@angular/core';
import { RouterModule } from '@angular/router';
import { TranslatePipe } from '@ngx-translate/core';
import { FdUiButtonComponent, FdUiHintDirective, FdUiIconComponent } from 'fd-ui-kit';

import type { MobileSheetId, SidebarActionId, SidebarDirectRouteRequest, SidebarRouteItem } from './sidebar.models';
import { SidebarRouteLinksComponent } from './sidebar-route-links.component';

@Component({
    selector: 'fd-sidebar-mobile-sheet',
    imports: [RouterModule, FdUiButtonComponent, FdUiHintDirective, FdUiIconComponent, SidebarRouteLinksComponent, TranslatePipe],
    templateUrl: './sidebar-mobile-sheet.component.html',
    styleUrl: './sidebar.component.scss',
    changeDetection: ChangeDetectionStrategy.OnPush,
})
export class SidebarMobileSheetComponent {
    public readonly mobileSheet = input.required<MobileSheetId>();
    public readonly pendingRoute = input.required<string | null>();
    public readonly unreadNotificationCount = input.required<number>();
    public readonly isAdmin = input.required<boolean>();
    public readonly activeRouteItems = input.required<SidebarRouteItem[]>();

    public readonly directRouteClick = output<SidebarDirectRouteRequest>();
    public readonly closeMenus = output();
    public readonly mobileAction = output<SidebarActionId>();
    public readonly routeSelected = output<SidebarRouteItem>();
}
