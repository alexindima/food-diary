import { DecimalPipe } from '@angular/common';
import { ChangeDetectionStrategy, Component, computed, inject } from '@angular/core';
import { TranslatePipe } from '@ngx-translate/core';
import { FdUiIconComponent } from 'fd-ui-kit';
import { FdUiAccentSurfaceComponent } from 'fd-ui-kit/accent-surface/fd-ui-accent-surface.component';
import { FdUiCardComponent } from 'fd-ui-kit/card/fd-ui-card.component';

import { PageBodyComponent } from '../../../components/shared/page-body/page-body.component';
import { PageHeaderComponent } from '../../../components/shared/page-header/page-header.component';
import { FdPageContainerDirective } from '../../../directives/layout/page-container.directive';
import { WeeklyCheckInFacade } from '../lib/weekly-check-in.facade';

@Component({
    selector: 'fd-weekly-check-in-page',
    standalone: true,
    imports: [
        DecimalPipe,
        TranslatePipe,
        FdUiIconComponent,
        PageHeaderComponent,
        PageBodyComponent,
        FdPageContainerDirective,
        FdUiCardComponent,
        FdUiAccentSurfaceComponent,
    ],
    templateUrl: './weekly-check-in-page.component.html',
    styleUrl: './weekly-check-in-page.component.scss',
    changeDetection: ChangeDetectionStrategy.OnPush,
    providers: [WeeklyCheckInFacade],
})
export class WeeklyCheckInPageComponent {
    private readonly facade = inject(WeeklyCheckInFacade);

    public readonly isLoading = this.facade.isLoading;
    public readonly thisWeek = this.facade.thisWeek;
    public readonly lastWeek = this.facade.lastWeek;
    public readonly suggestions = this.facade.suggestions;
    public readonly suggestionRows = computed(() =>
        this.suggestions().map(suggestion => ({
            key: suggestion,
            labelKey: `WEEKLY_CHECK_IN.${suggestion}`,
        })),
    );
    public readonly trendCards = computed<WeeklyCheckInTrendCardViewModel[]>(() => {
        const trends = this.facade.trends();

        if (!trends) {
            return [];
        }

        const cards: WeeklyCheckInTrendCardViewModel[] = [
            this.createTrendCard('calories', 'WEEKLY_CHECK_IN.CALORIES', trends.calorieChange, 'GENERAL.UNITS.KCAL', '1.0-0'),
            this.createTrendCard('protein', 'WEEKLY_CHECK_IN.PROTEIN', trends.proteinChange, 'GENERAL.UNITS.G', '1.1-1', false, ''),
            this.createTrendCard('hydration', 'WEEKLY_CHECK_IN.HYDRATION', trends.hydrationChange, 'GENERAL.UNITS.ML', '1.0-0'),
        ];

        if (trends.weightChange !== null) {
            cards.splice(
                2,
                0,
                this.createTrendCard('weight', 'WEEKLY_CHECK_IN.WEIGHT', trends.weightChange, 'GENERAL.UNITS.KG', '1.1-1', true),
            );
        }

        return cards;
    });

    protected readonly Math = Math;

    public constructor() {
        this.facade.initialize();
    }

    private createTrendCard(
        key: WeeklyCheckInTrendCardKey,
        labelKey: string,
        value: number,
        unitKey: string,
        numberFormat: string,
        invertPositive = false,
        unitSeparator = ' ',
    ): WeeklyCheckInTrendCardViewModel {
        return {
            key,
            labelKey,
            value,
            unitKey,
            unitSeparator,
            numberFormat,
            valuePrefix: value > 0 ? '+' : '',
            color: this.facade.getTrendColor(value, invertPositive),
            icon: this.facade.getTrendIcon(value),
        };
    }
}

type WeeklyCheckInTrendCardKey = 'calories' | 'protein' | 'weight' | 'hydration';

interface WeeklyCheckInTrendCardViewModel {
    key: WeeklyCheckInTrendCardKey;
    labelKey: string;
    value: number;
    unitKey: string;
    unitSeparator: string;
    numberFormat: string;
    valuePrefix: string;
    color: string;
    icon: string;
}
