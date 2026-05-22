import { ChangeDetectionStrategy, Component, computed, input, output } from '@angular/core';

import { FdUiButtonComponent } from '../button/fd-ui-button.component';
import { FdUiHintDirective } from '../hint/fd-ui-hint.directive';
import type {
    FdUiSidebarActionRequest,
    FdUiSidebarRouteRequest,
    FdUiSidebarSection,
    FdUiSidebarSectionRequest,
} from './fd-ui-sidebar.models';
import { FdUiSidebarSectionComponent } from './fd-ui-sidebar-section.component';

@Component({
    selector: 'fd-ui-sidebar',
    imports: [FdUiButtonComponent, FdUiHintDirective, FdUiSidebarSectionComponent],
    templateUrl: './fd-ui-sidebar.component.html',
    styleUrl: './fd-ui-sidebar.component.scss',
    changeDetection: ChangeDetectionStrategy.OnPush,
})
export class FdUiSidebarComponent {
    public readonly brandTitle = input.required<string>();
    public readonly brandSubtitle = input<string | undefined>(undefined);
    public readonly logoText = input<string | undefined>(undefined);
    public readonly logoMaskUrl = input<string | undefined>(undefined);
    public readonly notificationAriaLabel = input<string | undefined>(undefined);
    public readonly notificationHint = input<string | undefined>(undefined);
    public readonly notificationBadge = input(0);
    public readonly pendingRoute = input<string | null>(null);
    public readonly sections = input.required<FdUiSidebarSection[]>();

    public readonly notificationClick = output();
    public readonly routeSelected = output<FdUiSidebarRouteRequest>();
    public readonly actionSelected = output<FdUiSidebarActionRequest>();
    public readonly sectionToggled = output<FdUiSidebarSectionRequest>();

    protected readonly logoMaskStyle = computed(() => {
        const url = this.logoMaskUrl();

        return url === undefined || url.length === 0 ? undefined : `url('${url}')`;
    });
}
