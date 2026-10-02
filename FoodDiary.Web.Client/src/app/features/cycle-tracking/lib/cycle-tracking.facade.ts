import { buildDayEditModel, buildFertilitySignalPayload, buildSymptomClearCategories, buildSymptomPayload } from './cycle-day.mapper';
import { runCycleExport } from './cycle-export.workflow';
import {
    createDefaultCycleDayFormModel,
    type CycleDayFormModel,
    type CycleFactorFormModel,
    type CycleSettingsFormModel,
    type MenstrualEpisodeFormModel,
    type StartCycleFormModel,
} from './cycle-tracking.form-models';
export type { CycleDayFormModel, CycleSettingsFormModel } from './cycle-tracking.form-models';
import { computed, DestroyRef, inject, Injectable, signal } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { disabled, form, max, min, required, validate } from '@angular/forms/signals';
import { finalize, firstValueFrom } from 'rxjs';

import { ExportService } from '../../../shared/api/export.service';
import { formatDateInputValue } from '../../../shared/lib/local-date.utils';
import { getRecordProperty, getStringProperty } from '../../../shared/lib/unknown-value.utils';
import { CyclesService } from '../api/cycles.service';
import {
    BLEEDING_TYPE_BLEEDING,
    type BleedingEntry,
    type CreateCyclePayload,
    CYCLE_CONSENT_PURPOSE_FERTILITY_SIGNALS,
    CYCLE_CONSENT_PURPOSE_NUTRITION_INSIGHTS,
    CYCLE_FACTOR_TYPE_HORMONAL_CONTRACEPTION,
    CYCLE_FLOW_LIGHT,
    CYCLE_REPRODUCTIVE_STATE_CYCLING,
    CYCLE_TRACKING_GOAL_PERIOD_AWARENESS,
    CYCLE_TRACKING_MODE_PERIOD_TRACKING,
    type CycleConsentPurpose,
    type CycleFactor,
    type CycleLogDay,
    type CycleNutritionSummary,
    type CyclePredictions,
    type CycleResponse,
    type CycleSymptomEntry,
    type FertilitySignal,
    type FertilitySignalPayload,
    type MenstrualEpisode,
    type UpdateCycleSettingsPayload,
    type UpsertCycleDayPayload,
    type UpsertCycleFactorPayload,
} from '../models/cycle.data';
import { getCycleFactorStatus } from './cycle-factor-status.utils';
import {
    DEFAULT_AVERAGE_CYCLE_LENGTH,
    DEFAULT_AVERAGE_PERIOD_LENGTH,
    DEFAULT_LUTEAL_LENGTH,
    MAX_AVERAGE_CYCLE_LENGTH,
    MAX_AVERAGE_PERIOD_LENGTH,
    MAX_BASAL_BODY_TEMPERATURE,
    MAX_CERVICAL_FLUID_LENGTH,
    MAX_CYCLE_NOTES_LENGTH,
    MAX_LUTEAL_LENGTH,
    MIN_AVERAGE_CYCLE_LENGTH,
    MIN_AVERAGE_PERIOD_LENGTH,
    MIN_BASAL_BODY_TEMPERATURE,
    MIN_LUTEAL_LENGTH,
} from './cycle-tracking.config';
import { clampCycleSymptom, getCycleNotesLength, toCycleDateKey, toOptionalCycleText } from './cycle-tracking.mapper';

@Injectable()
export class CycleTrackingFacade {
    private readonly cyclesService = inject(CyclesService);
    private readonly exportService = inject(ExportService);
    private readonly destroyRef = inject(DestroyRef);

    public readonly isLoading = signal(false);
    public readonly loadError = signal<string | null>(null);
    public readonly isSavingCycle = signal(false);
    public readonly isSavingSettings = signal(false);
    public readonly isDeletingCycle = signal(false);
    private readonly isSettingsBusy = computed(() => this.isSavingSettings() || this.isDeletingCycle());
    public readonly isSavingDay = signal(false);
    public readonly isSavingFactor = signal(false);
    public readonly isSavingEpisode = signal(false);
    public readonly excludingEpisodeId = signal<string | null>(null);
    public readonly deletingEpisodeId = signal<string | null>(null);
    public readonly isExportingCycle = signal(false);
    public readonly exportError = signal<string | null>(null);
    public readonly daySaveRevision = signal(0);
    public readonly dayError = signal<string | null>(null);
    public readonly settingsSaveRevision = signal(0);
    public readonly settingsError = signal<string | null>(null);
    public readonly factorError = signal<string | null>(null);
    public readonly clearingDayDate = signal<string | null>(null);
    public readonly dayClearError = signal<string | null>(null);
    private readonly isDayMutationPending = computed(() => this.isSavingDay() || this.clearingDayDate() !== null);
    public readonly editingDayDate = signal<string | null>(null);
    public readonly editingFactorId = signal<string | null>(null);
    public readonly editingEpisodeId = signal<string | null>(null);
    public readonly isLoadingNutritionSummary = signal(false);
    public readonly cycle = signal<CycleResponse | null>(null);
    public readonly nutritionSummary = signal<CycleNutritionSummary | null>(null);

