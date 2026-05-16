import { ChangeDetectionStrategy, Component, input, output } from '@angular/core';
import { TranslatePipe } from '@ngx-translate/core';
import { FdUiLoaderComponent } from 'fd-ui-kit/loader/fd-ui-loader.component';

import { DashboardCardShellComponent } from '../../../components/dashboard-card-shell/dashboard-card-shell.component';
import { WeightTrendCardComponent } from '../../../components/weight-trend-card/weight-trend-card.component';
import type { DashboardBlockState, DashboardWeightTrendPoint } from '../../dashboard-lib/dashboard-view.types';

@Component({
    selector: 'fd-dashboard-trend-block',
    imports: [TranslatePipe, FdUiLoaderComponent, DashboardCardShellComponent, WeightTrendCardComponent],
    templateUrl: './dashboard-trend-block.component.html',
    styleUrl: '../../dashboard.component.scss',
    changeDetection: ChangeDetectionStrategy.OnPush,
})
export class DashboardTrendBlockComponent {
    public readonly shouldRender = input.required<boolean>();
    public readonly state = input.required<DashboardBlockState>();
    public readonly cardClass = input.required<string>();
    public readonly current = input.required<number | null>();
    public readonly change = input.required<number | null>();
    public readonly points = input.required<DashboardWeightTrendPoint[]>();
    public readonly isLoading = input.required<boolean>();
    public readonly title = input<string>('WEIGHT_CARD.TITLE');
    public readonly unitKey = input<string>('WEIGHT_CARD.KG');
    public readonly iconName = input<string | null>('monitor_weight');
    public readonly accentColor = input<string>('var(--fd-color-blue-500)');

    public readonly blockToggle = output();
}
