import { ChangeDetectionStrategy, Component, input } from '@angular/core';
import { TranslatePipe } from '@ngx-translate/core';
import { FdUiCardComponent } from 'fd-ui-kit';

import { DashboardWidgetFrameComponent } from '../../../../components/shared/dashboard-widget-frame/dashboard-widget-frame';
import { injectCurrentLanguage } from '../../../../shared/i18n/inject-current-language';
import { LocalizedNumberPipe } from '../../../../shared/i18n/localized-number.pipe';
import type { StatisticsMealStructureData, StatisticsMealStructureItem } from '../../models/statistics-dashboard-card.models';

@Component({
    selector: 'fd-statistics-meal-structure-card',
    imports: [DashboardWidgetFrameComponent, LocalizedNumberPipe, TranslatePipe, FdUiCardComponent],
    templateUrl: './statistics-meal-structure-card.html',
    styleUrl: './statistics-meal-structure-card.scss',
    changeDetection: ChangeDetectionStrategy.OnPush,
})
export class StatisticsMealStructureCardComponent {
    protected readonly locale = injectCurrentLanguage();
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
}
