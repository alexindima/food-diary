import { ChangeDetectionStrategy, Component, inject } from '@angular/core';
import { TranslateModule } from '@ngx-translate/core';

import { PageBodyComponent } from '../../../../components/shared/page-body/page-body.component';
import { PageHeaderComponent } from '../../../../components/shared/page-header/page-header.component';
import { PeriodFilterComponent } from '../../../../components/shared/period-filter/period-filter.component';
import { FdPageContainerDirective } from '../../../../directives/layout/page-container.directive';
import { WaistHistoryChartCardComponent } from '../../components/waist-history-chart-card/waist-history-chart-card.component';
import { WaistHistoryEntriesCardComponent } from '../../components/waist-history-entries-card/waist-history-entries-card.component';
import { WaistHistoryFormCardComponent } from '../../components/waist-history-form-card/waist-history-form-card.component';
import { WaistHistoryGoalCardComponent } from '../../components/waist-history-goal-card/waist-history-goal-card.component';
import { WaistHistoryWhtCardComponent } from '../../components/waist-history-wht-card/waist-history-wht-card.component';
import { WaistHistoryFacade } from '../../lib/waist-history.facade';
import { WAIST_HISTORY_RANGE_TABS } from '../../lib/waist-history-page.config';
import type { WaistEntry } from '../../models/waist-entry.data';

@Component({
    selector: 'fd-waist-history-page',
    imports: [
        TranslateModule,
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
    templateUrl: './waist-history-page.component.html',
    styleUrls: ['./waist-history-page.component.scss'],
    changeDetection: ChangeDetectionStrategy.OnPush,
    providers: [WaistHistoryFacade],
})
export class WaistHistoryPageComponent {
    private readonly facade = inject(WaistHistoryFacade);

    public readonly selectedRange = this.facade.selectedRange;
    public readonly currentRange = this.facade.currentRange;
    public readonly isLoading = this.facade.isLoading;
    public readonly isSaving = this.facade.isSaving;
    public readonly isEditing = this.facade.isEditing;
    public readonly isSummaryLoading = this.facade.isSummaryLoading;
    public readonly customRangeControl = this.facade.customRangeControl;
    public readonly isDesiredWaistSaving = this.facade.isDesiredWaistSaving;
    public readonly desiredWaistControl = this.facade.desiredWaistControl;
    public readonly entriesDescending = this.facade.entriesDescending;
    public readonly chartData = this.facade.chartData;
    public readonly form = this.facade.form;
    public readonly whtViewModel = this.facade.whtViewModel;

    public readonly rangeTabs = WAIST_HISTORY_RANGE_TABS;

    public constructor() {
        this.facade.initialize();
    }

    public submit(): void {
        this.facade.submit();
    }

    public startEdit(entry: WaistEntry): void {
        this.facade.startEdit(entry);
    }

    public cancelEdit(): void {
        this.facade.cancelEdit();
    }

    public deleteEntry(entry: WaistEntry): void {
        this.facade.deleteEntry(entry);
    }

    public saveDesiredWaist(): void {
        this.facade.saveDesiredWaist();
    }

    public changeRange(value: string): void {
        this.facade.changeRange(value);
    }
}
