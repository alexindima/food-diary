import { CdkDragHandle } from '@angular/cdk/drag-drop';
import { ChangeDetectionStrategy, Component, computed, effect, inject, input, output, signal } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { type FormArray, type FormGroup, ReactiveFormsModule } from '@angular/forms';
import { TranslatePipe, TranslateService } from '@ngx-translate/core';
import { FdUiHintDirective } from 'fd-ui-kit';
import { FdUiButtonComponent } from 'fd-ui-kit/button/fd-ui-button.component';
import { FdUiCardComponent } from 'fd-ui-kit/card/fd-ui-card.component';
import { FdUiInputComponent } from 'fd-ui-kit/input/fd-ui-input.component';
import { FdUiTextareaComponent } from 'fd-ui-kit/textarea/fd-ui-textarea.component';

import { ImageUploadFieldComponent } from '../../../../../components/shared/image-upload-field/image-upload-field.component';
import type { FormGroupControls } from '../../../../../shared/lib/common.data';
import { resolveRecipeControlError } from '../recipe-manage-lib/recipe-form-error.utils';
import type { IngredientFormValues, StepFormData } from '../recipe-manage-lib/recipe-manage.types';

@Component({
    selector: 'fd-recipe-step-card',
    imports: [
        ReactiveFormsModule,
        TranslatePipe,
        FdUiHintDirective,
        FdUiCardComponent,
        FdUiButtonComponent,
        FdUiInputComponent,
        FdUiTextareaComponent,
        ImageUploadFieldComponent,
        CdkDragHandle,
    ],
    templateUrl: './recipe-step-card.component.html',
    styleUrls: ['./recipe-step-card.component.scss'],
    changeDetection: ChangeDetectionStrategy.OnPush,
})
export class RecipeStepCardComponent {
    private readonly translateService = inject(TranslateService);
    private readonly formRevision = signal(0);
    private readonly currentLanguage = signal(this.getCurrentLanguage());

    public readonly stepFormGroup = input.required<FormGroup<StepFormData>>();
    public readonly stepIndex = input.required<number>();
    public readonly isExpanded = input.required<boolean>();
    public readonly dragDisabled = input.required<boolean>();

    public readonly removeStep = output();
    public readonly toggleExpanded = output();
    public readonly addIngredient = output();
    public readonly removeIngredient = output<number>();
    public readonly selectProduct = output<number>();

    public readonly isStepTitleEditing = signal(false);
    public readonly ingredientsCount = computed(() => {
        this.formRevision();
        return this.ingredients.length;
    });
    public readonly descriptionSummary = computed(() => {
        this.formRevision();
        this.currentLanguage();
        const description = this.stepFormGroup().controls.description.value.trim();
        if (description.length === 0) {
            return this.translateService.instant('RECIPE_MANAGE.STEP_NO_DESCRIPTION');
        }
        return description;
    });
    public readonly titleDisplay = computed(() => {
        this.formRevision();
        this.currentLanguage();
        const titleValue = this.stepFormGroup().controls.title.value;
        const trimmedTitle = typeof titleValue === 'string' ? titleValue.trim() : '';
        if (trimmedTitle.length > 0) {
            return trimmedTitle;
        }
        return this.translateService.instant('RECIPE_MANAGE.STEP_TITLE', { index: this.stepIndex() + 1 });
    });
    public readonly descriptionError = computed(() => {
        this.formRevision();
        this.currentLanguage();
        return resolveRecipeControlError(this.stepFormGroup().controls.description, this.translateService);
    });
    public readonly expandedIcon = computed(() => (this.isExpanded() ? 'expand_less' : 'expand_more'));
    public readonly isFirst = computed(() => this.stepIndex() === 0);
    public readonly ingredientRows = computed<RecipeIngredientRowView[]>(() => {
        this.formRevision();
        this.currentLanguage();

        return this.ingredients.controls.map((ingredient, index) => {
            const food = ingredient.controls.food.value;
            const nestedRecipeId = ingredient.controls.nestedRecipeId.value;
            const unitKey = food?.baseUnit !== undefined ? `PRODUCT_AMOUNT_UNITS.${food.baseUnit}` : null;

            return {
                index,
                prefixIcon: nestedRecipeId !== null && nestedRecipeId.length > 0 ? 'menu_book' : food !== null ? 'restaurant' : 'search',
                amountLabel: this.resolveIngredientAmountLabel(nestedRecipeId !== null && nestedRecipeId.length > 0, unitKey),
                foodNameError: resolveRecipeControlError(ingredient.controls.foodName, this.translateService),
                amountError: resolveRecipeControlError(ingredient.controls.amount, this.translateService),
            };
        });
    });

