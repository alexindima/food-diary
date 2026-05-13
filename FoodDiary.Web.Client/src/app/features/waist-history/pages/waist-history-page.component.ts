import { ChangeDetectionStrategy, Component, computed, inject } from '@angular/core';
import { TranslateModule, TranslateService } from '@ngx-translate/core';
import type { ChartConfiguration } from 'chart.js';
import type { FdUiTab } from 'fd-ui-kit/tabs/fd-ui-tabs.component';

import { PageBodyComponent } from '../../../components/shared/page-body/page-body.component';
import { PageHeaderComponent } from '../../../components/shared/page-header/page-header.component';
import { PeriodFilterComponent } from '../../../components/shared/period-filter/period-filter.component';
import { FdPageContainerDirective } from '../../../directives/layout/page-container.directive';
import { resolveAppLocale } from '../../../shared/lib/locale.constants';
import { PERCENT_MULTIPLIER } from '../../../shared/lib/nutrition.constants';
import { WaistHistoryFacade } from '../lib/waist-history.facade';
import type { WaistEntry } from '../models/waist-entry.data';
import { WaistHistoryChartCardComponent } from './waist-history-chart-card.component';
import { WaistHistoryEntriesCardComponent } from './waist-history-entries-card.component';
import { WaistHistoryFormCardComponent } from './waist-history-form-card.component';
import { WaistHistoryGoalCardComponent } from './waist-history-goal-card.component';
import type { WaistEntryViewModel, WhtSegment } from './waist-history-page.types';
import { WaistHistoryWhtCardComponent } from './waist-history-wht-card.component';

const WHT_SCALE_MAX = 0.8;
const WHT_UNDER_MAX = 0.4;
const WHT_NORMAL_MAX = 0.5;
const WHT_ELEVATED_MAX = 0.6;

@Component({
    selector: 'fd-waist-history-page',
    standalone: true,
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
    private readonly translateService = inject(TranslateService);

    private readonly whtScaleMax = WHT_SCALE_MAX;

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
    public readonly entryItems = computed<WaistEntryViewModel[]>(() =>
        this.entriesDescending().map(entry => ({
            entry,
            dateLabel: this.formatNumericDate(entry.date),
        })),
    );
    public readonly chartData = this.facade.chartData;
    public readonly form = this.facade.form;
    public readonly latestWaist = this.facade.latestWaist;
    public readonly whtrValue = this.facade.whtrValue;
    public readonly whtrPointerPosition = this.facade.whtrPointerPosition;
    public readonly whtrStatusInfo = this.facade.whtrStatusInfo;
    public readonly hasSummaryPoints = computed(() => this.summaryPoints().length > 0);

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
        this.createWhtSegment('WAIST_HISTORY.WHT_SEGMENTS.UNDER', 0, WHT_UNDER_MAX, 'waist-history-page__wht-segment--under'),
        this.createWhtSegment(
            'WAIST_HISTORY.WHT_SEGMENTS.NORMAL',
            WHT_UNDER_MAX,
            WHT_NORMAL_MAX,
            'waist-history-page__wht-segment--normal',
        ),
        this.createWhtSegment(
            'WAIST_HISTORY.WHT_SEGMENTS.ELEVATED',
            WHT_NORMAL_MAX,
            WHT_ELEVATED_MAX,
            'waist-history-page__wht-segment--elevated',
        ),
        this.createWhtSegment(
            'WAIST_HISTORY.WHT_SEGMENTS.HIGH',
            WHT_ELEVATED_MAX,
            this.whtScaleMax,
            'waist-history-page__wht-segment--high',
        ),
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

    private createWhtSegment(labelKey: string, from: number, to: number, className: string): WhtSegment {
        return {
            labelKey,
            width: `${((to - from) / this.whtScaleMax) * PERCENT_MULTIPLIER}%`,
            class: className,
        };
    }

    private formatNumericDate(value: string): string {
        const date = new Date(value);
        if (Number.isNaN(date.getTime())) {
            return value;
        }

        return new Intl.DateTimeFormat(resolveAppLocale(this.translateService.getCurrentLang()), {
            day: '2-digit',
            month: '2-digit',
            year: 'numeric',
        }).format(date);
    }
}
