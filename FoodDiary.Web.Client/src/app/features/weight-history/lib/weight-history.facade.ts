import { computed, DestroyRef, effect, inject, Injectable, signal } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { form, required, validate } from '@angular/forms/signals';
import { TranslateService } from '@ngx-translate/core';
import { finalize, firstValueFrom } from 'rxjs';

import { UserService } from '../../../shared/api/user.service';
import { resolveTranslateLanguage } from '../../../shared/i18n/translate-language.utils';
import { compareDatesDesc } from '../../../shared/lib/local-date.utils';
import { parseDecimalInput } from '../../../shared/lib/number.utils';
import { getRecordProperty, getStringProperty } from '../../../shared/lib/unknown-value.utils';
import type { DesiredWeightResponse, WeightGoalHistoryItem } from '../../../shared/models/user.data';
import { NutritionDataInvalidationService } from '../../../shared/state/nutrition-data-invalidation.service';
import { WeightEntriesService } from '../api/weight-entries.service';
import type {
    CreateWeightEntryPayload,
    WeightEntry,
    WeightEntrySummaryFilters,
    WeightEntrySummaryPoint,
} from '../models/weight-entry.data';
import { MAX_WEIGHT_KG, MIN_WEIGHT_KG, WEIGHT_HISTORY_ENTRIES_LIMIT_MAX } from './weight-history.constants';
import type { WeightHistoryCustomRange, WeightHistoryDateRange, WeightHistoryRange } from './weight-history.types';
import { buildBmiViewModel } from './weight-history-bmi.mapper';
import { buildWeightHistoryChartPoints } from './weight-history-chart.mapper';
import {
    buildDefaultWeightHistoryCustomRange,
    buildWeightHistoryFiltersForRange,
    calculateWeightHistoryRangeDates,
    formatWeightHistoryDateInput,
    isWeightHistoryRange,
    normalizeStartOfDay,
} from './weight-history-range.utils';

type WeightEntryFormModel = {
    date: string;
    weight: string;
};

type DesiredWeightFormModel = {
    weight: string;
};

type WeightCustomRangeFormModel = {
    range: WeightHistoryCustomRange | null;
};

@Injectable()
export class WeightHistoryFacade {
    private readonly weightEntriesService = inject(WeightEntriesService);
    private readonly userService = inject(UserService);
    private readonly invalidation = inject(NutritionDataInvalidationService);
    private readonly translate = inject(TranslateService);
    private readonly destroyRef = inject(DestroyRef);

    private readonly userHeightCm = signal<number | null>(null);
    private readonly defaultRange: WeightHistoryRange = 'month';
    private readonly editingEntryId = signal<string | null>(null);
    private readonly initialized = signal(false);
    private lastLoadedRangeKey: string | null = null;

    public readonly selectedRange = signal<WeightHistoryRange>(this.defaultRange);
    public readonly currentRange = computed<WeightHistoryDateRange>(() =>
        calculateWeightHistoryRangeDates(this.selectedRange(), this.customRangeModel().range),
    );
    public readonly entries = signal<WeightEntry[]>([]);
    public readonly latestEntry = signal<WeightEntry | null>(null);
    public readonly isLoading = signal(false);
    public readonly isSaving = signal(false);
    public readonly entryError = signal<string | null>(null);
    public readonly entrySaveVersion = signal(0);
    public readonly isEditing = signal(false);
    public readonly weightGoal = signal<DesiredWeightResponse>({ desiredWeight: null, startWeight: null, startedAtUtc: null });
    public readonly desiredWeight = computed(() => this.weightGoal().desiredWeight);
    public readonly weightGoalHistory = signal<WeightGoalHistoryItem[]>([]);
    public readonly hasCompletedWeightGoals = computed(() => this.weightGoalHistory().some(goal => goal.status !== 'Active'));
    public readonly lastCompletedWeightGoal = computed(() => this.weightGoalHistory().find(goal => goal.status !== 'Active') ?? null);
    public readonly isDesiredWeightSaving = signal(false);
    public readonly desiredWeightSaveVersion = signal(0);
    public readonly summaryPoints = signal<WeightEntrySummaryPoint[]>([]);
    public readonly rollingMonthSummaryPoints = signal<WeightEntrySummaryPoint[]>([]);
    public readonly isSummaryLoading = signal(false);
    public readonly customRangeModel = signal<WeightCustomRangeFormModel>({ range: null });
    public readonly customRangeForm = form(this.customRangeModel);

