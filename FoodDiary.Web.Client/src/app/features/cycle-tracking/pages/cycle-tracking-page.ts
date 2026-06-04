import { ChangeDetectionStrategy, Component, computed, DestroyRef, inject, signal } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { type FieldTree, FormField } from '@angular/forms/signals';
import { TranslatePipe, TranslateService } from '@ngx-translate/core';
import { FdUiButtonComponent } from 'fd-ui-kit/button/fd-ui-button';
import { FdUiCardComponent } from 'fd-ui-kit/card/fd-ui-card';
import { FdUiCheckboxComponent } from 'fd-ui-kit/checkbox/fd-ui-checkbox';
import { FdUiDateInputComponent } from 'fd-ui-kit/date-input/fd-ui-date-input';
import { FdUiInputComponent } from 'fd-ui-kit/input/fd-ui-input';

import { PageBodyComponent } from '../../../components/shared/page-body/page-body';
import { PageHeaderComponent } from '../../../components/shared/page-header/page-header';
import { resolveAppLocale } from '../../../shared/lib/locale.constants';
import { FdPageContainerDirective } from '../../../shared/ui/layout/page-container.directive';
import { CycleTrackingFacade } from '../lib/cycle-tracking.facade';
import type { DailySymptoms } from '../models/cycle.data';
import { CycleCurrentCardComponent } from './cycle-current-card/cycle-current-card';
import { CycleDaysCardComponent } from './cycle-days-card/cycle-days-card';
import { CYCLE_SYMPTOM_FIELDS } from './cycle-tracking-page-lib/cycle-tracking-page.config';
import { buildCycleCurrentView, buildCycleDayItems, buildCyclePredictionView } from './cycle-tracking-page-lib/cycle-tracking-page.mapper';

@Component({
    selector: 'fd-cycle-tracking-page',
    imports: [
        TranslatePipe,
        PageHeaderComponent,
        PageBodyComponent,
        FdPageContainerDirective,
        FormField,
        FdUiCardComponent,
        FdUiButtonComponent,
        FdUiInputComponent,
        FdUiDateInputComponent,
        FdUiCheckboxComponent,
        CycleCurrentCardComponent,
        CycleDaysCardComponent,
    ],
    templateUrl: './cycle-tracking-page.html',
    styleUrl: './cycle-tracking-page.scss',
    changeDetection: ChangeDetectionStrategy.OnPush,
    providers: [CycleTrackingFacade],
})
export class CycleTrackingPageComponent {
    private readonly facade = inject(CycleTrackingFacade);
    private readonly translateService = inject(TranslateService);
    private readonly destroyRef = inject(DestroyRef);
    private readonly languageVersion = signal(0);

    protected readonly isLoading = this.facade.isLoading;
    protected readonly isSavingCycle = this.facade.isSavingCycle;
    protected readonly isSavingDay = this.facade.isSavingDay;
    protected readonly cycle = this.facade.cycle;
    protected readonly startCycleForm = this.facade.startCycleForm;
    protected readonly dayForm = this.facade.dayForm;
    protected readonly symptomFields = CYCLE_SYMPTOM_FIELDS;

    protected readonly predictions = this.facade.predictions;
    protected readonly days = this.facade.days;
    protected readonly currentCycleView = computed(() => buildCycleCurrentView(this.cycle(), this.appLocale()));
    protected readonly predictionView = computed(() => buildCyclePredictionView(this.predictions(), this.appLocale()));
    protected readonly dayItems = computed(() => buildCycleDayItems(this.days(), this.appLocale()));

    public constructor() {
        this.translateService.onLangChange.pipe(takeUntilDestroyed(this.destroyRef)).subscribe(() => {
            this.languageVersion.update(version => version + 1);
        });
        this.facade.initialize();
    }

    protected startCycle(): void {
        this.facade.startCycle();
    }

    protected saveDay(): void {
        this.facade.saveDay();
    }

    protected symptomField(key: keyof DailySymptoms): FieldTree<number> {
        return this.dayForm[key];
    }

    private appLocale(): string {
        this.languageVersion();
        return resolveAppLocale(this.translateService.getCurrentLang());
    }
}
