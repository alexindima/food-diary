import { computed, DestroyRef, inject, Service, signal } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { form, max, min, required } from '@angular/forms/signals';
import { finalize } from 'rxjs';

import { ExportService } from '../../../shared/api/export.service';
import { formatDateInputValue } from '../../../shared/lib/local-date.utils';
import { CyclesService } from '../api/cycles.service';
import {
    BLEEDING_TYPE_BLEEDING,
    type BleedingEntry,
    type BleedingType,
    type CreateCyclePayload,
    CYCLE_FACTOR_TYPE_HORMONAL_CONTRACEPTION,
    CYCLE_FLOW_LIGHT,
    CYCLE_FLOW_MEDIUM,
    CYCLE_TRACKING_MODE_PERIOD_TRACKING,
    type CycleFactor,
    type CycleFactorType,
    type CycleFlowLevel,
    type CycleNutritionSummary,
    type CyclePredictions,
    type CycleResponse,
    type CycleSymptomEntry,
    type CycleTrackingMode,
    type FertilitySignal,
    type FertilitySignalPayload,
    OVULATION_TEST_RESULT_UNKNOWN,
    type OvulationTestResult,
    type SymptomLogPayload,
    type UpsertCycleFactorPayload,
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
    mode: CycleTrackingMode | null;
    averageCycleLength: number | null;
    averagePeriodLength: number | null;
    lutealLength: number | null;
    isRegular: boolean;
    showFertilityEstimates: boolean;
    discreetNotifications: boolean;
};

export type CycleDayFormModel = {
    date: string | null;
    isBleeding: boolean;
    bleedingType: BleedingType | null;
    flow: CycleFlowLevel | null;
    pain: number;
    mood: number;
    energy: number;
    sleepQuality: number;
    bloating: number;
    headache: number;
    libido: number;
    basalBodyTemperatureCelsius: number | null;
    ovulationTestResult: OvulationTestResult | null;
    cervicalFluid: string | null;
    hadSex: boolean;
    notes: string | null;
};

type CycleFactorFormModel = {
    type: CycleFactorType | null;
    startDate: string | null;
    endDate: string | null;
    notes: string | null;
};

const DAY_END_HOURS = 23;
const DAY_END_MINUTES = 59;
const DAY_END_SECONDS = 59;
const DAY_END_MILLISECONDS = 999;
const ISO_DATE_KEY_LENGTH = 10;

@Service()
export class CycleTrackingFacade {
    private readonly cyclesService = inject(CyclesService);
    private readonly exportService = inject(ExportService);
    private readonly destroyRef = inject(DestroyRef);

