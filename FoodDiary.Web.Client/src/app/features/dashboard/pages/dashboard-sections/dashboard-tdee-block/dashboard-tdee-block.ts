import { ChangeDetectionStrategy, Component, input, output } from '@angular/core';
import { FdUiLoaderComponent } from 'fd-ui-kit/loader/fd-ui-loader';

import { DashboardCardShellComponent } from '../../../components/dashboard-card-shell/dashboard-card-shell';
import { TdeeInsightCardComponent } from '../../../components/tdee-insight-card/tdee-insight-card';
import { DashboardBlockContentDirective, DashboardBlockHostDirective } from '../../dashboard-lib/dashboard-block-host.directive';
import type { DashboardBlockState, DashboardTdeeInsight } from '../../dashboard-lib/dashboard-view.types';

@Component({
    selector: 'fd-dashboard-tdee-block',
    imports: [
        FdUiLoaderComponent,
        DashboardBlockContentDirective,
        DashboardBlockHostDirective,
        DashboardCardShellComponent,
        TdeeInsightCardComponent,
    ],
    templateUrl: './dashboard-tdee-block.html',
    styleUrl: '../../dashboard.scss',
    changeDetection: ChangeDetectionStrategy.OnPush,
})
export class DashboardTdeeBlockComponent {
    public readonly shouldRender = input.required<boolean>();
    public readonly state = input.required<DashboardBlockState>();
    public readonly insight = input.required<DashboardTdeeInsight>();
    public readonly isLoading = input.required<boolean>();

    public readonly blockClick = output<Event>();
    public readonly applyGoal = output<number>();
}
