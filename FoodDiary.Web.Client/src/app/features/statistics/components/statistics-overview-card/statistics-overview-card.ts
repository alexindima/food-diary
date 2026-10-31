import { ChangeDetectionStrategy, Component, input } from '@angular/core';
import { TranslatePipe } from '@ngx-translate/core';
import { FdUiCardComponent } from 'fd-ui-kit';

import { injectCurrentLanguage } from '../../../../shared/i18n/inject-current-language';
import { LocalizedNumberPipe } from '../../../../shared/i18n/localized-number.pipe';
import type { StatisticsNutrientProgress, StatisticsOverviewData } from '../../models/statistics-dashboard-card.models';

@Component({
    selector: 'fd-statistics-overview-card',
    imports: [LocalizedNumberPipe, TranslatePipe, FdUiCardComponent],
    templateUrl: './statistics-overview-card.html',
    styleUrl: './statistics-overview-card.scss',
    changeDetection: ChangeDetectionStrategy.OnPush,
})
export class StatisticsOverviewCardComponent {
    protected readonly locale = injectCurrentLanguage();
    public readonly data = input.required<StatisticsOverviewData>();

    protected getNutrientLabelKey(key: StatisticsNutrientProgress['key']): string {
        return `STATISTICS.DASHBOARD.NUTRIENTS.${key.toUpperCase()}`;
    }
}