    public readonly isLoading = signal(false);
    public readonly isSavingCycle = signal(false);
    public readonly isSavingDay = signal(false);
    public readonly isSavingFactor = signal(false);
    public readonly isExportingCycle = signal(false);
    public readonly clearingDayDate = signal<string | null>(null);
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
    });
    public readonly startCycleForm = form(this.startCycleModel, path => {
        required(path.trackingStartDate);
        required(path.mode);
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
        bleedingType: BLEEDING_TYPE_BLEEDING,
        flow: CYCLE_FLOW_MEDIUM,
        pain: 0,
        mood: 0,
        energy: 0,
        sleepQuality: 0,
        bloating: 0,
        headache: 0,
        libido: 0,
        basalBodyTemperatureCelsius: null,
        ovulationTestResult: null,
        cervicalFluid: null,
        hadSex: false,
        notes: null,
    });
    public readonly dayForm = form(this.dayModel, path => {
        required(path.date);
    });

    public readonly factorModel = signal<CycleFactorFormModel>({
        type: CYCLE_FACTOR_TYPE_HORMONAL_CONTRACEPTION,
        startDate: formatDateInputValue(new Date()),
        endDate: null,
        notes: null,
    });
    public readonly factorForm = form(this.factorModel, path => {
        required(path.type);
        required(path.startDate);
    });

    public readonly predictions = computed<CyclePredictions | null>(() => this.cycle()?.predictions ?? null);
    public readonly bleedingEntries = computed<BleedingEntry[]>(() => [...(this.cycle()?.bleedingEntries ?? [])]);
    public readonly symptoms = computed<CycleSymptomEntry[]>(() => [...(this.cycle()?.symptoms ?? [])]);
    public readonly factors = computed<CycleFactor[]>(() => [...(this.cycle()?.factors ?? [])]);
    public readonly fertilitySignals = computed<FertilitySignal[]>(() => [...(this.cycle()?.fertilitySignals ?? [])]);

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
            mode: formValue.mode ?? CYCLE_TRACKING_MODE_PERIOD_TRACKING,
            averageCycleLength: formValue.averageCycleLength ?? undefined,
            averagePeriodLength: formValue.averagePeriodLength ?? undefined,
            lutealLength: formValue.lutealLength ?? undefined,
            isRegular: formValue.isRegular,
            isOnboardingComplete: true,
            showFertilityEstimates: formValue.showFertilityEstimates,
            discreetNotifications: formValue.discreetNotifications,
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
                this.loadNutritionSummary(cycle);
            });
    }

    private loadNutritionSummary(cycle: CycleResponse | null): void {
        if (cycle === null) {
            this.nutritionSummary.set(null);
            return;
        }

        this.isLoadingNutritionSummary.set(true);
        this.cyclesService
            .getNutritionSummary(
                this.normalizeStartOfDay(new Date(cycle.trackingStartDate)).toISOString(),
                this.normalizeEndOfDay(new Date()).toISOString(),
            )
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
                          type: formValue.bleedingType ?? BLEEDING_TYPE_BLEEDING,
                          flow: formValue.flow ?? CYCLE_FLOW_LIGHT,
                          painImpact: this.clampSymptom(formValue.pain),
                          notes: formValue.notes ?? undefined,
                          clearNotes: false,
                      }
                    : null,
                symptoms,
                fertilitySignal: this.buildFertilitySignalPayload(formValue),
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
                const updatedCycle = {
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
                };
                this.cycle.set(updatedCycle);
                this.loadNutritionSummary(updatedCycle);
            });
    }

    public saveFactor(): void {
        const currentCycle = this.cycle();
        if (currentCycle === null || currentCycle.id.length === 0) {
            return;
        }

        if (this.factorForm().invalid()) {
            this.factorForm().markAsTouched();
            return;
        }

        const formValue = this.factorModel();
        if (formValue.type === null || formValue.startDate === null || formValue.startDate.length === 0) {
            return;
        }

        const payload: UpsertCycleFactorPayload = {
            type: formValue.type,
            startDate: new Date(formValue.startDate).toISOString(),
            endDate: formValue.endDate === null || formValue.endDate.length === 0 ? null : new Date(formValue.endDate).toISOString(),
            notes: this.toOptionalText(formValue.notes),
            clearNotes: false,
        };

        this.isSavingFactor.set(true);
        this.cyclesService
            .upsertFactor(currentCycle.id, payload)
            .pipe(
                finalize(() => {
                    this.isSavingFactor.set(false);
                }),
            )
            .subscribe(cycle => {
                this.cycle.set(cycle);
                this.loadNutritionSummary(cycle);
            });
    }

    public clearDay(date: string): void {
        const currentCycle = this.cycle();
        if (currentCycle === null || currentCycle.id.length === 0 || this.clearingDayDate() !== null) {
            return;
        }

        const dateKey = this.toDateKey(date);
        if (dateKey.length === 0) {
            return;
        }

        this.clearingDayDate.set(date);
        this.cyclesService
            .clearDay(currentCycle.id, new Date(date).toISOString())
            .pipe(
                finalize(() => {
                    this.clearingDayDate.set(null);
                }),
                takeUntilDestroyed(this.destroyRef),
            )
            .subscribe(() => {
                const current = this.cycle();
                if (current === null) {
                    return;
                }

                const updatedCycle = {
                    ...current,
                    bleedingEntries: current.bleedingEntries.filter(entry => this.toDateKey(entry.date) !== dateKey),
                    symptoms: current.symptoms.filter(symptom => this.toDateKey(symptom.date) !== dateKey),
                    fertilitySignals: current.fertilitySignals.filter(fertilitySignal => this.toDateKey(fertilitySignal.date) !== dateKey),
                };
                this.cycle.set(updatedCycle);
                this.loadNutritionSummary(updatedCycle);
            });
    }

    public exportCycle(): void {
        const currentCycle = this.cycle();
        if (currentCycle === null || this.isExportingCycle()) {
            return;
        }

        this.isExportingCycle.set(true);
        this.exportService
            .exportCycle({
                dateFrom: this.normalizeStartOfDay(new Date(currentCycle.trackingStartDate)).toISOString(),
                dateTo: this.normalizeEndOfDay(new Date()).toISOString(),
                timeZoneOffsetMinutes: -new Date().getTimezoneOffset(),
            })
            .pipe(
                finalize(() => {
                    this.isExportingCycle.set(false);
                }),
                takeUntilDestroyed(this.destroyRef),
            )
            .subscribe();
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

    private buildFertilitySignalPayload(formValue: CycleDayFormModel): FertilitySignalPayload | null {
        const basalBodyTemperatureCelsius = this.toNullableNumber(formValue.basalBodyTemperatureCelsius);
        const cervicalFluid = this.toOptionalText(formValue.cervicalFluid);
        const hasSignal =
            basalBodyTemperatureCelsius !== null ||
            formValue.ovulationTestResult !== null ||
            cervicalFluid !== undefined ||
            formValue.hadSex;

        if (!hasSignal) {
            return null;
        }

        return {
            basalBodyTemperatureCelsius,
            ovulationTestResult: formValue.ovulationTestResult ?? OVULATION_TEST_RESULT_UNKNOWN,
            cervicalFluid,
            hadSex: formValue.hadSex,
            notes: undefined,
            clearNotes: false,
        };
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
                this.loadNutritionSummary(cycle);
            });
    }

    private clampSymptom(value: number | null | undefined): number {
        if (value === null || value === undefined || Number.isNaN(value)) {
            return MIN_SYMPTOM_VALUE;
        }

        return Math.min(MAX_SYMPTOM_VALUE, Math.max(MIN_SYMPTOM_VALUE, value));
    }

    private toNullableNumber(value: number | string | null | undefined): number | null {
        if (value === null || value === undefined || value === '') {
            return null;
        }

        const numberValue = Number(value);
        return Number.isNaN(numberValue) ? null : numberValue;
    }

    private toOptionalText(value: string | null | undefined): string | undefined {
        const trimmed = value?.trim();
        return trimmed === undefined || trimmed.length === 0 ? undefined : trimmed;
    }

    private normalizeStartOfDay(value: Date): Date {
        const result = new Date(value);
        result.setHours(0, 0, 0, 0);
        return result;
    }

    private normalizeEndOfDay(value: Date): Date {
        const result = new Date(value);
        result.setHours(DAY_END_HOURS, DAY_END_MINUTES, DAY_END_SECONDS, DAY_END_MILLISECONDS);
        return result;
    }

    private toDateKey(value: string): string {
        const date = new Date(value);
        return Number.isNaN(date.getTime()) ? '' : date.toISOString().slice(0, ISO_DATE_KEY_LENGTH);
    }
}
