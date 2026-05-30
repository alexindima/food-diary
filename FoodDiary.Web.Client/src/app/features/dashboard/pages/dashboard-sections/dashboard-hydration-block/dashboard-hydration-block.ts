import { ChangeDetectionStrategy, Component, input, output } from '@angular/core';
import { FdUiLoaderComponent } from 'fd-ui-kit/loader/fd-ui-loader';

import { DashboardCardShellComponent } from '../../../components/dashboard-card-shell/dashboard-card-shell';
import { HydrationCardComponent } from '../../../components/hydration-card/hydration-card';
import { DashboardBlockContentDirective, DashboardBlockHostDirective } from '../../dashboard-lib/dashboard-block-host.directive';
import type { DashboardBlockState, DashboardHydrationCardState } from '../../dashboard-lib/dashboard-view.types';

@Component({
    selector: 'fd-dashboard-hydration-block',
    imports: [
        FdUiLoaderComponent,
        DashboardBlockContentDirective,
        DashboardBlockHostDirective,
        DashboardCardShellComponent,
        HydrationCardComponent,
    ],
    templateUrl: './dashboard-hydration-block.html',
    styleUrl: '../../dashboard.scss',
    changeDetection: ChangeDetectionStrategy.OnPush,
})
export class DashboardHydrationBlockComponent {
    public readonly shouldRender = input.required<boolean>();
    public readonly state = input.required<DashboardBlockState>();
    public readonly cardState = input.required<DashboardHydrationCardState>();
    public readonly isLoading = input.required<boolean>();
    public readonly canAdd = input.required<boolean>();

    public readonly blockToggle = output();
    public readonly addClick = output<number>();
    public readonly goalAction = output();
}
