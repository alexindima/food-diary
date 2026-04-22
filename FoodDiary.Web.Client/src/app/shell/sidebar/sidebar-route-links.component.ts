import { ChangeDetectionStrategy, Component, ViewEncapsulation, input, output } from '@angular/core';
import { RouterModule } from '@angular/router';
import { TranslatePipe } from '@ngx-translate/core';
import { FdUiIconComponent } from 'fd-ui-kit';
import { SidebarRouteItem } from './sidebar.models';

@Component({
    selector: 'fd-sidebar-route-links',
    standalone: true,
    imports: [RouterModule, TranslatePipe, FdUiIconComponent],
    templateUrl: './sidebar-route-links.component.html',
    styleUrls: ['./sidebar-route-links.component.scss'],
    encapsulation: ViewEncapsulation.None,
    changeDetection: ChangeDetectionStrategy.OnPush,
})
export class SidebarRouteLinksComponent {
    public readonly items = input.required<SidebarRouteItem[]>();
    public readonly linkClass = input.required<string>();
    public readonly pendingRoute = input<string | null>(null);
    public readonly itemSelected = output<SidebarRouteItem>();
}