    public readonly startCycleModel = signal<StartCycleFormModel>({
        trackingStartDate: formatDateInputValue(new Date()),
        mode: CYCLE_TRACKING_MODE_PERIOD_TRACKING,
        averageCycleLength: DEFAULT_AVERAGE_CYCLE_LENGTH,
        averagePeriodLength: DEFAULT_AVERAGE_PERIOD_LENGTH,
        lutealLength: DEFAULT_LUTEAL_LENGTH,
        isRegular: false,
        showFertilityEstimates: false,
        discreetNotifications: true,
        goal: CYCLE_TRACKING_GOAL_PERIOD_AWARENESS,
        reproductiveState: CYCLE_REPRODUCTIVE_STATE_CYCLING,
        hideFromDashboard: false,
        cycleTrackingConsentGranted: false,
        nutritionInsightsConsentGranted: false,
        fertilitySignalsConsentGranted: false,
    });
    private readonly submitStartCycleFormAsync = async (): Promise<void> => {
        await this.startCycleAsync();
    };
    public readonly startCycleForm = form(
        this.startCycleModel,
        path => {
            required(path.trackingStartDate);
            required(path.mode);
            required(path.goal);
            required(path.reproductiveState);
            required(path.cycleTrackingConsentGranted);
            validate(path.averageCycleLength, ({ value }) =>
                value() === null || Number.isInteger(value()) ? undefined : { kind: 'integer' },
            );
            min(path.averageCycleLength, MIN_AVERAGE_CYCLE_LENGTH);
            max(path.averageCycleLength, MAX_AVERAGE_CYCLE_LENGTH);
            validate(path.averagePeriodLength, ({ value }) =>
                value() === null || Number.isInteger(value()) ? undefined : { kind: 'integer' },
            );
            min(path.averagePeriodLength, MIN_AVERAGE_PERIOD_LENGTH);
            max(path.averagePeriodLength, MAX_AVERAGE_PERIOD_LENGTH);
            validate(path.lutealLength, ({ value }) => (value() === null || Number.isInteger(value()) ? undefined : { kind: 'integer' }));
            min(path.lutealLength, MIN_LUTEAL_LENGTH);
            max(path.lutealLength, MAX_LUTEAL_LENGTH);
        },
        {
            submission: {
                action: this.submitStartCycleFormAsync,
            },
        },
    );

    public readonly settingsModel = signal<CycleSettingsFormModel>({
        mode: CYCLE_TRACKING_MODE_PERIOD_TRACKING,
        averageCycleLength: DEFAULT_AVERAGE_CYCLE_LENGTH,
        averagePeriodLength: DEFAULT_AVERAGE_PERIOD_LENGTH,
        lutealLength: DEFAULT_LUTEAL_LENGTH,
        isRegular: false,
        showFertilityEstimates: false,
        discreetNotifications: true,
        goal: CYCLE_TRACKING_GOAL_PERIOD_AWARENESS,
        reproductiveState: CYCLE_REPRODUCTIVE_STATE_CYCLING,
        hideFromDashboard: false,
        cycleTrackingConsentGranted: true,
        nutritionInsightsConsentGranted: false,
        fertilitySignalsConsentGranted: false,
    });
    private readonly submitSettingsFormAsync = async (): Promise<void> => {
        await this.saveSettingsAsync();
    };
    public readonly settingsForm = form(
        this.settingsModel,
        path => {
            disabled(path, { when: () => this.isSettingsBusy() });
            required(path.mode);
            required(path.goal);
            required(path.reproductiveState);
            validate(path.averageCycleLength, ({ value }) =>
                value() === null || Number.isInteger(value()) ? undefined : { kind: 'integer' },
            );
            required(path.averageCycleLength);
            min(path.averageCycleLength, MIN_AVERAGE_CYCLE_LENGTH);
            max(path.averageCycleLength, MAX_AVERAGE_CYCLE_LENGTH);
            validate(path.averagePeriodLength, ({ value }) =>
                value() === null || Number.isInteger(value()) ? undefined : { kind: 'integer' },
            );
            required(path.averagePeriodLength);
            min(path.averagePeriodLength, MIN_AVERAGE_PERIOD_LENGTH);
            max(path.averagePeriodLength, MAX_AVERAGE_PERIOD_LENGTH);
            validate(path.lutealLength, ({ value }) => (value() === null || Number.isInteger(value()) ? undefined : { kind: 'integer' }));
            required(path.lutealLength);
            min(path.lutealLength, MIN_LUTEAL_LENGTH);
            max(path.lutealLength, MAX_LUTEAL_LENGTH);
        },
        { submission: { action: this.submitSettingsFormAsync } },
    );

