import { CommonModule } from '@angular/common';
import { ChangeDetectionStrategy, Component, computed, DestroyRef, inject, signal } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { ReactiveFormsModule } from '@angular/forms';
import { TranslatePipe, TranslateService } from '@ngx-translate/core';
import { FdUiAccentSurfaceComponent } from 'fd-ui-kit/accent-surface/fd-ui-accent-surface.component';
import { FdUiButtonComponent } from 'fd-ui-kit/button/fd-ui-button.component';
import { FdUiCardComponent } from 'fd-ui-kit/card/fd-ui-card.component';
import { FdUiCheckboxComponent } from 'fd-ui-kit/checkbox/fd-ui-checkbox.component';
import { FdUiDateInputComponent } from 'fd-ui-kit/date-input/fd-ui-date-input.component';
import { FdUiInputComponent } from 'fd-ui-kit/input/fd-ui-input.component';

import { PageBodyComponent } from '../../../components/shared/page-body/page-body.component';
import { PageHeaderComponent } from '../../../components/shared/page-header/page-header.component';
import { FdPageContainerDirective } from '../../../directives/layout/page-container.directive';
import { CycleTrackingFacade } from '../lib/cycle-tracking.facade';
import { type CycleDay, type CyclePredictions, type CycleResponse } from '../models/cycle.data';

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
    providers: [CycleTrackingFacade],
})
export class CycleTrackingPageComponent {
    private readonly facade = inject(CycleTrackingFacade);
    private readonly translateService = inject(TranslateService);
    private readonly destroyRef = inject(DestroyRef);
    private readonly languageVersion = signal(0);

    public readonly isLoading = this.facade.isLoading;
    public readonly isSavingCycle = this.facade.isSavingCycle;
    public readonly isSavingDay = this.facade.isSavingDay;
    public readonly cycle = this.facade.cycle;
    public readonly startCycleForm = this.facade.startCycleForm;
    public readonly dayForm = this.facade.dayForm;

    public readonly symptomFields = [
        { key: 'pain', labelKey: 'CYCLE_TRACKING.SYMPTOM_PAIN' },
        { key: 'mood', labelKey: 'CYCLE_TRACKING.SYMPTOM_MOOD' },
        { key: 'edema', labelKey: 'CYCLE_TRACKING.SYMPTOM_EDEMA' },
        { key: 'headache', labelKey: 'CYCLE_TRACKING.SYMPTOM_HEADACHE' },
        { key: 'energy', labelKey: 'CYCLE_TRACKING.SYMPTOM_ENERGY' },
        { key: 'sleepQuality', labelKey: 'CYCLE_TRACKING.SYMPTOM_SLEEP' },
        { key: 'libido', labelKey: 'CYCLE_TRACKING.SYMPTOM_LIBIDO' },
    ] as const;

    public readonly predictions = this.facade.predictions;
    public readonly days = this.facade.days;
    public readonly currentCycleTitle = this.facade.currentCycleTitle;
    public readonly currentCycleView = computed<CycleViewModel | null>(() => {
        this.languageVersion();
        const cycle = this.cycle();
        if (!cycle) {
            return null;
        }

        return {
            cycle,
            startDateLabel: this.formatDate(cycle.startDate, { day: 'numeric', month: 'short', year: 'numeric' }),
        };
    });
    public readonly predictionView = computed<CyclePredictionViewModel | null>(() => {
        this.languageVersion();
        const prediction = this.predictions();
        if (!prediction) {
            return null;
        }

        return {
            prediction,
            nextPeriodStartLabel: this.formatDate(prediction.nextPeriodStart, { day: 'numeric', month: 'short' }, 'UTC'),
            ovulationDateLabel: this.formatDate(prediction.ovulationDate, { day: 'numeric', month: 'short' }, 'UTC'),
            pmsStartLabel: this.formatDate(prediction.pmsStart, { day: 'numeric', month: 'short' }, 'UTC'),
        };
    });
    public readonly dayItems = computed<CycleDayViewModel[]>(() => {
        this.languageVersion();

        return this.days().map(day => ({
            day,
            dateLabel: this.formatDate(day.date, { day: 'numeric', month: 'short', year: 'numeric' }),
        }));
    });

    public constructor() {
        this.translateService.onLangChange.pipe(takeUntilDestroyed(this.destroyRef)).subscribe(() => {
            this.languageVersion.update(version => version + 1);
        });
        this.facade.initialize();
    }

    public startCycle(): void {
        this.facade.startCycle();
    }

    public saveDay(): void {
        this.facade.saveDay();
    }

    private formatDate(
        value: string | null | undefined,
        options: Intl.DateTimeFormatOptions,
        timeZone?: Intl.DateTimeFormatOptions['timeZone'],
    ): string {
        if (!value) {
            return '';
        }

        const date = new Date(value);
        if (Number.isNaN(date.getTime())) {
            return value;
        }

        return new Intl.DateTimeFormat(this.translateService.currentLang === 'ru' ? 'ru-RU' : 'en-US', {
            ...options,
            timeZone,
        }).format(date);
    }
}

interface CycleViewModel {
    cycle: CycleResponse;
    startDateLabel: string;
}

interface CyclePredictionViewModel {
    prediction: CyclePredictions;
    nextPeriodStartLabel: string;
    ovulationDateLabel: string;
    pmsStartLabel: string;
}

interface CycleDayViewModel {
    day: CycleDay;
    dateLabel: string;
}
