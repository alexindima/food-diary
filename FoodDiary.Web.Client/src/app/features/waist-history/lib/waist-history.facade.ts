import { computed, DestroyRef, effect, inject, Injectable, signal } from '@angular/core';
import { takeUntilDestroyed, toSignal } from '@angular/core/rxjs-interop';
import { FormBuilder, FormControl, Validators } from '@angular/forms';
import { TranslateService } from '@ngx-translate/core';
import type { ChartConfiguration } from 'chart.js';
import { distinctUntilChanged, finalize, startWith } from 'rxjs';

import { UserService } from '../../../shared/api/user.service';
import { WaistEntriesService } from '../api/waist-entries.service';
import type {
    CreateWaistEntryPayload,
    WaistEntry,
    WaistEntryFilters,
    WaistEntrySummaryFilters,
    WaistEntrySummaryPoint,
} from '../models/waist-entry.data';

export type WaistHistoryRange = 'week' | 'month' | 'year' | 'custom';

const WHT_SCALE_MAX = 0.8;
const POINTER_PADDING_PERCENT = 1;
const MIN_WAIST_CM = 1;
const MAX_WAIST_CM = 300;
const MAX_DESIRED_WAIST_CM = 400;
const ROUNDING_FACTOR = 100;
const PERCENT_MAX = 100;
const WHT_UNDER_MAX = 0.4;
const WHT_NORMAL_MAX = 0.5;
const WHT_ELEVATED_MAX = 0.6;
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

export interface WhtStatusInfo {
    labelKey: string;
    descriptionKey: string;
    class: string;
}

@Injectable({ providedIn: 'root' })
export class WaistHistoryFacade {
    private readonly waistEntriesService = inject(WaistEntriesService);
    private readonly userService = inject(UserService);
    private readonly translate = inject(TranslateService);
    private readonly destroyRef = inject(DestroyRef);
    private readonly fb = inject(FormBuilder);

    private readonly defaultRange: WaistHistoryRange = 'month';
    private readonly editingEntryId = signal<string | null>(null);
    private readonly userHeightCm = signal<number | null>(null);
    private readonly initialized = signal(false);
    private lastLoadedRangeKey: string | null = null;

    public readonly selectedRange = signal<WaistHistoryRange>(this.defaultRange);
    public readonly currentRange = computed<{ start: Date; end: Date }>(() => this.calculateRangeDates(this.selectedRange()));
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

    public readonly form = this.fb.group({
        date: [this.formatDateInput(new Date()), Validators.required],
        circumference: ['', [Validators.required, Validators.min(MIN_WAIST_CM), Validators.max(MAX_WAIST_CM)]],
    });

    public readonly entriesDescending = computed(() =>
        [...this.entries()].sort((a, b) => new Date(b.date).getTime() - new Date(a.date).getTime()),
    );

