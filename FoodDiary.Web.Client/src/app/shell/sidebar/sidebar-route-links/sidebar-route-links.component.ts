import { ChangeDetectionStrategy, Component, input, output } from '@angular/core';
import { RouterModule } from '@angular/router';
import { TranslatePipe } from '@ngx-translate/core';
import { FdUiIconComponent } from 'fd-ui-kit';

import type { SidebarRouteItem } from '../sidebar-lib/sidebar.models';

@Component({
    selector: 'fd-sidebar-route-links',
    imports: [RouterModule, TranslatePipe, FdUiIconComponent],
    templateUrl: './sidebar-route-links.component.html',
    styleUrls: ['./sidebar-route-links.component.scss'],
    changeDetection: ChangeDetectionStrategy.OnPush,
})
export class SidebarRouteLinksComponent {
    public readonly items = input.required<SidebarRouteItem[]>();
    public readonly linkClass = input.required<string>();
    public readonly pendingRoute = input.required<string | null>();
    public readonly itemSelected = output<SidebarRouteItem>();
}
