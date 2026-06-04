import { computed, inject, Service, signal } from '@angular/core';
import { form, max, min, required } from '@angular/forms/signals';
import { finalize } from 'rxjs';

import { formatDateInputValue } from '../../../shared/lib/local-date.utils';
import { CyclesService } from '../api/cycles.service';
import type { CreateCyclePayload, CycleDay, CyclePredictions, CycleResponse, DailySymptoms } from '../models/cycle.data';
import {
    DEFAULT_AVERAGE_CYCLE_LENGTH,
    DEFAULT_LUTEAL_LENGTH,
    MAX_AVERAGE_CYCLE_LENGTH,
    MAX_LUTEAL_LENGTH,
    MAX_SYMPTOM_VALUE,
    MIN_AVERAGE_CYCLE_LENGTH,
    MIN_LUTEAL_LENGTH,
    MIN_SYMPTOM_VALUE,
} from './cycle-tracking.config';

type StartCycleFormModel = {
    startDate: string | null;
    averageLength: number | null;
    lutealLength: number | null;
};

export type CycleDayFormModel = {
    date: string | null;
    isPeriod: boolean;
    pain: number;
    mood: number;
    edema: number;
    headache: number;
    energy: number;
    sleepQuality: number;
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
        startDate: formatDateInputValue(new Date()),
        averageLength: DEFAULT_AVERAGE_CYCLE_LENGTH,
        lutealLength: DEFAULT_LUTEAL_LENGTH,
    });
    public readonly startCycleForm = form(this.startCycleModel, path => {
        required(path.startDate);
        min(path.averageLength, MIN_AVERAGE_CYCLE_LENGTH);
        max(path.averageLength, MAX_AVERAGE_CYCLE_LENGTH);
        min(path.lutealLength, MIN_LUTEAL_LENGTH);
        max(path.lutealLength, MAX_LUTEAL_LENGTH);
    });

    public readonly dayModel = signal<CycleDayFormModel>({
        date: formatDateInputValue(new Date()),
        isPeriod: false,
        pain: 0,
        mood: 0,
        edema: 0,
        headache: 0,
        energy: 0,
        sleepQuality: 0,
        libido: 0,
        notes: null,
    });
    public readonly dayForm = form(this.dayModel, path => {
        required(path.date);
    });

    public readonly predictions = computed<CyclePredictions | null>(() => this.cycle()?.predictions ?? null);
    public readonly days = computed<CycleDay[]>(() => {
        const list = this.cycle()?.days ?? [];
        return [...list].sort((a, b) => b.date.localeCompare(a.date));
    });

    public initialize(): void {
        this.loadCycle();
    }

    public startCycle(): void {
        if (this.startCycleForm().invalid()) {
            this.startCycleForm().markAsTouched();
            return;
        }

        const formValue = this.startCycleModel();
        if (formValue.startDate === null || formValue.startDate.length === 0) {
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
                isPeriod: formValue.isPeriod,
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
}
