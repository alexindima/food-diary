import { ChangeDetectionStrategy, Component, computed, input, output } from '@angular/core';
import { RouterModule } from '@angular/router';

import { FdUiIconComponent } from '../icon/fd-ui-icon';
import type { FdUiSidebarActionItem, FdUiSidebarItem, FdUiSidebarRouteItem } from './fd-ui-sidebar.models';

@Component({
    selector: 'fd-ui-sidebar-item',
    imports: [RouterModule, FdUiIconComponent],
    templateUrl: './fd-ui-sidebar-item.html',
    styleUrl: './fd-ui-sidebar-item.scss',
    changeDetection: ChangeDetectionStrategy.OnPush,
})
export class FdUiSidebarItemComponent {
    public readonly item = input.required<FdUiSidebarItem>();
    public readonly secondary = input(false);
    public readonly isPending = input(false);

    public readonly routeSelected = output<FdUiSidebarRouteItem>();
    public readonly actionSelected = output<FdUiSidebarActionItem>();

    protected readonly routeItem = computed(() => {
        const item = this.item();

        return 'route' in item ? item : null;
    });

    protected readonly actionItem = computed(() => {
        const item = this.item();

        return 'action' in item ? item : null;
    });

    protected itemClasses(item: FdUiSidebarItem): string {
        return [
            'fd-ui-sidebar__link',
            this.secondary() ? 'fd-ui-sidebar__link--secondary' : '',
            item.tone === 'danger' ? 'fd-ui-sidebar__link--danger' : '',
            item.tone === 'brand' ? 'fd-ui-sidebar__link--brand' : '',
        ]
            .filter(className => className.length > 0)
            .join(' ');
    }

    protected hasBadge(item: FdUiSidebarItem): boolean {
        return item.badge !== undefined && item.badge > 0;
    }
}
