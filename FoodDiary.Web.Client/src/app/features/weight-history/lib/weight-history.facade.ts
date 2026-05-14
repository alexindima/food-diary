import { computed, DestroyRef, effect, inject, Injectable, signal } from '@angular/core';
import { takeUntilDestroyed, toSignal } from '@angular/core/rxjs-interop';
import { FormBuilder, FormControl, Validators } from '@angular/forms';
import { TranslateService } from '@ngx-translate/core';
import { distinctUntilChanged, finalize, startWith } from 'rxjs';

import { UserService } from '../../../shared/api/user.service';
import { WeightEntriesService } from '../api/weight-entries.service';
import type {
    CreateWeightEntryPayload,
    WeightEntry,
    WeightEntrySummaryFilters,
    WeightEntrySummaryPoint,
} from '../models/weight-entry.data';
import { MAX_WEIGHT_KG, MIN_WEIGHT_KG } from './weight-history.constants';
import type { WeightHistoryCustomRange, WeightHistoryDateRange, WeightHistoryRange } from './weight-history.types';
import { buildBmiViewModel } from './weight-history-bmi.mapper';
import { buildWeightHistoryChartData } from './weight-history-chart.mapper';
import {
    buildDefaultWeightHistoryCustomRange,
    buildWeightHistoryFiltersForRange,
    calculateWeightHistoryRangeDates,
    formatWeightHistoryDateInput,
    isWeightHistoryRange,
    normalizeStartOfDay,
} from './weight-history-range.utils';

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
    public readonly currentRange = computed<WeightHistoryDateRange>(() =>
        calculateWeightHistoryRangeDates(this.selectedRange(), this.customRangeControl.value),
    );
    public readonly entries = signal<WeightEntry[]>([]);
    public readonly isLoading = signal(false);
    public readonly isSaving = signal(false);
    public readonly isEditing = signal(false);
    public readonly desiredWeight = signal<number | null>(null);
    public readonly isDesiredWeightSaving = signal(false);
    public readonly summaryPoints = signal<WeightEntrySummaryPoint[]>([]);
    public readonly isSummaryLoading = signal(false);
    public readonly customRangeControl = new FormControl<WeightHistoryCustomRange | null>(null);

    public readonly form = this.fb.group({
        date: [formatWeightHistoryDateInput(new Date()), Validators.required],
        weight: ['', [Validators.required, Validators.min(MIN_WEIGHT_KG), Validators.max(MAX_WEIGHT_KG)]],
    });

    public readonly desiredWeightControl = new FormControl<string>('');

    public readonly entriesDescending = computed(() =>
        [...this.entries()].sort((a, b) => new Date(b.date).getTime() - new Date(a.date).getTime()),
    );

    public readonly chartData = computed(() =>
        buildWeightHistoryChartData(
            this.summaryPoints(),
            this.translate.instant('WEIGHT_HISTORY.CHART_LABEL'),
            this.translate.getCurrentLang(),
        ),
    );

    public readonly latestWeight = computed<number | null>(() => {
        const entries = this.entriesDescending();
        return entries.length > 0 ? entries[0].weight : null;
    });

    public readonly bmiViewModel = computed(() => buildBmiViewModel(this.userHeightCm(), this.latestWeight()));

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

    public constructor() {
        effect(() => {
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
    }

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
            date: formatWeightHistoryDateInput(new Date(entry.date)),
            weight: entry.weight.toString(),
        });
    }

    public cancelEdit(): void {
        this.resetEditingState();
        const latest = (this.entriesDescending() as Array<WeightEntry | undefined>)[0];
        this.form.setValue({
            date: formatWeightHistoryDateInput(new Date()),
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
                this.customRangeControl.setValue(buildDefaultWeightHistoryCustomRange(), { emitEvent: true });
            }
            return;
        }

        this.customRangeControl.setValue(null, { emitEvent: false });
    }

    private loadEntries(showLoader = true, force = false): void {
        const { entriesParams, summaryParams, rangeKey } = buildWeightHistoryFiltersForRange(
            this.selectedRange(),
            this.customRangeControl.value,
        );

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
        const utcDate = normalizeStartOfDay(date);
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
            date: formatWeightHistoryDateInput(new Date()),
        });
    }
}
