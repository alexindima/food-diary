import { ChangeDetectionStrategy, Component, input, output } from '@angular/core';
import { FdUiLoaderComponent } from 'fd-ui-kit/loader/fd-ui-loader';

import { DailyAdviceCardComponent } from '../../../components/daily-advice-card/daily-advice-card';
import { DashboardCardShellComponent } from '../../../components/dashboard-card-shell/dashboard-card-shell';
import { DashboardBlockContentDirective, DashboardBlockHostDirective } from '../../dashboard-lib/dashboard-block-host.directive';
import type { DashboardBlockState, DashboardDailyAdvice } from '../../dashboard-lib/dashboard-view.types';

@Component({
    selector: 'fd-dashboard-advice-block',
    imports: [
        FdUiLoaderComponent,
        DashboardBlockContentDirective,
        DashboardBlockHostDirective,
        DashboardCardShellComponent,
        DailyAdviceCardComponent,
    ],
    templateUrl: './dashboard-advice-block.html',
    styleUrl: '../../dashboard.scss',
    changeDetection: ChangeDetectionStrategy.OnPush,
})
export class DashboardAdviceBlockComponent {
    public readonly shouldRender = input.required<boolean>();
    public readonly state = input.required<DashboardBlockState>();
    public readonly advice = input.required<DashboardDailyAdvice>();
    public readonly isLoading = input.required<boolean>();

    public readonly blockToggle = output();
}
