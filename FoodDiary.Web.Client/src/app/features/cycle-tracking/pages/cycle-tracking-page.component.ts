import { ChangeDetectionStrategy, Component, computed, DestroyRef, inject, signal } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { ReactiveFormsModule } from '@angular/forms';
import { TranslatePipe, TranslateService } from '@ngx-translate/core';
import { FdUiButtonComponent } from 'fd-ui-kit/button/fd-ui-button.component';
import { FdUiCardComponent } from 'fd-ui-kit/card/fd-ui-card.component';
import { FdUiCheckboxComponent } from 'fd-ui-kit/checkbox/fd-ui-checkbox.component';
import { FdUiDateInputComponent } from 'fd-ui-kit/date-input/fd-ui-date-input.component';
import { FdUiInputComponent } from 'fd-ui-kit/input/fd-ui-input.component';

import { PageBodyComponent } from '../../../components/shared/page-body/page-body.component';
import { PageHeaderComponent } from '../../../components/shared/page-header/page-header.component';
import { FdPageContainerDirective } from '../../../directives/layout/page-container.directive';
import { resolveAppLocale } from '../../../shared/lib/locale.constants';
import { CycleTrackingFacade } from '../lib/cycle-tracking.facade';
import { CycleCurrentCardComponent } from './cycle-current-card/cycle-current-card.component';
import { CycleDaysCardComponent } from './cycle-days-card/cycle-days-card.component';
import { CYCLE_SYMPTOM_FIELDS } from './cycle-tracking-page-lib/cycle-tracking-page.config';
import { buildCycleCurrentView, buildCycleDayItems, buildCyclePredictionView } from './cycle-tracking-page-lib/cycle-tracking-page.mapper';

@Component({
    selector: 'fd-cycle-tracking-page',
    imports: [
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
        CycleCurrentCardComponent,
        CycleDaysCardComponent,
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
    public readonly symptomFields = CYCLE_SYMPTOM_FIELDS;

    public readonly predictions = this.facade.predictions;
    public readonly days = this.facade.days;
    public readonly currentCycleView = computed(() => buildCycleCurrentView(this.cycle(), this.appLocale()));
    public readonly predictionView = computed(() => buildCyclePredictionView(this.predictions(), this.appLocale()));
    public readonly dayItems = computed(() => buildCycleDayItems(this.days(), this.appLocale()));

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

    private appLocale(): string {
        this.languageVersion();
        return resolveAppLocale(this.translateService.getCurrentLang());
    }
}