    public readonly dayModel = signal<CycleDayFormModel>(createDefaultCycleDayFormModel());
    private readonly submitDayFormAsync = async (): Promise<void> => {
        await this.saveDayAsync();
    };
    public readonly dayForm = form(
        this.dayModel,
        path => {
            disabled(path, { when: () => this.isSavingDay() });
            required(path.date);
            min(path.basalBodyTemperatureCelsius, MIN_BASAL_BODY_TEMPERATURE);
            max(path.basalBodyTemperatureCelsius, MAX_BASAL_BODY_TEMPERATURE);
            validate(path.basalBodyTemperatureCelsius, ({ value }) =>
                value() !== null && !Number.isFinite(value()) ? { kind: 'temperatureRange' } : undefined,
            );
            validate(path.cervicalFluid, context =>
                getCycleNotesLength(context.value()) > MAX_CERVICAL_FLUID_LENGTH ? { kind: 'cervicalFluidTooLong' } : undefined,
            );
            validate(path.notes, context =>
                getCycleNotesLength(context.value()) > MAX_CYCLE_NOTES_LENGTH ? { kind: 'notesTooLong' } : undefined,
            );
        },
        {
            submission: {
                action: this.submitDayFormAsync,
            },
        },
    );

    public readonly factorModel = signal<CycleFactorFormModel>({
        type: CYCLE_FACTOR_TYPE_HORMONAL_CONTRACEPTION,
        startDate: formatDateInputValue(new Date()),
        endDate: null,
        notes: null,
    });
    private readonly submitFactorFormAsync = async (): Promise<void> => {
        await this.saveFactorAsync();
    };
    public readonly factorForm = form(
        this.factorModel,
        path => {
            disabled(path, { when: () => this.isSavingFactor() });
            required(path.type);
            required(path.startDate);
            validate(path.notes, context => {
                // Match the trimmed payload and the server's Unicode whitespace trimming.
                return getCycleNotesLength(context.value()) > MAX_CYCLE_NOTES_LENGTH ? { kind: 'notesTooLong' } : undefined;
            });
            validate(path.endDate, context => {
                const start = context.valueOf(path.startDate);
                const end = context.value();
                if (start === null || end === null || start.length === 0 || end.length === 0) {
                    return;
                }
                return toCycleDateKey(end) < toCycleDateKey(start) ? { kind: 'dateOrder' } : undefined;
            });
        },
        {
            submission: {
                action: this.submitFactorFormAsync,
            },
        },
    );

    public readonly episodeModel = signal<MenstrualEpisodeFormModel>({
        startDate: null,
        endDate: null,
    });
    private readonly submitEpisodeFormAsync = async (): Promise<void> => {
        await this.saveMenstrualEpisodeAsync();
    };
    public readonly episodeForm = form(
        this.episodeModel,
        path => {
            required(path.startDate);
        },
        {
            submission: {
                action: this.submitEpisodeFormAsync,
            },
        },
    );

    public readonly predictions = computed<CyclePredictions | null>(() => this.cycle()?.predictions ?? null);
    public readonly bleedingEntries = computed<BleedingEntry[]>(() => [...(this.cycle()?.bleedingEntries ?? [])]);
    public readonly symptoms = computed<CycleSymptomEntry[]>(() => [...(this.cycle()?.symptoms ?? [])]);
    public readonly factors = computed<CycleFactor[]>(() => [...(this.cycle()?.factors ?? [])]);
    public readonly fertilitySignals = computed<FertilitySignal[]>(() => [...(this.cycle()?.fertilitySignals ?? [])]);
    public readonly menstrualEpisodes = computed<MenstrualEpisode[]>(() =>
        [...(this.cycle()?.menstrualEpisodes ?? [])].sort((left, right) => right.startDate.localeCompare(left.startDate)),
    );
    public readonly fertilityConsentGranted = computed(() => this.hasActiveConsent(this.cycle(), CYCLE_CONSENT_PURPOSE_FERTILITY_SIGNALS));

    public initialize(): void {
        this.loadCycle();
    }

    public cancelSettingsEdit(): void {
        if (this.isSettingsBusy()) {
            return;
        }
        this.settingsError.set(null);
        const cycle = this.cycle();
        if (cycle === null) {
            return;
        }
        this.settingsForm().reset({
            mode: cycle.mode,
            averageCycleLength: cycle.averageCycleLength,
            averagePeriodLength: cycle.averagePeriodLength,
            lutealLength: cycle.lutealLength,
            isRegular: cycle.isRegular,
            showFertilityEstimates: cycle.showFertilityEstimates,
            discreetNotifications: cycle.discreetNotifications,
            goal: cycle.goal,
            reproductiveState: cycle.reproductiveState,
            hideFromDashboard: cycle.hideFromDashboard,
            cycleTrackingConsentGranted: true,
            nutritionInsightsConsentGranted: this.hasActiveConsent(cycle, CYCLE_CONSENT_PURPOSE_NUTRITION_INSIGHTS),
            fertilitySignalsConsentGranted: this.hasActiveConsent(cycle, CYCLE_CONSENT_PURPOSE_FERTILITY_SIGNALS),
        });
    }

