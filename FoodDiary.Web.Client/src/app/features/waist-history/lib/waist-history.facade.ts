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
    UpdateWaistEntryPayload,
    WaistEntry,
    WaistEntryFilters,
    WaistEntrySummaryFilters,
    WaistEntrySummaryPoint,
} from '../models/waist-entry.data';

export type WaistHistoryRange = 'week' | 'month' | 'year' | 'custom';

export interface WhtStatusInfo {
    labelKey: string;
    descriptionKey: string;
    class: string;
}

@Injectable()
export class WaistHistoryFacade {
    private readonly waistEntriesService = inject(WaistEntriesService);
    private readonly userService = inject(UserService);
    private readonly translate = inject(TranslateService);
    private readonly destroyRef = inject(DestroyRef);
    private readonly fb = inject(FormBuilder);

    private readonly defaultRange: WaistHistoryRange = 'month';
    private readonly whtScaleMax = 0.8;
    private readonly pointerPaddingPercent = 1;
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
        circumference: ['', [Validators.required, Validators.min(1), Validators.max(300)]],
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
        const clamped = Math.max(this.pointerPaddingPercent, Math.min(100 - this.pointerPaddingPercent, rawPercent));
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

        if (customRange?.start && customRange.end) {
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
        if (!payload) {
            return;
        }

        const editingId = this.editingEntryId();
        const request$ = editingId ? this.waistEntriesService.update(editingId, payload) : this.waistEntriesService.create(payload);

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
                if (editingId) {
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
            circumference: latest ? latest.circumference.toString() : '',
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

        const rawValue = this.desiredWaistControl.value?.trim();
        const parsedValue = rawValue ? Number(rawValue.replace(',', '.')) : null;
        if (rawValue && (parsedValue === null || Number.isNaN(parsedValue) || parsedValue <= 0 || parsedValue > 400)) {
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

    public changeRange(value: string): void {
        if (!isWaistHistoryRange(value) || value === this.selectedRange()) {
            return;
        }

        this.selectedRange.set(value);

        if (value === 'custom') {
            const current = this.customRangeControl.value;
            if (!current?.start || !current.end) {
                const end = new Date();
                const start = new Date(end);
                start.setMonth(start.getMonth() - 1);
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

    private buildPayload(): CreateWaistEntryPayload | UpdateWaistEntryPayload | null {
        const rawDate = this.form.value.date;
        const rawCircumference = this.form.value.circumference;
        if (!rawDate || rawCircumference === null || rawCircumference === undefined) {
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
        const limit = Math.min(500, totalDays * 5);
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

        return { start, end };
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

    private formatDateInput(date: Date): string {
        const year = date.getFullYear();
        const month = String(date.getMonth() + 1).padStart(2, '0');
        const day = String(date.getDate()).padStart(2, '0');
        return `${year}-${month}-${day}`;
    }

    private formatDateLabel(dateString: string): string {
        return new Date(dateString).toLocaleDateString();
    }
}

const MS_IN_DAY = 24 * 60 * 60 * 1000;

function isWaistHistoryRange(value: string): value is WaistHistoryRange {
    return value === 'week' || value === 'month' || value === 'year' || value === 'custom';
}
