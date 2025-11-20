import { CommonModule } from '@angular/common';
import {
    ChangeDetectionStrategy,
    Component,
    DestroyRef,
    OnInit,
    computed,
    inject,
    signal,
} from '@angular/core';
import { FormBuilder, FormControl, ReactiveFormsModule, Validators } from '@angular/forms';
import { TranslateModule, TranslateService } from '@ngx-translate/core';
import { BaseChartDirective } from 'ng2-charts';
import { ChartConfiguration } from 'chart.js';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { FdUiTabsComponent, FdUiTab } from 'fd-ui-kit/tabs/fd-ui-tabs.component';
import { FdUiCardComponent } from 'fd-ui-kit/card/fd-ui-card.component';
import { FdUiButtonComponent } from 'fd-ui-kit/button/fd-ui-button.component';
import { FdUiDateInputComponent } from 'fd-ui-kit/date-input/fd-ui-date-input.component';
import { FdUiDateRangeInputComponent } from 'fd-ui-kit/date-range-input/fd-ui-date-range-input.component';
import { FdUiInputComponent } from 'fd-ui-kit/input/fd-ui-input.component';
import { WaistEntriesService } from '../../services/waist-entries.service';
import { NavigationService } from '../../services/navigation.service';
import {
    CreateWaistEntryPayload,
    UpdateWaistEntryPayload,
    WaistEntry,
    WaistEntryFilters,
    WaistEntrySummaryFilters,
    WaistEntrySummaryPoint,
} from '../../types/waist-entry.data';
import { UserService } from '../../services/user.service';
import { PageHeaderComponent } from '../shared/page-header/page-header.component';

@Component({
    selector: 'fd-waist-history-page',
    standalone: true,
    imports: [
        CommonModule,
        TranslateModule,
        ReactiveFormsModule,
        BaseChartDirective,
        FdUiTabsComponent,
        FdUiCardComponent,
        FdUiButtonComponent,
        FdUiDateInputComponent,
        FdUiDateRangeInputComponent,
        FdUiInputComponent,
        PageHeaderComponent,
    ],
    templateUrl: './waist-history-page.component.html',
    styleUrls: ['./waist-history-page.component.scss'],
    changeDetection: ChangeDetectionStrategy.OnPush,
})
export class WaistHistoryPageComponent implements OnInit {
    private readonly waistEntriesService = inject(WaistEntriesService);
    private readonly userService = inject(UserService);
    private readonly navigationService = inject(NavigationService);
    private readonly destroyRef = inject(DestroyRef);
    private readonly translate = inject(TranslateService);
    private readonly fb = inject(FormBuilder);

    private readonly defaultRange: WaistHistoryRange = 'month';
    private readonly whtScaleMax = 0.8;
    private readonly pointerPaddingPercent = 1;

    public readonly selectedRange = signal<WaistHistoryRange>(this.defaultRange);
    public readonly entries = signal<WaistEntry[]>([]);
    public readonly isLoading = signal(false);
    public readonly isSaving = signal(false);
    public readonly isEditing = signal(false);
    public readonly summaryPoints = signal<WaistEntrySummaryPoint[]>([]);
    public readonly isSummaryLoading = signal(false);
    public readonly customRangeControl = new FormControl<{ start: Date | null; end: Date | null } | null>(null);
    public readonly desiredWaist = signal<number | null>(null);
    public readonly isDesiredWaistSaving = signal(false);
    public readonly desiredWaistControl = new FormControl<string>('');
    private readonly editingEntryId = signal<string | null>(null);
    private readonly userHeightCm = signal<number | null>(null);

    public readonly entriesDescending = computed(() =>
        [...this.entries()].sort((a, b) => new Date(b.date).getTime() - new Date(a.date).getTime()),
    );

