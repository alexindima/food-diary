import { ChangeDetectionStrategy, Component, computed, input } from '@angular/core';
import { TranslatePipe } from '@ngx-translate/core';
import { FdUiCardComponent, FdUiIconComponent, FdUiProgressRingComponent } from 'fd-ui-kit';

import { injectCurrentLanguage } from '../../../../shared/i18n/inject-current-language';
import { LocalizedNumberPipe } from '../../../../shared/i18n/localized-number.pipe';
import type { StatisticsNutrientProgress, StatisticsOverviewData } from '../../models/statistics-dashboard-card.models';

const PERCENT_MAX = 100;

const MIN_RECORDED_DAYS = 3;

@Component({
    selector: 'fd-statistics-overview-card',
    imports: [LocalizedNumberPipe, TranslatePipe, FdUiCardComponent, FdUiIconComponent, FdUiProgressRingComponent],
    templateUrl: './statistics-overview-card.html',
    styleUrl: './statistics-overview-card.scss',
    changeDetection: ChangeDetectionStrategy.OnPush,
})
export class StatisticsOverviewCardComponent {
    protected readonly locale = injectCurrentLanguage();
    protected readonly hasEnoughData = computed(
        () => this.data().trackedDays >= MIN_RECORDED_DAYS && this.data().trackedDays === this.data().periodDays,
    );

    public readonly data = input.required<StatisticsOverviewData>();
    protected readonly calorieProgress = computed(() => this.getProgress(this.data().averageCalories, this.data().calorieGoal));

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
