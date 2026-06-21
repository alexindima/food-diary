import { computed, DestroyRef, effect, inject, Service, signal } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { form, required, validate } from '@angular/forms/signals';
import { TranslateService } from '@ngx-translate/core';
import { finalize } from 'rxjs';

import { UserService } from '../../../shared/api/user.service';
import { resolveTranslateLanguage } from '../../../shared/i18n/translate-language.utils';
import { compareDatesDesc } from '../../../shared/lib/local-date.utils';
import { parseDecimalInput } from '../../../shared/lib/number.utils';
import { getRecordProperty, getStringProperty } from '../../../shared/lib/unknown-value.utils';
import { WaistEntriesService } from '../api/waist-entries.service';
import type { CreateWaistEntryPayload, WaistEntry, WaistEntrySummaryFilters, WaistEntrySummaryPoint } from '../models/waist-entry.data';
import { MAX_DESIRED_WAIST_CM, MAX_WAIST_CM, MIN_WAIST_CM } from './waist-history.constants';
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
    circumference: string;
};

type DesiredWaistFormModel = {
    circumference: string;
};

type WaistCustomRangeFormModel = {
    range: WaistHistoryCustomRange | null;
};

@Service()
export class WaistHistoryFacade {
    private readonly waistEntriesService = inject(WaistEntriesService);
    private readonly userService = inject(UserService);
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
    public readonly isEditing = signal(false);
    public readonly summaryPoints = signal<WaistEntrySummaryPoint[]>([]);
    public readonly isSummaryLoading = signal(false);
    public readonly customRangeModel = signal<WaistCustomRangeFormModel>({ range: null });
    public readonly customRangeForm = form(this.customRangeModel);
    public readonly desiredWaist = signal<number | null>(null);
    public readonly isDesiredWaistSaving = signal(false);
    public readonly desiredWaistModel = signal<DesiredWaistFormModel>({ circumference: '' });
    public readonly desiredWaistForm = form(this.desiredWaistModel);

    public readonly formModel = signal<WaistEntryFormModel>({
        date: formatWaistHistoryDateInput(new Date()),
        circumference: '',
    });
    private readonly submitWaistEntryFormAsync = async (): Promise<void> => {
        this.submit();
        await Promise.resolve(undefined);
    };
    public readonly form = form(
        this.formModel,
        path => {
            required(path.date);
            required(path.circumference);
            validate(path.circumference, ({ value }) => {
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

    public readonly latestWaist = computed<number | null>(() => {
        const entries = this.entriesDescending();
        return entries.length > 0 ? entries[0].circumference : null;
    });

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

        this.initialized.set(true);
        this.loadUserProfile();
        this.loadDesiredWaist();
        this.loadEntries();
    }

    public submit(): void {
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
        request$
            .pipe(
                finalize(() => {
                    this.isSaving.set(false);
                }),
                takeUntilDestroyed(this.destroyRef),
            )
            .subscribe({
                next: () => {
                    this.loadEntries(false, true);
                    if (editingId !== null) {
                        this.resetEditingState();
                        return;
                    }

                    this.form.circumference().value.set(payload.circumference.toString());
                },
                error: (error: unknown) => {
                    this.handleEntrySaveError(error);
                },
            });
    }

    public startEdit(entry: WaistEntry): void {
        this.isEditing.set(true);
        this.editingEntryId.set(entry.id);
        this.formModel.set({
            date: formatWaistHistoryDateInput(new Date(entry.date)),
            circumference: entry.circumference.toString(),
        });
    }

    public cancelEdit(): void {
        this.resetEditingState();
        const latest = (this.entriesDescending() as Array<WaistEntry | undefined>)[0];
        this.formModel.set({
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
        if (this.desiredWaistForm().invalid()) {
            return;
        }

        const parsedValue = this.parseDesiredWaist();
        if (parsedValue === undefined) {
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
                this.desiredWaistModel.set({ circumference: value?.toString() ?? '' });
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
        return parsedValue === null || parsedValue <= 0 || parsedValue > MAX_DESIRED_WAIST_CM ? undefined : parsedValue;
    }

    private loadEntries(showLoader = true, force = false): void {
        const { entriesParams, summaryParams, rangeKey } = buildWaistHistoryFiltersForRange(
            this.selectedRange(),
            this.customRangeModel().range,
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
                    const latest = [...entries].sort((a, b) => compareDatesDesc(a.date, b.date))[0];
                    this.form.circumference().value.set(latest.circumference.toString());
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
                this.desiredWaistModel.set({ circumference: value?.toString() ?? '' });
            });
    }

    private buildPayload(): CreateWaistEntryPayload | null {
        const { date: rawDate, circumference: rawCircumference } = this.formModel();
        if (rawDate.length === 0 || rawCircumference.length === 0) {
            return null;
        }

        const date = new Date(rawDate);
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
        this.form.date().value.set(formatWaistHistoryDateInput(new Date()));
    }

    private handleEntrySaveError(error: unknown): void {
        const responseBody = getRecordProperty(error, 'error');
        const errorCode = getStringProperty(responseBody, 'error');
        const errorKey = errorCode === 'WaistEntry.AlreadyExists' ? 'WAIST_HISTORY.ERROR_DUPLICATE_DATE' : 'FORM_ERRORS.UNKNOWN';
        this.entryError.set(this.translate.instant(errorKey));
    }
}
