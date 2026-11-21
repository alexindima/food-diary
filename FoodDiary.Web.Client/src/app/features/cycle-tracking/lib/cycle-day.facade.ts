import { DestroyRef, inject, Injectable, signal } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { disabled, form, max, min, required, validate } from '@angular/forms/signals';
import { filter, finalize, firstValueFrom, map, Subject, switchMap, takeUntil, tap } from 'rxjs';

import {
    BLEEDING_TYPE_BLEEDING,
    type CycleLogDay,
    type CycleResponse,
    type FertilitySignalPayload,
    type UpsertCycleDayPayload,
} from '../../../shared/models/cycle.data';
import { type CalendarDate, calendarDate } from '../../../shared/models/semantics/date-value';
import type { CycleProfileId } from '../../../shared/models/semantics/entity-id';
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
    private readonly profileChanged = new Subject<void>();
    private profileRevision = 0;
    private draftProfileId: CycleProfileId | null = null;
    public readonly dayModel = signal<CycleDayFormModel>(createDefaultCycleDayFormModel());

    private readonly submitDayFormAsync = async (): Promise<void> => {
        await this.saveDayAsync();
    };

    public readonly dayForm = form(
        this.dayModel,
        path => {
            disabled(path, { when: () => this.state.isSavingDay() });
            // An edit replaces the log for the calendar day already loaded into the form.
            disabled(path.date, { when: () => this.state.editingDayDate() !== null });
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
        const draft = this.prepareDaySave();
        if (draft === null) {
            return;
        }
        const { profileId, formValue, date } = draft;
        const ownerRevision = this.profileRevision;
        const ownsProfile = (cycle: CycleResponse | null): cycle is CycleResponse =>
            this.profileRevision === ownerRevision && cycle?.id === profileId;
        this.state.dayError.set(null);
        this.state.isSavingDay.set(true);
        let savedToServer = false;
        try {
            const payload = this.buildDayPayload(formValue, date);
            const day = await firstValueFrom(
                this.cyclesService.upsertDay(profileId, payload).pipe(takeUntil(this.profileChanged), takeUntilDestroyed(this.destroyRef)),
            );
            const current = this.state.cycle();
            if (!ownsProfile(current)) {
                return;
            }
            savedToServer = true;
            this.applySavedDay(current, day, payload.clearFertilitySignal === true);
            await this.refreshPredictionsAsync(profileId, ownerRevision);
        } catch {
            if (!ownsProfile(this.state.cycle())) {
                return;
            }
            if (!savedToServer) {
                this.state.dayError.set('CYCLE_TRACKING.SAVE_DAY_FAILED');
                return;
            }
            this.state.setRefreshError('CYCLE_TRACKING.DAY_SAVED_REFRESH_FAILED');
        } finally {
            if (this.profileRevision === ownerRevision) {
                this.state.isSavingDay.set(false);
            }
        }
        if (ownsProfile(this.state.cycle())) {
            this.state.daySaveRevision.update(revision => revision + 1);
        }
    }

    private prepareDaySave(): { profileId: CycleProfileId; formValue: CycleDayFormModel; date: string } | null {
        const currentCycle = this.state.cycle();
        if (currentCycle === null || currentCycle.id.length === 0 || this.state.isDayMutationPending()) {
            return null;
        }

        if (this.draftProfileId !== null && this.draftProfileId !== currentCycle.id) {
            return null;
        }

        if (this.dayForm().invalid()) {
            this.dayForm().markAsTouched();
            return null;
        }

        const formValue = this.dayModel();
        const date = formValue.date;
        if (date === null || date.length === 0) {
            return null;
        }

        return { profileId: currentCycle.id, formValue, date };
    }

    private buildDayPayload(formValue: CycleDayFormModel, date: string): UpsertCycleDayPayload {
        const notes = toOptionalCycleText(formValue.notes);
        const fertilitySignal = buildFertilitySignalPayload(formValue);
        return {
            date: calendarDate(toCycleDateKey(date)),
            bleeding: this.buildDayBleedingPayload(formValue, notes === undefined),
            clearBleeding: this.shouldClearBleeding(formValue),
            symptoms: buildSymptomPayload(formValue),
            clearSymptomCategories: buildSymptomClearCategories(formValue, this.state.editingDayDate(), this.state.symptoms()),
            fertilitySignal: fertilitySignal === null ? null : { ...fertilitySignal, clearNotes: notes === undefined },
            clearFertilitySignal: this.shouldClearFertilitySignal(fertilitySignal),
            notes,
            clearNotes: notes === undefined,
        };
    }

    private buildDayBleedingPayload(formValue: CycleDayFormModel, clearNotes: boolean): UpsertCycleDayPayload['bleeding'] {
        if (!formValue.isBleeding) {
            return null;
        }
        return {
            type: formValue.bleedingType,
            flow: formValue.flow,
            painImpact: clampCycleSymptom(formValue.pain),
            clearNotes,
        };
    }

    private async refreshPredictionsAsync(profileId: CycleProfileId, revision: number): Promise<void> {
        const read = this.state.startProfileRead();
        const response = read.response.pipe(takeUntil(this.profileChanged), takeUntilDestroyed(this.destroyRef));
        const refreshedCycle = await firstValueFrom(response).catch((error: unknown) => {
            if (this.state.hasNewerFullRead(read.revision)) {
                return null;
            }
            throw error;
        });
        const current = this.state.cycle();
        if (this.profileRevision !== revision || current?.id !== profileId || this.state.hasNewerFullRead(read.revision)) {
            return;
        }
        if (refreshedCycle?.id !== profileId) {
            throw new Error('Cycle predictions refresh returned a different or missing profile.');
        }

        this.state.cycle.set({
            ...current,
            menstrualEpisodes: refreshedCycle.menstrualEpisodes,
            predictions: refreshedCycle.predictions,
        });
        this.state.setRefreshError(null);
    }

    private applySavedDay(current: CycleResponse, day: CycleLogDay, clearFertilitySignal: boolean): void {
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
        this.state.acceptProfileWrite(updatedCycle);
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

    public beginDayEntry(): void {
        this.resetDayDraft();
        this.draftProfileId = this.state.cycle()?.id ?? null;
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
        this.draftProfileId = currentCycle.id;
        this.state.dayError.set(null);
        this.dayForm().reset({ ...model, notes: notes ?? model.notes });
        this.state.editingDayDate.set(date);
    }

    public cancelDayEdit(): void {
        if (this.state.isSavingDay()) {
            return;
        }
        this.resetDayDraft();
    }

    public clearAfterProfileChange(): void {
        this.profileRevision++;
        this.profileChanged.next();
        this.state.isSavingDay.set(false);
        this.state.dayClearError.set(null);
        this.state.periodStartError.set(null);
        this.resetDayDraft();
    }

    private resetDayDraft(): void {
        // A reset can precede redraw. Keep the prior owner until the next draft opens.
        this.state.dayError.set(null);
        this.state.editingDayDate.set(null);
        this.dayForm().reset(createDefaultCycleDayFormModel());
    }

    public clearDay(date: string): void {
        const target = this.resolveDayMutationTarget(date);
        if (target === null) {
            return;
        }

        const { profileId, dateKey } = target;
        this.state.dayClearError.set(null);
        this.state.clearingDayDate.set(date);
        let clearedOnServer = false;
        let refreshRevision: number | undefined;
        this.cyclesService
            .clearDay(profileId, dateKey)
            .pipe(
                map(() => this.state.cycle()),
                filter((current): current is CycleResponse => current?.id === profileId),
                tap(current => {
                    clearedOnServer = true;
                    const updated = {
                        ...current,
                        dayNotes: (current.dayNotes ?? []).filter(note => toCycleDateKey(note.date) !== dateKey),
                        bleedingEntries: current.bleedingEntries.filter(entry => toCycleDateKey(entry.date) !== dateKey),
                        symptoms: current.symptoms.filter(symptom => toCycleDateKey(symptom.date) !== dateKey),
                        fertilitySignals: current.fertilitySignals.filter(entry => toCycleDateKey(entry.date) !== dateKey),
                    };
                    this.state.acceptProfileWrite(updated);
                    this.state.loadNutritionSummary(updated);
                }),
                switchMap(() => {
                    const read = this.state.startProfileRead();
                    refreshRevision = read.revision;
                    return read.response.pipe(map(cycle => ({ cycle, revision: read.revision })));
                }),
                takeUntil(this.profileChanged),
                finalize(() => {
                    this.state.clearingDayDate.set(null);
                }),
                takeUntilDestroyed(this.destroyRef),
            )
            .subscribe({
                next: ({ cycle: refreshedCycle, revision }) => {
                    const current = this.state.cycle();
                    if (current?.id !== profileId || this.state.hasNewerFullRead(revision)) {
                        return;
                    }
                    if (refreshedCycle?.id !== profileId) {
                        this.state.setRefreshError('CYCLE_TRACKING.DAY_CLEARED_REFRESH_FAILED');
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
                    if (
                        this.state.cycle()?.id !== profileId ||
                        (refreshRevision !== undefined && this.state.hasNewerFullRead(refreshRevision))
                    ) {
                        return;
                    }
                    if (clearedOnServer) {
                        this.state.setRefreshError('CYCLE_TRACKING.DAY_CLEARED_REFRESH_FAILED');
                    } else {
                        this.state.dayClearError.set('CYCLE_TRACKING.CLEAR_DAY_FAILED');
                    }
                },
            });
    }

    public confirmPeriodStart(date: string): void {
        const target = this.resolveDayMutationTarget(date);
        if (target === null || this.state.isEpisodeBusy()) {
            return;
        }

        const { profileId, dateKey } = target;
        this.state.periodStartError.set(null);
        this.state.confirmingPeriodStartDate.set(date);

        this.cyclesService
            .confirmPeriodStart(profileId, dateKey)
            .pipe(
                takeUntil(this.profileChanged),
                finalize(() => {
                    this.state.confirmingPeriodStartDate.set(null);
                }),
                takeUntilDestroyed(this.destroyRef),
            )
            .subscribe({
                next: cycle => {
                    if (this.state.cycle()?.id === profileId) {
                        this.state.acceptProfileWrite(cycle);
                    }
                },
                error: () => {
                    if (this.state.cycle()?.id === profileId) {
                        this.state.periodStartError.set('CYCLE_TRACKING.CONFIRM_PERIOD_START_FAILED');
                    }
                },
            });
    }

    private resolveDayMutationTarget(date: string): { profileId: CycleProfileId; dateKey: CalendarDate } | null {
        const cycle = this.state.cycle();
        if (cycle === null || cycle.id.length === 0 || this.state.isDayMutationPending()) {
            return null;
        }

        const dateKey = toCycleDateKey(date);
        return dateKey.length === 0 ? null : { profileId: cycle.id, dateKey: calendarDate(dateKey) };
    }
}