    public readonly chartData = computed<ChartConfiguration<'line'>['data']>(() => {
        const ordered = [...this.summaryPoints()].sort((a, b) => new Date(a.startDate).getTime() - new Date(b.startDate).getTime());
        const label = this.translate.instant('WAIST_HISTORY.CHART_LABEL');

        return {
            labels: ordered.map(point => this.formatDateLabel(point.startDate)),
            datasets: [
                {
                    data: ordered.map(point => (point.averageCircumference > 0 ? point.averageCircumference : null)),
                    label,
                    borderColor: 'var(--fd-color-sky-500)',
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

    public readonly latestWaist = computed<number | null>(() => {
        const entries = this.entriesDescending();
        return entries.length > 0 ? entries[0].circumference : null;
    });

    public readonly whtrValue = computed<number | null>(() => {
        const height = this.userHeightCm();
        const waist = this.latestWaist();
        if (height === null || waist === null || height <= 0 || waist <= 0) {
            return null;
        }

        const ratio = waist / height;
        return Math.round(ratio * ROUNDING_FACTOR) / ROUNDING_FACTOR;
    });

    public readonly whtrPointerPosition = computed(() => {
        const ratio = this.whtrValue();
        if (ratio === null) {
            return '0%';
        }

        const rawPercent = (ratio / WHT_SCALE_MAX) * PERCENT_MAX;
        const clamped = Math.max(POINTER_PADDING_PERCENT, Math.min(PERCENT_MAX - POINTER_PADDING_PERCENT, rawPercent));
        return `${clamped}%`;
    });

    public readonly whtrStatusInfo = computed<WhtStatusInfo | null>(() => {
        const ratio = this.whtrValue();
        if (ratio === null) {
            return null;
        }

        if (ratio < WHT_UNDER_MAX) {
            return {
                labelKey: 'WAIST_HISTORY.WHT_STATUS.UNDER',
                descriptionKey: 'WAIST_HISTORY.WHT_STATUS_DESC.UNDER',
                class: 'waist-history-page__wht-status--under',
            };
        }

        if (ratio < WHT_NORMAL_MAX) {
            return {
                labelKey: 'WAIST_HISTORY.WHT_STATUS.NORMAL',
                descriptionKey: 'WAIST_HISTORY.WHT_STATUS_DESC.NORMAL',
                class: 'waist-history-page__wht-status--normal',
            };
        }

        if (ratio < WHT_ELEVATED_MAX) {
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
        this.loadUserProfile();
        this.loadDesiredWaist();
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
            editingId !== null ? this.waistEntriesService.update(editingId, payload) : this.waistEntriesService.create(payload);

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

                this.form.controls.circumference.setValue(payload.circumference.toString());
            });
    }

    public startEdit(entry: WaistEntry): void {
        this.isEditing.set(true);
        this.editingEntryId.set(entry.id);
        this.form.setValue({
            date: this.formatDateInput(new Date(entry.date)),
            circumference: entry.circumference.toString(),
        });
    }

    public cancelEdit(): void {
        this.resetEditingState();
        const latest = (this.entriesDescending() as Array<WaistEntry | undefined>)[0];
        this.form.setValue({
            date: this.formatDateInput(new Date()),
            circumference: latest !== undefined ? latest.circumference.toString() : '',
        });
    }

    public deleteEntry(entry: WaistEntry): void {
        this.isSaving.set(true);
        this.waistEntriesService
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

    public saveDesiredWaist(): void {
        if (this.desiredWaistControl.invalid) {
            return;
        }

        const parsedValue = this.parseDesiredWaist();
        if (parsedValue === undefined) {
            this.desiredWaistControl.setErrors({ invalid: true });
            return;
        }

        this.isDesiredWaistSaving.set(true);
        this.userService
            .updateDesiredWaist(parsedValue)
            .pipe(
                finalize(() => {
                    this.isDesiredWaistSaving.set(false);
                }),
                takeUntilDestroyed(this.destroyRef),
            )
            .subscribe(value => {
                this.desiredWaist.set(value);
                this.desiredWaistControl.setValue(value?.toString() ?? '');
            });
    }

    private parseDesiredWaist(): number | null | undefined {
        const rawValue = this.desiredWaistControl.value?.trim();
        if (rawValue === undefined || rawValue.length === 0) {
            return null;
        }

        const parsedValue = Number(rawValue.replace(',', '.'));
        return Number.isNaN(parsedValue) || parsedValue <= 0 || parsedValue > MAX_DESIRED_WAIST_CM ? undefined : parsedValue;
    }

    public changeRange(value: string): void {
        if (!isWaistHistoryRange(value) || value === this.selectedRange()) {
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

    private loadEntries(initializeRange = true, force = false): void {
        const { entriesParams, summaryParams, rangeKey } = this.buildFiltersForRange(this.selectedRange());

        if (!force && rangeKey === this.lastLoadedRangeKey) {
            return;
        }

        this.lastLoadedRangeKey = rangeKey;
        this.isLoading.set(true);

        if (initializeRange && this.selectedRange() === 'custom') {
            const start = new Date(summaryParams.dateFrom);
            const end = new Date(summaryParams.dateTo);
            this.customRangeControl.setValue({ start, end }, { emitEvent: false });
        }

        this.waistEntriesService
            .getEntries(entriesParams)
            .pipe(
                finalize(() => {
                    this.isLoading.set(false);
                }),
                takeUntilDestroyed(this.destroyRef),
            )
            .subscribe(entries => {
                this.entries.set(entries);
            });

        this.loadSummary(summaryParams);
    }

    private loadSummary(filters: WaistEntrySummaryFilters): void {
        this.isSummaryLoading.set(true);
        this.waistEntriesService
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

    private buildPayload(): CreateWaistEntryPayload | null {
        const rawDate = this.form.value.date;
        const rawCircumference = this.form.value.circumference;
        if (
            rawDate === null ||
            rawDate === undefined ||
            rawDate.length === 0 ||
            rawCircumference === null ||
            rawCircumference === undefined
        ) {
            return null;
        }

        const date = typeof rawDate === 'string' ? new Date(rawDate) : rawDate;
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
            date: this.formatDateInput(new Date()),
        });
    }

    private buildFiltersForRange(range: WaistHistoryRange): {
        entriesParams: WaistEntryFilters;
        summaryParams: WaistEntrySummaryFilters;
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

    private calculateRangeDates(range: WaistHistoryRange): { start: Date; end: Date } {
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

    private getQuantizationDays(range: WaistHistoryRange, totalDays: number): number {
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

    private formatDateLabel(dateString: string): string {
        return new Date(dateString).toLocaleDateString();
    }
}

function isWaistHistoryRange(value: string): value is WaistHistoryRange {
    return value === 'week' || value === 'month' || value === 'year' || value === 'custom';
}
