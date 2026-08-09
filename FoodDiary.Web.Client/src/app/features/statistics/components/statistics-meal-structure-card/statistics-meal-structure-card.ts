import { DecimalPipe } from '@angular/common';
import { ChangeDetectionStrategy, Component, input } from '@angular/core';
import { TranslatePipe } from '@ngx-translate/core';
import { FdUiCardComponent } from 'fd-ui-kit';

import type { StatisticsMealStructureData, StatisticsMealStructureItem } from '../../models/statistics-dashboard-card.models';

@Component({
    selector: 'fd-statistics-meal-structure-card',
    imports: [DecimalPipe, TranslatePipe, FdUiCardComponent],
    templateUrl: './statistics-meal-structure-card.html',
    styleUrl: './statistics-meal-structure-card.scss',
    changeDetection: ChangeDetectionStrategy.OnPush,
})
export class StatisticsMealStructureCardComponent {
    public readonly data = input.required<StatisticsMealStructureData>();

    protected getLabelKey(item: StatisticsMealStructureItem): string {
        return `STATISTICS.DASHBOARD.MEAL_STRUCTURE.MEALS.${item.key.toUpperCase()}`;
    }

    protected getBarSegmentClass(item: StatisticsMealStructureItem): string {
        return `statistics-meal-structure-card__bar-segment statistics-meal-structure-card__bar-segment--${item.key}`;
    }

    protected getRowClass(item: StatisticsMealStructureItem): string {
        return `statistics-meal-structure-card__row statistics-meal-structure-card__row--${item.key}`;
    }

    protected getMealLabelKey(key: StatisticsMealStructureData['dominantMeal']): string {
        return key === null
            ? 'STATISTICS.DASHBOARD.MEAL_STRUCTURE.NO_DATA'
            : `STATISTICS.DASHBOARD.MEAL_STRUCTURE.MEALS.${key.toUpperCase()}`;
    }

    protected getDominantPercentage(): number {
        return this.data().items.find(item => item.key === this.data().dominantMeal)?.percentage ?? 0;
    }
}
