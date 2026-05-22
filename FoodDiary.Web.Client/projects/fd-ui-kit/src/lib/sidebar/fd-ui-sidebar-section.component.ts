import { ChangeDetectionStrategy, Component, input, output } from '@angular/core';

import { FdUiIconComponent } from '../icon/fd-ui-icon.component';
import type {
    FdUiSidebarActionItem,
    FdUiSidebarItem,
    FdUiSidebarRouteItem,
    FdUiSidebarSection,
    FdUiSidebarSectionRequest,
} from './fd-ui-sidebar.models';
import { FdUiSidebarItemComponent } from './fd-ui-sidebar-item.component';

@Component({
    selector: 'fd-ui-sidebar-section',
    imports: [FdUiIconComponent, FdUiSidebarItemComponent],
    templateUrl: './fd-ui-sidebar-section.component.html',
    styleUrl: './fd-ui-sidebar-section.component.scss',
    changeDetection: ChangeDetectionStrategy.OnPush,
})
export class FdUiSidebarSectionComponent {
    public readonly section = input.required<FdUiSidebarSection>();
    public readonly pendingRoute = input<string | null>(null);

    public readonly routeSelected = output<FdUiSidebarRouteItem>();
    public readonly actionSelected = output<FdUiSidebarActionItem>();
    public readonly sectionToggled = output<FdUiSidebarSectionRequest>();

    protected isPending(item: FdUiSidebarItem): boolean {
        return 'route' in item && this.pendingRoute() === item.route;
    }
}
