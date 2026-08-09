import { DecimalPipe } from '@angular/common';
import { ChangeDetectionStrategy, Component, computed, input } from '@angular/core';
import { TranslatePipe } from '@ngx-translate/core';
import { FdUiCardComponent, FdUiIconComponent } from 'fd-ui-kit';

import type { StatisticsNutrientProgress, StatisticsOverviewData } from '../../models/statistics-dashboard-card.models';

const PERCENT_MAX = 100;
const RING_RADIUS = 104;
const RING_CIRCUMFERENCE = 2 * Math.PI * RING_RADIUS;

@Component({
    selector: 'fd-statistics-overview-card',
    imports: [DecimalPipe, TranslatePipe, FdUiCardComponent, FdUiIconComponent],
    templateUrl: './statistics-overview-card.html',
    styleUrl: './statistics-overview-card.scss',
    changeDetection: ChangeDetectionStrategy.OnPush,
})
export class StatisticsOverviewCardComponent {
    public readonly data = input.required<StatisticsOverviewData>();
    protected readonly ringCircumference = RING_CIRCUMFERENCE;

    protected readonly calorieProgress = computed(() => this.getProgress(this.data().averageCalories, this.data().calorieGoal));
    protected readonly ringDasharray = computed(() => {
        const progress = (this.calorieProgress() / PERCENT_MAX) * RING_CIRCUMFERENCE;
        return `${progress} ${RING_CIRCUMFERENCE}`;
    });

    protected getNutrientProgress(nutrient: StatisticsNutrientProgress): number {
        return this.getProgress(nutrient.current, nutrient.goal);
    }

    protected getNutrientLabelKey(key: StatisticsNutrientProgress['key']): string {
        return `STATISTICS.DASHBOARD.NUTRIENTS.${key.toUpperCase()}`;
    }

    protected getNutrientIcon(key: StatisticsNutrientProgress['key']): string {
        return key === 'protein' ? 'fitness_center' : key === 'fat' ? 'water_drop' : key === 'carbs' ? 'grass' : 'spa';
    }

    private getProgress(current: number, goal: number): number {
        return goal <= 0 ? 0 : Math.min(PERCENT_MAX, Math.max(0, Math.round((current / goal) * PERCENT_MAX)));
    }
}
