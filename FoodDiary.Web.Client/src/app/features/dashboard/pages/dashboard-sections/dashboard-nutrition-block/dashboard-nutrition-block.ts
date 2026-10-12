import { ChangeDetectionStrategy, Component, input, output } from '@angular/core';

import {
    type NutritionTrendInsight,
    type NutritionTrendPoint,
    NutritionWeeklyTrendCardComponent,
} from '../../../../../components/shared/nutrition-weekly-trend-card/nutrition-weekly-trend-card';
import { DashboardBlockContentDirective, DashboardBlockHostDirective } from '../../dashboard-lib/dashboard-block-host.directive';
import type { DashboardBlockState } from '../../dashboard-lib/dashboard-view.types';

@Component({
    selector: 'fd-dashboard-nutrition-block',
    imports: [NutritionWeeklyTrendCardComponent, DashboardBlockHostDirective, DashboardBlockContentDirective],
    templateUrl: './dashboard-nutrition-block.html',
    styleUrl: '../../dashboard.scss',
    changeDetection: ChangeDetectionStrategy.OnPush,
})
export class DashboardNutritionBlockComponent {
    public readonly shouldRender = input.required<boolean>();
    public readonly state = input.required<DashboardBlockState>();
    public readonly currentDate = input.required<Date>();
    public readonly points = input.required<NutritionTrendPoint[]>();
    public readonly dailyGoal = input.required<number>();
    public readonly insight = input.required<NutritionTrendInsight>();
    public readonly isToday = input.required<boolean>();

    public readonly blockToggle = output();
    public readonly details = output();
}
