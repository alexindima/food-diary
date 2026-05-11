import { ChangeDetectionStrategy, Component, input, output } from '@angular/core';
import { TranslatePipe } from '@ngx-translate/core';

import { FastingTimerCardComponent } from '../../fasting/components/fasting-timer-card/fasting-timer-card.component';
import { DashboardCardShellComponent } from '../components/dashboard-card-shell/dashboard-card-shell.component';
import type { DashboardBlockState, DashboardFastingSession } from './dashboard-view.types';

@Component({
    selector: 'fd-dashboard-fasting-block',
    imports: [TranslatePipe, DashboardCardShellComponent, FastingTimerCardComponent],
    templateUrl: './dashboard-fasting-block.component.html',
    styleUrl: './dashboard.component.scss',
    changeDetection: ChangeDetectionStrategy.OnPush,
})
export class DashboardFastingBlockComponent {
    public readonly shouldRender = input.required<boolean>();
    public readonly state = input.required<DashboardBlockState>();
    public readonly isEditingLayout = input.required<boolean>();
    public readonly session = input.required<DashboardFastingSession>();

    public readonly blockClick = output<void>();
}
