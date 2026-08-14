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
import type { DesiredWaistResponse, WaistGoalHistoryItem } from '../../../shared/models/user.data';
import { NutritionDataInvalidationService } from '../../../shared/state/nutrition-data-invalidation.service';
import { WaistEntriesService } from '../api/waist-entries.service';
import type { CreateWaistEntryPayload, WaistEntry, WaistEntrySummaryFilters, WaistEntrySummaryPoint } from '../models/waist-entry.data';
import { MAX_DESIRED_WAIST_CM, MAX_WAIST_CM, MIN_WAIST_CM, WAIST_HISTORY_ENTRIES_LIMIT_MAX } from './waist-history.constants';
import type { WaistHistoryCustomRange, WaistHistoryDateRange, WaistHistoryRange } from './waist-history.types';
import { buildWaistHistoryChartPoints } from './waist-history-chart.mapper';
import {
    buildDefaultWaistHistoryCustomRange,
    buildWaistHistoryFiltersForRange,
    calculateWaistHistoryRangeDates,
    formatWaistHistoryDateInput,
    isWaistHistoryRange,
    normalizeStartOfDay,
} from './waist-history-range.utils';
import { buildWhtViewModel } from './waist-history-wht.mapper';

type WaistEntryFormModel = {
    date: string;
    circumferenceCm: string;
};