    public readonly chartData = computed<ChartConfiguration<'line'>['data']>(() => {
        const ordered = [...this.summaryPoints()].sort(
            (a, b) => new Date(a.dateFrom).getTime() - new Date(b.dateFrom).getTime(),
        );

        const label = this.translate.instant('WAIST_HISTORY.CHART_LABEL');
        return {
            labels: ordered.map(point => this.formatDateLabel(point.dateFrom)),
            datasets: [
                {
                    data: ordered.map(point => (point.averageCircumference > 0 ? point.averageCircumference : null)),
                    label,
                    borderColor: '#0ea5e9',
                    backgroundColor: 'transparent',
                    fill: false,
                    tension: 0.35,
                    pointRadius: 4,
                    pointBackgroundColor: '#fff',
                    borderWidth: 2,
                    spanGaps: true,
                },
            ],
        };
    });

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

    public readonly form = this.fb.group({
        date: [new Date(), Validators.required],
        circumference: ['', [Validators.required, Validators.min(1), Validators.max(300)]],
    });

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

    public readonly latestWaist = computed<number | null>(() => {
        const entries = this.entriesDescending();
        return entries.length > 0 ? entries[0].circumference : null;
    });

    public readonly whtrValue = computed<number | null>(() => {
        const height = this.userHeightCm();
        const waist = this.latestWaist();
        if (!height || !waist || height <= 0) {
            return null;
        }
        const ratio = waist / height;
        return Math.round(ratio * 100) / 100;
    });

    public readonly whtrPointerPosition = computed(() => {
        const ratio = this.whtrValue();
        if (ratio === null) {
            return '0%';
        }
        const rawPercent = (ratio / this.whtScaleMax) * 100;
        const clamped = Math.max(
            this.pointerPaddingPercent,
            Math.min(100 - this.pointerPaddingPercent, rawPercent),
        );
        return `${clamped}%`;
    });

    public readonly whtrStatusInfo = computed<WhtStatusInfo | null>(() => {
        const ratio = this.whtrValue();
        if (ratio === null) {
            return null;
        }

        if (ratio < 0.4) {
            return {
                labelKey: 'WAIST_HISTORY.WHT_STATUS.UNDER',
                descriptionKey: 'WAIST_HISTORY.WHT_STATUS_DESC.UNDER',
                class: 'waist-history-page__wht-status--under',
            };
        }

        if (ratio < 0.5) {
            return {
                labelKey: 'WAIST_HISTORY.WHT_STATUS.NORMAL',
                descriptionKey: 'WAIST_HISTORY.WHT_STATUS_DESC.NORMAL',
                class: 'waist-history-page__wht-status--normal',
            };
        }

        if (ratio < 0.6) {
            return {
                labelKey: 'WAIST_HISTORY.WHT_STATUS.ELEVATED',
                descriptionKey: 'WAIST_HISTORY.WHT_STATUS_DESC.ELEVATED',
                class: 'waist-history-page__wht-status--elevated',
            };
        }

        return {
            labelKey: 'WAIST_HISTORY.WHT_STATUS.HIGH',
            descriptionKey: 'WAIST_HISTORY.WHT_STATUS_DESC.HIGH',
            class: 'waist-history-page__wht-status--high',
        };
    });

    public ngOnInit(): void {
        this.customRangeControl.valueChanges.pipe(takeUntilDestroyed(this.destroyRef)).subscribe(range => {
            if (this.selectedRange() === 'custom' && range?.start && range?.end) {
                this.loadEntries();
            }
        });

        this.loadEntries();
        this.loadUserProfile();
        this.loadDesiredWaist();
    }

    public submit(): void {
        if (this.form.invalid) {
            this.form.markAllAsTouched();
            return;
        }

        const payload = this.buildPayload();
        if (!payload) {
            return;
        }

        this.isSaving.set(true);
        const editingId = this.editingEntryId();
        const request$ = editingId
            ? this.waistEntriesService.update(editingId, payload)
            : this.waistEntriesService.create(payload);

        request$
            .pipe(takeUntilDestroyed(this.destroyRef))
            .subscribe({
                next: () => {
                    this.isSaving.set(false);
                    this.loadEntries(false);
                    if (editingId) {
                        this.resetEditingState();
                    } else {
                        this.form.controls.circumference.setValue(payload.circumference.toString());
                    }
                },
                error: () => {
                    this.isSaving.set(false);
                },
            });
    }

