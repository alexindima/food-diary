import { computed, inject, Service, signal } from '@angular/core';
import { form, max, min, required } from '@angular/forms/signals';
import { finalize } from 'rxjs';

import { formatDateInputValue } from '../../../shared/lib/local-date.utils';
import { CyclesService } from '../api/cycles.service';
import {
    BLEEDING_TYPE_BLEEDING,
    type BleedingEntry,
    type CreateCyclePayload,
    CYCLE_FLOW_MEDIUM,
    CYCLE_TRACKING_MODE_PERIOD_TRACKING,
    type CyclePredictions,
    type CycleResponse,
    type CycleSymptomEntry,
    type SymptomLogPayload,
} from '../models/cycle.data';
import { CYCLE_SYMPTOM_FIELDS } from '../pages/cycle-tracking-page-lib/cycle-tracking-page.config';
import {
    DEFAULT_AVERAGE_CYCLE_LENGTH,
    DEFAULT_AVERAGE_PERIOD_LENGTH,
    DEFAULT_LUTEAL_LENGTH,
    MAX_AVERAGE_CYCLE_LENGTH,
    MAX_AVERAGE_PERIOD_LENGTH,
    MAX_LUTEAL_LENGTH,
    MAX_SYMPTOM_VALUE,
    MIN_AVERAGE_CYCLE_LENGTH,
    MIN_AVERAGE_PERIOD_LENGTH,
    MIN_LUTEAL_LENGTH,
    MIN_SYMPTOM_VALUE,
} from './cycle-tracking.config';

type StartCycleFormModel = {
    trackingStartDate: string | null;
    averageCycleLength: number | null;
    averagePeriodLength: number | null;
    lutealLength: number | null;
    isRegular: boolean;
    showFertilityEstimates: boolean;
};

export type CycleDayFormModel = {
    date: string | null;
    isBleeding: boolean;
    pain: number;
    mood: number;
    energy: number;
    sleepQuality: number;
    bloating: number;
    headache: number;
    libido: number;
    notes: string | null;
};

@Service()
export class CycleTrackingFacade {
    private readonly cyclesService = inject(CyclesService);

    public readonly isLoading = signal(false);
    public readonly isSavingCycle = signal(false);
    public readonly isSavingDay = signal(false);
    public readonly cycle = signal<CycleResponse | null>(null);

    public readonly startCycleModel = signal<StartCycleFormModel>({
        trackingStartDate: formatDateInputValue(new Date()),
        averageCycleLength: DEFAULT_AVERAGE_CYCLE_LENGTH,
        averagePeriodLength: DEFAULT_AVERAGE_PERIOD_LENGTH,
        lutealLength: DEFAULT_LUTEAL_LENGTH,
        isRegular: false,
        showFertilityEstimates: false,
    });
    public readonly startCycleForm = form(this.startCycleModel, path => {
        required(path.trackingStartDate);
        min(path.averageCycleLength, MIN_AVERAGE_CYCLE_LENGTH);
        max(path.averageCycleLength, MAX_AVERAGE_CYCLE_LENGTH);
        min(path.averagePeriodLength, MIN_AVERAGE_PERIOD_LENGTH);
        max(path.averagePeriodLength, MAX_AVERAGE_PERIOD_LENGTH);
        min(path.lutealLength, MIN_LUTEAL_LENGTH);
        max(path.lutealLength, MAX_LUTEAL_LENGTH);
    });

    public readonly dayModel = signal<CycleDayFormModel>({
        date: formatDateInputValue(new Date()),
        isBleeding: false,
        pain: 0,
        mood: 0,
        energy: 0,
        sleepQuality: 0,
        bloating: 0,
        headache: 0,
        libido: 0,
        notes: null,
    });
    public readonly dayForm = form(this.dayModel, path => {
        required(path.date);
    });

    public readonly predictions = computed<CyclePredictions | null>(() => this.cycle()?.predictions ?? null);
    public readonly bleedingEntries = computed<BleedingEntry[]>(() => [...(this.cycle()?.bleedingEntries ?? [])]);
    public readonly symptoms = computed<CycleSymptomEntry[]>(() => [...(this.cycle()?.symptoms ?? [])]);

