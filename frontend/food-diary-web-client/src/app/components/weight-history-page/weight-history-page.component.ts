import { CommonModule } from '@angular/common';
import {
    ChangeDetectionStrategy,
    Component,
    DestroyRef,
    OnInit,
    computed,
    inject,
    signal,
} from '@angular/core';
import { TranslateModule, TranslateService } from '@ngx-translate/core';
import { FormBuilder, FormControl, ReactiveFormsModule, Validators } from '@angular/forms';
import { BaseChartDirective } from 'ng2-charts';
import { ChartConfiguration } from 'chart.js';
import { MatIconModule } from '@angular/material/icon';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { WeightEntriesService } from '../../services/weight-entries.service';
import { WeightEntry } from '../../types/weight-entry.data';
import { FdUiCardComponent } from '../../ui-kit/card/fd-ui-card.component';
import { FdUiButtonComponent } from '../../ui-kit/button/fd-ui-button.component';
import { FdUiDateInputComponent } from '../../ui-kit/date-input/fd-ui-date-input.component';
import { FdUiInputComponent } from '../../ui-kit/input/fd-ui-input.component';
import { UserService } from '../../services/user.service';
import { NavigationService } from '../../services/navigation.service';

@Component({
    selector: 'fd-weight-history-page',
    standalone: true,
    imports: [
        CommonModule,
        TranslateModule,
        ReactiveFormsModule,
        BaseChartDirective,
        MatIconModule,
        FdUiCardComponent,
        FdUiButtonComponent,
        FdUiDateInputComponent,
        FdUiInputComponent,
    ],
    templateUrl: './weight-history-page.component.html',
    styleUrls: ['./weight-history-page.component.less'],
    changeDetection: ChangeDetectionStrategy.OnPush,
})
export class WeightHistoryPageComponent implements OnInit {
    private readonly weightEntriesService = inject(WeightEntriesService);
    private readonly userService = inject(UserService);
    private readonly navigationService = inject(NavigationService);
    private readonly destroyRef = inject(DestroyRef);
    private readonly translate = inject(TranslateService);
    private readonly fb = inject(FormBuilder);

    private hasUnsavedChanges = false;

    public readonly entries = signal<WeightEntry[]>([]);
    public readonly isLoading = signal<boolean>(false);
    public readonly isSaving = signal<boolean>(false);
    public readonly isEditing = signal<boolean>(false);
    public readonly desiredWeight = signal<number | null>(null);
    public readonly isDesiredWeightSaving = signal<boolean>(false);
    private readonly editingEntryId = signal<string | null>(null);

    public readonly entriesDescending = computed(() =>
        [...this.entries()].sort(
            (a, b) => new Date(b.date).getTime() - new Date(a.date).getTime(),
        ),
    );

    public readonly chartData = computed<ChartConfiguration<'line'>['data']>(() => {
        const ordered = [...this.entries()].sort(
            (a, b) => new Date(a.date).getTime() - new Date(b.date).getTime(),
        );

        const label = this.translate.instant('WEIGHT_HISTORY.CHART_LABEL');
        return {
            labels: ordered.map(entry =>
                new Date(entry.date).toLocaleDateString(),
            ),
            datasets: [
                {
                    data: ordered.map(entry => entry.weight),
                    label,
                    borderColor: '#2563eb',
                    backgroundColor: 'rgba(37, 99, 235, 0.1)',
                    fill: true,
                    tension: 0.3,
                    pointRadius: 3,
                },
            ],
        };
    });

    public readonly chartOptions: ChartConfiguration<'line'>['options'] = {
        responsive: true,
        scales: {
            x: {
                ticks: {
                    maxRotation: 0,
                    autoSkip: true,
                    maxTicksLimit: 6,
                },
            },
            y: {
                beginAtZero: false,
            },
        },
        plugins: {
            legend: {
                display: false,
            },
        },
    };

    public readonly form = this.fb.group({
        date: [new Date(), Validators.required],
        weight: ['', [Validators.required, Validators.min(1), Validators.max(500)]],
    });

    public readonly desiredWeightControl = new FormControl<string>('');

    public ngOnInit(): void {
        this.loadEntries();
        this.loadDesiredWeight();
    }

    public navigateBack(): void {
        void this.navigationService.navigateToHome();
    }

