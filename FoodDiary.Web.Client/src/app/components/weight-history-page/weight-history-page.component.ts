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
import { TranslateModule, TranslateService } from '@ngx-translate/core';
import { FormBuilder, FormControl, ReactiveFormsModule, Validators } from '@angular/forms';
import { BaseChartDirective } from 'ng2-charts';
import { ChartConfiguration } from 'chart.js';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { WeightEntriesService } from '../../services/weight-entries.service';
import { WeightEntry, WeightEntryFilters, WeightEntrySummaryFilters, WeightEntrySummaryPoint } from '../../types/weight-entry.data';
import { FdUiCardComponent } from 'fd-ui-kit/card/fd-ui-card.component';
import { FdUiButtonComponent } from 'fd-ui-kit/button/fd-ui-button.component';
import { FdUiDateInputComponent } from 'fd-ui-kit/date-input/fd-ui-date-input.component';
import { FdUiInputComponent } from 'fd-ui-kit/input/fd-ui-input.component';
import { FdUiTabsComponent, FdUiTab } from 'fd-ui-kit/tabs/fd-ui-tabs.component';
import { UserService } from '../../services/user.service';
import { NavigationService } from '../../services/navigation.service';

@Component({
    selector: 'fd-weight-history-page',
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
        FdUiInputComponent,
    ],
    templateUrl: './weight-history-page.component.html',
    styleUrls: ['./weight-history-page.component.scss'],
    changeDetection: ChangeDetectionStrategy.OnPush,
})
export class WeightHistoryPageComponent implements OnInit {
    private readonly weightEntriesService = inject(WeightEntriesService);
    private readonly userService = inject(UserService);
    private readonly navigationService = inject(NavigationService);
    private readonly destroyRef = inject(DestroyRef);
    private readonly translate = inject(TranslateService);
    private readonly fb = inject(FormBuilder);

    private readonly userHeightCm = signal<number | null>(null);
    private readonly bmiScaleMax = 40;

    private readonly defaultRange: WeightHistoryRange = 'month';
    public readonly selectedRange = signal<WeightHistoryRange>(this.defaultRange);

    public readonly entries = signal<WeightEntry[]>([]);
    public readonly isLoading = signal<boolean>(false);
    public readonly isSaving = signal<boolean>(false);
    public readonly isEditing = signal<boolean>(false);
    public readonly desiredWeight = signal<number | null>(null);
    public readonly isDesiredWeightSaving = signal<boolean>(false);
    public readonly summaryPoints = signal<WeightEntrySummaryPoint[]>([]);
    public readonly isSummaryLoading = signal<boolean>(false);
    public readonly customRangeControl = new FormControl<{ start: Date | null; end: Date | null } | null>(null);
    private readonly editingEntryId = signal<string | null>(null);

    public readonly entriesDescending = computed(() =>
        [...this.entries()].sort(
            (a, b) => new Date(b.date).getTime() - new Date(a.date).getTime(),
        ),
    );

