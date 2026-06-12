import { ChangeDetectionStrategy, Component, computed, DestroyRef, inject, input, signal } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { type FieldTree, FormField } from '@angular/forms/signals';
import { TranslatePipe, TranslateService } from '@ngx-translate/core';
import { FdUiCardComponent } from 'fd-ui-kit/card/fd-ui-card';
import { FD_VALIDATION_ERRORS, type FdValidationErrors, resolveSignalFormFieldError } from 'fd-ui-kit/form-error/fd-ui-form-error';
import { FdUiIconComponent } from 'fd-ui-kit/icon/fd-ui-icon';
import { FdUiInputComponent } from 'fd-ui-kit/input/fd-ui-input';
import { FdUiSelectComponent, type FdUiSelectOption } from 'fd-ui-kit/select/fd-ui-select';
import { FdUiTextareaComponent } from 'fd-ui-kit/textarea/fd-ui-textarea';

import { ImageUploadFieldComponent } from '../../../../../components/shared/image-upload-field/image-upload-field';
import { RecipeVisibility } from '../../../models/recipe.data';
import type { RecipeFormValues } from '../recipe-manage-lib/recipe-manage.types';

const ERROR_FIELDS = ['name', 'cookTime', 'prepTime', 'servings', 'description', 'visibility', 'comment'] as const;
type ErrorField = (typeof ERROR_FIELDS)[number];
type FieldErrors = Record<ErrorField, string | null>;

@Component({
    selector: 'fd-recipe-basic-info',
    imports: [
        FormField,
        TranslatePipe,
        FdUiCardComponent,
        FdUiIconComponent,
        FdUiInputComponent,
        FdUiTextareaComponent,
        FdUiSelectComponent,
        ImageUploadFieldComponent,
    ],
    templateUrl: './recipe-basic-info.html',
    styleUrls: ['./recipe-basic-info.scss'],
    changeDetection: ChangeDetectionStrategy.OnPush,
})
export class RecipeBasicInfoComponent {
    private readonly translateService = inject(TranslateService);
    private readonly destroyRef = inject(DestroyRef);
    private readonly validationErrors = inject<FdValidationErrors>(FD_VALIDATION_ERRORS, { optional: true });
    private readonly languageVersion = signal(0);

    public readonly form = input.required<FieldTree<RecipeFormValues>>();
    protected readonly isAdvancedOpen = signal(false);
    protected readonly advancedToggleIcon = computed(() => (this.isAdvancedOpen() ? 'expand_less' : 'expand_more'));
    protected readonly visibilitySelectOptions = computed<Array<FdUiSelectOption<RecipeVisibility>>>(() => {
        this.languageVersion();

        return Object.values(RecipeVisibility).map(option => ({
            value: option,
            label: this.translateService.instant(`RECIPE_VISIBILITY.${option}`),
        }));
    });
    protected readonly fieldErrors = computed<FieldErrors>(() => {
        this.languageVersion();

        return this.buildFieldErrors();
    });

    public constructor() {
        this.translateService.onLangChange.pipe(takeUntilDestroyed(this.destroyRef)).subscribe(() => {
            this.languageVersion.update(version => version + 1);
        });
    }

    protected toggleAdvanced(): void {
        this.isAdvancedOpen.update(isOpen => !isOpen);
    }

    private buildFieldErrors(): FieldErrors {
        return ERROR_FIELDS.reduce<FieldErrors>((errors, field) => {
            errors[field] = this.getControlError(field);
            return errors;
        }, this.createEmptyFieldErrors());
    }

    private createEmptyFieldErrors(): FieldErrors {
        return {
            name: null,
            cookTime: null,
            prepTime: null,
            servings: null,
            description: null,
            visibility: null,
            comment: null,
        };
    }

    private getControlError(controlName: ErrorField): string | null {
        return resolveSignalFormFieldError(this.form()[controlName], this.validationErrors, this.translateService);
    }
}
