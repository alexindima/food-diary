import { CommonModule } from '@angular/common';
import { ChangeDetectionStrategy, Component, inject } from '@angular/core';
import { ReactiveFormsModule } from '@angular/forms';
import { TranslatePipe } from '@ngx-translate/core';
import { FdUiEmptyStateComponent } from 'fd-ui-kit/empty-state/fd-ui-empty-state.component';

import { ErrorStateComponent } from '../../../components/shared/error-state/error-state.component';
import { PageBodyComponent } from '../../../components/shared/page-body/page-body.component';
import { PageHeaderComponent } from '../../../components/shared/page-header/page-header.component';
import { PeriodFilterComponent } from '../../../components/shared/period-filter/period-filter.component';
import { SkeletonCardComponent } from '../../../components/shared/skeleton-card/skeleton-card.component';
import { StatisticsBodyComponent } from '../../../components/shared/statistics-body/statistics-body.component';
import { StatisticsNutritionComponent } from '../../../components/shared/statistics-nutrition/statistics-nutrition.component';
import { StatisticsSummaryComponent } from '../../../components/shared/statistics-summary/statistics-summary.component';
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
    templateUrl: './statistics.component.html',
    styleUrls: ['./statistics.component.scss'],
    changeDetection: ChangeDetectionStrategy.OnPush,
})
export class StatisticsComponent {
    protected readonly facade = inject(StatisticsFacade);

    public constructor() {
        this.facade.initialize();
    }

    public readonly rangeTabs = STATISTICS_RANGE_TABS;
    public readonly nutritionTabs = STATISTICS_NUTRITION_TABS;
    public readonly bodyTabs = STATISTICS_BODY_TABS;

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
    public readonly summarySparklinePoints = this.facade.summarySparklinePoints;
    public readonly macroSparklinePoints = this.facade.macroSparklinePoints;
    public readonly hasStatisticsData = this.facade.hasStatisticsData;
    public readonly caloriesTrendPoints = this.facade.caloriesTrendPoints;
    public readonly nutrientTrendGroups = this.facade.nutrientTrendGroups;
    public readonly nutrientPieSegments = this.facade.nutrientPieSegments;
    public readonly nutrientBarItems = this.facade.nutrientBarItems;
    public readonly bodyChartPoints = this.facade.bodyChartPoints;
    public readonly hasBodyData = this.facade.hasBodyData;
    public readonly exportingFormat = this.facade.exportingFormat;

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

    public exportDiary(format: ExportFormat): void {
        this.facade.exportDiary(format);
    }
}