    public submit(): void {
        if (this.form.invalid) {
            this.form.markAllAsTouched();
            return;
        }

        const payload = this.buildPayload();
        if (!payload) {
            return;
        }

        this.isSaving.set(true);
        const editingId = this.editingEntryId();
        const request$ = editingId
            ? this.weightEntriesService.update(editingId, payload)
            : this.weightEntriesService.create(payload);

        request$
            .pipe(takeUntilDestroyed(this.destroyRef))
            .subscribe({
                next: () => {
                    this.hasUnsavedChanges = true;
                    this.isSaving.set(false);
                    this.loadEntries(false);
                    if (editingId) {
                        this.resetEditingState();
                    } else {
                        this.form.controls.weight.setValue(payload.weight.toString());
                    }
                },
                error: () => {
                    this.isSaving.set(false);
                },
            });
    }

    public startEdit(entry: WeightEntry): void {
        this.isEditing.set(true);
        this.editingEntryId.set(entry.id);
        this.form.setValue({
            date: new Date(entry.date),
            weight: entry.weight.toString(),
        });
    }

    public cancelEdit(): void {
        this.resetEditingState();
        const latest = this.entriesDescending()[0];
        this.form.setValue({
            date: new Date(),
            weight: (latest?.weight ?? '').toString(),
        });
    }

    public deleteEntry(entry: WeightEntry): void {
        this.isSaving.set(true);
        this.weightEntriesService
            .remove(entry.id)
            .pipe(takeUntilDestroyed(this.destroyRef))
            .subscribe({
                next: () => {
                    this.hasUnsavedChanges = true;
                    this.isSaving.set(false);
                    this.loadEntries(false);
                    if (this.editingEntryId() === entry.id) {
                        this.resetEditingState();
                    }
                },
                error: () => {
                    this.isSaving.set(false);
                },
            });
    }

    public saveDesiredWeight(): void {
        if (this.desiredWeightControl.invalid) {
            return;
        }

        const rawValue = this.desiredWeightControl.value?.trim();
        const parsedValue = rawValue ? Number(rawValue.replace(',', '.')) : null;
        if (rawValue && (isNaN(parsedValue!) || parsedValue! <= 0 || parsedValue! > 500)) {
            this.desiredWeightControl.setErrors({ invalid: true });
            return;
        }

        this.isDesiredWeightSaving.set(true);
        this.userService
            .updateDesiredWeight(parsedValue)
            .pipe(takeUntilDestroyed(this.destroyRef))
            .subscribe({
                next: value => {
                    this.desiredWeight.set(value);
                    this.isDesiredWeightSaving.set(false);
                },
                error: () => {
                    this.isDesiredWeightSaving.set(false);
                },
            });
    }

    private loadEntries(showLoader = true): void {
        if (showLoader) {
            this.isLoading.set(true);
        }

        this.weightEntriesService
            .getEntries({ limit: 180, sort: 'desc' })
            .pipe(takeUntilDestroyed(this.destroyRef))
            .subscribe({
                next: entries => {
                    this.entries.set(entries);
                    this.isLoading.set(false);
                    if (!this.isEditing() && entries.length > 0) {
                        const latest = [...entries].sort(
                            (a, b) => new Date(b.date).getTime() - new Date(a.date).getTime(),
                        )[0];
                        this.form.patchValue({
                            weight: latest.weight.toString(),
                        });
                    }
                },
                error: () => {
                    this.isLoading.set(false);
                },
            });
    }

    private loadDesiredWeight(): void {
        this.userService
            .getDesiredWeight()
            .pipe(takeUntilDestroyed(this.destroyRef))
            .subscribe(value => {
                this.desiredWeight.set(value);
                this.desiredWeightControl.setValue(value?.toString() ?? '');
            });
    }

    private buildPayload() {
        const rawDate = this.form.value.date;
        const rawWeight = this.form.value.weight;
        if (!rawDate || rawWeight === null || rawWeight === undefined) {
            return null;
        }

        const date = rawDate instanceof Date ? rawDate : new Date(rawDate);
        const weight = Number(rawWeight);
        return {
            date: date.toISOString(),
            weight,
        };
    }

    private resetEditingState(): void {
        this.isEditing.set(false);
        this.editingEntryId.set(null);
        this.form.patchValue({
            date: new Date(),
        });
    }
}
