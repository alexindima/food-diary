import { computed, inject, Injectable, signal } from '@angular/core';
import { FormBuilder, FormControl, Validators } from '@angular/forms';
import { finalize } from 'rxjs';

import { CyclesService } from '../api/cycles.service';
import type { CreateCyclePayload, CycleDay, CyclePredictions, CycleResponse, DailySymptoms } from '../models/cycle.data';

const DEFAULT_AVERAGE_CYCLE_LENGTH = 28;
const MIN_AVERAGE_CYCLE_LENGTH = 18;
const MAX_AVERAGE_CYCLE_LENGTH = 60;
const DEFAULT_LUTEAL_LENGTH = 14;
const MIN_LUTEAL_LENGTH = 8;
const MAX_LUTEAL_LENGTH = 18;
const MIN_SYMPTOM_VALUE = 0;
const MAX_SYMPTOM_VALUE = 9;
const NEXT_MONTH_OFFSET = 1;
const PADDED_DATE_PART_LENGTH = 2;

@Injectable({ providedIn: 'root' })
export class CycleTrackingFacade {
    private readonly cyclesService = inject(CyclesService);
    private readonly fb = inject(FormBuilder);

    public readonly isLoading = signal(false);
    public readonly isSavingCycle = signal(false);
    public readonly isSavingDay = signal(false);
    public readonly cycle = signal<CycleResponse | null>(null);

    public readonly startCycleForm = this.fb.group({
        startDate: new FormControl<string | null>(this.formatDateInput(new Date()), { validators: [Validators.required] }),
        averageLength: new FormControl<number | null>(DEFAULT_AVERAGE_CYCLE_LENGTH, {
            validators: [Validators.min(MIN_AVERAGE_CYCLE_LENGTH), Validators.max(MAX_AVERAGE_CYCLE_LENGTH)],
        }),
        lutealLength: new FormControl<number | null>(DEFAULT_LUTEAL_LENGTH, {
            validators: [Validators.min(MIN_LUTEAL_LENGTH), Validators.max(MAX_LUTEAL_LENGTH)],
        }),
    });

    public readonly dayForm = this.fb.group({
        date: new FormControl<string | null>(this.formatDateInput(new Date()), { validators: [Validators.required] }),
        isPeriod: new FormControl<boolean>(false),
        pain: new FormControl<number>(0),
        mood: new FormControl<number>(0),
        edema: new FormControl<number>(0),
        headache: new FormControl<number>(0),
        energy: new FormControl<number>(0),
        sleepQuality: new FormControl<number>(0),
        libido: new FormControl<number>(0),
        notes: new FormControl<string | null>(null),
    });

    public readonly predictions = computed<CyclePredictions | null>(() => this.cycle()?.predictions ?? null);
    public readonly days = computed<CycleDay[]>(() => {
        const list = this.cycle()?.days ?? [];
        return [...list].sort((a, b) => b.date.localeCompare(a.date));
    });
    public readonly currentCycleTitle = computed(() => {
        const cycle = this.cycle();
        return cycle !== null ? 'CYCLE_TRACKING.CURRENT_CYCLE' : 'CYCLE_TRACKING.NO_CYCLE';
    });

    public initialize(): void {
        this.loadCycle();
    }

    public startCycle(): void {
        if (this.startCycleForm.invalid) {
            this.startCycleForm.markAllAsTouched();
            return;
        }

        const formValue = this.startCycleForm.value;
        if (formValue.startDate === null || formValue.startDate === undefined || formValue.startDate.length === 0) {
            return;
        }

        const startDate = new Date(formValue.startDate);
        const payload: CreateCyclePayload = {
            startDate: startDate.toISOString(),
            averageLength: formValue.averageLength ?? undefined,
            lutealLength: formValue.lutealLength ?? undefined,
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

        if (this.dayForm.invalid) {
            this.dayForm.markAllAsTouched();
            return;
        }

        const formValue = this.dayForm.value;
        const date = formValue.date;
        if (date === null || date === undefined || date.length === 0) {
            return;
        }

        const entryDate = new Date(date);
        const symptoms: DailySymptoms = {
            pain: this.clampSymptom(formValue.pain),
            mood: this.clampSymptom(formValue.mood),
            edema: this.clampSymptom(formValue.edema),
            headache: this.clampSymptom(formValue.headache),
            energy: this.clampSymptom(formValue.energy),
            sleepQuality: this.clampSymptom(formValue.sleepQuality),
            libido: this.clampSymptom(formValue.libido),
        };

        this.isSavingDay.set(true);
        this.cyclesService
            .upsertDay(currentCycle.id, {
                date: entryDate.toISOString(),
                isPeriod: formValue.isPeriod === true,
                symptoms,
                notes: formValue.notes ?? undefined,
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

                const filtered = current.days.filter(existing => existing.id !== day.id && existing.date !== day.date);
                this.cycle.set({
                    ...current,
                    days: [...filtered, day],
                });
            });
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

    private formatDateInput(date: Date): string {
        const year = date.getFullYear();
        const month = String(date.getMonth() + NEXT_MONTH_OFFSET).padStart(PADDED_DATE_PART_LENGTH, '0');
        const day = String(date.getDate()).padStart(PADDED_DATE_PART_LENGTH, '0');
        return `${year}-${month}-${day}`;
    }
}
