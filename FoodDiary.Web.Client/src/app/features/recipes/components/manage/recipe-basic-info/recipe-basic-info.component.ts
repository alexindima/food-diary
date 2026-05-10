import { ChangeDetectionStrategy, Component, effect, inject, input, signal } from '@angular/core';
import { type AbstractControl, type FormGroup, ReactiveFormsModule } from '@angular/forms';
import { TranslatePipe, TranslateService } from '@ngx-translate/core';
import { FdUiCardComponent } from 'fd-ui-kit/card/fd-ui-card.component';
import { FdUiInputComponent } from 'fd-ui-kit/input/fd-ui-input.component';
import { FdUiSelectComponent, type FdUiSelectOption } from 'fd-ui-kit/select/fd-ui-select.component';
import { FdUiTextareaComponent } from 'fd-ui-kit/textarea/fd-ui-textarea.component';
import { EMPTY, merge, type Observable } from 'rxjs';

import { ImageUploadFieldComponent } from '../../../../../components/shared/image-upload-field/image-upload-field.component';
import { type RecipeVisibility } from '../../../models/recipe.data';
import { type RecipeFormData } from '../recipe-manage.types';

const ERROR_FIELDS = ['name', 'cookTime', 'prepTime', 'servings', 'description', 'visibility', 'comment'] as const;
type ErrorField = (typeof ERROR_FIELDS)[number];
type FieldErrors = Record<ErrorField, string | null>;

@Component({
    selector: 'fd-recipe-basic-info',
    standalone: true,
    imports: [
        ReactiveFormsModule,
        TranslatePipe,
        FdUiCardComponent,
        FdUiInputComponent,
        FdUiTextareaComponent,
        FdUiSelectComponent,
        ImageUploadFieldComponent,
    ],
    templateUrl: './recipe-basic-info.component.html',
    styleUrls: ['./recipe-basic-info.component.scss'],
    changeDetection: ChangeDetectionStrategy.OnPush,
})
export class RecipeBasicInfoComponent {
    private readonly translateService = inject(TranslateService);

    public readonly formGroup = input.required<FormGroup<RecipeFormData>>();
    public readonly visibilitySelectOptions = input.required<FdUiSelectOption<RecipeVisibility>[]>();
    public readonly fieldErrors = signal<FieldErrors>(this.createEmptyFieldErrors());

    private readonly errorSync = effect(onCleanup => {
        const form = this.formGroup();
        const formEvents = (form as { events?: Observable<unknown> }).events ?? EMPTY;
        const languageChanges = (this.translateService as { onLangChange?: Observable<unknown> }).onLangChange ?? EMPTY;
        const refresh = (): void => {
            this.fieldErrors.set(this.buildFieldErrors(form));
        };

        refresh();
        const subscription = merge(formEvents, form.statusChanges, form.valueChanges, languageChanges).subscribe(() => {
            refresh();
        });
        onCleanup(() => {
            subscription.unsubscribe();
        });
    });

    private buildFieldErrors(form: FormGroup<RecipeFormData>): FieldErrors {
        return ERROR_FIELDS.reduce<FieldErrors>((errors, field) => {
            errors[field] = this.resolveControlError(form.controls[field]);
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
