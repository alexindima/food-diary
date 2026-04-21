import { CommonModule } from '@angular/common';
import { ChangeDetectionStrategy, Component, inject } from '@angular/core';
import { ReactiveFormsModule } from '@angular/forms';
import { TranslatePipe, TranslateService } from '@ngx-translate/core';

import { FdUiTab } from 'fd-ui-kit/tabs/fd-ui-tabs.component';
import { FdUiEmptyStateComponent } from 'fd-ui-kit/empty-state/fd-ui-empty-state.component';
import { PageBodyComponent } from '../../../components/shared/page-body/page-body.component';
import { PageHeaderComponent } from '../../../components/shared/page-header/page-header.component';
import { PeriodFilterComponent } from '../../../components/shared/period-filter/period-filter.component';
import { ErrorStateComponent } from '../../../components/shared/error-state/error-state.component';
import { SkeletonCardComponent } from '../../../components/shared/skeleton-card/skeleton-card.component';
import { StatisticsBodyComponent } from '../../../components/shared/statistics-body/statistics-body.component';
import { StatisticsNutritionComponent } from '../../../components/shared/statistics-nutrition/statistics-nutrition.component';
import { StatisticsSummaryComponent } from '../../../components/shared/statistics-summary/statistics-summary.component';
import { FdPageContainerDirective } from '../../../directives/layout/page-container.directive';
import {
    createCaloriesLineChartOptions,
    createPieChartOptions,
    nutrientsLineChartOptions,
    radarChartOptions,
    barChartOptions,
    bodyChartOptions,
    summarySparklineOptions,
} from '../lib/statistics-chart-config';
import { isBodyTab, isNutritionTab, isStatisticsRange } from '../lib/statistics-data-mapper';
import { StatisticsFacade } from '../lib/statistics.facade';

@Component({
    selector: 'fd-statistics',
    standalone: true,
    providers: [StatisticsFacade],
    imports: [
        CommonModule,
        TranslatePipe,
        ReactiveFormsModule,
        FdUiEmptyStateComponent,
        PageHeaderComponent,
        PageBodyComponent,
        FdPageContainerDirective,
        PeriodFilterComponent,
        ErrorStateComponent,
        SkeletonCardComponent,
        StatisticsSummaryComponent,
        StatisticsNutritionComponent,
        StatisticsBodyComponent,
    ],
    templateUrl: './statistics.component.html',
    styleUrls: ['./statistics.component.scss'],
    changeDetection: ChangeDetectionStrategy.OnPush,
})
export class StatisticsComponent {
    private readonly translateService = inject(TranslateService);
    protected readonly facade = inject(StatisticsFacade);

    public constructor() {
        this.facade.initialize();
    }

    public readonly rangeTabs: FdUiTab[] = [
        { value: 'week', labelKey: 'STATISTICS.RANGES.WEEK' },
        { value: 'month', labelKey: 'STATISTICS.RANGES.MONTH' },
        { value: 'year', labelKey: 'STATISTICS.RANGES.YEAR' },
        { value: 'custom', labelKey: 'STATISTICS.RANGES.CUSTOM' },
    ];
    public readonly nutritionTabs: FdUiTab[] = [
        { value: 'calories', labelKey: 'STATISTICS.NUTRITION_TABS.CALORIES' },
        { value: 'macros', labelKey: 'STATISTICS.NUTRITION_TABS.MACROS' },
        { value: 'distribution', labelKey: 'STATISTICS.NUTRITION_TABS.DISTRIBUTION' },
    ];
    public readonly bodyTabs: FdUiTab[] = [
        { value: 'weight', labelKey: 'STATISTICS.BODY_TABS.WEIGHT' },
        { value: 'bmi', labelKey: 'STATISTICS.BODY_TABS.BMI' },
        { value: 'waist', labelKey: 'STATISTICS.BODY_TABS.WAIST' },
        { value: 'whtr', labelKey: 'STATISTICS.BODY_TABS.WHTR' },
    ];

    public readonly selectedRange = this.facade.selectedRange;
    public readonly selectedNutritionTab = this.facade.selectedNutritionTab;
    public readonly selectedBodyTab = this.facade.selectedBodyTab;
    public readonly customRangeControl = this.facade.customRangeControl;
    public readonly currentRange = this.facade.currentRange;
    public readonly isLoading = this.facade.isLoading;
    public readonly isBodyLoading = this.facade.isBodyLoading;
    public readonly hasLoadError = this.facade.hasLoadError;
    public readonly hasBodyLoadError = this.facade.hasBodyLoadError;
    public readonly summaryMetrics = this.facade.summaryMetrics;
    public readonly summarySparklineData = this.facade.summarySparklineData;
    public readonly macroSparklineData = this.facade.macroSparklineData;
    public readonly hasStatisticsData = this.facade.hasStatisticsData;
    public readonly caloriesLineChartData = this.facade.caloriesLineChartData;
    public readonly nutrientsLineChartData = this.facade.nutrientsLineChartData;
    public readonly nutrientsPieChartData = this.facade.nutrientsPieChartData;
    public readonly nutrientsRadarChartData = this.facade.nutrientsRadarChartData;
    public readonly nutrientsBarChartData = this.facade.nutrientsBarChartData;
    public readonly bodyChartData = this.facade.bodyChartData;
    public readonly hasBodyData = this.facade.hasBodyData;

    public readonly caloriesLineChartOptions = createCaloriesLineChartOptions(
        (label, value) => `${label}: ${parseFloat(value.toFixed(2))} ${this.translateService.instant('GENERAL.UNITS.KCAL')}`,
    );
    public readonly nutrientsLineChartOptions = nutrientsLineChartOptions;
    public readonly pieChartOptions = createPieChartOptions(
        (label, value) => `${label}: ${parseFloat(value.toFixed(2))} ${this.translateService.instant('GENERAL.UNITS.G')}`,
    );
    public readonly radarChartOptions = radarChartOptions;
    public readonly barChartOptions = barChartOptions;
    public readonly bodyChartOptions = bodyChartOptions;
    public readonly summarySparklineOptions = summarySparklineOptions;

    public changeRange(value: unknown): void {
        if (isStatisticsRange(value)) {
            this.facade.changeRange(value);
        }
    }

    public changeNutritionTab(value: unknown): void {
        if (isNutritionTab(value)) {
            this.facade.changeNutritionTab(value);
        }
    }

    public changeBodyTab(value: unknown): void {
        if (isBodyTab(value)) {
            this.facade.changeBodyTab(value);
        }
    }

    public reload(): void {
        this.facade.reload();
    }
}
