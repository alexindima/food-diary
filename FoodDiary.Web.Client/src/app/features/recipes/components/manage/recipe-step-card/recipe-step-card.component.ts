import { ChangeDetectionStrategy, Component, inject, input, output } from '@angular/core';
import { AbstractControl, FormArray, FormGroup, ReactiveFormsModule } from '@angular/forms';
import { TranslatePipe, TranslateService } from '@ngx-translate/core';
import { FdUiHintDirective } from 'fd-ui-kit';
import { FormGroupControls } from '../../../../../shared/lib/common.data';
import { FdUiCardComponent } from 'fd-ui-kit/card/fd-ui-card.component';
import { FdUiButtonComponent } from 'fd-ui-kit/button/fd-ui-button.component';
import { FdUiInputComponent } from 'fd-ui-kit/input/fd-ui-input.component';
import { FdUiTextareaComponent } from 'fd-ui-kit/textarea/fd-ui-textarea.component';
import { ImageUploadFieldComponent } from '../../../../../components/shared/image-upload-field/image-upload-field.component';
import { StepFormData, IngredientFormValues } from '../recipe-manage.types';
import { CdkDragHandle } from '@angular/cdk/drag-drop';

@Component({
    selector: 'fd-recipe-step-card',
    standalone: true,
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

    public readonly stepFormGroup = input.required<FormGroup<StepFormData>>();
    public readonly stepIndex = input.required<number>();
    public readonly isExpanded = input<boolean>(true);
    public readonly isFirst = input<boolean>(false);
    public readonly dragDisabled = input<boolean>(false);

    public readonly removeStep = output<void>();
    public readonly toggleExpanded = output<void>();
    public readonly addIngredient = output<void>();
    public readonly removeIngredient = output<number>();
    public readonly selectProduct = output<number>();

    private isTitleEditing = false;

    public get ingredients(): FormArray<FormGroup<FormGroupControls<IngredientFormValues>>> {
        return this.stepFormGroup().controls.ingredients;
    }

    public get ingredientsCount(): number {
        return this.ingredients.length;
    }

    public get descriptionSummary(): string {
        const description = this.stepFormGroup().controls.description.value?.trim() ?? '';
        if (!description) {
            return this.translateService.instant('RECIPE_MANAGE.STEP_NO_DESCRIPTION');
        }
        return description;
    }

    public get titleDisplay(): string {
        const titleValue = this.stepFormGroup().controls.title.value;
        const trimmedTitle = typeof titleValue === 'string' ? titleValue.trim() : '';
        if (trimmedTitle.length > 0) {
            return trimmedTitle;
        }
        return this.translateService.instant('RECIPE_MANAGE.STEP_TITLE', { index: this.stepIndex() + 1 });
    }

    public get descriptionError(): string | null {
        return this.resolveControlError(this.stepFormGroup().controls.description);
    }

    public get isStepTitleEditing(): boolean {
        return this.isTitleEditing;
    }

    public toggleStepTitleEdit(): void {
        if (this.isTitleEditing) {
            this.commitStepTitle();
            this.isTitleEditing = false;
            return;
        }
        this.isTitleEditing = true;
    }

    public onStepTitleBlur(): void {
        this.commitStepTitle();
        this.isTitleEditing = false;
    }

    public getIngredientIcon(ingredientIndex: number): string {
        const ingredient = this.ingredients.at(ingredientIndex);
        if (ingredient.controls.nestedRecipeId.value) {
            return 'menu_book';
        }
        if (ingredient.controls.food.value) {
            return 'restaurant';
        }
        return 'search';
    }

    public getIngredientAmountLabel(ingredientIndex: number): string {
        const ingredient = this.ingredients.at(ingredientIndex);
        if (ingredient.controls.nestedRecipeId.value) {
            return this.translateService.instant('RECIPE_SELECT_DIALOG.SERVINGS');
        }
        const baseLabel = this.translateService.instant('RECIPE_MANAGE.INGREDIENT_AMOUNT');
        const unit = this.getProductUnit(ingredientIndex);
        return unit ? `${baseLabel} (${unit})` : baseLabel;
    }

    public getIngredientControlError(ingredientIndex: number, controlName: 'food' | 'foodName' | 'amount'): string | null {
        const ingredient = this.ingredients.at(ingredientIndex);
        return this.resolveControlError(ingredient.controls[controlName]);
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

    private getProductUnit(ingredientIndex: number): string | null {
        const foodControl = this.ingredients.at(ingredientIndex).controls.food;
        const unit = foodControl.value?.baseUnit;
        return unit ? this.translateService.instant('PRODUCT_AMOUNT_UNITS.' + unit.toUpperCase()) : null;
    }

    private commitStepTitle(): void {
        const titleControl = this.stepFormGroup().controls.title;
        const titleValue = titleControl.value;
        const trimmedTitle = typeof titleValue === 'string' ? titleValue.trim() : '';
        titleControl.setValue(trimmedTitle.length > 0 ? trimmedTitle : null);
    }

    private resolveControlError(control: AbstractControl | null): string | null {
        if (!control) {
            return null;
        }

        if (!control.touched && !control.dirty) {
            return null;
        }

        const errors = control.errors;
        if (!errors) {
            return null;
        }

        if (errors['required']) {
            return this.translateService.instant('FORM_ERRORS.REQUIRED');
        }

        if (errors['min']) {
            const min = errors['min'].min ?? 0;
            return this.translateService.instant('FORM_ERRORS.INVALID_MIN_AMOUNT_MUST_BE_MORE_ZERO', { min });
        }

        if (errors['nonEmptyArray']) {
            return this.translateService.instant('FORM_ERRORS.NON_EMPTY_ARRAY');
        }

        return this.translateService.instant('FORM_ERRORS.UNKNOWN');
    }
}