    public readonly formModel = signal<WeightEntryFormModel>({
        date: formatWeightHistoryDateInput(new Date()),
        weight: '',
    });
    private readonly submitWeightEntryFormAsync = async (): Promise<void> => {
        await this.submitAsync();
    };
    public readonly form = form(
        this.formModel,
        path => {
            required(path.date);
            required(path.weight);
            validate(path.weight, ({ value }) => {
                const parsed = parseDecimalInput(value());
                return parsed === null || parsed < MIN_WEIGHT_KG || parsed > MAX_WEIGHT_KG
                    ? { kind: 'weightRange', message: 'Weight is out of range' }
                    : undefined;
            });
        },
        {
            submission: {
                action: this.submitWeightEntryFormAsync,
            },
        },
    );

    public readonly desiredWeightModel = signal<DesiredWeightFormModel>({ weight: '' });
    public readonly desiredWeightForm = form(this.desiredWeightModel);

    public readonly entriesDescending = computed(() => [...this.entries()].sort((a, b) => compareDatesDesc(a.date, b.date)));

    public readonly chartPoints = computed(() =>
        buildWeightHistoryChartPoints(this.summaryPoints(), resolveTranslateLanguage(this.translate)),
    );

    public readonly latestWeight = computed<number | null>(() => this.latestEntry()?.weight ?? null);
    public readonly latestWeightDate = computed<string | null>(() => this.latestEntry()?.date ?? null);

    public readonly bmiViewModel = computed(() => buildBmiViewModel(this.userHeightCm(), this.latestWeight()));

