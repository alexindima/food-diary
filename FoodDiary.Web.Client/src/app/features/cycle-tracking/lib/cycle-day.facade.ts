import { DestroyRef, inject, Injectable, signal } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { disabled, form, max, min, required, validate } from '@angular/forms/signals';
import { finalize, firstValueFrom, switchMap, tap } from 'rxjs';

import {
    BLEEDING_TYPE_BLEEDING,
    CYCLE_FLOW_LIGHT,
    type CycleLogDay,
    type FertilitySignalPayload,
    type UpsertCycleDayPayload,
} from '../../../shared/models/cycle.data';
import { calendarDate } from '../../../shared/models/semantics/date-value';
import { CyclesService } from '../api/cycles.service';
import { buildDayEditModel, buildFertilitySignalPayload, buildSymptomClearCategories, buildSymptomPayload } from './cycle-day.mapper';
import {
    MAX_BASAL_BODY_TEMPERATURE,
    MAX_CERVICAL_FLUID_LENGTH,
    MAX_CYCLE_NOTES_LENGTH,
    MIN_BASAL_BODY_TEMPERATURE,
} from './cycle-tracking.config';
import { createDefaultCycleDayFormModel, type CycleDayFormModel } from './cycle-tracking.form-models';
import { clampCycleSymptom, getCycleNotesLength, toCycleDateKey, toOptionalCycleText } from './cycle-tracking.mapper';
import { CycleTrackingStateFacade } from './cycle-tracking-state.facade';

@Injectable()
export class CycleDayFacade {
    private readonly cyclesService = inject(CyclesService);
    private readonly destroyRef = inject(DestroyRef);

    private readonly state = inject(CycleTrackingStateFacade);
    public readonly dayModel = signal<CycleDayFormModel>(createDefaultCycleDayFormModel());

    private readonly submitDayFormAsync = async (): Promise<void> => {
        await this.saveDayAsync();
    };

