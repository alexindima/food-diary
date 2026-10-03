import { computed, DestroyRef, effect, inject, Injectable, signal } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { disabled, form, required, validate } from '@angular/forms/signals';
import { TranslateService } from '@ngx-translate/core';
import { finalize, firstValueFrom } from 'rxjs';

import { UserService } from '../../../shared/api/user.service';
import { resolveTranslateLanguage } from '../../../shared/i18n/translate-language.utils';
import { compareDatesDesc } from '../../../shared/lib/local-date.utils';
import { toMeasurementDateIso } from '../../../shared/lib/measurement-date.utils';
import { parseDecimalInput } from '../../../shared/lib/number.utils';
import { getRecordProperty, getStringProperty } from '../../../shared/lib/unknown-value.utils';
import { RECENT_MEASUREMENT_FETCH_LIMIT } from '../../../shared/measurements/measurement-history.constants';
import { type MeasurementSystem, MeasurementSystemService } from '../../../shared/measurements/measurement-system.service';
import type { DesiredWaistResponse, WaistGoalHistoryItem } from '../../../shared/models/user.data';
import type {
    CreateWaistEntryPayload,
    WaistEntry,
    WaistEntrySummaryFilters,
    WaistEntrySummaryPoint,
} from '../../../shared/models/waist-entry.data';
import { NutritionDataInvalidationService } from '../../../shared/state/nutrition-data-invalidation.service';
import { WaistEntriesService } from '../api/waist-entries.service';
import { MAX_DESIRED_WAIST_CM, MAX_WAIST_CM, MIN_WAIST_CM, WAIST_INPUT_FRACTION_DIGITS } from './waist-history.constants';
import type { WaistHistoryCustomRange, WaistHistoryDateRange, WaistHistoryRange } from './waist-history.types';
import { buildWaistHistoryChartPoints } from './waist-history-chart.mapper';
import {
    buildDefaultWaistHistoryCustomRange,
    buildWaistHistoryFiltersForRange,
    calculateWaistHistoryRangeDates,
    formatWaistHistoryDateInput,
    isWaistHistoryRange,
} from './waist-history-range.utils';
import { buildWhtViewModel } from './waist-history-wht.mapper';

type WaistEntryFormModel = {
    date: string;
    circumference: string;
};

type DesiredWaistFormModel = {
    circumference: string;
};

type WaistCustomRangeFormModel = {
    range: WaistHistoryCustomRange | null;
};

@Injectable()
export class WaistHistoryFacade {
    private readonly waistEntriesService = inject(WaistEntriesService);
    private readonly userService = inject(UserService);
    private readonly invalidation = inject(NutritionDataInvalidationService);
    private readonly translate = inject(TranslateService);
    private readonly measurements = inject(MeasurementSystemService);
    private readonly destroyRef = inject(DestroyRef);

    private readonly defaultRange: WaistHistoryRange = 'month';
    private readonly editingEntry = signal<WaistEntry | null>(null);
    private readonly editingEntryId = signal<string | null>(null);
    private readonly userHeightCm = signal<number | null>(null);
    private readonly initialized = signal(false);
    private lastLoadedRangeKey: string | null = null;
    private currentMeasurementSystem: MeasurementSystem = this.measurements.system();

    public readonly selectedRange = signal<WaistHistoryRange>(this.defaultRange);
    public readonly currentRange = computed<WaistHistoryDateRange>(() =>
        calculateWaistHistoryRangeDates(this.selectedRange(), this.customRangeModel().range),
    );
    public readonly entries = signal<WaistEntry[]>([]);
    public readonly isLoading = signal(false);
    public readonly isSaving = signal(false);
    public readonly entryError = signal<string | null>(null);
    public readonly deleteError = signal<string | null>(null);
    public readonly goalActionError = signal<string | null>(null);
    public readonly entrySaveVersion = signal(0);
    public readonly isEditing = signal(false);
    public readonly summaryPoints = signal<WaistEntrySummaryPoint[]>([]);
    public readonly rollingMonthSummaryPoints = signal<WaistEntrySummaryPoint[]>([]);
    public readonly isSummaryLoading = signal(false);
    public readonly pageLoadError = signal(false);
    public readonly summaryLoadError = signal(false);
    public readonly customRangeModel = signal<WaistCustomRangeFormModel>({ range: null });
    public readonly customRangeForm = form(this.customRangeModel);
    public readonly waistGoal = signal<DesiredWaistResponse>({ desiredWaistCm: null, startWaistCm: null, startedAtUtc: null });
    public readonly desiredWaistCm = computed(() => this.waistGoal().desiredWaistCm);
    public readonly waistGoalHistory = signal<WaistGoalHistoryItem[]>([]);
    public readonly hasCompletedWaistGoals = computed(() => this.waistGoalHistory().some(goal => goal.status !== 'Active'));
    public readonly lastCompletedWaistGoal = computed(() => this.waistGoalHistory().find(goal => goal.status !== 'Active') ?? null);
    public readonly isDesiredWaistSaving = signal(false);
    public readonly desiredWaistSaveVersion = signal(0);
    public readonly latestEntry = signal<WaistEntry | null>(null);
    public readonly desiredWaistModel = signal<DesiredWaistFormModel>({ circumference: '' });
    public readonly desiredWaistForm = form(this.desiredWaistModel, path => {
        disabled(path.circumference, { when: () => this.isDesiredWaistSaving() });
        validate(path.circumference, ({ value }) => {
            if (value().trim().length === 0) {
                return;
            }
            const parsed = parseDecimalInput(value());
            const target = parsed === null ? null : this.measurements.canonicalLength(parsed);
            return target === null || target <= 0 || target > MAX_DESIRED_WAIST_CM
                ? { kind: 'goalRange', message: 'Goal is out of range' }
                : undefined;
        });
    });

