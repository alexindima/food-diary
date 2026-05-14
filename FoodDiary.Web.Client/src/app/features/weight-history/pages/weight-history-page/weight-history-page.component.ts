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
    standalone: true,
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

    public readonly selectedRange = this.facade.selectedRange;
    public readonly currentRange = this.facade.currentRange;
    public readonly entries = this.facade.entries;
    public readonly isLoading = this.facade.isLoading;
    public readonly isSaving = this.facade.isSaving;
    public readonly isEditing = this.facade.isEditing;
    public readonly desiredWeight = this.facade.desiredWeight;
    public readonly isDesiredWeightSaving = this.facade.isDesiredWeightSaving;
    public readonly isSummaryLoading = this.facade.isSummaryLoading;
    public readonly customRangeControl = this.facade.customRangeControl;
    public readonly entriesDescending = this.facade.entriesDescending;
    public readonly chartData = this.facade.chartData;
    public readonly form = this.facade.form;
    public readonly desiredWeightControl = this.facade.desiredWeightControl;
    public readonly bmiViewModel = this.facade.bmiViewModel;

    public readonly rangeTabs = WEIGHT_HISTORY_RANGE_TABS;

    public constructor() {
        this.facade.initialize();
    }

    public navigateBack(): void {
        void this.navigationService.navigateToHomeAsync();
    }

    public submit(): void {
        this.facade.submit();
    }

    public startEdit(entry: WeightEntry): void {
        this.facade.startEdit(entry);
    }

    public cancelEdit(): void {
        this.facade.cancelEdit();
    }

    public deleteEntry(entry: WeightEntry): void {
        this.facade.deleteEntry(entry);
    }

    public saveDesiredWeight(): void {
        this.facade.saveDesiredWeight();
    }

    public changeRange(value: string): void {
        this.facade.changeRange(value);
    }
}