    public startEdit(entry: WaistEntry): void {
        this.isEditing.set(true);
        this.editingEntryId.set(entry.id);
        this.form.setValue({
            date: new Date(entry.date),
            circumference: entry.circumference.toString(),
        });
    }

    public cancelEdit(): void {
        this.resetEditingState();
        const latest = this.entriesDescending()[0];
        this.form.setValue({
            date: new Date(),
            circumference: (latest?.circumference ?? '').toString(),
        });
    }

    public deleteEntry(entry: WaistEntry): void {
        this.isSaving.set(true);
        this.waistEntriesService
            .remove(entry.id)
            .pipe(takeUntilDestroyed(this.destroyRef))
            .subscribe({
                next: () => {
                    this.isSaving.set(false);
                    this.loadEntries(false);
                    if (this.editingEntryId() === entry.id) {
                        this.resetEditingState();
                    }
                },
                error: () => {
                    this.isSaving.set(false);
                },
            });
    }

    public saveDesiredWaist(): void {
        if (this.desiredWaistControl.invalid) {
            return;
        }

        const rawValue = this.desiredWaistControl.value?.trim();
        const parsedValue = rawValue ? Number(rawValue.replace(',', '.')) : null;
        if (rawValue && (isNaN(parsedValue!) || parsedValue! <= 0 || parsedValue! > 400)) {
            this.desiredWaistControl.setErrors({ invalid: true });
            return;
        }

        this.isDesiredWaistSaving.set(true);
        this.userService
            .updateDesiredWaist(parsedValue)
            .pipe(takeUntilDestroyed(this.destroyRef))
            .subscribe({
                next: value => {
                    this.desiredWaist.set(value);
                    this.isDesiredWaistSaving.set(false);
                },
                error: () => {
                    this.isDesiredWaistSaving.set(false);
                },
            });
    }

    public changeRange(value: string): void {
        if (!isWaistHistoryRange(value) || value === this.selectedRange()) {
            return;
        }
        this.selectedRange.set(value);

        if (value === 'custom') {
            const current = this.customRangeControl.value;
            if (!current?.start || !current?.end) {
                const end = new Date();
                const start = new Date(end);
                start.setMonth(start.getMonth() - 1);
                this.customRangeControl.setValue({ start, end }, { emitEvent: false });
            }
        } else {
            this.customRangeControl.setValue(null, { emitEvent: false });
        }

        this.loadEntries();
    }

    public getSegmentWidth(segment: WhtSegment): string {
        const width = ((segment.to - segment.from) / this.whtScaleMax) * 100;
        return `${width}%`;
    }

    private loadEntries(initializeRange = true): void {
        this.isLoading.set(true);
        const { entriesParams, summaryParams } = this.buildFiltersForRange(this.selectedRange());

        if (initializeRange && this.selectedRange() === 'custom') {
            const start = new Date(summaryParams.dateFrom);
            const end = new Date(summaryParams.dateTo);
            this.customRangeControl.setValue({ start, end }, { emitEvent: false });
        }

        this.waistEntriesService
            .getEntries(entriesParams)
            .pipe(takeUntilDestroyed(this.destroyRef))
            .subscribe({
                next: entries => {
                    this.entries.set(entries);
                    this.isLoading.set(false);
                },
                error: () => {
                    this.isLoading.set(false);
                },
            });

        this.loadSummary(summaryParams);
    }

    private loadSummary(filters: WaistEntrySummaryFilters): void {
        this.isSummaryLoading.set(true);
        this.waistEntriesService
            .getSummary(filters)
            .pipe(takeUntilDestroyed(this.destroyRef))
            .subscribe({
                next: points => {
                    this.summaryPoints.set(points);
                    this.isSummaryLoading.set(false);
                },
                error: () => {
                    this.isSummaryLoading.set(false);
                },
            });
    }

    private loadUserProfile(): void {
        this.userService
            .getInfo()
            .pipe(takeUntilDestroyed(this.destroyRef))
            .subscribe(user => {
                this.userHeightCm.set(user?.height ?? null);
            });
    }