    public readonly formModel = signal<WaistEntryFormModel>({
        date: formatWaistHistoryDateInput(new Date()),
        circumference: '',
    });
    private readonly submitWaistEntryFormAsync = async (): Promise<void> => {
        await this.submitAsync();
    };
    public readonly form = form(
        this.formModel,
        path => {
            disabled(path.date, { when: () => this.isSaving() });
            disabled(path.circumference, { when: () => this.isSaving() });
            required(path.date);
            required(path.circumference);
            validate(path.circumference, ({ value }) => {
                const parsed = parseDecimalInput(value());
                const circumferenceCm = parsed === null ? null : this.measurements.canonicalLength(parsed);
                return circumferenceCm === null || circumferenceCm < MIN_WAIST_CM || circumferenceCm > MAX_WAIST_CM
                    ? { kind: 'waistRange', message: 'Waist circumference is out of range' }
                    : undefined;
            });
        },
        {
            submission: {
                action: this.submitWaistEntryFormAsync,
            },
        },
    );

    public readonly entriesDescending = computed(() => [...this.entries()].sort((a, b) => compareDatesDesc(a.date, b.date)));

    public readonly chartPoints = computed(() =>
        buildWaistHistoryChartPoints(this.summaryPoints(), resolveTranslateLanguage(this.translate)),
    );

    public readonly latestWaist = computed<number | null>(() => this.latestEntry()?.circumferenceCm ?? null);
    public readonly latestWaistDate = computed<string | null>(() => this.latestEntry()?.date ?? null);

    public readonly whtViewModel = computed(() => buildWhtViewModel(this.userHeightCm(), this.latestWaist()));

