import { ChangeDetectionStrategy, Component, input, output } from '@angular/core';
import { TranslatePipe } from '@ngx-translate/core';

import { DayNutritionSummaryComponent } from '../../../../../components/shared/day-nutrition-summary/day-nutrition-summary';
import { DashboardCardShellComponent } from '../../../components/dashboard-card-shell/dashboard-card-shell';
import { DashboardBlockContentDirective, DashboardBlockHostDirective } from '../../dashboard-lib/dashboard-block-host.directive';
import type { DashboardBlockState, DashboardSummaryData } from '../../dashboard-lib/dashboard-view.types';

@Component({
    selector: 'fd-dashboard-summary-block',
    imports: [
        TranslatePipe,
        DashboardBlockContentDirective,
        DashboardBlockHostDirective,
        DashboardCardShellComponent,
        DayNutritionSummaryComponent,
    ],
    templateUrl: './dashboard-summary-block.html',
    styleUrl: '../../dashboard.scss',
    changeDetection: ChangeDetectionStrategy.OnPush,
})
export class DashboardSummaryBlockComponent {
    public readonly shouldRender = input.required<boolean>();
    public readonly state = input.required<DashboardBlockState>();
    public readonly isEditingLayout = input.required<boolean>();
    public readonly data = input.required<DashboardSummaryData>();

    public readonly blockToggle = output();
}
