import { ChangeDetectionStrategy, Component, input, output } from '@angular/core';
import { RouterModule } from '@angular/router';
import { TranslatePipe } from '@ngx-translate/core';
import { FdUiIconComponent } from 'fd-ui-kit';

import type { SidebarRouteItem } from '../sidebar-lib/sidebar.models';
import { isSidebarRouterLinkActivation } from '../sidebar-lib/sidebar-view.utils';

@Component({
    selector: 'fd-sidebar-route-links',
    imports: [RouterModule, TranslatePipe, FdUiIconComponent],
    templateUrl: './sidebar-route-links.html',
    styleUrls: ['./sidebar-route-links.scss'],
    changeDetection: ChangeDetectionStrategy.OnPush,
})
export class SidebarRouteLinksComponent {
    public readonly items = input.required<SidebarRouteItem[]>();
    public readonly linkClass = input.required<string>();
    public readonly pendingRoute = input.required<string | null>();
    public readonly itemSelected = output<SidebarRouteItem>();

    protected activateRoute(event: MouseEvent, item: SidebarRouteItem): void {
        if (isSidebarRouterLinkActivation(event)) {
            this.itemSelected.emit(item);
        }
    }
}
