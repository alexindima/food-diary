import { CommonModule } from '@angular/common';
import { ChangeDetectionStrategy, Component, inject } from '@angular/core';
import { ReactiveFormsModule } from '@angular/forms';
import { TranslateModule } from '@ngx-translate/core';
import { ChartConfiguration } from 'chart.js';
import { FdUiButtonComponent } from 'fd-ui-kit/button/fd-ui-button.component';
import { FdUiCardComponent } from 'fd-ui-kit/card/fd-ui-card.component';
import { FdUiDateInputComponent } from 'fd-ui-kit/date-input/fd-ui-date-input.component';
import { FdUiInputComponent } from 'fd-ui-kit/input/fd-ui-input.component';
import { FdUiLoaderComponent } from 'fd-ui-kit/loader/fd-ui-loader.component';
import { FdUiTab } from 'fd-ui-kit/tabs/fd-ui-tabs.component';
import { BaseChartDirective } from 'ng2-charts';

import { PageBodyComponent } from '../../../components/shared/page-body/page-body.component';
import { PageHeaderComponent } from '../../../components/shared/page-header/page-header.component';
import { PeriodFilterComponent } from '../../../components/shared/period-filter/period-filter.component';
import { FdPageContainerDirective } from '../../../directives/layout/page-container.directive';
import { NavigationService } from '../../../services/navigation.service';
import { WeightHistoryFacade } from '../lib/weight-history.facade';
import { WeightEntry } from '../models/weight-entry.data';

@Component({
    selector: 'fd-weight-history-page',
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
    templateUrl: './weight-history-page.component.html',
    styleUrls: ['./weight-history-page.component.scss'],
    changeDetection: ChangeDetectionStrategy.OnPush,
    providers: [WeightHistoryFacade],
})
export class WeightHistoryPageComponent {
    private readonly navigationService = inject(NavigationService);
    private readonly facade = inject(WeightHistoryFacade);

    private readonly bmiScaleMax = 40;
    public readonly selectedRange = this.facade.selectedRange;
    public readonly currentRange = this.facade.currentRange;
    public readonly entries = this.facade.entries;
    public readonly isLoading = this.facade.isLoading;
    public readonly isSaving = this.facade.isSaving;
    public readonly isEditing = this.facade.isEditing;
    public readonly desiredWeight = this.facade.desiredWeight;
    public readonly isDesiredWeightSaving = this.facade.isDesiredWeightSaving;
    public readonly summaryPoints = this.facade.summaryPoints;
    public readonly isSummaryLoading = this.facade.isSummaryLoading;
    public readonly customRangeControl = this.facade.customRangeControl;
    public readonly entriesDescending = this.facade.entriesDescending;
    public readonly chartData = this.facade.chartData;
    public readonly form = this.facade.form;
    public readonly desiredWeightControl = this.facade.desiredWeightControl;
    public readonly latestWeight = this.facade.latestWeight;
    public readonly bmiValue = this.facade.bmiValue;
    public readonly bmiPointerPosition = this.facade.bmiPointerPosition;
    public readonly bmiStatusInfo = this.facade.bmiStatusInfo;

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
        { value: 'week', labelKey: 'WEIGHT_HISTORY.RANGE_WEEK' },
        { value: 'month', labelKey: 'WEIGHT_HISTORY.RANGE_MONTH' },
        { value: 'year', labelKey: 'WEIGHT_HISTORY.RANGE_YEAR' },
        { value: 'custom', labelKey: 'WEIGHT_HISTORY.RANGE_CUSTOM' },
    ];
    public readonly bmiSegments: BmiSegment[] = [
        { labelKey: 'WEIGHT_HISTORY.BMI_SEGMENTS.UNDER', from: 0, to: 18.5, class: 'weight-history-page__bmi-segment--under' },
        { labelKey: 'WEIGHT_HISTORY.BMI_SEGMENTS.NORMAL', from: 18.5, to: 25, class: 'weight-history-page__bmi-segment--normal' },
        { labelKey: 'WEIGHT_HISTORY.BMI_SEGMENTS.OVER', from: 25, to: 30, class: 'weight-history-page__bmi-segment--over' },
        { labelKey: 'WEIGHT_HISTORY.BMI_SEGMENTS.OBESE', from: 30, to: this.bmiScaleMax, class: 'weight-history-page__bmi-segment--obese' },
    ];

    public constructor() {
        this.facade.initialize();
    }

    public navigateBack(): void {
        void this.navigationService.navigateToHome();
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

    public getBmiSegmentWidth(segment: BmiSegment): string {
        const width = ((segment.to - segment.from) / this.bmiScaleMax) * 100;
        return `${width}%`;
    }
}

interface BmiSegment {
    labelKey: string;
    from: number;
    to: number;
    class: string;
}
