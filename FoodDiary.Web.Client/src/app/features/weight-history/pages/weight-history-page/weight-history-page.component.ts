import { ChangeDetectionStrategy, Component, inject } from '@angular/core';
import { TranslateModule } from '@ngx-translate/core';

import { PageBodyComponent } from '../../../../components/shared/page-body/page-body.component';
import { PageHeaderComponent } from '../../../../components/shared/page-header/page-header.component';
import { PeriodFilterComponent } from '../../../../components/shared/period-filter/period-filter.component';
import { FdPageContainerDirective } from '../../../../directives/layout/page-container.directive';
import { NavigationService } from '../../../../services/navigation.service';
import { WeightHistoryBmiCardComponent } from '../../components/weight-history-bmi-card/weight-history-bmi-card.component';
import { WeightHistoryChartCardComponent } from '../../components/weight-history-chart-card/weight-history-chart-card.component';
import { WeightHistoryEntriesCardComponent } from '../../components/weight-history-entries-card/weight-history-entries-card.component';
import { WeightHistoryFormCardComponent } from '../../components/weight-history-form-card/weight-history-form-card.component';
import { WeightHistoryGoalCardComponent } from '../../components/weight-history-goal-card/weight-history-goal-card.component';
import { WeightHistoryFacade } from '../../lib/weight-history.facade';
import { WEIGHT_HISTORY_RANGE_TABS } from '../../lib/weight-history-page.config';
import type { WeightEntry } from '../../models/weight-entry.data';

@Component({
    selector: 'fd-weight-history-page',
    imports: [
        TranslateModule,
        PageHeaderComponent,
        PageBodyComponent,
        FdPageContainerDirective,
        PeriodFilterComponent,
        WeightHistoryBmiCardComponent,
        WeightHistoryChartCardComponent,
        WeightHistoryEntriesCardComponent,
        WeightHistoryFormCardComponent,
        WeightHistoryGoalCardComponent,
    ],
    templateUrl: './weight-history-page.component.html',
    styleUrls: ['./weight-history-page.component.scss'],
    changeDetection: ChangeDetectionStrategy.OnPush,
    providers: [WeightHistoryFacade],
})
export class WeightHistoryPageComponent {
    private readonly navigationService = inject(NavigationService);
    private readonly facade = inject(WeightHistoryFacade);

    protected readonly selectedRange = this.facade.selectedRange;
    protected readonly currentRange = this.facade.currentRange;
    protected readonly entries = this.facade.entries;
    protected readonly isLoading = this.facade.isLoading;
    protected readonly isSaving = this.facade.isSaving;
    protected readonly isEditing = this.facade.isEditing;
    protected readonly desiredWeight = this.facade.desiredWeight;
    protected readonly isDesiredWeightSaving = this.facade.isDesiredWeightSaving;
    protected readonly isSummaryLoading = this.facade.isSummaryLoading;
    protected readonly customRangeControl = this.facade.customRangeControl;
    protected readonly entriesDescending = this.facade.entriesDescending;
    protected readonly chartPoints = this.facade.chartPoints;
    protected readonly form = this.facade.form;
    protected readonly desiredWeightControl = this.facade.desiredWeightControl;
    protected readonly bmiViewModel = this.facade.bmiViewModel;

    protected readonly rangeTabs = WEIGHT_HISTORY_RANGE_TABS;

    public constructor() {
        this.facade.initialize();
    }

    protected navigateBack(): void {
        void this.navigationService.navigateToHomeAsync();
    }

    protected submit(): void {
        this.facade.submit();
    }

    protected startEdit(entry: WeightEntry): void {
        this.facade.startEdit(entry);
    }

    protected cancelEdit(): void {
        this.facade.cancelEdit();
    }

    protected deleteEntry(entry: WeightEntry): void {
        this.facade.deleteEntry(entry);
    }

    protected saveDesiredWeight(): void {
        this.facade.saveDesiredWeight();
    }

    protected changeRange(value: string): void {
        this.facade.changeRange(value);
    }
}