    public readonly dayForm = form(
        this.dayModel,
        path => {
            disabled(path, { when: () => this.state.isSavingDay() });
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

    public saveDay(): void {
        void this.saveDayAsync();
    }

    private async saveDayAsync(): Promise<void> {
        const currentCycle = this.state.cycle();
        if (currentCycle === null || currentCycle.id.length === 0 || this.state.isDayMutationPending()) {
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
        const clearSymptomCategories = buildSymptomClearCategories(formValue, this.state.editingDayDate(), this.state.symptoms());
        const fertilitySignal = buildFertilitySignalPayload(formValue);
        const clearFertilitySignal = this.shouldClearFertilitySignal(fertilitySignal);

        this.state.dayError.set(null);
        this.state.isSavingDay.set(true);
        let savedToServer = false;
        try {
            const day = await firstValueFrom(
                this.cyclesService
                    .upsertDay(currentCycle.id, {
                        date: calendarDate(toCycleDateKey(date)),
                        bleeding: this.buildDayBleedingPayload(formValue, notes === undefined),
                        clearBleeding: this.shouldClearBleeding(formValue),
                        symptoms,
                        clearSymptomCategories,
                        fertilitySignal: fertilitySignal === null ? null : { ...fertilitySignal, clearNotes: notes === undefined },
                        clearFertilitySignal,
                        notes,
                        clearNotes: notes === undefined,
                    })
                    .pipe(takeUntilDestroyed(this.destroyRef)),
            );
            savedToServer = true;
            this.applySavedDay(day, clearFertilitySignal);
            await this.refreshPredictionsAsync();
        } catch {
            this.state.dayError.set(savedToServer ? 'CYCLE_TRACKING.DAY_SAVED_REFRESH_FAILED' : 'CYCLE_TRACKING.SAVE_DAY_FAILED');
            return;
        } finally {
            this.state.isSavingDay.set(false);
        }
        this.state.daySaveRevision.update(revision => revision + 1);
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
        const refreshedCycle = await firstValueFrom(this.cyclesService.getCurrent().pipe(takeUntilDestroyed(this.destroyRef)));
        if (refreshedCycle === null) {
            throw new Error('Cycle predictions refresh returned no profile.');
        }

        this.state.cycle.update(current =>
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
        const current = this.state.cycle();
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
        this.state.editingDayDate.set(null);
        this.state.cycle.set(updatedCycle);
        this.state.loadNutritionSummary(updatedCycle);
    }

    private shouldClearBleeding(formValue: CycleDayFormModel): boolean {
        const editingDate = this.state.editingDayDate();
        if (editingDate === null) {
            return false;
        }

        const editingDateKey = toCycleDateKey(editingDate);
        const existingEntries = this.state.bleedingEntries().filter(entry => toCycleDateKey(entry.date) === editingDateKey);
        if (!formValue.isBleeding) {
            return existingEntries.length > 0;
        }

        return existingEntries.some(entry => entry.type !== formValue.bleedingType);
    }

    private shouldClearFertilitySignal(fertilitySignal: FertilitySignalPayload | null): boolean {
        const editingDate = this.state.editingDayDate();
        if (editingDate === null || fertilitySignal !== null) {
            return false;
        }

        const editingDateKey = toCycleDateKey(editingDate);
        return this.state.fertilitySignals().some(fertilitySignalItem => toCycleDateKey(fertilitySignalItem.date) === editingDateKey);
    }

    public editDay(date: string): void {
        const currentCycle = this.state.cycle();
        if (currentCycle === null || this.state.isDayMutationPending()) {
            return;
        }

        const dateKey = toCycleDateKey(date);
        const dayBleeding = currentCycle.bleedingEntries.filter(entry => toCycleDateKey(entry.date) === dateKey);
        const daySymptoms = currentCycle.symptoms.filter(symptom => toCycleDateKey(symptom.date) === dateKey);
        const fertilitySignal = currentCycle.fertilitySignals.find(item => toCycleDateKey(item.date) === dateKey);
        const bleeding = dayBleeding.find(entry => entry.type === BLEEDING_TYPE_BLEEDING) ?? dayBleeding[0];

        const model = buildDayEditModel(date, daySymptoms, bleeding, fertilitySignal);
        const notes = currentCycle.dayNotes?.find(note => toCycleDateKey(note.date) === dateKey)?.notes;
        this.state.dayError.set(null);
        this.dayForm().reset({ ...model, notes: notes ?? model.notes });
        this.state.editingDayDate.set(date);
    }

    public cancelDayEdit(): void {
        if (this.state.isSavingDay()) {
            return;
        }
        this.state.dayError.set(null);
        this.state.editingDayDate.set(null);
        this.dayForm().reset(createDefaultCycleDayFormModel());
    }

    public clearDay(date: string): void {
        const currentCycle = this.state.cycle();
        if (currentCycle === null || currentCycle.id.length === 0 || this.state.isDayMutationPending()) {
            return;
        }

        const dateKey = toCycleDateKey(date);
        if (dateKey.length === 0) {
            return;
        }

        this.state.dayClearError.set(null);
        this.state.clearingDayDate.set(date);
        let clearedOnServer = false;
        this.cyclesService
            .clearDay(currentCycle.id, calendarDate(toCycleDateKey(date)))
            .pipe(takeUntilDestroyed(this.destroyRef))
            .pipe(
                tap(() => {
                    clearedOnServer = true;
                    this.state.cycle.update(current =>
                        current === null
                            ? null
                            : {
                                  ...current,
                                  dayNotes: (current.dayNotes ?? []).filter(note => toCycleDateKey(note.date) !== dateKey),
                                  bleedingEntries: current.bleedingEntries.filter(entry => toCycleDateKey(entry.date) !== dateKey),
                                  symptoms: current.symptoms.filter(symptom => toCycleDateKey(symptom.date) !== dateKey),
                                  fertilitySignals: current.fertilitySignals.filter(entry => toCycleDateKey(entry.date) !== dateKey),
                              },
                    );
                    this.state.loadNutritionSummary(this.state.cycle());
                }),
                switchMap(() => this.cyclesService.getCurrent().pipe(takeUntilDestroyed(this.destroyRef))),
                finalize(() => {
                    this.state.clearingDayDate.set(null);
                }),
                takeUntilDestroyed(this.destroyRef),
            )
            .subscribe({
                next: refreshedCycle => {
                    if (refreshedCycle === null) {
                        this.state.setRefreshError('CYCLE_TRACKING.DAY_CLEARED_REFRESH_FAILED');
                        return;
                    }
                    const current = this.state.cycle();
                    if (current === null) {
                        return;
                    }

                    const updatedCycle = {
                        ...current,
                        menstrualEpisodes: refreshedCycle.menstrualEpisodes,
                        predictions: refreshedCycle.predictions,
                    };
                    this.state.cycle.set(updatedCycle);
                    this.state.setRefreshError(null);
                },
                error: () => {
                    if (clearedOnServer) {
                        this.state.setRefreshError('CYCLE_TRACKING.DAY_CLEARED_REFRESH_FAILED');
                    } else {
                        this.state.dayClearError.set('CYCLE_TRACKING.CLEAR_DAY_FAILED');
                    }
                },
            });
    }

    public confirmPeriodStart(date: string): void {
        const currentCycle = this.state.cycle();
        if (currentCycle === null || currentCycle.id.length === 0 || this.state.isDayMutationPending() || this.state.isEpisodeBusy()) {
            return;
        }

        const dateKey = toCycleDateKey(date);
        if (dateKey.length === 0) {
            return;
        }
        this.state.periodStartError.set(null);
        this.state.confirmingPeriodStartDate.set(date);

        this.cyclesService
            .confirmPeriodStart(currentCycle.id, calendarDate(dateKey))
            .pipe(takeUntilDestroyed(this.destroyRef))
            .pipe(
                finalize(() => {
                    this.state.confirmingPeriodStartDate.set(null);
                }),
                takeUntilDestroyed(this.destroyRef),
            )
            .subscribe({
                next: cycle => {
                    this.state.cycle.set(cycle);
                },
                error: () => {
                    this.state.periodStartError.set('CYCLE_TRACKING.CONFIRM_PERIOD_START_FAILED');
                },
            });
    }
}
