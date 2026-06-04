import { ChangeDetectionStrategy, Component, DestroyRef, effect, inject, input, signal } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { type FieldTree, FormField } from '@angular/forms/signals';
import { TranslatePipe, TranslateService } from '@ngx-translate/core';
import { FdUiCardComponent } from 'fd-ui-kit/card/fd-ui-card';
import { FdUiInputComponent } from 'fd-ui-kit/input/fd-ui-input';
import { FdUiSelectComponent, type FdUiSelectOption } from 'fd-ui-kit/select/fd-ui-select';
import { FdUiTextareaComponent } from 'fd-ui-kit/textarea/fd-ui-textarea';

import { ImageUploadFieldComponent } from '../../../../../components/shared/image-upload-field/image-upload-field';
import { getNumberProperty } from '../../../../../shared/lib/unknown-value.utils';
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

    public readonly form = input.required<FieldTree<RecipeFormValues>>();
    protected readonly visibilitySelectOptions = signal<Array<FdUiSelectOption<RecipeVisibility>>>([]);
    protected readonly fieldErrors = signal<FieldErrors>(this.createEmptyFieldErrors());

    public constructor() {
        effect(() => {
            this.trackErrorDependencies();
            this.fieldErrors.set(this.buildFieldErrors());
        });

        this.translateService.onLangChange.pipe(takeUntilDestroyed(this.destroyRef)).subscribe(() => {
            this.refreshTranslatedState();
        });

        effect(() => {
            this.refreshTranslatedState();
        });
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
        const state = this.form()[controlName]();

        if (!state.touched() && !state.dirty()) {
            return null;
        }

        if (!state.invalid()) {
            return null;
        }

        const errors = state.errors();
        if (errors.some(error => error.kind === 'required')) {
            return this.translateService.instant('FORM_ERRORS.REQUIRED');
        }

        if (errors.some(error => error.kind === 'min')) {
            const min = getNumberProperty(state.getError('min'), 'min') ?? 0;
            return this.translateService.instant('FORM_ERRORS.INVALID_MIN_AMOUNT_MUST_BE_MORE_ZERO', { min });
        }

        if (errors.some(error => error.kind === 'maxLength')) {
            return this.translateService.instant('FORM_ERRORS.UNKNOWN');
        }

        return this.translateService.instant('FORM_ERRORS.UNKNOWN');
    }

    private buildVisibilityOptions(): void {
        this.visibilitySelectOptions.set(
            Object.values(RecipeVisibility).map(option => ({
                value: option,
                label: this.translateService.instant(`RECIPE_VISIBILITY.${option}`),
            })),
        );
    }

    private refreshTranslatedState(): void {
        this.buildVisibilityOptions();
        this.fieldErrors.set(this.buildFieldErrors());
    }

    private trackErrorDependencies(): void {
        ERROR_FIELDS.forEach(field => {
            const state = this.form()[field]();
            state.errors();
            state.touched();
            state.dirty();
        });
    }
}
