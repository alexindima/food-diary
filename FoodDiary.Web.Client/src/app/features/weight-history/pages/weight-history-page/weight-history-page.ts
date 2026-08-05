import { DecimalPipe } from '@angular/common';
import { ChangeDetectionStrategy, Component, computed, inject } from '@angular/core';
import { TranslatePipe } from '@ngx-translate/core';
import { FdTourService } from 'fd-tour';
import { FdUiCardComponent, FdUiHintDirective, FdUiIconComponent } from 'fd-ui-kit';
import { FdUiButtonComponent } from 'fd-ui-kit/button/fd-ui-button';
import { FdUiDialogService } from 'fd-ui-kit/dialog/fd-ui-dialog.service';

import { PageBodyComponent } from '../../../../components/shared/page-body/page-body';
import { PageHeaderComponent } from '../../../../components/shared/page-header/page-header';
import { PeriodFilterComponent } from '../../../../components/shared/period-filter/period-filter';
import { NavigationService } from '../../../../services/navigation.service';
import { ViewportService } from '../../../../shared/platform/viewport.service';
import { LocalizedTourDefinitionService } from '../../../../shared/tours/localized-tour-definition.service';
import { FdPageContainerDirective } from '../../../../shared/ui/layout/page-container.directive';
import { WeightHistoryChartCardComponent } from '../../components/weight-history-chart-card/weight-history-chart-card';
import { WeightHistoryEntriesCardComponent } from '../../components/weight-history-entries-card/weight-history-entries-card';
import { WeightHistoryGoalCardComponent } from '../../components/weight-history-goal-card/weight-history-goal-card';
import { WeightHistoryEntryDialogComponent } from '../../dialogs/weight-history-entry-dialog/weight-history-entry-dialog';
import { WeightHistoryFacade } from '../../lib/weight-history.facade';
import { WEIGHT_HISTORY_RANGE_TABS } from '../../lib/weight-history-page.config';
import type { WeightEntry } from '../../models/weight-entry.data';
import { WEIGHT_HISTORY_TOUR } from './weight-history-tour';

@Component({
    selector: 'fd-weight-history-page',
    imports: [
        TranslatePipe,
        DecimalPipe,
        FdUiHintDirective,
        FdUiCardComponent,
        FdUiIconComponent,
        FdUiButtonComponent,
        PageHeaderComponent,
        PageBodyComponent,
        FdPageContainerDirective,
        PeriodFilterComponent,
        WeightHistoryChartCardComponent,
        WeightHistoryEntriesCardComponent,
        WeightHistoryGoalCardComponent,
    ],
    templateUrl: './weight-history-page.html',
    styleUrls: ['./weight-history-page.scss'],
    changeDetection: ChangeDetectionStrategy.OnPush,
    providers: [WeightHistoryFacade],
})
export class WeightHistoryPageComponent {
    private readonly navigationService = inject(NavigationService);
    private readonly facade = inject(WeightHistoryFacade);
    private readonly tourService = inject(FdTourService);
    private readonly localizedTour = inject(LocalizedTourDefinitionService);
    private readonly viewportService = inject(ViewportService);
    private readonly dialogService = inject(FdUiDialogService);

    protected readonly selectedRange = this.facade.selectedRange;
    protected readonly currentRange = this.facade.currentRange;
    protected readonly entries = this.facade.entries;
    protected readonly isLoading = this.facade.isLoading;
    protected readonly desiredWeight = this.facade.desiredWeight;
    protected readonly isDesiredWeightSaving = this.facade.isDesiredWeightSaving;
    protected readonly isSummaryLoading = this.facade.isSummaryLoading;
    protected readonly customRangeForm = this.facade.customRangeForm;
    protected readonly entriesDescending = this.facade.entriesDescending;
    protected readonly chartPoints = this.facade.chartPoints;
    protected readonly desiredWeightForm = this.facade.desiredWeightForm;
    protected readonly bmiViewModel = this.facade.bmiViewModel;
    protected readonly latestWeight = this.facade.latestWeight;
    protected readonly isMobileView = this.viewportService.isMobile;

    protected readonly weightChange = computed<{ value: number; isDecrease: boolean; isIncrease: boolean } | null>(() => {
        const values = this.chartPoints()
            .map(point => point.value)
            .filter((value): value is number => value !== null);

        const latestValue = values.at(-1);
        const firstValue = values.at(0);
        if (latestValue === undefined || firstValue === undefined || values.length < 2) {
            return null;
        }

        const value = latestValue - firstValue;
        return { value, isDecrease: value <= 0, isIncrease: value > 0 };
    });

    protected readonly weightToGoal = computed<{ value: number } | null>(() => {
        const latestWeight = this.latestWeight();
        const desiredWeight = this.desiredWeight();

        return latestWeight === null || desiredWeight === null ? null : { value: Math.max(0, latestWeight - desiredWeight) };
    });

    protected readonly rangeTabs = WEIGHT_HISTORY_RANGE_TABS;

    public constructor() {
        this.facade.initialize();
    }

    protected navigateBack(): void {
        void this.navigationService.navigateToHomeAsync();
    }

    protected startEdit(entry: WeightEntry): void {
        this.facade.startEdit(entry);
        this.openEntryDialog();
    }

    protected deleteEntry(entry: WeightEntry): void {
        this.facade.deleteEntry(entry);
    }

    protected saveDesiredWeight(): void {
        this.facade.saveDesiredWeight();
    }

    protected openEntryDialog(): void {
        this.dialogService
            .open(WeightHistoryEntryDialogComponent, {
                preset: 'form',
                providers: [{ provide: WeightHistoryFacade, useValue: this.facade }],
            })
            .afterClosed()
            .subscribe(() => {
                if (this.facade.isEditing()) {
                    this.facade.cancelEdit();
                }
            });
    }

    protected changeRange(value: string): void {
        this.facade.changeRange(value);
    }

    protected startWeightHistoryTour(force = true): void {
        this.tourService.start(this.localizedTour.build(WEIGHT_HISTORY_TOUR), { force });
    }
}