    public startCycle(): void {
        void this.startCycleAsync();
    }

    private async startCycleAsync(): Promise<void> {
        if (this.startCycleForm().invalid()) {
            this.startCycleForm().markAsTouched();
            return;
        }

        const formValue = this.startCycleModel();
        if (formValue.trackingStartDate === null || formValue.trackingStartDate.length === 0) {
            return;
        }

        const payload: CreateCyclePayload = {
            trackingStartDate: toCycleDateKey(formValue.trackingStartDate),
            mode: formValue.mode ?? CYCLE_TRACKING_MODE_PERIOD_TRACKING,
            averageCycleLength: formValue.averageCycleLength ?? undefined,
            averagePeriodLength: formValue.averagePeriodLength ?? undefined,
            lutealLength: formValue.lutealLength ?? undefined,
            isRegular: formValue.isRegular,
            isOnboardingComplete: true,
            showFertilityEstimates: formValue.showFertilityEstimates,
            discreetNotifications: formValue.discreetNotifications,
            goal: formValue.goal ?? CYCLE_TRACKING_GOAL_PERIOD_AWARENESS,
            reproductiveState: formValue.reproductiveState ?? CYCLE_REPRODUCTIVE_STATE_CYCLING,
            hideFromDashboard: formValue.hideFromDashboard,
            cycleTrackingConsentGranted: formValue.cycleTrackingConsentGranted,
            nutritionInsightsConsentGranted: formValue.nutritionInsightsConsentGranted,
            fertilitySignalsConsentGranted: formValue.fertilitySignalsConsentGranted,
        };

        this.isSavingCycle.set(true);
        const cycle = await firstValueFrom(
            this.cyclesService.create(payload).pipe(
                finalize(() => {
                    this.isSavingCycle.set(false);
                }),
            ),
        );
        this.cycle.set(cycle);
        this.loadNutritionSummary(cycle);
    }

    private async saveSettingsAsync(): Promise<void> {
        const currentCycle = this.cycle();
        if (this.isSettingsBusy()) {
            return;
        }
        if (currentCycle === null || this.settingsForm().invalid()) {
            this.settingsForm().markAsTouched();
            return;
        }

        const formValue = this.settingsModel();
        const payload = this.buildSettingsPayload(formValue);
        if (payload === null) {
            return;
        }

        this.settingsError.set(null);
        this.isSavingSettings.set(true);
        try {
            let updatedCycle = await firstValueFrom(this.cyclesService.updateSettings(currentCycle.id, payload));
            this.cycle.set(updatedCycle);
            updatedCycle = await this.updateConsentIfChangedAsync(
                updatedCycle,
                CYCLE_CONSENT_PURPOSE_NUTRITION_INSIGHTS,
                formValue.nutritionInsightsConsentGranted,
            );
            this.cycle.set(updatedCycle);
            updatedCycle = await this.updateConsentIfChangedAsync(
                updatedCycle,
                CYCLE_CONSENT_PURPOSE_FERTILITY_SIGNALS,
                formValue.fertilitySignalsConsentGranted,
            );

            this.cycle.set(updatedCycle);
            this.loadNutritionSummary(updatedCycle);
            this.settingsSaveRevision.update(revision => revision + 1);
        } catch {
            this.settingsError.set('CYCLE_TRACKING.SAVE_SETTINGS_FAILED');
            this.loadNutritionSummary(this.cycle());
        } finally {
            this.isSavingSettings.set(false);
        }
    }

    private buildSettingsPayload(formValue: CycleSettingsFormModel): UpdateCycleSettingsPayload | null {
        if (
            formValue.mode === null ||
            formValue.averageCycleLength === null ||
            formValue.averagePeriodLength === null ||
            formValue.lutealLength === null
        ) {
            return null;
        }

        return {
            mode: formValue.mode,
            averageCycleLength: formValue.averageCycleLength,
            averagePeriodLength: formValue.averagePeriodLength,
            lutealLength: formValue.lutealLength,
            isRegular: formValue.isRegular,
            showFertilityEstimates: formValue.showFertilityEstimates,
            discreetNotifications: formValue.discreetNotifications,
            goal: formValue.goal ?? undefined,
            reproductiveState: formValue.reproductiveState ?? undefined,
            hideFromDashboard: formValue.hideFromDashboard,
        };
    }