    public constructor() {
        this.translateService.onLangChange.pipe(takeUntilDestroyed()).subscribe(() => {
            this.currentLanguage.set(this.getCurrentLanguage());
        });

        effect(onCleanup => {
            const form = this.stepFormGroup();
            const bumpFormRevision = (): void => {
                this.formRevision.update(revision => revision + 1);
            };
            const valueSubscription = form.valueChanges.subscribe(bumpFormRevision);
            const statusSubscription = form.statusChanges.subscribe(bumpFormRevision);

            onCleanup(() => {
                valueSubscription.unsubscribe();
                statusSubscription.unsubscribe();
            });
        });
    }

    public get ingredients(): FormArray<FormGroup<FormGroupControls<IngredientFormValues>>> {
        return this.stepFormGroup().controls.ingredients;
    }

    public toggleStepTitleEdit(): void {
        if (this.isStepTitleEditing()) {
            this.commitStepTitle();
            this.isStepTitleEditing.set(false);
            return;
        }
        this.isStepTitleEditing.set(true);
    }

    public onStepTitleBlur(): void {
        this.commitStepTitle();
        this.isStepTitleEditing.set(false);
    }

    public onProductSelectClick(ingredientIndex: number): void {
        this.selectProduct.emit(ingredientIndex);
    }

    public onRemoveIngredient(ingredientIndex: number): void {
        this.removeIngredient.emit(ingredientIndex);
    }

    public onAddIngredient(): void {
        this.addIngredient.emit();
    }

    public onRemoveStep(): void {
        this.removeStep.emit();
    }

    public onToggleExpanded(): void {
        this.toggleExpanded.emit();
    }

    private commitStepTitle(): void {
        const titleControl = this.stepFormGroup().controls.title;
        const titleValue = titleControl.value;
        const trimmedTitle = typeof titleValue === 'string' ? titleValue.trim() : '';
        titleControl.setValue(trimmedTitle.length > 0 ? trimmedTitle : null);
    }

    private resolveIngredientAmountLabel(isNestedRecipe: boolean, unitKey: string | null): string {
        if (isNestedRecipe) {
            return this.translateService.instant('RECIPE_SELECT_DIALOG.SERVINGS');
        }

        const amountLabel = this.translateService.instant('RECIPE_MANAGE.INGREDIENT_AMOUNT');
        if (unitKey === null || unitKey.length === 0) {
            return amountLabel;
        }

        return `${amountLabel} (${this.translateService.instant(unitKey)})`;
    }

    private getCurrentLanguage(): string {
        const currentLang = this.normalizeLanguage(this.translateService.getCurrentLang());
        if (currentLang.length > 0) {
            return currentLang;
        }

        const fallbackLang = this.normalizeLanguage(this.translateService.getFallbackLang());
        return fallbackLang.length > 0 ? fallbackLang : 'en';
    }

    private normalizeLanguage(value: unknown): string {
        return typeof value === 'string' ? value : '';
    }
}

type RecipeIngredientRowView = {
    index: number;
    prefixIcon: 'menu_book' | 'restaurant' | 'search';
    amountLabel: string;
    foodNameError: string | null;
    amountError: string | null;
};