    private loadDesiredWaist(): void {
        this.userService
            .getDesiredWaist()
            .pipe(takeUntilDestroyed(this.destroyRef))
            .subscribe(value => {
                this.desiredWaist.set(value);
                this.desiredWaistControl.setValue(value?.toString() ?? '');
            });
    }

    private buildPayload(): CreateWaistEntryPayload | UpdateWaistEntryPayload | null {
        const rawDate = this.form.value.date;
        const rawCircumference = this.form.value.circumference;
        if (!rawDate || rawCircumference === null || rawCircumference === undefined) {
            return null;
        }

        const date = rawDate instanceof Date ? rawDate : new Date(rawDate);
        const utcDate = this.normalizeStartOfDay(date);
        const circumference = Number(rawCircumference);
        return {
            date: utcDate.toISOString(),
            circumference,
        };
    }

    private resetEditingState(): void {
        this.isEditing.set(false);
        this.editingEntryId.set(null);
        this.form.patchValue({
            date: new Date(),
        });
    }

    private buildFiltersForRange(range: WaistHistoryRange): {
        entriesParams: WaistEntryFilters;
        summaryParams: WaistEntrySummaryFilters;
    } {
        const now = new Date();
        let start = new Date(now);
        let end = new Date(now);

        if (range === 'week') {
            start.setDate(start.getDate() - 7);
        } else if (range === 'month') {
            start.setMonth(start.getMonth() - 1);
        } else if (range === 'year') {
            start.setFullYear(start.getFullYear() - 1);
        } else {
            const custom = this.customRangeControl.value;
            if (custom?.start) {
                start = new Date(custom.start);
            } else {
                start.setMonth(start.getMonth() - 1);
            }

            if (custom?.end) {
                end = new Date(custom.end);
            }
        }

        const normalizedStart = this.normalizeStartOfDay(start);
        const normalizedEnd = this.normalizeEndOfDay(end);
        const totalDays = Math.max(1, Math.ceil((normalizedEnd.getTime() - normalizedStart.getTime()) / MS_IN_DAY));
        const quantizationDays = this.getQuantizationDays(range, totalDays);
        const limit = Math.min(500, totalDays * 5);

        return {
            entriesParams: {
                dateFrom: normalizedStart.toISOString(),
                dateTo: normalizedEnd.toISOString(),
                sort: 'desc',
                limit,
            },
            summaryParams: {
                dateFrom: normalizedStart.toISOString(),
                dateTo: normalizedEnd.toISOString(),
                quantizationDays,
            },
        };
    }

    private getQuantizationDays(range: WaistHistoryRange, totalDays: number): number {
        if (range === 'week') {
            return 1;
        }
        if (range === 'month') {
            return 3;
        }
        if (range === 'year') {
            return 14;
        }

        return Math.max(1, Math.round(totalDays / 12));
    }

    private normalizeStartOfDay(date: Date): Date {
        return new Date(Date.UTC(date.getFullYear(), date.getMonth(), date.getDate()));
    }

    private normalizeEndOfDay(date: Date): Date {
        return new Date(Date.UTC(date.getFullYear(), date.getMonth(), date.getDate(), 23, 59, 59, 999));
    }

    private formatDateLabel(dateString: string): string {
        const date = this.normalizeUtcDate(dateString);
        return date.toLocaleDateString();
    }

    private normalizeUtcDate(dateString: string): Date {
        return new Date(dateString);
    }
}

const MS_IN_DAY = 24 * 60 * 60 * 1000;

type WaistHistoryRange = 'week' | 'month' | 'year' | 'custom';

interface WhtSegment {
    labelKey: string;
    from: number;
    to: number;
    class: string;
}

interface WhtStatusInfo {
    labelKey: string;
    descriptionKey: string;
    class: string;
}

function isWaistHistoryRange(value: string): value is WaistHistoryRange {
    return value === 'week' || value === 'month' || value === 'year' || value === 'custom';
}
