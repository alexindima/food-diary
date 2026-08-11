import { ChangeDetectionStrategy, Component, input, output } from '@angular/core';
import { TranslatePipe } from '@ngx-translate/core';

import { DashboardCardShellComponent } from '../../../components/dashboard-card-shell/dashboard-card-shell';
import { DashboardFastingCardComponent } from '../../../components/dashboard-fasting-card/dashboard-fasting-card';
import { DashboardBlockContentDirective, DashboardBlockHostDirective } from '../../dashboard-lib/dashboard-block-host.directive';
import type { DashboardBlockState, DashboardFastingSession } from '../../dashboard-lib/dashboard-view.types';

@Component({
    selector: 'fd-dashboard-fasting-block',
    imports: [
        TranslatePipe,
        DashboardBlockContentDirective,
        DashboardBlockHostDirective,
        DashboardCardShellComponent,
        DashboardFastingCardComponent,
    ],
    templateUrl: './dashboard-fasting-block.html',
    styleUrl: '../../dashboard.scss',
    changeDetection: ChangeDetectionStrategy.OnPush,
})
export class DashboardFastingBlockComponent {
    public readonly shouldRender = input.required<boolean>();
    public readonly state = input.required<DashboardBlockState>();
    public readonly isEditingLayout = input.required<boolean>();
    public readonly session = input.required<DashboardFastingSession>();

    public readonly blockClick = output();
}
