import { ChangeDetectionStrategy, Component, computed, input } from '@angular/core';
import { TranslatePipe } from '@ngx-translate/core';
import { FdUiCardComponent } from 'fd-ui-kit';

import { injectCurrentLanguage } from '../../../../shared/i18n/inject-current-language';
import { LocalizedNumberPipe } from '../../../../shared/i18n/localized-number.pipe';
import type { StatisticsOverviewData } from '../../models/statistics-dashboard-card.models';

@Component({
    selector: 'fd-statistics-nutrient-balance-card',
    imports: [LocalizedNumberPipe, TranslatePipe, FdUiCardComponent],
    templateUrl: './statistics-nutrient-balance-card.html',
    styleUrl: './statistics-nutrient-balance-card.scss',
    changeDetection: ChangeDetectionStrategy.OnPush,
})
export class StatisticsNutrientBalanceCardComponent {
    protected readonly locale = injectCurrentLanguage();
    public readonly data = input.required<StatisticsOverviewData>();
    protected readonly rows = computed(() => [
        {
            key: 'calories',
            labelKey: 'STATISTICS.NUTRITION_TABS.CALORIES',
            current: this.data().averageCalories,
            goal: this.data().calorieGoal,
            unitKey: 'GENERAL.UNITS.KCAL',
            fractionDigits: 0,
        },
        ...this.data().nutrients.map(nutrient => ({
            ...nutrient,
            labelKey: `STATISTICS.DASHBOARD.NUTRIENTS.${nutrient.key.toUpperCase()}`,
            unitKey: 'GENERAL.UNITS.G',
            fractionDigits: 1,
        })),
    ]);
}
