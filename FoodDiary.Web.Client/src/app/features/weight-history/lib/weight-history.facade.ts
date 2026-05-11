import { computed, DestroyRef, effect, inject, Injectable, signal } from '@angular/core';
import { takeUntilDestroyed, toSignal } from '@angular/core/rxjs-interop';
import { FormBuilder, FormControl, Validators } from '@angular/forms';
import { TranslateService } from '@ngx-translate/core';
import type { ChartConfiguration } from 'chart.js';
import { distinctUntilChanged, finalize, startWith } from 'rxjs';

import { UserService } from '../../../shared/api/user.service';
import { WeightEntriesService } from '../api/weight-entries.service';
import type {
    CreateWeightEntryPayload,
    WeightEntry,
    WeightEntryFilters,
    WeightEntrySummaryFilters,
    WeightEntrySummaryPoint,
} from '../models/weight-entry.data';

export type WeightHistoryRange = 'week' | 'month' | 'year' | 'custom';

const BMI_SCALE_MAX = 40;
const BMI_POINTER_PADDING_PERCENT = 1;
const MIN_WEIGHT_KG = 1;
const MAX_WEIGHT_KG = 500;
const CENTIMETERS_PER_METER = 100;
const ROUNDING_FACTOR = 10;
const PERCENT_MAX = 100;
const BMI_UNDERWEIGHT_MAX = 18.5;
const BMI_NORMAL_MAX = 25;
const BMI_OVERWEIGHT_MAX = 30;
const DEFAULT_MONTH_OFFSET = 1;
const WEEK_DAYS = 7;
const ENTRIES_LIMIT_MAX = 500;
const ENTRIES_LIMIT_PER_DAY = 5;
const MONTH_QUANTIZATION_DAYS = 3;
const YEAR_QUANTIZATION_DAYS = 14;
const CUSTOM_QUANTIZATION_DIVISOR = 12;
const END_OF_DAY_HOURS = 23;
const END_OF_DAY_MINUTES = 59;
const END_OF_DAY_SECONDS = 59;
const END_OF_DAY_MS = 999;
const DATE_PART_PAD_LENGTH = 2;
const DATE_PART_PAD = '0';
const HOURS_PER_DAY = 24;
const MINUTES_PER_HOUR = 60;
const SECONDS_PER_MINUTE = 60;
const MS_PER_SECOND = 1000;
const MS_IN_DAY = HOURS_PER_DAY * MINUTES_PER_HOUR * SECONDS_PER_MINUTE * MS_PER_SECOND;

export interface BmiStatusInfo {
    labelKey: string;
    descriptionKey: string;
    class: string;
}

@Injectable({ providedIn: 'root' })
export class WeightHistoryFacade {
    private readonly weightEntriesService = inject(WeightEntriesService);
    private readonly userService = inject(UserService);
    private readonly translate = inject(TranslateService);
    private readonly destroyRef = inject(DestroyRef);
    private readonly fb = inject(FormBuilder);

    private readonly userHeightCm = signal<number | null>(null);
    private readonly defaultRange: WeightHistoryRange = 'month';
    private readonly editingEntryId = signal<string | null>(null);
    private readonly initialized = signal(false);
    private lastLoadedRangeKey: string | null = null;

    public readonly selectedRange = signal<WeightHistoryRange>(this.defaultRange);
    public readonly currentRange = computed<{ start: Date; end: Date }>(() => this.calculateRangeDates(this.selectedRange()));
    public readonly entries = signal<WeightEntry[]>([]);
    public readonly isLoading = signal(false);
    public readonly isSaving = signal(false);
    public readonly isEditing = signal(false);
    public readonly desiredWeight = signal<number | null>(null);
    public readonly isDesiredWeightSaving = signal(false);
    public readonly summaryPoints = signal<WeightEntrySummaryPoint[]>([]);
    public readonly isSummaryLoading = signal(false);
    public readonly customRangeControl = new FormControl<{ start: Date | null; end: Date | null } | null>(null);

    public readonly form = this.fb.group({
        date: [this.formatDateInput(new Date()), Validators.required],
        weight: ['', [Validators.required, Validators.min(MIN_WEIGHT_KG), Validators.max(MAX_WEIGHT_KG)]],
    });

    public readonly desiredWeightControl = new FormControl<string>('');

