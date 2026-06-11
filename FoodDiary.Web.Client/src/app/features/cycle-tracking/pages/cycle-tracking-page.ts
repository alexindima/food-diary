import { ChangeDetectionStrategy, Component, computed, DestroyRef, inject, signal } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { type FieldTree, FormField } from '@angular/forms/signals';
import { TranslatePipe, TranslateService } from '@ngx-translate/core';
import { FdUiButtonComponent } from 'fd-ui-kit/button/fd-ui-button';
import { FdUiCardComponent } from 'fd-ui-kit/card/fd-ui-card';
import { FdUiCheckboxComponent } from 'fd-ui-kit/checkbox/fd-ui-checkbox';
import { FdUiDateInputComponent } from 'fd-ui-kit/date-input/fd-ui-date-input';
import { FdUiInputComponent } from 'fd-ui-kit/input/fd-ui-input';
import { FdUiSelectComponent, type FdUiSelectOption } from 'fd-ui-kit/select/fd-ui-select';
import { FdUiTextareaComponent } from 'fd-ui-kit/textarea/fd-ui-textarea';

import { PageBodyComponent } from '../../../components/shared/page-body/page-body';
import { PageHeaderComponent } from '../../../components/shared/page-header/page-header';
import { resolveAppLocale } from '../../../shared/lib/locale.constants';
import { FdPageContainerDirective } from '../../../shared/ui/layout/page-container.directive';
import { CycleTrackingFacade } from '../lib/cycle-tracking.facade';
import {
    BLEEDING_TYPE_BLEEDING,
    BLEEDING_TYPE_SPOTTING,
    type BleedingType,
    CYCLE_FACTOR_TYPE_HORMONAL_CONTRACEPTION,
    CYCLE_FACTOR_TYPE_LACTATION,
    CYCLE_FACTOR_TYPE_NO_PERIOD,
    CYCLE_FACTOR_TYPE_NON_HORMONAL_CONTRACEPTION,
    CYCLE_FACTOR_TYPE_PERIMENOPAUSE,
    CYCLE_FACTOR_TYPE_POSTPARTUM,
    CYCLE_FACTOR_TYPE_PREGNANCY,
    CYCLE_FLOW_HEAVY,
    CYCLE_FLOW_LIGHT,
    CYCLE_FLOW_MEDIUM,
    CYCLE_TRACKING_MODE_NO_PERIOD,
    CYCLE_TRACKING_MODE_PERIMENOPAUSE,
    CYCLE_TRACKING_MODE_PERIOD_TRACKING,
    CYCLE_TRACKING_MODE_POSTPARTUM_LACTATION,
    CYCLE_TRACKING_MODE_PREGNANCY,
    CYCLE_TRACKING_MODE_TRYING_TO_CONCEIVE,
    type CycleFactorType,
    type CycleFlowLevel,
    type CycleTrackingMode,
    OVULATION_TEST_RESULT_NEGATIVE,
    OVULATION_TEST_RESULT_POSITIVE,
    OVULATION_TEST_RESULT_UNKNOWN,
    type OvulationTestResult,
} from '../models/cycle.data';
import { CycleCurrentCardComponent } from './cycle-current-card/cycle-current-card';
import { CycleDaysCardComponent } from './cycle-days-card/cycle-days-card';
import { CycleFactorListComponent } from './cycle-factor-list/cycle-factor-list';
import { CycleNutritionSummaryCardComponent } from './cycle-nutrition-summary-card/cycle-nutrition-summary-card';
import { CYCLE_SYMPTOM_FIELDS, type CycleSymptomField } from './cycle-tracking-page-lib/cycle-tracking-page.config';
import {
    buildCycleCurrentView,
    buildCycleDayItems,
    buildCycleFactorItems,
    buildCycleNutritionSummaryView,
    buildCyclePredictionView,
} from './cycle-tracking-page-lib/cycle-tracking-page.mapper';

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
        FdUiSelectComponent,
        FdUiTextareaComponent,
        FdUiDateInputComponent,
        FdUiCheckboxComponent,
        CycleCurrentCardComponent,
        CycleDaysCardComponent,
        CycleFactorListComponent,
        CycleNutritionSummaryCardComponent,
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
    protected readonly isSavingFactor = this.facade.isSavingFactor;
    protected readonly isExportingCycle = this.facade.isExportingCycle;
    protected readonly clearingDayDate = this.facade.clearingDayDate;
    protected readonly editingDayDate = this.facade.editingDayDate;
    protected readonly editingFactorId = this.facade.editingFactorId;
    protected readonly isLoadingNutritionSummary = this.facade.isLoadingNutritionSummary;
    protected readonly cycle = this.facade.cycle;
    protected readonly nutritionSummary = this.facade.nutritionSummary;
    protected readonly startCycleForm = this.facade.startCycleForm;
    protected readonly dayForm = this.facade.dayForm;
    protected readonly factorForm = this.facade.factorForm;
    protected readonly symptomFields = CYCLE_SYMPTOM_FIELDS;

    protected readonly predictions = this.facade.predictions;
    protected readonly bleedingEntries = this.facade.bleedingEntries;
    protected readonly symptoms = this.facade.symptoms;
    protected readonly factors = this.facade.factors;
    protected readonly fertilitySignals = this.facade.fertilitySignals;
    protected readonly currentCycleView = computed(() => buildCycleCurrentView(this.cycle(), this.appLocale()));
    protected readonly predictionView = computed(() => buildCyclePredictionView(this.predictions(), this.appLocale()));
    protected readonly nutritionSummaryView = computed(() => buildCycleNutritionSummaryView(this.nutritionSummary(), this.appLocale()));
    protected readonly dayItems = computed(() =>
        buildCycleDayItems(this.bleedingEntries(), this.symptoms(), this.fertilitySignals(), this.appLocale()),
    );
    protected readonly factorItems = computed(() => buildCycleFactorItems(this.factors(), this.appLocale()));
    protected readonly modeOptions = computed<Array<FdUiSelectOption<CycleTrackingMode>>>(() => {
        this.languageVersion();
        return [
            this.option(CYCLE_TRACKING_MODE_PERIOD_TRACKING, 'CYCLE_TRACKING.MODE_PERIOD_TRACKING'),
            this.option(CYCLE_TRACKING_MODE_TRYING_TO_CONCEIVE, 'CYCLE_TRACKING.MODE_TRYING_TO_CONCEIVE'),
            this.option(CYCLE_TRACKING_MODE_PREGNANCY, 'CYCLE_TRACKING.MODE_PREGNANCY'),
            this.option(CYCLE_TRACKING_MODE_POSTPARTUM_LACTATION, 'CYCLE_TRACKING.MODE_POSTPARTUM_LACTATION'),
            this.option(CYCLE_TRACKING_MODE_PERIMENOPAUSE, 'CYCLE_TRACKING.MODE_PERIMENOPAUSE'),
            this.option(CYCLE_TRACKING_MODE_NO_PERIOD, 'CYCLE_TRACKING.MODE_NO_PERIOD'),
        ];
    });
    protected readonly bleedingTypeOptions = computed<Array<FdUiSelectOption<BleedingType>>>(() => {
        this.languageVersion();
        return [
            this.option(BLEEDING_TYPE_BLEEDING, 'CYCLE_TRACKING.BLEEDING_TYPE_BLEEDING'),
            this.option(BLEEDING_TYPE_SPOTTING, 'CYCLE_TRACKING.BLEEDING_TYPE_SPOTTING'),
        ];
    });
    protected readonly flowOptions = computed<Array<FdUiSelectOption<CycleFlowLevel>>>(() => {
        this.languageVersion();
        return [
            this.option(CYCLE_FLOW_LIGHT, 'CYCLE_TRACKING.FLOW_LIGHT'),
            this.option(CYCLE_FLOW_MEDIUM, 'CYCLE_TRACKING.FLOW_MEDIUM'),
            this.option(CYCLE_FLOW_HEAVY, 'CYCLE_TRACKING.FLOW_HEAVY'),
        ];
    });
    protected readonly ovulationTestOptions = computed<Array<FdUiSelectOption<OvulationTestResult>>>(() => {
        this.languageVersion();
        return [
            this.option(OVULATION_TEST_RESULT_UNKNOWN, 'CYCLE_TRACKING.OVULATION_TEST_NONE'),
            this.option(OVULATION_TEST_RESULT_NEGATIVE, 'CYCLE_TRACKING.OVULATION_TEST_NEGATIVE'),
            this.option(OVULATION_TEST_RESULT_POSITIVE, 'CYCLE_TRACKING.OVULATION_TEST_POSITIVE'),
        ];
    });
    protected readonly factorTypeOptions = computed<Array<FdUiSelectOption<CycleFactorType>>>(() => {
        this.languageVersion();
        return [
            this.option(CYCLE_FACTOR_TYPE_PREGNANCY, 'CYCLE_TRACKING.FACTOR_PREGNANCY'),
            this.option(CYCLE_FACTOR_TYPE_LACTATION, 'CYCLE_TRACKING.FACTOR_LACTATION'),
            this.option(CYCLE_FACTOR_TYPE_HORMONAL_CONTRACEPTION, 'CYCLE_TRACKING.FACTOR_HORMONAL_CONTRACEPTION'),
            this.option(CYCLE_FACTOR_TYPE_NON_HORMONAL_CONTRACEPTION, 'CYCLE_TRACKING.FACTOR_NON_HORMONAL_CONTRACEPTION'),
            this.option(CYCLE_FACTOR_TYPE_POSTPARTUM, 'CYCLE_TRACKING.FACTOR_POSTPARTUM'),
            this.option(CYCLE_FACTOR_TYPE_PERIMENOPAUSE, 'CYCLE_TRACKING.FACTOR_PERIMENOPAUSE'),
            this.option(CYCLE_FACTOR_TYPE_NO_PERIOD, 'CYCLE_TRACKING.FACTOR_NO_PERIOD'),
        ];
    });

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

    protected saveFactor(): void {
        this.facade.saveFactor();
    }

    protected editDay(date: string): void {
        this.facade.editDay(date);
    }

    protected cancelDayEdit(): void {
        this.facade.cancelDayEdit();
    }

    protected clearDay(date: string): void {
        this.facade.clearDay(date);
    }

    protected editFactor(factorId: string): void {
        this.facade.editFactor(factorId);
    }

    protected cancelFactorEdit(): void {
        this.facade.cancelFactorEdit();
    }

    protected endFactorToday(factorId: string): void {
        this.facade.endFactorToday(factorId);
    }

    protected exportCycle(): void {
        this.facade.exportCycle();
    }

    protected symptomField(key: CycleSymptomField['key']): FieldTree<number> {
        return this.dayForm[key];
    }

    private appLocale(): string {
        this.languageVersion();
        return resolveAppLocale(this.translateService.getCurrentLang());
    }

    private option<T>(value: T, labelKey: string): FdUiSelectOption<T> {
        return {
            value,
            label: this.translateService.instant(labelKey),
        };
    }
}
