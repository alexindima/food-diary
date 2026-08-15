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
import { MeasurementUnitPipe, MeasurementValuePipe } from '../../../../shared/measurements/measurement-display.pipe';
import { MeasurementSystemService } from '../../../../shared/measurements/measurement-system.service';
import { ViewportService } from '../../../../shared/platform/viewport.service';
import { LocalizedTourDefinitionService } from '../../../../shared/tours/localized-tour-definition.service';
import { FdPageContainerDirective } from '../../../../shared/ui/layout/page-container.directive';
import { WaistHistoryChartCardComponent } from '../../components/waist-history-chart-card/waist-history-chart-card';
import { WaistHistoryEntriesCardComponent } from '../../components/waist-history-entries-card/waist-history-entries-card';
import { WaistHistoryGoalCardComponent } from '../../components/waist-history-goal-card/waist-history-goal-card';
import { WaistGoalHistoryDialogComponent } from '../../dialogs/waist-goal-history-dialog/waist-goal-history-dialog';
import {
    WaistHistoryEntriesDialogComponent,
    type WaistHistoryEntriesDialogData,
    type WaistHistoryEntriesDialogResult,
} from '../../dialogs/waist-history-entries-dialog/waist-history-entries-dialog';
import { WaistHistoryEntryDialogComponent } from '../../dialogs/waist-history-entry-dialog/waist-history-entry-dialog';
import { WaistHistoryGoalDialogComponent } from '../../dialogs/waist-history-goal-dialog/waist-history-goal-dialog';
import { WaistHistoryFacade } from '../../lib/waist-history.facade';
import { WAIST_HISTORY_RANGE_TABS } from '../../lib/waist-history-page.config';
import type { WaistEntry } from '../../models/waist-entry.data';
import { WAIST_HISTORY_TOUR } from './waist-history-tour';

@Component({
    selector: 'fd-waist-history-page',
    imports: [
        MeasurementUnitPipe,
        MeasurementValuePipe,
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
        WaistHistoryChartCardComponent,
        WaistHistoryEntriesCardComponent,
        WaistHistoryGoalCardComponent,
    ],
    templateUrl: './waist-history-page.html',
    styleUrls: ['./waist-history-page.scss'],
    changeDetection: ChangeDetectionStrategy.OnPush,
    providers: [WaistHistoryFacade],
})
export class WaistHistoryPageComponent {
    protected readonly measurements = inject(MeasurementSystemService);
    private readonly facade = inject(WaistHistoryFacade);
    private readonly tourService = inject(FdTourService);
    private readonly localizedTour = inject(LocalizedTourDefinitionService);
    private readonly dialogService = inject(FdUiDialogService);
    private readonly viewportService = inject(ViewportService);

    protected readonly selectedRange = this.facade.selectedRange;
    protected readonly currentRange = this.facade.currentRange;
    protected readonly isLoading = this.facade.isLoading;
    protected readonly isSummaryLoading = this.facade.isSummaryLoading;
    protected readonly customRangeForm = this.facade.customRangeForm;
    protected readonly desiredWaistCm = this.facade.desiredWaistCm;
    protected readonly waistGoal = this.facade.waistGoal;
    protected readonly latestWaist = this.facade.latestWaist;
    protected readonly latestWaistDate = this.facade.latestWaistDate;
    protected readonly hasCompletedWaistGoals = this.facade.hasCompletedWaistGoals;
    protected readonly lastCompletedWaistGoal = this.facade.lastCompletedWaistGoal;
    protected readonly entriesDescending = this.facade.entriesDescending;
    protected readonly chartPoints = this.facade.chartPoints;
    protected readonly whtViewModel = this.facade.whtViewModel;
    protected readonly isMobileView = this.viewportService.isMobile;

    protected readonly waistChange = computed<{ value: number; tone: 'positive' | 'negative' | 'neutral' } | null>(() => {
        const values = this.facade
            .rollingMonthSummaryPoints()
            .map(point => point.averageCircumferenceCm)
            .filter(value => value > 0);
        const first = values.at(0);
        const latest = values.at(-1);
        if (first === undefined || latest === undefined || values.length < 2) {
            return null;
        }

        const value = latest - first;
        const goal = this.desiredWaistCm();
        const tone =
            value === 0 || goal === null || latest === goal
                ? 'neutral'
                : Math.sign(value) === Math.sign(goal - latest)
                  ? 'positive'
                  : 'negative';
        return { value, tone };
    });

    protected readonly waistToGoal = computed(() => {
        const current = this.latestWaist();
        const goal = this.desiredWaistCm();
        const start = this.waistGoal().startWaistCm;
        if (current === null || goal === null || start === null) {
            return null;
        }

        const direction = Math.sign(goal - start);
        return direction === 0 ? 0 : Math.max(0, (goal - current) * direction);
    });

    protected readonly rangeTabs = WAIST_HISTORY_RANGE_TABS;

    public constructor() {
        this.facade.initialize();
    }

    protected startEdit(entry: WaistEntry): void {
        this.facade.startEdit(entry);
        this.openEntryDialog();
    }

    protected deleteEntry(entry: WaistEntry): void {
        this.facade.deleteEntry(entry);
    }

    protected openGoalDialog(): void {
        this.dialogService.open(WaistHistoryGoalDialogComponent, {
            preset: 'form',
            providers: [{ provide: WaistHistoryFacade, useValue: this.facade }],
        });
    }

    protected openGoalHistoryDialog(): void {
        this.dialogService.open(WaistGoalHistoryDialogComponent, {
            preset: 'form',
            providers: [{ provide: WaistHistoryFacade, useValue: this.facade }],
        });
    }

    protected openEntryDialog(): void {
        this.dialogService
            .open(WaistHistoryEntryDialogComponent, {
                preset: 'form',
                providers: [{ provide: WaistHistoryFacade, useValue: this.facade }],
            })
            .afterClosed()
            .subscribe(() => {
                if (this.facade.isEditing()) {
                    this.facade.cancelEdit();
                }
            });
    }

    protected openEntriesDialog(): void {
        this.dialogService
            .open<WaistHistoryEntriesDialogComponent, WaistHistoryEntriesDialogData, WaistHistoryEntriesDialogResult>(
                WaistHistoryEntriesDialogComponent,
                { data: { entries: this.entriesDescending(), desiredWaistCm: this.desiredWaistCm() }, preset: 'form' },
            )
            .afterClosed()
            .subscribe(result => {
                if (result?.action === 'edit') {
                    this.startEdit(result.entry);
                } else if (result?.action === 'remove') {
                    this.deleteEntry(result.entry);
                }
            });
    }

    protected changeRange(value: string): void {
        this.facade.changeRange(value);
    }

    protected startWaistHistoryTour(force = true): void {
        this.tourService.start(this.localizedTour.build(WAIST_HISTORY_TOUR), { force });
    }
}
