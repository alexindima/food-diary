import { ChangeDetectionStrategy, Component, input, output } from '@angular/core';
import { TranslatePipe } from '@ngx-translate/core';

import { DashboardSummaryCardComponent } from '../../../components/shared/dashboard-summary-card/dashboard-summary-card.component';
import { DashboardCardShellComponent } from '../components/dashboard-card-shell/dashboard-card-shell.component';
import type { DashboardBlockState, DashboardSummaryData } from './dashboard-view.types';

@Component({
    selector: 'fd-dashboard-summary-block',
    imports: [TranslatePipe, DashboardCardShellComponent, DashboardSummaryCardComponent],
    templateUrl: './dashboard-summary-block.component.html',
    styleUrl: './dashboard.component.scss',
    changeDetection: ChangeDetectionStrategy.OnPush,
})
export class DashboardSummaryBlockComponent {
    public readonly shouldRender = input.required<boolean>();
    public readonly state = input.required<DashboardBlockState>();
    public readonly isEditingLayout = input.required<boolean>();
    public readonly data = input.required<DashboardSummaryData>();
    public readonly caloriesBurned = input.required<number>();

    public readonly blockToggle = output();
    public readonly goalAction = output();
}
