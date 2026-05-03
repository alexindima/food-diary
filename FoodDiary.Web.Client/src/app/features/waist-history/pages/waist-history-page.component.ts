import { CommonModule } from '@angular/common';
import { ChangeDetectionStrategy, Component, inject } from '@angular/core';
import { ReactiveFormsModule } from '@angular/forms';
import { TranslateModule } from '@ngx-translate/core';
import { type ChartConfiguration } from 'chart.js';
import { FdUiButtonComponent } from 'fd-ui-kit/button/fd-ui-button.component';
import { FdUiCardComponent } from 'fd-ui-kit/card/fd-ui-card.component';
import { FdUiDateInputComponent } from 'fd-ui-kit/date-input/fd-ui-date-input.component';
import { FdUiInputComponent } from 'fd-ui-kit/input/fd-ui-input.component';
import { FdUiLoaderComponent } from 'fd-ui-kit/loader/fd-ui-loader.component';
import { type FdUiTab } from 'fd-ui-kit/tabs/fd-ui-tabs.component';
import { BaseChartDirective } from 'ng2-charts';

import { PageBodyComponent } from '../../../components/shared/page-body/page-body.component';
import { PageHeaderComponent } from '../../../components/shared/page-header/page-header.component';
import { PeriodFilterComponent } from '../../../components/shared/period-filter/period-filter.component';
import { FdPageContainerDirective } from '../../../directives/layout/page-container.directive';
import { NavigationService } from '../../../services/navigation.service';
import { WaistHistoryFacade } from '../lib/waist-history.facade';
import { type WaistEntry } from '../models/waist-entry.data';

@Component({
    selector: 'fd-waist-history-page',
    standalone: true,
    imports: [
        CommonModule,
        TranslateModule,
        ReactiveFormsModule,
        BaseChartDirective,
        FdUiCardComponent,
        FdUiButtonComponent,
        FdUiDateInputComponent,
        FdUiInputComponent,
        FdUiLoaderComponent,
        PageHeaderComponent,
        PageBodyComponent,
        FdPageContainerDirective,
        PeriodFilterComponent,
    ],
    templateUrl: './waist-history-page.component.html',
    styleUrls: ['./waist-history-page.component.scss'],
    changeDetection: ChangeDetectionStrategy.OnPush,
    providers: [WaistHistoryFacade],
})
export class WaistHistoryPageComponent {
    private readonly navigationService = inject(NavigationService);
    private readonly facade = inject(WaistHistoryFacade);

    private readonly whtScaleMax = 0.8;

    public readonly selectedRange = this.facade.selectedRange;
    public readonly currentRange = this.facade.currentRange;
    public readonly entries = this.facade.entries;
    public readonly isLoading = this.facade.isLoading;
    public readonly isSaving = this.facade.isSaving;
    public readonly isEditing = this.facade.isEditing;
    public readonly summaryPoints = this.facade.summaryPoints;
    public readonly isSummaryLoading = this.facade.isSummaryLoading;
    public readonly customRangeControl = this.facade.customRangeControl;
    public readonly desiredWaist = this.facade.desiredWaist;
    public readonly isDesiredWaistSaving = this.facade.isDesiredWaistSaving;
    public readonly desiredWaistControl = this.facade.desiredWaistControl;
    public readonly entriesDescending = this.facade.entriesDescending;
    public readonly chartData = this.facade.chartData;
    public readonly form = this.facade.form;
    public readonly latestWaist = this.facade.latestWaist;
    public readonly whtrValue = this.facade.whtrValue;
    public readonly whtrPointerPosition = this.facade.whtrPointerPosition;
    public readonly whtrStatusInfo = this.facade.whtrStatusInfo;

    public readonly chartOptions: ChartConfiguration<'line'>['options'] = {
        responsive: true,
        scales: {
            x: {
                ticks: {
                    maxRotation: 0,
                    autoSkip: true,
                    maxTicksLimit: 6,
                },
            },
            y: {
                beginAtZero: true,
            },
        },
        plugins: {
            legend: {
                display: false,
            },
        },
    };

    public readonly rangeTabs: FdUiTab[] = [
        { value: 'week', labelKey: 'WAIST_HISTORY.RANGE_WEEK' },
        { value: 'month', labelKey: 'WAIST_HISTORY.RANGE_MONTH' },
        { value: 'year', labelKey: 'WAIST_HISTORY.RANGE_YEAR' },
        { value: 'custom', labelKey: 'WAIST_HISTORY.RANGE_CUSTOM' },
    ];

    public readonly whtSegments: WhtSegment[] = [
        { labelKey: 'WAIST_HISTORY.WHT_SEGMENTS.UNDER', from: 0, to: 0.4, class: 'waist-history-page__wht-segment--under' },
        { labelKey: 'WAIST_HISTORY.WHT_SEGMENTS.NORMAL', from: 0.4, to: 0.5, class: 'waist-history-page__wht-segment--normal' },
        { labelKey: 'WAIST_HISTORY.WHT_SEGMENTS.ELEVATED', from: 0.5, to: 0.6, class: 'waist-history-page__wht-segment--elevated' },
        { labelKey: 'WAIST_HISTORY.WHT_SEGMENTS.HIGH', from: 0.6, to: this.whtScaleMax, class: 'waist-history-page__wht-segment--high' },
    ];

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

    public getSegmentWidth(segment: WhtSegment): string {
        const width = ((segment.to - segment.from) / this.whtScaleMax) * 100;
        return `${width}%`;
    }
}

interface WhtSegment {
    labelKey: string;
    from: number;
    to: number;
    class: string;
}
