import { ChangeDetectionStrategy, Component, input, output } from '@angular/core';
import { FdUiLoaderComponent } from 'fd-ui-kit/loader/fd-ui-loader';

import { CycleSummaryCardComponent } from '../../../components/cycle-summary-card/cycle-summary-card';
import { DashboardCardShellComponent } from '../../../components/dashboard-card-shell/dashboard-card-shell';
import { DashboardBlockContentDirective, DashboardBlockHostDirective } from '../../dashboard-lib/dashboard-block-host.directive';
import type { DashboardBlockState, DashboardCycleCardState } from '../../dashboard-lib/dashboard-view.types';

@Component({
    selector: 'fd-dashboard-cycle-block',
    imports: [
        FdUiLoaderComponent,
        DashboardBlockContentDirective,
        DashboardBlockHostDirective,
        DashboardCardShellComponent,
        CycleSummaryCardComponent,
    ],
    templateUrl: './dashboard-cycle-block.html',
    styleUrl: '../../dashboard.scss',
    changeDetection: ChangeDetectionStrategy.OnPush,
})
export class DashboardCycleBlockComponent {
    public readonly shouldRender = input.required<boolean>();
    public readonly state = input.required<DashboardBlockState>();
    public readonly cardState = input.required<DashboardCycleCardState>();
    public readonly referenceDate = input.required<Date>();
    public readonly isLoading = input.required<boolean>();

    public readonly blockToggle = output();
    public readonly setupAction = output();
}