    public readonly entriesDescending = computed(() =>
        [...this.entries()].sort((a, b) => new Date(b.date).getTime() - new Date(a.date).getTime()),
    );

    public readonly chartData = computed<ChartConfiguration<'line'>['data']>(() => {
        const ordered = [...this.summaryPoints()].sort((a, b) => new Date(a.startDate).getTime() - new Date(b.startDate).getTime());
        const label = this.translate.instant('WEIGHT_HISTORY.CHART_LABEL');

        return {
            labels: ordered.map(point => this.formatDateLabel(point.startDate)),
            datasets: [
                {
                    data: ordered.map(point => (point.averageWeight > 0 ? point.averageWeight : null)),
                    label,
                    borderColor: 'var(--fd-color-primary-600)',
                    backgroundColor: 'transparent',
                    fill: false,
                    tension: 0.35,
                    pointRadius: 4,
                    pointBackgroundColor: 'var(--fd-color-white)',
                    borderWidth: 2,
                    spanGaps: true,
                },
            ],
        };
    });

    public readonly latestWeight = computed<number | null>(() => {
        const entries = this.entriesDescending();
        return entries.length > 0 ? entries[0].weight : null;
    });

    public readonly bmiValue = computed<number | null>(() => {
        const heightCm = this.userHeightCm();
        const latest = this.latestWeight();
        if (heightCm === null || latest === null || heightCm <= 0 || latest <= 0) {
            return null;
        }

        const heightMeters = heightCm / CENTIMETERS_PER_METER;
        const bmi = latest / (heightMeters * heightMeters);
        return Math.round(bmi * ROUNDING_FACTOR) / ROUNDING_FACTOR;
    });

    public readonly bmiPointerPosition = computed(() => {
        const bmi = this.bmiValue();
        if (bmi === null) {
            return '0%';
        }

        const rawPercent = (bmi / BMI_SCALE_MAX) * PERCENT_MAX;
        const percent = Math.max(BMI_POINTER_PADDING_PERCENT, Math.min(PERCENT_MAX - BMI_POINTER_PADDING_PERCENT, rawPercent));
        return `${percent}%`;
    });

