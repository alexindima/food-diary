import { CommonModule } from '@angular/common';
import {
    ChangeDetectionStrategy,
    Component,
    DestroyRef,
    OnInit,
    computed,
    effect,
    inject,
    signal,
} from '@angular/core';
import { FormBuilder, FormControl, FormGroup, ReactiveFormsModule, Validators } from '@angular/forms';
import { TranslatePipe } from '@ngx-translate/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { PageHeaderComponent } from '../shared/page-header/page-header.component';
import { PageBodyComponent } from '../shared/page-body/page-body.component';
import { FdPageContainerDirective } from '../../directives/layout/page-container.directive';
import { FdUiCardComponent } from 'fd-ui-kit/card/fd-ui-card.component';
import { FdUiButtonComponent } from 'fd-ui-kit/button/fd-ui-button.component';
import { FdUiInputComponent } from 'fd-ui-kit/input/fd-ui-input.component';
import { FdUiDateInputComponent } from 'fd-ui-kit/date-input/fd-ui-date-input.component';
import { FdUiCheckboxComponent } from 'fd-ui-kit/checkbox/fd-ui-checkbox.component';
import { CyclesService } from '../../services/cycles.service';
import {
    CycleDay,
    CyclePredictions,
    CycleResponse,
    DailySymptoms,
    CreateCyclePayload,
} from '../../types/cycle.data';
import { FdUiAccentSurfaceComponent } from 'fd-ui-kit/accent-surface/fd-ui-accent-surface.component';

@Component({
    selector: 'fd-cycle-tracking-page',
    standalone: true,
    imports: [
        CommonModule,
        TranslatePipe,
        PageHeaderComponent,
        PageBodyComponent,
        FdPageContainerDirective,
        ReactiveFormsModule,
        FdUiCardComponent,
        FdUiButtonComponent,
        FdUiInputComponent,
        FdUiDateInputComponent,
        FdUiCheckboxComponent,
        FdUiAccentSurfaceComponent,
    ],
    templateUrl: './cycle-tracking-page.component.html',
    styleUrl: './cycle-tracking-page.component.scss',
    changeDetection: ChangeDetectionStrategy.OnPush,
})
export class CycleTrackingPageComponent implements OnInit {
    private readonly cyclesService = inject(CyclesService);
    private readonly fb = inject(FormBuilder);
    private readonly destroyRef = inject(DestroyRef);

    public readonly isLoading = signal(false);
    public readonly isSavingCycle = signal(false);
    public readonly isSavingDay = signal(false);
    public readonly cycle = signal<CycleResponse | null>(null);

    public readonly startCycleForm = this.fb.group({
        startDate: new FormControl<string | null>(this.formatDateInput(new Date()), { validators: [Validators.required] }),
        averageLength: new FormControl<number | null>(28, { validators: [Validators.min(18), Validators.max(60)] }),
        lutealLength: new FormControl<number | null>(14, { validators: [Validators.min(8), Validators.max(18)] }),
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

    public readonly symptomFields = [
        { key: 'pain', labelKey: 'CYCLE_TRACKING.SYMPTOM_PAIN' },
        { key: 'mood', labelKey: 'CYCLE_TRACKING.SYMPTOM_MOOD' },
        { key: 'edema', labelKey: 'CYCLE_TRACKING.SYMPTOM_EDEMA' },
        { key: 'headache', labelKey: 'CYCLE_TRACKING.SYMPTOM_HEADACHE' },
        { key: 'energy', labelKey: 'CYCLE_TRACKING.SYMPTOM_ENERGY' },
        { key: 'sleepQuality', labelKey: 'CYCLE_TRACKING.SYMPTOM_SLEEP' },
        { key: 'libido', labelKey: 'CYCLE_TRACKING.SYMPTOM_LIBIDO' },
    ] as const;

    public readonly predictions = computed<CyclePredictions | null>(() => this.cycle()?.predictions ?? null);
    public readonly days = computed<CycleDay[]>(() => {
        const list = this.cycle()?.days ?? [];
        return [...list].sort((a, b) => b.date.localeCompare(a.date));
    });
    public readonly currentCycleTitle = computed(() => {
        const cycle = this.cycle();
        if (!cycle) {
            return 'CYCLE_TRACKING.NO_CYCLE';
        }
        return 'CYCLE_TRACKING.CURRENT_CYCLE';
    });

    public ngOnInit(): void {
        this.loadCycle();
    }

    public startCycle(): void {
        if (this.startCycleForm.invalid) {
            this.startCycleForm.markAllAsTouched();
            return;
        }

        const formValue = this.startCycleForm.value;
        if (!formValue.startDate) {
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
            .pipe(takeUntilDestroyed(this.destroyRef))
            .subscribe({
                next: cycle => {
                    this.cycle.set(cycle);
                    this.isSavingCycle.set(false);
                },
                error: () => this.isSavingCycle.set(false),
            });
    }

    public saveDay(): void {
        if (!this.cycle()?.id) {
            return;
        }

        if (this.dayForm.invalid) {
            this.dayForm.markAllAsTouched();
            return;
        }

        const formValue = this.dayForm.value;
        const date = formValue.date;
        if (!date) {
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
            .upsertDay(this.cycle()!.id, {
                date: entryDate.toISOString(),
                isPeriod: !!formValue.isPeriod,
                symptoms,
                notes: formValue.notes ?? undefined,
            })
            .pipe(takeUntilDestroyed(this.destroyRef))
            .subscribe({
                next: day => {
                    const current = this.cycle();
                    if (!current) {
                        return;
                    }
                    const filtered = current.days.filter(d => d.id !== day.id && d.date !== day.date);
                    this.cycle.set({
                        ...current,
                        days: [...filtered, day],
                    });
                    this.isSavingDay.set(false);
                },
                error: () => this.isSavingDay.set(false),
            });
    }

    private loadCycle(): void {
        this.isLoading.set(true);
        this.cyclesService
            .getCurrent()
            .pipe(takeUntilDestroyed(this.destroyRef))
            .subscribe({
                next: cycle => {
                    this.cycle.set(cycle);
                    this.isLoading.set(false);
                },
                error: () => {
                    this.isLoading.set(false);
                },
            });
    }

    private clampSymptom(value: number | null | undefined): number {
        if (value === null || value === undefined || Number.isNaN(value)) {
            return 0;
        }
        return Math.min(9, Math.max(0, value));
    }

    private formatDateInput(date: Date): string {
        const year = date.getFullYear();
        const month = String(date.getMonth() + 1).padStart(2, '0');
        const day = String(date.getDate()).padStart(2, '0');
        return `${year}-${month}-${day}`;
    }
}

