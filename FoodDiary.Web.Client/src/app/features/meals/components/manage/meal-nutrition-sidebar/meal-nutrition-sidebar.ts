import { ChangeDetectionStrategy, Component, computed, DestroyRef, inject, input, output, signal } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { type FormGroup, ReactiveFormsModule } from '@angular/forms';
import { TranslatePipe, TranslateService } from '@ngx-translate/core';
import { FdUiHintDirective } from 'fd-ui-kit';
import { FdUiButtonComponent } from 'fd-ui-kit/button/fd-ui-button';
import { FdUiCardComponent } from 'fd-ui-kit/card/fd-ui-card';
import { FdUiFormErrorComponent } from 'fd-ui-kit/form-error/fd-ui-form-error';
import { FdUiSegmentedToggleComponent } from 'fd-ui-kit/segmented-toggle/fd-ui-segmented-toggle';

import { type NutritionControlNames, NutritionEditorComponent } from '../../../../../components/shared/nutrition-editor/nutrition-editor';
import type { CalorieMismatchWarning, ConsumptionFormData, MacroBarState, NutritionMode } from '../meal-manage-lib/meal-manage.types';
import { buildMealNutritionModeOptions } from '../meal-manage-lib/meal-manage-options.mapper';

@Component({
    selector: 'fd-meal-nutrition-sidebar',
    templateUrl: './meal-nutrition-sidebar.html',
    styleUrls: ['../meal-manage-form.scss'],
    changeDetection: ChangeDetectionStrategy.OnPush,
    imports: [
        ReactiveFormsModule,
        TranslatePipe,
        FdUiHintDirective,
        FdUiCardComponent,
        FdUiButtonComponent,
        FdUiSegmentedToggleComponent,
        FdUiFormErrorComponent,
        NutritionEditorComponent,
    ],
})
export class MealNutritionSidebarComponent {
    private readonly translateService = inject(TranslateService);
    private readonly destroyRef = inject(DestroyRef);

    public readonly consumptionForm = input.required<FormGroup<ConsumptionFormData>>();
    public readonly nutritionControlNames = input.required<NutritionControlNames>();
    public readonly macroBarState = input.required<MacroBarState>();
    public readonly nutritionMode = input.required<NutritionMode>();
    public readonly nutritionWarning = input.required<CalorieMismatchWarning | null>();
    public readonly caloriesError = input.required<string | null>();
    public readonly macrosError = input.required<string | null>();
    public readonly isEditMode = input.required<boolean>();
    public readonly globalError = input.required<string | null>();

    public readonly nutritionModeChange = output<string>();
    public readonly cancelRequested = output();
    private readonly activeLang = signal(this.translateService.getCurrentLang());
    protected readonly nutritionModeOptions = computed(() => {
        this.activeLang();
        return buildMealNutritionModeOptions(this.translateService);
    });
    protected readonly isNutritionReadonly = computed(() => this.nutritionMode() === 'auto');
    protected readonly showManualNutritionHint = computed(() => !this.isNutritionReadonly());

    public constructor() {
        this.translateService.onLangChange.pipe(takeUntilDestroyed(this.destroyRef)).subscribe(event => {
            this.activeLang.set(event.lang);
        });
    }

    protected onNutritionModeChange(nextMode: string): void {
        this.nutritionModeChange.emit(nextMode);
    }

    protected onCancel(): void {
        this.cancelRequested.emit();
    }
}
