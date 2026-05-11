import { ChangeDetectionStrategy, Component, input, output } from '@angular/core';
import { FdUiLoaderComponent } from 'fd-ui-kit/loader/fd-ui-loader.component';

import { DailyAdviceCardComponent } from '../components/daily-advice-card/daily-advice-card.component';
import { DashboardCardShellComponent } from '../components/dashboard-card-shell/dashboard-card-shell.component';
import type { DashboardBlockState, DashboardDailyAdvice } from './dashboard-view.types';

@Component({
    selector: 'fd-dashboard-advice-block',
    imports: [FdUiLoaderComponent, DashboardCardShellComponent, DailyAdviceCardComponent],
    templateUrl: './dashboard-advice-block.component.html',
    styleUrl: './dashboard.component.scss',
    changeDetection: ChangeDetectionStrategy.OnPush,
})
export class DashboardAdviceBlockComponent {
    public readonly shouldRender = input.required<boolean>();
    public readonly state = input.required<DashboardBlockState>();
    public readonly advice = input.required<DashboardDailyAdvice>();
    public readonly isLoading = input.required<boolean>();

    public readonly blockToggle = output<void>();
}
