import { ChangeDetectionStrategy, Component, input, output } from '@angular/core';
import { FdUiLoaderComponent } from 'fd-ui-kit/loader/fd-ui-loader.component';

import { CycleSummaryCardComponent } from '../../../components/cycle-summary-card/cycle-summary-card.component';
import { DashboardCardShellComponent } from '../../../components/dashboard-card-shell/dashboard-card-shell.component';
import type { DashboardBlockState, DashboardCycleCardState } from '../../dashboard-lib/dashboard-view.types';

@Component({
    selector: 'fd-dashboard-cycle-block',
    imports: [FdUiLoaderComponent, DashboardCardShellComponent, CycleSummaryCardComponent],
    templateUrl: './dashboard-cycle-block.component.html',
    styleUrl: '../../dashboard.component.scss',
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