    public async deleteCycleAsync(): Promise<void> {
        const currentCycle = this.cycle();
        if (currentCycle === null || this.isDeletingCycle() || this.isSavingSettings()) {
            return;
        }

        this.settingsError.set(null);
        this.isDeletingCycle.set(true);
        try {
            await firstValueFrom(this.cyclesService.deleteCycle(currentCycle.id));
            this.cycle.set(null);
            this.nutritionSummary.set(null);
            this.cancelDayEdit();
            this.cancelFactorEdit();
            this.cancelMenstrualEpisodeEdit();
        } catch {
            this.settingsError.set('CYCLE_TRACKING.DELETE_CYCLE_FAILED');
        } finally {
            this.isDeletingCycle.set(false);
        }
    }

    private loadNutritionSummary(cycle: CycleResponse | null): void {
        if (cycle === null) {
            this.nutritionSummary.set(null);
            return;
        }

        this.isLoadingNutritionSummary.set(true);
        this.cyclesService
            .getNutritionSummary(toCycleDateKey(cycle.trackingStartDate), formatDateInputValue(new Date()))
            .pipe(
                finalize(() => {
                    this.isLoadingNutritionSummary.set(false);
                }),
                takeUntilDestroyed(this.destroyRef),
            )
            .subscribe(summary => {
                this.nutritionSummary.set(summary);
            });
    }

    public saveDay(): void {
        void this.saveDayAsync();
    }

    private async saveDayAsync(): Promise<void> {
        const currentCycle = this.cycle();
        if (currentCycle === null || currentCycle.id.length === 0 || this.isDayMutationPending()) {
            return;
        }

        if (this.dayForm().invalid()) {
            this.dayForm().markAsTouched();
            return;
        }

        const formValue = this.dayModel();
        const date = formValue.date;
        if (date === null || date.length === 0) {
            return;
        }

        const notes = toOptionalCycleText(formValue.notes);
        const symptoms = buildSymptomPayload(formValue);
        const clearSymptomCategories = buildSymptomClearCategories(formValue, this.editingDayDate(), this.symptoms());
        const fertilitySignal = buildFertilitySignalPayload(formValue);
        const clearFertilitySignal = this.shouldClearFertilitySignal(fertilitySignal);

        this.dayError.set(null);
        this.isSavingDay.set(true);
        let savedToServer = false;
        try {
            const day = await firstValueFrom(
                this.cyclesService.upsertDay(currentCycle.id, {
                    date: toCycleDateKey(date),
                    bleeding: this.buildDayBleedingPayload(formValue, notes === undefined),
                    clearBleeding: this.shouldClearBleeding(formValue),
                    symptoms,
                    clearSymptomCategories,
                    fertilitySignal: fertilitySignal === null ? null : { ...fertilitySignal, clearNotes: notes === undefined },
                    clearFertilitySignal,
                    notes,
                    clearNotes: notes === undefined,
                }),
            );
            savedToServer = true;
            this.applySavedDay(day, clearFertilitySignal);
            await this.refreshPredictionsAsync();
        } catch {
            this.dayError.set(savedToServer ? 'CYCLE_TRACKING.DAY_SAVED_REFRESH_FAILED' : 'CYCLE_TRACKING.SAVE_DAY_FAILED');
            return;
        } finally {
            this.isSavingDay.set(false);
        }
        this.daySaveRevision.update(revision => revision + 1);
    }

    private buildDayBleedingPayload(formValue: CycleDayFormModel, clearNotes: boolean): UpsertCycleDayPayload['bleeding'] {
        if (!formValue.isBleeding) {
            return null;
        }
        return {
            type: formValue.bleedingType ?? BLEEDING_TYPE_BLEEDING,
            flow: formValue.flow ?? CYCLE_FLOW_LIGHT,
            painImpact: clampCycleSymptom(formValue.pain),
            clearNotes,
        };
    }

    private async refreshPredictionsAsync(): Promise<void> {
        const refreshedCycle = await firstValueFrom(this.cyclesService.getCurrent());
        if (refreshedCycle === null) {
            throw new Error('Cycle predictions refresh returned no profile.');
        }

        this.cycle.update(current =>
            current === null
                ? refreshedCycle
                : {
                      ...current,
                      menstrualEpisodes: refreshedCycle.menstrualEpisodes,
                      predictions: refreshedCycle.predictions,
                  },
        );
    }