    public initialize(): void {
        this.loadCycle();
    }

    public startCycle(): void {
        if (this.startCycleForm().invalid()) {
            this.startCycleForm().markAsTouched();
            return;
        }

        const formValue = this.startCycleModel();
        if (formValue.trackingStartDate === null || formValue.trackingStartDate.length === 0) {
            return;
        }

        const startDate = new Date(formValue.trackingStartDate);
        const payload: CreateCyclePayload = {
            trackingStartDate: startDate.toISOString(),
            mode: CYCLE_TRACKING_MODE_PERIOD_TRACKING,
            averageCycleLength: formValue.averageCycleLength ?? undefined,
            averagePeriodLength: formValue.averagePeriodLength ?? undefined,
            lutealLength: formValue.lutealLength ?? undefined,
            isRegular: formValue.isRegular,
            isOnboardingComplete: true,
            showFertilityEstimates: formValue.showFertilityEstimates,
            discreetNotifications: true,
        };

        this.isSavingCycle.set(true);
        this.cyclesService
            .create(payload)
            .pipe(
                finalize(() => {
                    this.isSavingCycle.set(false);
                }),
            )
            .subscribe(cycle => {
                this.cycle.set(cycle);
            });
    }

    public saveDay(): void {
        const currentCycle = this.cycle();
        if (currentCycle === null || currentCycle.id.length === 0) {
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

        const entryDate = new Date(date);
        const symptoms = this.buildSymptomPayload(formValue);

        this.isSavingDay.set(true);
        this.cyclesService
            .upsertDay(currentCycle.id, {
                date: entryDate.toISOString(),
                bleeding: formValue.isBleeding
                    ? {
                          type: BLEEDING_TYPE_BLEEDING,
                          flow: CYCLE_FLOW_MEDIUM,
                          painImpact: this.clampSymptom(formValue.pain),
                          notes: formValue.notes ?? undefined,
                          clearNotes: false,
                      }
                    : null,
                symptoms,
            })
            .pipe(
                finalize(() => {
                    this.isSavingDay.set(false);
                }),
            )
            .subscribe(day => {
                const current = this.cycle();
                if (current === null) {
                    return;
                }

                const bleedingDates = new Set(day.bleedingEntries.map(entry => entry.date));
                const symptomDates = new Set(day.symptoms.map(symptom => symptom.date));
                this.cycle.set({
                    ...current,
                    bleedingEntries: [...current.bleedingEntries.filter(entry => !bleedingDates.has(entry.date)), ...day.bleedingEntries],
                    symptoms: [...current.symptoms.filter(symptom => !symptomDates.has(symptom.date)), ...day.symptoms],
                    fertilitySignals:
                        day.fertilitySignal === null || day.fertilitySignal === undefined
                            ? current.fertilitySignals
                            : [
                                  ...current.fertilitySignals.filter(fertilitySignal => fertilitySignal.date !== day.fertilitySignal?.date),
                                  day.fertilitySignal,
                              ],
                });
            });
    }

    private buildSymptomPayload(formValue: CycleDayFormModel): SymptomLogPayload[] {
        return CYCLE_SYMPTOM_FIELDS.map(field => ({
            category: field.category,
            intensity: this.clampSymptom(formValue[field.key]),
            tags: [],
            note: null,
            clearNote: false,
        }));
    }

    private loadCycle(): void {
        this.isLoading.set(true);
        this.cyclesService
            .getCurrent()
            .pipe(
                finalize(() => {
                    this.isLoading.set(false);
                }),
            )
            .subscribe(cycle => {
                this.cycle.set(cycle);
            });
    }

    private clampSymptom(value: number | null | undefined): number {
        if (value === null || value === undefined || Number.isNaN(value)) {
            return MIN_SYMPTOM_VALUE;
        }

        return Math.min(MAX_SYMPTOM_VALUE, Math.max(MIN_SYMPTOM_VALUE, value));
    }
}