    public constructor() {
        effect(() => {
            const system = this.measurements.system();
            if (system === this.currentMeasurementSystem) {
                return;
            }

            this.currentMeasurementSystem = system;
            this.syncDisplayForms();
        });

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
        if (this.isSaving()) {
            return;
        }
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
            editingId !== null ? this.waistEntriesService.update(editingId, payload) : this.waistEntriesService.create(payload);

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

            this.form.circumference().value.set(this.formatDisplayWaist(payload.circumferenceCm));
        } catch (error: unknown) {
            this.handleEntrySaveError(error);
        }
    }

    public startEdit(entry: WaistEntry): void {
        this.isEditing.set(true);
        this.editingEntryId.set(entry.id);
        this.editingEntry.set(entry);
        this.formModel.set({
            date: entry.date.split('T')[0] ?? '',
            circumference: this.formatDisplayWaist(entry.circumferenceCm),
        });
    }

    public cancelEdit(): void {
        this.resetEditingState();
        const latest = (this.entriesDescending() as Array<WaistEntry | undefined>)[0];
        this.formModel.set({
            date: formatWaistHistoryDateInput(new Date()),
            circumference: this.formatDisplayWaist(latest?.circumferenceCm ?? null),
        });
    }

    public deleteEntry(entry: WaistEntry): void {
        if (this.isSaving()) {
            return;
        }
        this.isSaving.set(true);
        this.deleteError.set(null);
        this.waistEntriesService
            .remove(entry.id)
            .pipe(
                finalize(() => {
                    this.isSaving.set(false);
                }),
                takeUntilDestroyed(this.destroyRef),
            )
            .subscribe({
                next: () => {
                    this.invalidation.reportBodyMetricMutation();
                    this.loadPageSummary(true);
                    this.loadRollingMonthSummaryIfNeeded();
                    if (this.editingEntryId() === entry.id) {
                        this.resetEditingState();
                    }
                },
                error: () => {
                    this.deleteError.set(this.translate.instant('WAIST_HISTORY.ERROR_DELETE_ENTRY'));
                },
            });
    }

    public saveDesiredWaist(): void {
        if (this.isDesiredWaistSaving()) {
            return;
        }
        if (this.desiredWaistForm().invalid()) {
            return;
        }

        const parsedValue = this.parseDesiredWaist();
        if (parsedValue === undefined) {
            return;
        }

        this.isDesiredWaistSaving.set(true);
        this.goalActionError.set(null);
        this.userService
            .updateWaistGoal(parsedValue)
            .pipe(
                finalize(() => {
                    this.isDesiredWaistSaving.set(false);
                }),
                takeUntilDestroyed(this.destroyRef),
            )
            .subscribe({
                next: goal => {
                    this.invalidation.reportGoalMutation();
                    this.waistGoal.set(goal);
                    this.desiredWaistModel.set({ circumference: this.formatDisplayWaist(goal.desiredWaistCm) });
                    this.desiredWaistSaveVersion.update(version => version + 1);
                    this.loadPageSummary(true);
                },
                error: () => {
                    this.goalActionError.set(this.translate.instant('WAIST_HISTORY.ERROR_SAVE_GOAL'));
                },
            });
    }

    public cancelWaistGoal(): void {
        if (this.isDesiredWaistSaving()) {
            return;
        }
        this.isDesiredWaistSaving.set(true);
        this.goalActionError.set(null);
        this.userService
            .updateWaistGoal(null)
            .pipe(
                finalize(() => {
                    this.isDesiredWaistSaving.set(false);
                }),
                takeUntilDestroyed(this.destroyRef),
            )
            .subscribe({
                next: goal => {
                    this.invalidation.reportGoalMutation();
                    this.waistGoal.set(goal);
                    this.desiredWaistModel.set({ circumference: '' });
                    this.desiredWaistSaveVersion.update(version => version + 1);
                    this.loadPageSummary(true);
                },
                error: () => {
                    this.goalActionError.set(this.translate.instant('WAIST_HISTORY.ERROR_CANCEL_GOAL'));
                },
            });
    }

    public changeRange(value: string): void {
        if (!isWaistHistoryRange(value) || value === this.selectedRange()) {
            return;
        }

        this.selectedRange.set(value);

        if (value === 'custom') {
            const current = this.customRangeModel().range;
            if (current?.start === undefined || current.start === null || current.end === null) {
                this.customRangeModel.set({ range: buildDefaultWaistHistoryCustomRange() });
            }
            return;
        }

        this.customRangeModel.set({ range: null });
    }

    private parseDesiredWaist(): number | null | undefined {
        const rawValue = this.desiredWaistModel().circumference.trim();
        if (rawValue.length === 0) {
            return null;
        }

        const parsedValue = parseDecimalInput(rawValue);
        const circumferenceCm = parsedValue === null ? null : this.measurements.canonicalLength(parsedValue);
        return circumferenceCm === null || circumferenceCm <= 0 || circumferenceCm > MAX_DESIRED_WAIST_CM ? undefined : circumferenceCm;
    }

    public retryPageLoad(): void {
        if (this.isLoading()) {
            return;
        }
        this.loadPageSummary(true);
        this.loadRollingMonthSummaryIfNeeded();
    }

    public retrySummaryLoad(): void {
        if (!this.isSummaryLoading()) {
            this.loadEntries(true);
        }
    }

    private loadEntries(force = false): void {
        const { summaryParams, rangeKey } = buildWaistHistoryFiltersForRange(this.selectedRange(), this.customRangeModel().range);

        if (!force && rangeKey === this.lastLoadedRangeKey) {
            return;
        }

        this.lastLoadedRangeKey = rangeKey;
        this.loadSummary(summaryParams, this.selectedRange() === 'month');
    }

    private loadPageSummary(force = false): void {
        const { summaryParams, rangeKey } = buildWaistHistoryFiltersForRange(this.selectedRange(), this.customRangeModel().range);
        if (!force && rangeKey === this.lastLoadedRangeKey) {
            return;
        }

        this.lastLoadedRangeKey = rangeKey;
        this.pageLoadError.set(false);
        this.summaryLoadError.set(false);
        this.isLoading.set(true);
        this.isSummaryLoading.set(true);
        this.waistEntriesService
            .getPageSummary({ ...summaryParams, entriesLimit: RECENT_MEASUREMENT_FETCH_LIMIT })
            .pipe(
                finalize(() => {
                    this.isLoading.set(false);
                    this.isSummaryLoading.set(false);
                }),
                takeUntilDestroyed(this.destroyRef),
            )
            .subscribe({
                next: page => {
                    const latestEntry = page.entries.at(0) ?? null;
                    this.entries.set(page.entries);
                    this.latestEntry.set(latestEntry);
                    this.summaryPoints.set(page.summary);
                    if (this.selectedRange() === 'month') {
                        this.rollingMonthSummaryPoints.set(page.summary);
                    }
                    this.userHeightCm.set(page.heightCm);
                    this.waistGoal.set(page.goal);
                    this.waistGoalHistory.set(page.goalHistory);
                    this.desiredWaistModel.set({ circumference: this.formatDisplayWaist(page.goal.desiredWaistCm) });
                    if (!this.isEditing()) {
                        this.form.circumference().value.set(this.formatDisplayWaist(latestEntry?.circumferenceCm ?? null));
                    }
                },
                error: () => {
                    this.pageLoadError.set(true);
                },
            });
    }

    public getEntryHistoryPage(dateTo?: string): ReturnType<WaistEntriesService['getHistoryPage']> {
        return this.waistEntriesService.getHistoryPage(dateTo);
    }

    public getGoalHistoryPage(cursor?: string): ReturnType<UserService['getWaistGoalHistoryPage']> {
        return this.userService.getWaistGoalHistoryPage(cursor);
    }

    private loadSummary(filters: WaistEntrySummaryFilters, updateRollingMonth = false): void {
        this.summaryLoadError.set(false);
        this.isSummaryLoading.set(true);
        this.waistEntriesService
            .getSummary(filters)
            .pipe(
                finalize(() => {
                    this.isSummaryLoading.set(false);
                }),
                takeUntilDestroyed(this.destroyRef),
            )
            .subscribe({
                next: points => {
                    this.summaryPoints.set(points);
                    if (updateRollingMonth) {
                        this.rollingMonthSummaryPoints.set(points);
                    }
                },
                error: () => {
                    this.summaryPoints.set([]);
                    this.summaryLoadError.set(true);
                },
            });
    }

    private loadRollingMonthSummaryIfNeeded(): void {
        if (this.selectedRange() === 'month') {
            return;
        }

        const { summaryParams } = buildWaistHistoryFiltersForRange('month', null);
        this.waistEntriesService
            .getSummary(summaryParams)
            .pipe(takeUntilDestroyed(this.destroyRef))
            .subscribe({
                next: points => {
                    this.rollingMonthSummaryPoints.set(points);
                },
                error: () => {
                    this.pageLoadError.set(true);
                },
            });
    }

    private buildPayload(): CreateWaistEntryPayload | null {
        const { date: rawDate, circumference: rawCircumference } = this.formModel();
        if (rawDate.length === 0 || rawCircumference.length === 0) {
            return null;
        }

        const date = toMeasurementDateIso(rawDate);
        const parsedValue = parseDecimalInput(rawCircumference);
        if (date === null || parsedValue === null) {
            return null;
        }
        const original = this.editingEntry();
        const circumferenceCm =
            original !== null && parsedValue === Number(this.formatDisplayWaist(original.circumferenceCm))
                ? original.circumferenceCm
                : this.measurements.canonicalLength(parsedValue);

        return {
            date,
            circumferenceCm,
        };
    }

    private resetEditingState(): void {
        this.isEditing.set(false);
        this.editingEntryId.set(null);
        this.editingEntry.set(null);
        this.form.date().value.set(formatWaistHistoryDateInput(new Date()));
    }

    private syncDisplayForms(): void {
        const editingEntry = this.editingEntry();
        const circumferenceCm = editingEntry?.circumferenceCm ?? this.latestWaist();
        this.form.circumference().value.set(this.formatDisplayWaist(circumferenceCm));
        this.desiredWaistModel.set({ circumference: this.formatDisplayWaist(this.desiredWaistCm()) });
    }

    private formatDisplayWaist(circumferenceCm: number | null): string {
        if (circumferenceCm === null) {
            return '';
        }
        return this.measurements.system() === 'metric'
            ? circumferenceCm.toString()
            : this.measurements.displayLength(circumferenceCm, WAIST_INPUT_FRACTION_DIGITS).toString();
    }

    private handleEntrySaveError(error: unknown): void {
        const responseBody = getRecordProperty(error, 'error');
        const errorCode = getStringProperty(responseBody, 'error');
        const errorKey = errorCode === 'WaistEntry.AlreadyExists' ? 'WAIST_HISTORY.ERROR_DUPLICATE_DATE' : 'WAIST_HISTORY.ERROR_SAVE_ENTRY';
        this.entryError.set(this.translate.instant(errorKey));
    }
}