    private applySavedDay(day: CycleLogDay, clearFertilitySignal: boolean): void {
        const current = this.cycle();
        if (current === null) {
            return;
        }

        const dayDateKey = toCycleDateKey(day.date);
        const updatedCycle = {
            ...current,
            dayNotes:
                day.notes === undefined
                    ? (current.dayNotes ?? [])
                    : [
                          ...(current.dayNotes ?? []).filter(note => toCycleDateKey(note.date) !== dayDateKey),
                          ...(day.notes === null ? [] : [{ date: day.date, notes: day.notes }]),
                      ],
            bleedingEntries: [
                ...current.bleedingEntries.filter(entry => toCycleDateKey(entry.date) !== dayDateKey),
                ...day.bleedingEntries,
            ],
            symptoms: [...current.symptoms.filter(symptom => toCycleDateKey(symptom.date) !== dayDateKey), ...day.symptoms],
            fertilitySignals:
                day.fertilitySignal === null || day.fertilitySignal === undefined
                    ? clearFertilitySignal
                        ? current.fertilitySignals.filter(fertilitySignal => toCycleDateKey(fertilitySignal.date) !== dayDateKey)
                        : current.fertilitySignals
                    : [
                          ...current.fertilitySignals.filter(fertilitySignal => toCycleDateKey(fertilitySignal.date) !== dayDateKey),
                          day.fertilitySignal,
                      ],
        };
        this.editingDayDate.set(null);
        this.cycle.set(updatedCycle);
        this.loadNutritionSummary(updatedCycle);
    }

    private shouldClearBleeding(formValue: CycleDayFormModel): boolean {
        const editingDate = this.editingDayDate();
        if (editingDate === null) {
            return false;
        }

        const editingDateKey = toCycleDateKey(editingDate);
        const existingEntries = this.bleedingEntries().filter(entry => toCycleDateKey(entry.date) === editingDateKey);
        if (!formValue.isBleeding) {
            return existingEntries.length > 0;
        }

        return existingEntries.some(entry => entry.type !== formValue.bleedingType);
    }

    private shouldClearFertilitySignal(fertilitySignal: FertilitySignalPayload | null): boolean {
        const editingDate = this.editingDayDate();
        if (editingDate === null || fertilitySignal !== null) {
            return false;
        }

        const editingDateKey = toCycleDateKey(editingDate);
        return this.fertilitySignals().some(fertilitySignalItem => toCycleDateKey(fertilitySignalItem.date) === editingDateKey);
    }

    public editDay(date: string): void {
        const currentCycle = this.cycle();
        if (currentCycle === null || this.isDayMutationPending()) {
            return;
        }

        const dateKey = toCycleDateKey(date);
        const dayBleeding = currentCycle.bleedingEntries.filter(entry => toCycleDateKey(entry.date) === dateKey);
        const daySymptoms = currentCycle.symptoms.filter(symptom => toCycleDateKey(symptom.date) === dateKey);
        const fertilitySignal = currentCycle.fertilitySignals.find(item => toCycleDateKey(item.date) === dateKey);
        const bleeding = dayBleeding.find(entry => entry.type === BLEEDING_TYPE_BLEEDING) ?? dayBleeding[0];

        const model = buildDayEditModel(date, daySymptoms, bleeding, fertilitySignal);
        const notes = currentCycle.dayNotes?.find(note => toCycleDateKey(note.date) === dateKey)?.notes;
        this.dayError.set(null);
        this.dayForm().reset({ ...model, notes: notes ?? model.notes });
        this.editingDayDate.set(date);
    }

    public cancelDayEdit(): void {
        if (this.isSavingDay()) {
            return;
        }
        this.dayError.set(null);
        this.editingDayDate.set(null);
        this.dayForm().reset(createDefaultCycleDayFormModel());
    }

    public saveFactor(): void {
        void this.saveFactorAsync();
    }

    private async saveFactorAsync(): Promise<void> {
        const currentCycle = this.cycle();
        if (currentCycle === null || currentCycle.id.length === 0 || this.isSavingFactor()) {
            return;
        }

        if (this.factorForm().invalid()) {
            this.factorForm().markAsTouched();
            return;
        }

        const payload = this.buildFactorPayload(this.factorModel());
        if (payload === null) {
            return;
        }

        this.factorError.set(null);
        this.isSavingFactor.set(true);
        try {
            const cycle = await firstValueFrom(this.cyclesService.upsertFactor(currentCycle.id, payload));
            this.cycle.set(cycle);
            this.resetFactorForm();
            this.loadNutritionSummary(cycle);
        } catch (error: unknown) {
            this.factorError.set(
                getStringProperty(getRecordProperty(error, 'error'), 'error') === 'Cycle.FactorIdentityConflict'
                    ? 'CYCLE_TRACKING.FACTOR_IDENTITY_CONFLICT'
                    : 'CYCLE_TRACKING.SAVE_FACTOR_FAILED',
            );
        } finally {
            this.isSavingFactor.set(false);
        }
    }

