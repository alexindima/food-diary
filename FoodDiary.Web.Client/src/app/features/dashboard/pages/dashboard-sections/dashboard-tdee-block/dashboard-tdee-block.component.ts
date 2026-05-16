import { ChangeDetectionStrategy, Component, input, output } from '@angular/core';
import { FdUiLoaderComponent } from 'fd-ui-kit/loader/fd-ui-loader.component';

import { DashboardCardShellComponent } from '../../../components/dashboard-card-shell/dashboard-card-shell.component';
import { TdeeInsightCardComponent } from '../../../components/tdee-insight-card/tdee-insight-card.component';
import type { DashboardBlockState, DashboardTdeeInsight } from '../../dashboard-lib/dashboard-view.types';

@Component({
    selector: 'fd-dashboard-tdee-block',
    imports: [FdUiLoaderComponent, DashboardCardShellComponent, TdeeInsightCardComponent],
    templateUrl: './dashboard-tdee-block.component.html',
    styleUrl: '../../dashboard.component.scss',
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