    public readonly chartData = computed<ChartConfiguration<'line'>['data']>(() => {
        const ordered = [...this.summaryPoints()].sort(
            (a, b) => new Date(a.dateFrom).getTime() - new Date(b.dateFrom).getTime(),
        );

        const label = this.translate.instant('WEIGHT_HISTORY.CHART_LABEL');
        return {
            labels: ordered.map(point =>
                this.formatDateLabel(point.dateFrom),
            ),
            datasets: [
                {
                    data: ordered.map(point =>
                        point.averageWeight > 0 ? point.averageWeight : null,
                    ),
                    label,
                    borderColor: '#2563eb',
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
        weight: ['', [Validators.required, Validators.min(1), Validators.max(500)]],
    });

    public readonly desiredWeightControl = new FormControl<string>('');
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

    public readonly latestWeight = computed<number | null>(() => {
        const entries = this.entriesDescending();
        if (entries.length > 0) {
            return entries[0].weight;
        }
        return null;
    });

    public readonly bmiValue = computed<number | null>(() => {
        const heightCm = this.userHeightCm();
        const latest = this.latestWeight();
        if (!heightCm || !latest) {
            return null;
        }
        const heightMeters = heightCm / 100;
        if (!heightMeters) {
            return null;
        }
        const bmi = latest / (heightMeters * heightMeters);
        return Math.round(bmi * 10) / 10;
    });

    private readonly bmiPointerPaddingPercent = 1;

    public readonly bmiPointerPosition = computed(() => {
        const bmi = this.bmiValue();
        if (bmi === null) {
            return '0%';
        }
        const rawPercent = (bmi / this.bmiScaleMax) * 100;
        const percent = Math.max(this.bmiPointerPaddingPercent, Math.min(100 - this.bmiPointerPaddingPercent, rawPercent));
        return `${percent}%`;
    });

    public readonly bmiStatusInfo = computed<BmiStatusInfo | null>(() => {
        const bmi = this.bmiValue();
        if (bmi === null) {
            return null;
        }

        if (bmi < 18.5) {
            return {
                labelKey: 'WEIGHT_HISTORY.BMI_STATUS.UNDERWEIGHT',
                descriptionKey: 'WEIGHT_HISTORY.BMI_STATUS_DESC.UNDERWEIGHT',
                class: 'weight-history-page__bmi-status--under',
            };
        }
        if (bmi < 25) {
            return {
                labelKey: 'WEIGHT_HISTORY.BMI_STATUS.NORMAL',
                descriptionKey: 'WEIGHT_HISTORY.BMI_STATUS_DESC.NORMAL',
                class: 'weight-history-page__bmi-status--normal',
            };
        }
        if (bmi < 30) {
            return {
                labelKey: 'WEIGHT_HISTORY.BMI_STATUS.OVER',
                descriptionKey: 'WEIGHT_HISTORY.BMI_STATUS_DESC.OVER',
                class: 'weight-history-page__bmi-status--over',
            };
        }
        return {
            labelKey: 'WEIGHT_HISTORY.BMI_STATUS.OBESE',
            descriptionKey: 'WEIGHT_HISTORY.BMI_STATUS_DESC.OBESE',
            class: 'weight-history-page__bmi-status--obese',
        };
    });

    public ngOnInit(): void {
        this.customRangeControl.valueChanges
            .pipe(takeUntilDestroyed(this.destroyRef))
            .subscribe(range => {
                if (this.selectedRange() === 'custom' && range?.start && range?.end) {
                    this.loadEntries();
                }
            });

        this.loadEntries();
        this.loadDesiredWeight();
        this.loadUserProfile();
    }

    public navigateBack(): void {
        void this.navigationService.navigateToHome();
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
            ? this.weightEntriesService.update(editingId, payload)
            : this.weightEntriesService.create(payload);

        request$
            .pipe(takeUntilDestroyed(this.destroyRef))
            .subscribe({
                next: () => {
                    this.isSaving.set(false);
                    this.loadEntries(false);
                    if (editingId) {
                        this.resetEditingState();
                    } else {
                        this.form.controls.weight.setValue(payload.weight.toString());
                    }
                },
                error: () => {
                    this.isSaving.set(false);
                },
            });
    }

    public startEdit(entry: WeightEntry): void {
        this.isEditing.set(true);
        this.editingEntryId.set(entry.id);
        this.form.setValue({
            date: new Date(entry.date),
            weight: entry.weight.toString(),
        });
    }

    public cancelEdit(): void {
        this.resetEditingState();
        const latest = this.entriesDescending()[0];
        this.form.setValue({
            date: new Date(),
            weight: (latest?.weight ?? '').toString(),
        });
    }

    public deleteEntry(entry: WeightEntry): void {
        this.isSaving.set(true);
        this.weightEntriesService
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

    public saveDesiredWeight(): void {
        if (this.desiredWeightControl.invalid) {
            return;
        }

        const rawValue = this.desiredWeightControl.value?.trim();
        const parsedValue = rawValue ? Number(rawValue.replace(',', '.')) : null;
        if (rawValue && (isNaN(parsedValue!) || parsedValue! <= 0 || parsedValue! > 500)) {
            this.desiredWeightControl.setErrors({ invalid: true });
            return;
        }

        this.isDesiredWeightSaving.set(true);
        this.userService
            .updateDesiredWeight(parsedValue)
            .pipe(takeUntilDestroyed(this.destroyRef))
            .subscribe({
                next: value => {
                    this.desiredWeight.set(value);
                    this.isDesiredWeightSaving.set(false);
                },
                error: () => {
                    this.isDesiredWeightSaving.set(false);
                },
            });
    }

    public changeRange(value: string): void {
        if (!isWeightHistoryRange(value) || value === this.selectedRange()) {
            return;
        }
        this.selectedRange.set(value);

        if (value === 'custom') {
            const current = this.customRangeControl.value;
            if (!current?.start || !current?.end) {
                const end = new Date();
                const start = new Date(end);
                start.setMonth(start.getMonth() - 1);
                this.customRangeControl.setValue(
                    {
                        start,
                        end,
                    },
                    { emitEvent: false },
                );
            }
        } else {
            this.customRangeControl.setValue(null, { emitEvent: false });
        }

        this.loadEntries();
    }

    private loadEntries(showLoader = true): void {
        if (showLoader) {
            this.isLoading.set(true);
        }

        const { entriesParams, summaryParams } = this.buildFiltersForRange(this.selectedRange());

        this.weightEntriesService
            .getEntries(entriesParams)
            .pipe(takeUntilDestroyed(this.destroyRef))
            .subscribe({
                next: entries => {
                    this.entries.set(entries);
                    this.isLoading.set(false);
                    if (!this.isEditing() && entries.length > 0) {
                        const latest = [...entries].sort(
                            (a, b) => new Date(b.date).getTime() - new Date(a.date).getTime(),
                        )[0];
                        this.form.patchValue({
                            weight: latest.weight.toString(),
                        });
                    }
                },
                error: () => {
                    this.isLoading.set(false);
                },
            });

        this.loadSummary(summaryParams);
    }

    private loadDesiredWeight(): void {
        this.userService
            .getDesiredWeight()
            .pipe(takeUntilDestroyed(this.destroyRef))
            .subscribe(value => {
                this.desiredWeight.set(value);
                this.desiredWeightControl.setValue(value?.toString() ?? '');
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

    public getBmiSegmentWidth(segment: BmiSegment): string {
        const width = ((segment.to - segment.from) / this.bmiScaleMax) * 100;
        return `${width}%`;
    }

    private loadSummary(filters: WeightEntrySummaryFilters): void {
        this.isSummaryLoading.set(true);
        this.weightEntriesService
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

    private buildPayload() {
        const rawDate = this.form.value.date;
        const rawWeight = this.form.value.weight;
        if (!rawDate || rawWeight === null || rawWeight === undefined) {
            return null;
        }

        const date = rawDate instanceof Date ? rawDate : new Date(rawDate);
        const utcDate = this.normalizeStartOfDay(date);
        const weight = Number(rawWeight);
        return {
            date: utcDate.toISOString(),
            weight,
        };
    }

    private resetEditingState(): void {
        this.isEditing.set(false);
        this.editingEntryId.set(null);
        this.form.patchValue({
            date: new Date(),
        });
    }

    private buildFiltersForRange(range: WeightHistoryRange): {
        entriesParams: WeightEntryFilters;
        summaryParams: WeightEntrySummaryFilters;
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

    private normalizeStartOfDay(date: Date): Date {
        return new Date(Date.UTC(date.getFullYear(), date.getMonth(), date.getDate()));
    }

    private normalizeEndOfDay(date: Date): Date {
        return new Date(Date.UTC(date.getFullYear(), date.getMonth(), date.getDate(), 23, 59, 59, 999));
    }

    private getQuantizationDays(range: WeightHistoryRange, totalDays: number): number {
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

    private formatDateLabel(dateString: string): string {
        const date = this.normalizeUtcDate(dateString);
        return date.toLocaleDateString();
    }

    private normalizeUtcDate(dateString: string): Date {
        return new Date(dateString);
    }
}

const MS_IN_DAY = 24 * 60 * 60 * 1000;

type WeightHistoryRange = 'week' | 'month' | 'year' | 'custom';

interface BmiSegment {
    labelKey: string;
    from: number;
    to: number;
    class: string;
}

interface BmiStatusInfo {
    labelKey: string;
    descriptionKey: string;
    class: string;
}

function isWeightHistoryRange(value: string): value is WeightHistoryRange {
    return value === 'week' || value === 'month' || value === 'year' || value === 'custom';
}