    private buildFactorPayload(formValue: CycleFactorFormModel): UpsertCycleFactorPayload | null {
        if (formValue.type === null || formValue.startDate === null || formValue.startDate.length === 0) {
            return null;
        }
        const notes = toOptionalCycleText(formValue.notes);
        const factorId = this.editingFactorId();
        return {
            ...(factorId === null ? {} : { factorId }),
            type: formValue.type,
            startDate: toCycleDateKey(formValue.startDate),
            endDate: formValue.endDate === null || formValue.endDate.length === 0 ? null : toCycleDateKey(formValue.endDate),
            notes,
            clearNotes: this.editingFactorId() !== null && notes === undefined,
        };
    }

    public editFactor(factorId: string): void {
        if (this.isSavingFactor()) {
            return;
        }
        const factor = this.factors().find(item => item.id === factorId);
        if (factor === undefined) {
            return;
        }

        this.factorError.set(null);
        this.factorForm().reset({
            type: factor.type,
            startDate: toCycleDateKey(factor.startDate),
            endDate: factor.endDate === null || factor.endDate === undefined ? null : toCycleDateKey(factor.endDate),
            notes: factor.notes ?? null,
        });
        this.editingFactorId.set(factorId);
    }

    public cancelFactorEdit(): void {
        if (this.isSavingFactor()) {
            return;
        }
        this.factorError.set(null);
        this.resetFactorForm();
    }

    private resetFactorForm(): void {
        this.editingFactorId.set(null);
        this.factorForm().reset({
            type: CYCLE_FACTOR_TYPE_HORMONAL_CONTRACEPTION,
            startDate: formatDateInputValue(new Date()),
            endDate: null,
            notes: null,
        });
    }

    public endFactorToday(factorId: string): void {
        void this.endFactorTodayAsync(factorId);
    }

    public async endFactorTodayAsync(factorId: string): Promise<void> {
        const currentCycle = this.cycle();
        const factor = this.factors().find(item => item.id === factorId);
        const today = formatDateInputValue(new Date());
        if (currentCycle === null || factor === undefined || this.isSavingFactor() || !getCycleFactorStatus(factor, today).canEndToday) {
            return;
        }

        this.factorError.set(null);
        this.isSavingFactor.set(true);
        try {
            const cycle = await firstValueFrom(
                this.cyclesService.upsertFactor(currentCycle.id, {
                    factorId: factor.id,
                    type: factor.type,
                    startDate: toCycleDateKey(factor.startDate),
                    endDate: today,
                    notes: factor.notes ?? undefined,
                    clearNotes: false,
                }),
            );
            this.cycle.set(cycle);
            if (this.editingFactorId() === factorId) {
                this.resetFactorForm();
            }
            this.loadNutritionSummary(cycle);
        } catch {
            this.factorError.set('CYCLE_TRACKING.END_FACTOR_FAILED');
        } finally {
            this.isSavingFactor.set(false);
        }
    }

    public clearDay(date: string): void {
        const currentCycle = this.cycle();
        if (currentCycle === null || currentCycle.id.length === 0 || this.isDayMutationPending()) {
            return;
        }

        const dateKey = toCycleDateKey(date);
        if (dateKey.length === 0) {
            return;
        }

        this.dayClearError.set(null);
        this.clearingDayDate.set(date);
        this.cyclesService
            .clearDay(currentCycle.id, toCycleDateKey(date))
            .pipe(
                finalize(() => {
                    this.clearingDayDate.set(null);
                }),
                takeUntilDestroyed(this.destroyRef),
            )
            .subscribe({
                next: () => {
                    const current = this.cycle();
                    if (current === null) {
                        return;
                    }

                    const updatedCycle = {
                        ...current,
                        dayNotes: (current.dayNotes ?? []).filter(note => toCycleDateKey(note.date) !== dateKey),
                        bleedingEntries: current.bleedingEntries.filter(entry => toCycleDateKey(entry.date) !== dateKey),
                        symptoms: current.symptoms.filter(symptom => toCycleDateKey(symptom.date) !== dateKey),
                        fertilitySignals: current.fertilitySignals.filter(
                            fertilitySignal => toCycleDateKey(fertilitySignal.date) !== dateKey,
                        ),
                    };
                    this.cycle.set(updatedCycle);
                    this.loadNutritionSummary(updatedCycle);
                },
                error: () => {
                    this.dayClearError.set('CYCLE_TRACKING.CLEAR_DAY_FAILED');
                },
            });
    }

    public confirmPeriodStart(date: string): void {
        const currentCycle = this.cycle();
        if (currentCycle === null) {
            return;
        }

        this.cyclesService
            .confirmPeriodStart(currentCycle.id, toCycleDateKey(date))
            .pipe(takeUntilDestroyed(this.destroyRef))
            .subscribe(cycle => {
                this.cycle.set(cycle);
            });
    }

    public editMenstrualEpisode(episodeId: string): void {
        const episode = this.menstrualEpisodes().find(item => item.id === episodeId);
        if (episode === undefined) {
            return;
        }

        this.episodeModel.set({
            startDate: toCycleDateKey(episode.startDate),
            endDate: episode.endDate === undefined || episode.endDate === null ? null : toCycleDateKey(episode.endDate),
        });
        this.editingEpisodeId.set(episodeId);
    }

