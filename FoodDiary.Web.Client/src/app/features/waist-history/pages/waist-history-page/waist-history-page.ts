import { ChangeDetectionStrategy, Component, inject } from '@angular/core';
import { TranslatePipe } from '@ngx-translate/core';

import { PageBodyComponent } from '../../../../components/shared/page-body/page-body';
import { PageHeaderComponent } from '../../../../components/shared/page-header/page-header';
import { PeriodFilterComponent } from '../../../../components/shared/period-filter/period-filter';
import { FdPageContainerDirective } from '../../../../shared/ui/layout/page-container.directive';
import { WaistHistoryChartCardComponent } from '../../components/waist-history-chart-card/waist-history-chart-card';
import { WaistHistoryEntriesCardComponent } from '../../components/waist-history-entries-card/waist-history-entries-card';
import { WaistHistoryFormCardComponent } from '../../components/waist-history-form-card/waist-history-form-card';
import { WaistHistoryGoalCardComponent } from '../../components/waist-history-goal-card/waist-history-goal-card';
import { WaistHistoryWhtCardComponent } from '../../components/waist-history-wht-card/waist-history-wht-card';
import { WaistHistoryFacade } from '../../lib/waist-history.facade';
import { WAIST_HISTORY_RANGE_TABS } from '../../lib/waist-history-page.config';
import type { WaistEntry } from '../../models/waist-entry.data';

@Component({
    selector: 'fd-waist-history-page',
    imports: [
        TranslatePipe,
        PageHeaderComponent,
        PageBodyComponent,
        FdPageContainerDirective,
        PeriodFilterComponent,
        WaistHistoryChartCardComponent,
        WaistHistoryEntriesCardComponent,
        WaistHistoryFormCardComponent,
        WaistHistoryGoalCardComponent,
        WaistHistoryWhtCardComponent,
    ],
    templateUrl: './waist-history-page.html',
    styleUrls: ['./waist-history-page.scss'],
    changeDetection: ChangeDetectionStrategy.OnPush,
    providers: [WaistHistoryFacade],
})
export class WaistHistoryPageComponent {
    private readonly facade = inject(WaistHistoryFacade);

    protected readonly selectedRange = this.facade.selectedRange;
    protected readonly currentRange = this.facade.currentRange;
    protected readonly isLoading = this.facade.isLoading;
    protected readonly isSaving = this.facade.isSaving;
    protected readonly entryError = this.facade.entryError;
    protected readonly isEditing = this.facade.isEditing;
    protected readonly isSummaryLoading = this.facade.isSummaryLoading;
    protected readonly customRangeForm = this.facade.customRangeForm;
    protected readonly isDesiredWaistSaving = this.facade.isDesiredWaistSaving;
    protected readonly desiredWaistForm = this.facade.desiredWaistForm;
    protected readonly entriesDescending = this.facade.entriesDescending;
    protected readonly chartPoints = this.facade.chartPoints;
    protected readonly form = this.facade.form;
    protected readonly whtViewModel = this.facade.whtViewModel;

    protected readonly rangeTabs = WAIST_HISTORY_RANGE_TABS;

    public constructor() {
        this.facade.initialize();
    }

    protected startEdit(entry: WaistEntry): void {
        this.facade.startEdit(entry);
    }

    protected cancelEdit(): void {
        this.facade.cancelEdit();
    }

    protected deleteEntry(entry: WaistEntry): void {
        this.facade.deleteEntry(entry);
    }

    protected saveDesiredWaist(): void {
        this.facade.saveDesiredWaist();
    }

    protected changeRange(value: string): void {
        this.facade.changeRange(value);
    }
}