type DesiredWaistFormModel = {
    circumferenceCm: string;
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
    private readonly destroyRef = inject(DestroyRef);

    private readonly defaultRange: WaistHistoryRange = 'month';
    private readonly editingEntryId = signal<string | null>(null);
    private readonly userHeightCm = signal<number | null>(null);
    private readonly initialized = signal(false);
    private lastLoadedRangeKey: string | null = null;

    public readonly selectedRange = signal<WaistHistoryRange>(this.defaultRange);
    public readonly currentRange = computed<WaistHistoryDateRange>(() =>
        calculateWaistHistoryRangeDates(this.selectedRange(), this.customRangeModel().range),
    );
    public readonly entries = signal<WaistEntry[]>([]);
    public readonly isLoading = signal(false);
    public readonly isSaving = signal(false);
    public readonly entryError = signal<string | null>(null);
    public readonly entrySaveVersion = signal(0);
    public readonly isEditing = signal(false);
    public readonly summaryPoints = signal<WaistEntrySummaryPoint[]>([]);
    public readonly rollingMonthSummaryPoints = signal<WaistEntrySummaryPoint[]>([]);
    public readonly isSummaryLoading = signal(false);
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
    public readonly desiredWaistModel = signal<DesiredWaistFormModel>({ circumferenceCm: '' });
    public readonly desiredWaistForm = form(this.desiredWaistModel);

    public readonly formModel = signal<WaistEntryFormModel>({
        date: formatWaistHistoryDateInput(new Date()),
        circumferenceCm: '',
    });
    private readonly submitWaistEntryFormAsync = async (): Promise<void> => {
        await this.submitAsync();
    };
    public readonly form = form(
        this.formModel,
        path => {
            required(path.date);
            required(path.circumferenceCm);
            validate(path.circumferenceCm, ({ value }) => {
                const parsed = parseDecimalInput(value());
                return parsed === null || parsed < MIN_WAIST_CM || parsed > MAX_WAIST_CM
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

            this.form.circumferenceCm().value.set(payload.circumferenceCm.toString());
        } catch (error: unknown) {
            this.handleEntrySaveError(error);
        }
    }

    public startEdit(entry: WaistEntry): void {
        this.isEditing.set(true);
        this.editingEntryId.set(entry.id);
        this.formModel.set({
            date: formatWaistHistoryDateInput(new Date(entry.date)),
            circumferenceCm: entry.circumferenceCm.toString(),
        });
    }

    public cancelEdit(): void {
        this.resetEditingState();
        const latest = (this.entriesDescending() as Array<WaistEntry | undefined>)[0];
        this.formModel.set({
            date: formatWaistHistoryDateInput(new Date()),
            circumferenceCm: latest !== undefined ? latest.circumferenceCm.toString() : '',
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
                this.invalidation.reportBodyMetricMutation();
                this.loadPageSummary(true);
                this.loadRollingMonthSummaryIfNeeded();
                if (this.editingEntryId() === entry.id) {
                    this.resetEditingState();
                }
            });
    }

    public saveDesiredWaist(): void {
        if (this.desiredWaistForm().invalid()) {
            return;
        }

        const parsedValue = this.parseDesiredWaist();
        if (parsedValue === undefined) {
            return;
        }

        this.isDesiredWaistSaving.set(true);
        this.userService
            .updateWaistGoal(parsedValue)
            .pipe(
                finalize(() => {
                    this.isDesiredWaistSaving.set(false);
                }),
                takeUntilDestroyed(this.destroyRef),
            )
            .subscribe(goal => {
                this.invalidation.reportGoalMutation();
                this.waistGoal.set(goal);
                this.desiredWaistModel.set({ circumferenceCm: goal.desiredWaistCm?.toString() ?? '' });
                this.desiredWaistSaveVersion.update(version => version + 1);
                this.loadWaistGoalHistory();
            });
    }

    public cancelWaistGoal(): void {
        this.isDesiredWaistSaving.set(true);
        this.userService
            .updateWaistGoal(null)
            .pipe(
                finalize(() => {
                    this.isDesiredWaistSaving.set(false);
                }),
                takeUntilDestroyed(this.destroyRef),
            )
            .subscribe(goal => {
                this.invalidation.reportGoalMutation();
                this.waistGoal.set(goal);
                this.desiredWaistModel.set({ circumferenceCm: '' });
                this.desiredWaistSaveVersion.update(version => version + 1);
                this.loadWaistGoalHistory();
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
        const rawValue = this.desiredWaistModel().circumferenceCm.trim();
        if (rawValue.length === 0) {
            return null;
        }

        const parsedValue = parseDecimalInput(rawValue);
        return parsedValue === null || parsedValue <= 0 || parsedValue > MAX_DESIRED_WAIST_CM ? undefined : parsedValue;
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
        this.isLoading.set(true);
        this.isSummaryLoading.set(true);
        this.waistEntriesService
            .getPageSummary({ ...summaryParams, entriesLimit: WAIST_HISTORY_ENTRIES_LIMIT_MAX })
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
                this.userHeightCm.set(page.heightCm);
                this.waistGoal.set(page.goal);
                this.waistGoalHistory.set(page.goalHistory);
                this.desiredWaistModel.set({ circumferenceCm: page.goal.desiredWaistCm?.toString() ?? '' });
                if (!this.isEditing()) {
                    this.form.circumferenceCm().value.set(latestEntry?.circumferenceCm.toString() ?? '');
                }
            });
    }

    private loadWaistGoalHistory(): void {
        this.userService
            .getWaistGoalHistory()
            .pipe(takeUntilDestroyed(this.destroyRef))
            .subscribe(history => {
                this.waistGoalHistory.set(history);
            });
    }

    private loadSummary(filters: WaistEntrySummaryFilters, updateRollingMonth = false): void {
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
                if (updateRollingMonth) {
                    this.rollingMonthSummaryPoints.set(points);
                }
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
            .subscribe(points => {
                this.rollingMonthSummaryPoints.set(points);
            });
    }

    private buildPayload(): CreateWaistEntryPayload | null {
        const { date: rawDate, circumferenceCm: rawCircumference } = this.formModel();
        if (rawDate.length === 0 || rawCircumference.length === 0) {
            return null;
        }

        const date = new Date(rawDate);
        const utcDate = normalizeStartOfDay(date);
        const circumferenceCm = Number(rawCircumference);

        return {
            date: utcDate.toISOString(),
            circumferenceCm,
        };
    }

    private resetEditingState(): void {
        this.isEditing.set(false);
        this.editingEntryId.set(null);
        this.form.date().value.set(formatWaistHistoryDateInput(new Date()));
    }

    private handleEntrySaveError(error: unknown): void {
        const responseBody = getRecordProperty(error, 'error');
        const errorCode = getStringProperty(responseBody, 'error');
        const errorKey = errorCode === 'WaistEntry.AlreadyExists' ? 'WAIST_HISTORY.ERROR_DUPLICATE_DATE' : 'FORM_ERRORS.UNKNOWN';
        this.entryError.set(this.translate.instant(errorKey));
    }
}
