import { DecimalPipe } from '@angular/common';
import { ChangeDetectionStrategy, Component, input } from '@angular/core';
import { TranslatePipe } from '@ngx-translate/core';
import { FdUiCardComponent } from 'fd-ui-kit';

import type { StatisticsNutrientBalanceItem } from '../../models/statistics-dashboard-card.models';

const PERCENT_MAX = 100;

@Component({
    selector: 'fd-statistics-nutrient-balance-card',
    imports: [DecimalPipe, TranslatePipe, FdUiCardComponent],
    templateUrl: './statistics-nutrient-balance-card.html',
    styleUrl: './statistics-nutrient-balance-card.scss',
    changeDetection: ChangeDetectionStrategy.OnPush,
})
export class StatisticsNutrientBalanceCardComponent {
    public readonly items = input.required<readonly StatisticsNutrientBalanceItem[]>();

    protected getProgress(item: StatisticsNutrientBalanceItem): number {
        return item.goal <= 0 ? 0 : Math.min(PERCENT_MAX, Math.max(0, Math.round((item.current / item.goal) * PERCENT_MAX)));
    }

    protected getLabelKey(item: StatisticsNutrientBalanceItem): string {
        return `STATISTICS.DASHBOARD.NUTRIENTS.${item.key.toUpperCase()}`;
    }
}