    public constructor() {
        effect(() => {
            if (!this.initialized()) {
                return;
            }

            const range = this.selectedRange();
            const customRange = this.customRangeModel().range;

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

        this.loadPageSummary();
        this.initialized.set(true);
    }

    public submit(): void {
        void this.submitAsync();
    }

    private async submitAsync(): Promise<void> {
        if (this.form().invalid()) {
            this.form().markAsTouched();
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
        this.entryError.set(null);
        try {
            await firstValueFrom(
                request$.pipe(
                    finalize(() => {
                        this.isSaving.set(false);
                    }),
                    takeUntilDestroyed(this.destroyRef),
                ),
            );
            this.entrySaveVersion.update(version => version + 1);
            this.invalidation.reportBodyMetricMutation();
            this.loadPageSummary(true);
            this.loadRollingMonthSummaryIfNeeded();
            if (editingId !== null) {
                this.resetEditingState();
                return;
            }

            this.form.weight().value.set(payload.weight.toString());
        } catch (error: unknown) {
            this.handleEntrySaveError(error);
        }
    }

    public startEdit(entry: WeightEntry): void {
        this.isEditing.set(true);
        this.editingEntryId.set(entry.id);
        this.formModel.set({
            date: formatWeightHistoryDateInput(new Date(entry.date)),
            weight: entry.weight.toString(),
        });
    }

    public cancelEdit(): void {
        this.resetEditingState();
        this.formModel.set({
            date: formatWeightHistoryDateInput(new Date()),
            weight: this.latestWeight()?.toString() ?? '',
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
                this.invalidation.reportBodyMetricMutation();
                this.loadPageSummary(true);
                this.loadRollingMonthSummaryIfNeeded();
                if (this.editingEntryId() === entry.id) {
                    this.resetEditingState();
                }
            });
    }

    public saveDesiredWeight(): void {
        if (this.desiredWeightForm().invalid()) {
            return;
        }

        const parsedValue = this.parseDesiredWeight();
        if (parsedValue === undefined) {
            return;
        }

        this.isDesiredWeightSaving.set(true);
        this.userService
            .updateWeightGoal(parsedValue)
            .pipe(
                finalize(() => {
                    this.isDesiredWeightSaving.set(false);
                }),
                takeUntilDestroyed(this.destroyRef),
            )
            .subscribe(goal => {
                this.invalidation.reportGoalMutation();
                this.weightGoal.set(goal);
                this.desiredWeightModel.set({ weight: goal.desiredWeight?.toString() ?? '' });
                this.desiredWeightSaveVersion.update(version => version + 1);
                this.loadWeightGoalHistory();
            });
    }

    public cancelWeightGoal(): void {
        this.isDesiredWeightSaving.set(true);
        this.userService
            .updateWeightGoal(null)
            .pipe(
                finalize(() => {
                    this.isDesiredWeightSaving.set(false);
                }),
                takeUntilDestroyed(this.destroyRef),
            )
            .subscribe(goal => {
                this.invalidation.reportGoalMutation();
                this.weightGoal.set(goal);
                this.desiredWeightModel.set({ weight: '' });
                this.desiredWeightSaveVersion.update(version => version + 1);
                this.loadWeightGoalHistory();
            });
    }

    private parseDesiredWeight(): number | null | undefined {
        const rawValue = this.desiredWeightModel().weight.trim();
        if (rawValue.length === 0) {
            return null;
        }

        const parsedValue = parseDecimalInput(rawValue);
        return parsedValue === null || parsedValue <= 0 || parsedValue > MAX_WEIGHT_KG ? undefined : parsedValue;
    }

    public changeRange(value: string): void {
        if (!isWeightHistoryRange(value) || value === this.selectedRange()) {
            return;
        }

        this.selectedRange.set(value);

        if (value === 'custom') {
            const current = this.customRangeModel().range;
            if (current?.start === undefined || current.start === null || current.end === null) {
                this.customRangeModel.set({ range: buildDefaultWeightHistoryCustomRange() });
            }
            return;
        }

        this.customRangeModel.set({ range: null });
    }

    private loadEntries(force = false): void {
        const { summaryParams, rangeKey } = buildWeightHistoryFiltersForRange(this.selectedRange(), this.customRangeModel().range);

        if (!force && rangeKey === this.lastLoadedRangeKey) {
            return;
        }

        this.lastLoadedRangeKey = rangeKey;
        this.loadSummary(summaryParams, this.selectedRange() === 'month');
    }

    private loadPageSummary(force = false): void {
        const { summaryParams, rangeKey } = buildWeightHistoryFiltersForRange(this.selectedRange(), this.customRangeModel().range);
        if (!force && rangeKey === this.lastLoadedRangeKey) {
            return;
        }

        this.lastLoadedRangeKey = rangeKey;
        this.isLoading.set(true);
        this.isSummaryLoading.set(true);
        this.weightEntriesService
            .getPageSummary({ ...summaryParams, entriesLimit: WEIGHT_HISTORY_ENTRIES_LIMIT_MAX })
            .pipe(
                finalize(() => {
                    this.isLoading.set(false);
                    this.isSummaryLoading.set(false);
                }),
                takeUntilDestroyed(this.destroyRef),
            )
            .subscribe(page => {
                const latestEntry = page.entries.at(0) ?? null;
                this.entries.set(page.entries);
                this.latestEntry.set(latestEntry);
                this.summaryPoints.set(page.summary);
                if (this.selectedRange() === 'month') {
                    this.rollingMonthSummaryPoints.set(page.summary);
                }
                this.userHeightCm.set(page.height);
                this.weightGoal.set(page.goal);
                this.weightGoalHistory.set(page.goalHistory);
                this.desiredWeightModel.set({ weight: page.goal.desiredWeight?.toString() ?? '' });
                if (!this.isEditing()) {
                    this.form.weight().value.set(latestEntry?.weight.toString() ?? '');
                }
            });
    }

    private loadWeightGoalHistory(): void {
        this.userService
            .getWeightGoalHistory()
            .pipe(takeUntilDestroyed(this.destroyRef))
            .subscribe(history => {
                this.weightGoalHistory.set(history);
            });
    }

    private loadSummary(filters: WeightEntrySummaryFilters, updateRollingMonth = false): void {
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
                if (updateRollingMonth) {
                    this.rollingMonthSummaryPoints.set(points);
                }
            });
    }

    private loadRollingMonthSummaryIfNeeded(): void {
        if (this.selectedRange() === 'month') {
            return;
        }

        const { summaryParams } = buildWeightHistoryFiltersForRange('month', null);
        this.weightEntriesService
            .getSummary(summaryParams)
            .pipe(takeUntilDestroyed(this.destroyRef))
            .subscribe(points => {
                this.rollingMonthSummaryPoints.set(points);
            });
    }

    private buildPayload(): CreateWeightEntryPayload | null {
        const { date: rawDate, weight: rawWeight } = this.formModel();
        if (rawDate.length === 0 || rawWeight.length === 0) {
            return null;
        }

        const date = new Date(rawDate);
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
        this.form.date().value.set(formatWeightHistoryDateInput(new Date()));
    }

    private handleEntrySaveError(error: unknown): void {
        const responseBody = getRecordProperty(error, 'error');
        const errorCode = getStringProperty(responseBody, 'error');
        const errorKey = errorCode === 'WeightEntry.AlreadyExists' ? 'WEIGHT_HISTORY.ERROR_DUPLICATE_DATE' : 'FORM_ERRORS.UNKNOWN';
        this.entryError.set(this.translate.instant(errorKey));
    }
}