    public cancelMenstrualEpisodeEdit(): void {
        this.editingEpisodeId.set(null);
        this.episodeModel.set({ startDate: null, endDate: null });
    }

    private async saveMenstrualEpisodeAsync(): Promise<void> {
        const currentCycle = this.cycle();
        const episodeId = this.editingEpisodeId();
        const formValue = this.episodeModel();
        if (
            currentCycle === null ||
            episodeId === null ||
            formValue.startDate === null ||
            formValue.startDate.length === 0 ||
            this.episodeForm().invalid() ||
            this.isSavingEpisode()
        ) {
            this.episodeForm().markAsTouched();
            return;
        }

        this.isSavingEpisode.set(true);
        try {
            const cycle = await firstValueFrom(
                this.cyclesService.updateMenstrualEpisode(currentCycle.id, episodeId, {
                    startDate: toCycleDateKey(formValue.startDate),
                    endDate: formValue.endDate === null || formValue.endDate.length === 0 ? null : toCycleDateKey(formValue.endDate),
                }),
            );
            this.cycle.set(cycle);
            this.cancelMenstrualEpisodeEdit();
        } finally {
            this.isSavingEpisode.set(false);
        }
    }

    public async toggleMenstrualEpisodePredictionAsync(episodeId: string): Promise<void> {
        const currentCycle = this.cycle();
        const episode = this.menstrualEpisodes().find(item => item.id === episodeId);
        if (currentCycle === null || episode?.status !== 1 || this.hasPendingEpisodeAction()) {
            return;
        }

        this.excludingEpisodeId.set(episodeId);
        try {
            const cycle = await firstValueFrom(
                this.cyclesService.updateMenstrualEpisode(currentCycle.id, episodeId, {
                    startDate: toCycleDateKey(episode.startDate),
                    endDate: episode.endDate === undefined || episode.endDate === null ? null : toCycleDateKey(episode.endDate),
                    excludedFromPredictions: !episode.excludedFromPredictions,
                }),
            );
            this.cycle.set(cycle);
        } finally {
            this.excludingEpisodeId.set(null);
        }
    }

    public async deleteMenstrualEpisodeAsync(episodeId: string): Promise<void> {
        const currentCycle = this.cycle();
        if (currentCycle === null || this.hasPendingEpisodeAction()) {
            return;
        }

        this.deletingEpisodeId.set(episodeId);
        try {
            const cycle = await firstValueFrom(this.cyclesService.deleteMenstrualEpisode(currentCycle.id, episodeId));
            this.cycle.set(cycle);
            if (this.editingEpisodeId() === episodeId) {
                this.cancelMenstrualEpisodeEdit();
            }
        } finally {
            this.deletingEpisodeId.set(null);
        }
    }

    private hasPendingEpisodeAction(): boolean {
        return this.excludingEpisodeId() !== null || this.deletingEpisodeId() !== null;
    }

    public exportCycle(): void {
        runCycleExport(
            { cycle: this.cycle(), exporting: this.isExportingCycle, error: this.exportError },
            this.exportService,
            this.destroyRef,
        );
    }

    public exportSensitiveCycle(currentPassword: string): void {
        runCycleExport(
            { cycle: this.cycle(), exporting: this.isExportingCycle, error: this.exportError },
            this.exportService,
            this.destroyRef,
            currentPassword,
        );
    }

    private loadCycle(): void {
        if (this.isLoading()) {
            return;
        }
        this.loadError.set(null);
        this.isLoading.set(true);
        this.cyclesService
            .getCurrent()
            .pipe(
                finalize(() => {
                    this.isLoading.set(false);
                }),
                takeUntilDestroyed(this.destroyRef),
            )
            .subscribe({
                next: cycle => {
                    this.cycle.set(cycle);
                    if (cycle !== null) {
                        this.cancelSettingsEdit();
                    }
                    this.loadNutritionSummary(cycle);
                },
                error: () => {
                    this.loadError.set('CYCLE_TRACKING.LOAD_FAILED');
                },
            });
    }

    private async updateConsentIfChangedAsync(
        cycle: CycleResponse,
        purpose: CycleConsentPurpose,
        granted: boolean,
    ): Promise<CycleResponse> {
        if (this.hasActiveConsent(cycle, purpose) === granted) {
            return cycle;
        }

        return firstValueFrom(this.cyclesService.updateConsent(cycle.id, purpose, { granted }));
    }

    private hasActiveConsent(cycle: CycleResponse | null, purpose: CycleConsentPurpose): boolean {
        return cycle?.consents?.some(consent => consent.purpose === purpose && consent.isActive) ?? false;
    }
}
