import { CommonModule } from '@angular/common';
import { ChangeDetectionStrategy, Component, inject } from '@angular/core';
import { ReactiveFormsModule } from '@angular/forms';
import { TranslatePipe } from '@ngx-translate/core';
import { FdUiEmptyStateComponent } from 'fd-ui-kit/empty-state/fd-ui-empty-state';

import { ErrorStateComponent } from '../../../components/shared/error-state/error-state';
import { PageBodyComponent } from '../../../components/shared/page-body/page-body';
import { PageHeaderComponent } from '../../../components/shared/page-header/page-header';
import { PeriodFilterComponent } from '../../../components/shared/period-filter/period-filter';
import { SkeletonCardComponent } from '../../../components/shared/skeleton-card/skeleton-card';
import { StatisticsBodyComponent } from '../../../components/shared/statistics-body/statistics-body';
import { StatisticsNutritionComponent } from '../../../components/shared/statistics-nutrition/statistics-nutrition';
import { StatisticsSummaryComponent } from '../../../components/shared/statistics-summary/statistics-summary';
import { FdPageContainerDirective } from '../../../directives/layout/page-container.directive';
import type { ExportFormat } from '../../meals/api/export.service';
import { StatisticsFacade } from '../lib/statistics.facade';
import { isBodyTab, isNutritionTab, isStatisticsRange } from '../lib/statistics-data-mapper';
import { STATISTICS_BODY_TABS, STATISTICS_NUTRITION_TABS, STATISTICS_RANGE_TABS } from '../lib/statistics-tabs.config';

@Component({
    selector: 'fd-statistics',
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
    templateUrl: './statistics.html',
    styleUrls: ['./statistics.scss'],
    changeDetection: ChangeDetectionStrategy.OnPush,
})
export class StatisticsComponent {
    protected readonly facade = inject(StatisticsFacade);

    public constructor() {
        this.facade.initialize();
    }

    protected readonly rangeTabs = STATISTICS_RANGE_TABS;
    protected readonly nutritionTabs = STATISTICS_NUTRITION_TABS;
    protected readonly bodyTabs = STATISTICS_BODY_TABS;

    protected readonly selectedRange = this.facade.selectedRange;
    protected readonly selectedNutritionTab = this.facade.selectedNutritionTab;
    protected readonly selectedBodyTab = this.facade.selectedBodyTab;
    protected readonly customRangeControl = this.facade.customRangeControl;
    protected readonly currentRange = this.facade.currentRange;
    protected readonly isLoading = this.facade.isLoading;
    protected readonly isBodyLoading = this.facade.isBodyLoading;
    protected readonly hasLoadError = this.facade.hasLoadError;
    protected readonly hasBodyLoadError = this.facade.hasBodyLoadError;
    protected readonly summaryMetrics = this.facade.summaryMetrics;
    protected readonly summarySparklinePoints = this.facade.summarySparklinePoints;
    protected readonly macroSparklinePoints = this.facade.macroSparklinePoints;
    protected readonly hasStatisticsData = this.facade.hasStatisticsData;
    protected readonly caloriesTrendPoints = this.facade.caloriesTrendPoints;
    protected readonly nutrientTrendGroups = this.facade.nutrientTrendGroups;
    protected readonly nutrientPieSegments = this.facade.nutrientPieSegments;
    protected readonly nutrientBarItems = this.facade.nutrientBarItems;
    protected readonly bodyChartPoints = this.facade.bodyChartPoints;
    protected readonly hasBodyData = this.facade.hasBodyData;
    protected readonly exportingFormat = this.facade.exportingFormat;

    protected changeRange(value: unknown): void {
        if (isStatisticsRange(value)) {
            this.facade.changeRange(value);
        }
    }

    protected changeNutritionTab(value: unknown): void {
        if (isNutritionTab(value)) {
            this.facade.changeNutritionTab(value);
        }
    }

    protected changeBodyTab(value: unknown): void {
        if (isBodyTab(value)) {
            this.facade.changeBodyTab(value);
        }
    }

    protected reload(): void {
        this.facade.reload();
    }

    protected exportDiary(format: ExportFormat): void {
        this.facade.exportDiary(format);
    }
}
