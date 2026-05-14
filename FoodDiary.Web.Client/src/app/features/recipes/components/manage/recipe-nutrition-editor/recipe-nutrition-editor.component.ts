import { ChangeDetectionStrategy, Component, computed, effect, inject, input, output, signal } from '@angular/core';
import { type FormControl, type FormGroup, ReactiveFormsModule } from '@angular/forms';
import { TranslatePipe, TranslateService } from '@ngx-translate/core';
import { FdUiHintDirective } from 'fd-ui-kit';
import { FdUiCardComponent } from 'fd-ui-kit/card/fd-ui-card.component';
import { FdUiSegmentedToggleComponent, type FdUiSegmentedToggleOption } from 'fd-ui-kit/segmented-toggle/fd-ui-segmented-toggle.component';
import { EMPTY, merge, type Observable } from 'rxjs';

import type {
    NutritionMacroState,
    NutritionMismatchWarning,
} from '../../../../../components/shared/nutrition-editor/nutrition-editor.component';
import {
    type NutritionControlNames,
    NutritionEditorComponent,
} from '../../../../../components/shared/nutrition-editor/nutrition-editor.component';
import { DEFAULT_CALORIE_MISMATCH_THRESHOLD } from '../../../../../shared/lib/nutrition.constants';
import {
    calculateCalorieMismatchWarning,
    calculateMacroBarState,
    checkCaloriesError,
    checkMacrosError,
    getControlNumericValue,
} from '../../../../../shared/lib/nutrition-form.utils';
import type { NutritionScaleMode, RecipeFormData } from '../recipe-manage-lib/recipe-manage.types';

@Component({
    selector: 'fd-recipe-nutrition-editor',
    templateUrl: './recipe-nutrition-editor.component.html',
    styleUrls: ['./recipe-nutrition-editor.component.scss'],
    changeDetection: ChangeDetectionStrategy.OnPush,
    imports: [
        ReactiveFormsModule,
        TranslatePipe,
        FdUiHintDirective,
        FdUiCardComponent,
        FdUiSegmentedToggleComponent,
        NutritionEditorComponent,
    ],
})
export class RecipeNutritionEditorComponent {
    private readonly translateService = inject(TranslateService);
    private readonly formRevision = signal(0);

    public readonly formGroup = input.required<FormGroup<RecipeFormData>>();
    public readonly nutritionScaleMode = input.required<NutritionScaleMode>();
    public readonly nutritionWarning = signal<NutritionMismatchWarning | null>(null);
    public readonly nutritionModeOptions = signal<FdUiSegmentedToggleOption[]>([]);
    public readonly nutritionScaleModeOptions = signal<FdUiSegmentedToggleOption[]>([]);
    public readonly nutritionMode = computed(() => {
        this.formRevision();
        return this.formGroup().controls.calculateNutritionAutomatically.value ? 'auto' : 'manual';
    });
    public readonly isNutritionReadonly = computed(() => this.nutritionMode() === 'auto');
    public readonly showManualNutritionHint = computed(() => !this.isNutritionReadonly());
    public readonly macroBarState = computed<NutritionMacroState>(() => {
        this.formRevision();
        const form = this.formGroup();
        return calculateMacroBarState(
            this.getNumberValue(form.controls.manualProteins),
            this.getNumberValue(form.controls.manualFats),
            this.getNumberValue(form.controls.manualCarbs),
        );
    });
    public readonly nutritionControlNames: NutritionControlNames = {
        calories: 'manualCalories',
        proteins: 'manualProteins',
        fats: 'manualFats',
        carbs: 'manualCarbs',
        fiber: 'manualFiber',
        alcohol: 'manualAlcohol',
    };

    public readonly nutritionModeChange = output<string>();
    public readonly nutritionScaleModeChange = output<string>();

    public constructor() {
        effect(onCleanup => {
            const languageChanges = (this.translateService as { onLangChange?: Observable<unknown> }).onLangChange ?? EMPTY;
            const refresh = (): void => {
                this.buildNutritionModeOptions();
                this.buildNutritionScaleModeOptions();
            };

            refresh();
            const subscription = languageChanges.subscribe(() => {
                refresh();
            });
            onCleanup(() => {
                subscription.unsubscribe();
            });
        });

        effect(onCleanup => {
            const form = this.formGroup();
            const refresh = (): void => {
                this.formRevision.update(revision => revision + 1);
                this.updateCalorieWarning();
            };

            refresh();
            const subscription = merge(form.valueChanges, form.statusChanges).subscribe(() => {
                refresh();
            });
            onCleanup(() => {
                subscription.unsubscribe();
            });
        });
    }

    public caloriesError(): string | null {
        if (this.formGroup().controls.calculateNutritionAutomatically.value) {
            return null;
        }

        return checkCaloriesError(this.formGroup().controls.manualCalories)
            ? this.translateService.instant('PRODUCT_MANAGE.NUTRITION_ERRORS.CALORIES_REQUIRED')
            : null;
    }

    public macrosError(): string | null {
        if (this.formGroup().controls.calculateNutritionAutomatically.value) {
            return null;
        }

        return checkMacrosError([
            this.formGroup().controls.manualProteins,
            this.formGroup().controls.manualFats,
            this.formGroup().controls.manualCarbs,
            this.formGroup().controls.manualAlcohol,
        ])
            ? this.translateService.instant('PRODUCT_MANAGE.NUTRITION_ERRORS.MACROS_REQUIRED')
            : null;
    }

    private buildNutritionModeOptions(): void {
        this.nutritionModeOptions.set([
            {
                value: 'auto',
                label: this.translateService.instant('RECIPE_MANAGE.NUTRITION_MODE.AUTO'),
            },
            {
                value: 'manual',
                label: this.translateService.instant('RECIPE_MANAGE.NUTRITION_MODE.MANUAL'),
            },
        ]);
    }

    private buildNutritionScaleModeOptions(): void {
        this.nutritionScaleModeOptions.set([
            {
                value: 'recipe',
                label: this.translateService.instant('RECIPE_MANAGE.NUTRITION_SCALE_MODE.RECIPE'),
            },
            {
                value: 'portion',
                label: this.translateService.instant('RECIPE_MANAGE.NUTRITION_SCALE_MODE.PORTION'),
            },
        ]);
    }

    private updateCalorieWarning(): void {
        const form = this.formGroup();
        if (form.controls.calculateNutritionAutomatically.value) {
            this.nutritionWarning.set(null);
            return;
        }

        const calories = this.getNumberValue(form.controls.manualCalories);
        const proteins = this.getNumberValue(form.controls.manualProteins);
        const fats = this.getNumberValue(form.controls.manualFats);
        const carbs = this.getNumberValue(form.controls.manualCarbs);
        const alcohol = this.getNumberValue(form.controls.manualAlcohol);
        this.nutritionWarning.set(
            calculateCalorieMismatchWarning({
                calories,
                proteins,
                fats,
                carbs,
                alcohol,
                threshold: DEFAULT_CALORIE_MISMATCH_THRESHOLD,
            }),
        );
    }

    private getNumberValue(control: FormControl<number | string | null>): number {
        return getControlNumericValue(control);
    }
}
