import { ChangeDetectionStrategy, Component, input, output } from '@angular/core';
import { FdUiLoaderComponent } from 'fd-ui-kit/loader/fd-ui-loader.component';

import { DashboardCardShellComponent } from '../components/dashboard-card-shell/dashboard-card-shell.component';
import { HydrationCardComponent } from '../components/hydration-card/hydration-card.component';
import type { DashboardBlockState, DashboardHydrationCardState } from './dashboard-view.types';

@Component({
    selector: 'fd-dashboard-hydration-block',
    imports: [FdUiLoaderComponent, DashboardCardShellComponent, HydrationCardComponent],
    templateUrl: './dashboard-hydration-block.component.html',
    styleUrl: './dashboard.component.scss',
    changeDetection: ChangeDetectionStrategy.OnPush,
})
export class DashboardHydrationBlockComponent {
    public readonly shouldRender = input.required<boolean>();
    public readonly state = input.required<DashboardBlockState>();
    public readonly cardState = input.required<DashboardHydrationCardState>();
    public readonly isLoading = input.required<boolean>();
    public readonly canAdd = input.required<boolean>();

    public readonly blockToggle = output<void>();
    public readonly addClick = output<number>();
    public readonly goalAction = output<void>();
}
