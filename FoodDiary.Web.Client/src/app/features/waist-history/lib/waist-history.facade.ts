import { computed, DestroyRef, effect, inject, Injectable, signal } from '@angular/core';
import { takeUntilDestroyed, toSignal } from '@angular/core/rxjs-interop';
import { FormBuilder, FormControl, Validators } from '@angular/forms';
import { TranslateService } from '@ngx-translate/core';
import { distinctUntilChanged, finalize, startWith } from 'rxjs';

import { UserService } from '../../../shared/api/user.service';
import { WaistEntriesService } from '../api/waist-entries.service';
import type { CreateWaistEntryPayload, WaistEntry, WaistEntrySummaryFilters, WaistEntrySummaryPoint } from '../models/waist-entry.data';
import { MAX_DESIRED_WAIST_CM, MAX_WAIST_CM, MIN_WAIST_CM } from './waist-history.constants';
import type { WaistHistoryCustomRange, WaistHistoryDateRange, WaistHistoryRange } from './waist-history.types';
import { buildWaistHistoryChartData } from './waist-history-chart.mapper';
import {
    buildDefaultWaistHistoryCustomRange,
    buildWaistHistoryFiltersForRange,
    calculateWaistHistoryRangeDates,
    formatWaistHistoryDateInput,
    isWaistHistoryRange,
    normalizeStartOfDay,
} from './waist-history-range.utils';
import { buildWhtViewModel } from './waist-history-wht.mapper';

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
    public readonly currentRange = computed<WaistHistoryDateRange>(() =>
        calculateWaistHistoryRangeDates(this.selectedRange(), this.customRangeControl.value),
    );
    public readonly entries = signal<WaistEntry[]>([]);
    public readonly isLoading = signal(false);
    public readonly isSaving = signal(false);
    public readonly isEditing = signal(false);
    public readonly summaryPoints = signal<WaistEntrySummaryPoint[]>([]);
    public readonly isSummaryLoading = signal(false);
    public readonly customRangeControl = new FormControl<WaistHistoryCustomRange | null>(null);
    public readonly desiredWaist = signal<number | null>(null);
    public readonly isDesiredWaistSaving = signal(false);
    public readonly desiredWaistControl = new FormControl<string>('');

    public readonly form = this.fb.group({
        date: [formatWaistHistoryDateInput(new Date()), Validators.required],
        circumference: ['', [Validators.required, Validators.min(MIN_WAIST_CM), Validators.max(MAX_WAIST_CM)]],
    });

    public readonly entriesDescending = computed(() =>
        [...this.entries()].sort((a, b) => new Date(b.date).getTime() - new Date(a.date).getTime()),
    );

    public readonly chartData = computed(() =>
        buildWaistHistoryChartData(
            this.summaryPoints(),
            this.translate.instant('WAIST_HISTORY.CHART_LABEL'),
            this.translate.getCurrentLang(),
        ),
    );

    public readonly latestWaist = computed<number | null>(() => {
        const entries = this.entriesDescending();
        return entries.length > 0 ? entries[0].circumference : null;
    });

    public readonly whtViewModel = computed(() => buildWhtViewModel(this.userHeightCm(), this.latestWaist()));

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
            date: formatWaistHistoryDateInput(new Date(entry.date)),
            circumference: entry.circumference.toString(),
        });
    }

    public cancelEdit(): void {
        this.resetEditingState();
        const latest = (this.entriesDescending() as Array<WaistEntry | undefined>)[0];
        this.form.setValue({
            date: formatWaistHistoryDateInput(new Date()),
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

    public changeRange(value: string): void {
        if (!isWaistHistoryRange(value) || value === this.selectedRange()) {
            return;
        }

        this.selectedRange.set(value);

        if (value === 'custom') {
            const current = this.customRangeControl.value;
            if (current?.start === undefined || current.start === null || current.end === null) {
                this.customRangeControl.setValue(buildDefaultWaistHistoryCustomRange(), { emitEvent: true });
            }
            return;
        }

        this.customRangeControl.setValue(null, { emitEvent: false });
    }

    private parseDesiredWaist(): number | null | undefined {
        const rawValue = this.desiredWaistControl.value?.trim();
        if (rawValue === undefined || rawValue.length === 0) {
            return null;
        }

        const parsedValue = Number(rawValue.replace(',', '.'));
        return Number.isNaN(parsedValue) || parsedValue <= 0 || parsedValue > MAX_DESIRED_WAIST_CM ? undefined : parsedValue;
    }

    private loadEntries(showLoader = true, force = false): void {
        const { entriesParams, summaryParams, rangeKey } = buildWaistHistoryFiltersForRange(
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
                if (!this.isEditing() && entries.length > 0) {
                    const latest = [...entries].sort((a, b) => new Date(b.date).getTime() - new Date(a.date).getTime())[0];
                    this.form.patchValue({
                        circumference: latest.circumference.toString(),
                    });
                }
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
        const utcDate = normalizeStartOfDay(date);
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
            date: formatWaistHistoryDateInput(new Date()),
        });
    }
}