    public readonly bmiStatusInfo = computed<BmiStatusInfo | null>(() => {
        const bmi = this.bmiValue();
        if (bmi === null) {
            return null;
        }

        if (bmi < BMI_UNDERWEIGHT_MAX) {
            return {
                labelKey: 'WEIGHT_HISTORY.BMI_STATUS.UNDERWEIGHT',
                descriptionKey: 'WEIGHT_HISTORY.BMI_STATUS_DESC.UNDERWEIGHT',
                class: 'weight-history-page__bmi-status--under',
            };
        }

        if (bmi < BMI_NORMAL_MAX) {
            return {
                labelKey: 'WEIGHT_HISTORY.BMI_STATUS.NORMAL',
                descriptionKey: 'WEIGHT_HISTORY.BMI_STATUS_DESC.NORMAL',
                class: 'weight-history-page__bmi-status--normal',
            };
        }

        if (bmi < BMI_OVERWEIGHT_MAX) {
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

    private readonly customRangeValue = toSignal(
        this.customRangeControl.valueChanges.pipe(
            startWith(this.customRangeControl.value),
            distinctUntilChanged((prev, curr) => {
                const prevStart = prev?.start?.getTime();
                const prevEnd = prev?.end?.getTime();
                const currStart = curr?.start?.getTime();
                const currEnd = curr?.end?.getTime();
                return prevStart === currStart && prevEnd === currEnd;
            }),
        ),
    );

    private readonly rangeEffect = effect(() => {
        if (!this.initialized()) {
            return;
        }

        const range = this.selectedRange();
        const customRange = this.customRangeValue();

        if (range !== 'custom') {
            this.loadEntries();
            return;
        }

        if (customRange?.start !== undefined && customRange.start !== null && customRange.end !== null) {
            this.loadEntries();
        }
    });

    public initialize(): void {
        if (this.initialized()) {
            return;
        }

        this.initialized.set(true);
        this.loadDesiredWeight();
        this.loadUserProfile();
        this.loadEntries();
    }

    public submit(): void {
        if (this.form.invalid) {
            this.form.markAllAsTouched();
            return;
        }

        const payload = this.buildPayload();
        if (payload === null) {
            return;
        }

        const editingId = this.editingEntryId();
        const request$ =
            editingId !== null ? this.weightEntriesService.update(editingId, payload) : this.weightEntriesService.create(payload);

        this.isSaving.set(true);
        request$
            .pipe(
                finalize(() => {
                    this.isSaving.set(false);
                }),
                takeUntilDestroyed(this.destroyRef),
            )
            .subscribe(() => {
                this.loadEntries(false, true);
                if (editingId !== null) {
                    this.resetEditingState();
                    return;
                }

                this.form.controls.weight.setValue(payload.weight.toString());
            });
    }

    public startEdit(entry: WeightEntry): void {
        this.isEditing.set(true);
        this.editingEntryId.set(entry.id);
        this.form.setValue({
            date: this.formatDateInput(new Date(entry.date)),
            weight: entry.weight.toString(),
        });
    }

    public cancelEdit(): void {
        this.resetEditingState();
        const latest = (this.entriesDescending() as Array<WeightEntry | undefined>)[0];
        this.form.setValue({
            date: this.formatDateInput(new Date()),
            weight: latest !== undefined ? latest.weight.toString() : '',
        });
    }

    public deleteEntry(entry: WeightEntry): void {
        this.isSaving.set(true);
        this.weightEntriesService
            .remove(entry.id)
            .pipe(
                finalize(() => {
                    this.isSaving.set(false);
                }),
                takeUntilDestroyed(this.destroyRef),
            )
            .subscribe(() => {
                this.loadEntries(false, true);
                if (this.editingEntryId() === entry.id) {
                    this.resetEditingState();
                }
            });
    }

    public saveDesiredWeight(): void {
        if (this.desiredWeightControl.invalid) {
            return;
        }

        const parsedValue = this.parseDesiredWeight();
        if (parsedValue === undefined) {
            this.desiredWeightControl.setErrors({ invalid: true });
            return;
        }

        this.isDesiredWeightSaving.set(true);
        this.userService
            .updateDesiredWeight(parsedValue)
            .pipe(
                finalize(() => {
                    this.isDesiredWeightSaving.set(false);
                }),
                takeUntilDestroyed(this.destroyRef),
            )
            .subscribe(value => {
                this.desiredWeight.set(value);
                this.desiredWeightControl.setValue(value?.toString() ?? '');
            });
    }

    private parseDesiredWeight(): number | null | undefined {
        const rawValue = this.desiredWeightControl.value?.trim();
        if (rawValue === undefined || rawValue.length === 0) {
            return null;
        }

        const parsedValue = Number(rawValue.replace(',', '.'));
        return Number.isNaN(parsedValue) || parsedValue <= 0 || parsedValue > MAX_WEIGHT_KG ? undefined : parsedValue;
    }

    public changeRange(value: string): void {
        if (!isWeightHistoryRange(value) || value === this.selectedRange()) {
            return;
        }

        this.selectedRange.set(value);

        if (value === 'custom') {
            const current = this.customRangeControl.value;
            if (current?.start === undefined || current.start === null || current.end === null) {
                const end = new Date();
                const start = new Date(end);
                start.setMonth(start.getMonth() - DEFAULT_MONTH_OFFSET);
                this.customRangeControl.setValue({ start, end }, { emitEvent: true });
            }
            return;
        }

        this.customRangeControl.setValue(null, { emitEvent: false });
    }

    private loadEntries(showLoader = true, force = false): void {
        const { entriesParams, summaryParams, rangeKey } = this.buildFiltersForRange(this.selectedRange());

        if (!force && rangeKey === this.lastLoadedRangeKey) {
            return;
        }

        this.lastLoadedRangeKey = rangeKey;
        if (showLoader) {
            this.isLoading.set(true);
        }

        this.weightEntriesService
            .getEntries(entriesParams)
            .pipe(
                finalize(() => {
                    this.isLoading.set(false);
                }),
                takeUntilDestroyed(this.destroyRef),
            )
            .subscribe(entries => {
                this.entries.set(entries);
                if (!this.isEditing() && entries.length > 0) {
                    const latest = [...entries].sort((a, b) => new Date(b.date).getTime() - new Date(a.date).getTime())[0];
                    this.form.patchValue({
                        weight: latest.weight.toString(),
                    });
                }
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

    private loadSummary(filters: WeightEntrySummaryFilters): void {
        this.isSummaryLoading.set(true);
        this.weightEntriesService
            .getSummary(filters)
            .pipe(
                finalize(() => {
                    this.isSummaryLoading.set(false);
                }),
                takeUntilDestroyed(this.destroyRef),
            )
            .subscribe(points => {
                this.summaryPoints.set(points);
            });
    }

    private buildPayload(): CreateWeightEntryPayload | null {
        const rawDate = this.form.value.date;
        const rawWeight = this.form.value.weight;
        if (rawDate === null || rawDate === undefined || rawDate.length === 0 || rawWeight === null || rawWeight === undefined) {
            return null;
        }

        const date = typeof rawDate === 'string' ? new Date(rawDate) : rawDate;
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
            date: this.formatDateInput(new Date()),
        });
    }

    private buildFiltersForRange(range: WeightHistoryRange): {
        entriesParams: WeightEntryFilters;
        summaryParams: WeightEntrySummaryFilters;
        rangeKey: string;
    } {
        const { start, end } = this.calculateRangeDates(range);
        const normalizedStart = this.normalizeStartOfDay(start);
        const normalizedEnd = this.normalizeEndOfDay(end);
        const totalDays = Math.max(1, Math.ceil((normalizedEnd.getTime() - normalizedStart.getTime()) / MS_IN_DAY));
        const quantizationDays = this.getQuantizationDays(range, totalDays);
        const limit = Math.min(ENTRIES_LIMIT_MAX, totalDays * ENTRIES_LIMIT_PER_DAY);
        const rangeKey = `${normalizedStart.toISOString()}_${normalizedEnd.toISOString()}`;

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
            rangeKey,
        };
    }

    private calculateRangeDates(range: WeightHistoryRange): { start: Date; end: Date } {
        const now = new Date();
        let start = new Date(now);
        let end = new Date(now);

        if (range === 'week') {
            start.setDate(start.getDate() - WEEK_DAYS);
        } else if (range === 'month') {
            start.setMonth(start.getMonth() - DEFAULT_MONTH_OFFSET);
        } else if (range === 'year') {
            start.setFullYear(start.getFullYear() - DEFAULT_MONTH_OFFSET);
        } else {
            const custom = this.customRangeControl.value;
            if (custom?.start !== undefined && custom.start !== null) {
                start = new Date(custom.start);
            } else {
                start.setMonth(start.getMonth() - DEFAULT_MONTH_OFFSET);
            }

            if (custom?.end !== undefined && custom.end !== null) {
                end = new Date(custom.end);
            }
        }

        return { start, end };
    }

    private normalizeStartOfDay(date: Date): Date {
        return new Date(Date.UTC(date.getFullYear(), date.getMonth(), date.getDate()));
    }

    private normalizeEndOfDay(date: Date): Date {
        return new Date(
            Date.UTC(
                date.getFullYear(),
                date.getMonth(),
                date.getDate(),
                END_OF_DAY_HOURS,
                END_OF_DAY_MINUTES,
                END_OF_DAY_SECONDS,
                END_OF_DAY_MS,
            ),
        );
    }

    private formatDateInput(date: Date): string {
        const year = date.getFullYear();
        const month = String(date.getMonth() + 1).padStart(DATE_PART_PAD_LENGTH, DATE_PART_PAD);
        const day = String(date.getDate()).padStart(DATE_PART_PAD_LENGTH, DATE_PART_PAD);
        return `${year}-${month}-${day}`;
    }

    private getQuantizationDays(range: WeightHistoryRange, totalDays: number): number {
        if (range === 'week') {
            return 1;
        }

        if (range === 'month') {
            return MONTH_QUANTIZATION_DAYS;
        }

        if (range === 'year') {
            return YEAR_QUANTIZATION_DAYS;
        }

        return Math.max(1, Math.round(totalDays / CUSTOM_QUANTIZATION_DIVISOR));
    }

    private formatDateLabel(dateString: string): string {
        return new Date(dateString).toLocaleDateString();
    }
}

function isWeightHistoryRange(value: string): value is WeightHistoryRange {
    return value === 'week' || value === 'month' || value === 'year' || value === 'custom';
}
