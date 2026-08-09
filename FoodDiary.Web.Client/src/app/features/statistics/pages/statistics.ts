import { CommonModule } from '@angular/common';
import { ChangeDetectionStrategy, Component, inject } from '@angular/core';
import { TranslatePipe } from '@ngx-translate/core';
import { FdTourService } from 'fd-tour';
import { FdUiHintDirective, FdUiIconComponent, FdUiMenuComponent, FdUiMenuItemComponent, FdUiMenuTriggerDirective } from 'fd-ui-kit';
import { FdUiButtonComponent } from 'fd-ui-kit/button/fd-ui-button';
import { FdUiEmptyStateComponent } from 'fd-ui-kit/empty-state/fd-ui-empty-state';

import { ErrorStateComponent } from '../../../components/shared/error-state/error-state';
import { PageBodyComponent } from '../../../components/shared/page-body/page-body';
import { PageHeaderComponent } from '../../../components/shared/page-header/page-header';
import { PeriodFilterComponent } from '../../../components/shared/period-filter/period-filter';
import { SkeletonCardComponent } from '../../../components/shared/skeleton-card/skeleton-card';
import type { ExportFormat } from '../../../shared/models/export.models';
import { LocalizedTourDefinitionService } from '../../../shared/tours/localized-tour-definition.service';
import { FdPageContainerDirective } from '../../../shared/ui/layout/page-container.directive';
import { StatisticsBodyTrendCardComponent } from '../components/statistics-body-trend-card/statistics-body-trend-card';
import { StatisticsDietStabilityCardComponent } from '../components/statistics-diet-stability-card/statistics-diet-stability-card';
import { StatisticsMealStructureCardComponent } from '../components/statistics-meal-structure-card/statistics-meal-structure-card';
import { StatisticsNutritionTrendCardComponent } from '../components/statistics-nutrition-trend-card/statistics-nutrition-trend-card';
import { StatisticsOverviewCardComponent } from '../components/statistics-overview-card/statistics-overview-card';
import { StatisticsFacade } from '../lib/statistics.facade';
import { isNutritionTab, isStatisticsRange } from '../lib/statistics-data-mapper';
import { STATISTICS_NUTRITION_TABS, STATISTICS_RANGE_TABS } from '../lib/statistics-tabs.config';
import { STATISTICS_TOUR } from './statistics-tour';

@Component({
    selector: 'fd-statistics',
    providers: [StatisticsFacade],
    imports: [
        CommonModule,
        TranslatePipe,
        FdUiHintDirective,
        FdUiIconComponent,
        FdUiMenuComponent,
        FdUiMenuItemComponent,
        FdUiMenuTriggerDirective,
        FdUiButtonComponent,
        FdUiEmptyStateComponent,
        PageHeaderComponent,
        PageBodyComponent,
        FdPageContainerDirective,
        PeriodFilterComponent,
        ErrorStateComponent,
        SkeletonCardComponent,
        StatisticsOverviewCardComponent,
        StatisticsNutritionTrendCardComponent,
        StatisticsMealStructureCardComponent,
        StatisticsBodyTrendCardComponent,
        StatisticsDietStabilityCardComponent,
    ],
    templateUrl: './statistics.html',
    styleUrls: ['./statistics.scss'],
    changeDetection: ChangeDetectionStrategy.OnPush,
})
export class StatisticsComponent {
    protected readonly facade = inject(StatisticsFacade);
    private readonly tourService = inject(FdTourService);
    private readonly localizedTour = inject(LocalizedTourDefinitionService);

    public constructor() {
        this.facade.initialize();
    }

    protected readonly rangeTabs = STATISTICS_RANGE_TABS;
    protected readonly nutritionTabs = STATISTICS_NUTRITION_TABS;

    protected readonly selectedRange = this.facade.selectedRange;
    protected readonly selectedNutritionTab = this.facade.selectedNutritionTab;
    protected readonly customRangeForm = this.facade.customRangeForm;
    protected readonly currentRange = this.facade.currentRange;
    protected readonly isLoading = this.facade.isLoading;
    protected readonly isBodyLoading = this.facade.isBodyLoading;
    protected readonly hasLoadError = this.facade.hasLoadError;
    protected readonly hasBodyLoadError = this.facade.hasBodyLoadError;
    protected readonly hasStatisticsData = this.facade.hasStatisticsData;
    protected readonly hasBodyData = this.facade.hasBodyData;
    protected readonly exportingFormat = this.facade.exportingFormat;
    protected readonly dashboardCardsView = this.facade.dashboardCardsView;

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

    protected reload(): void {
        this.facade.reload();
    }

    protected startStatisticsTour(force = true): void {
        this.tourService.start(this.localizedTour.build(STATISTICS_TOUR), { force });
    }

    protected exportDiary(format: ExportFormat): void {
        this.facade.exportDiary(format);
    }
}
