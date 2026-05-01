import { ChangeDetectionStrategy, Component, inject, input } from '@angular/core';
import { AbstractControl, FormGroup, ReactiveFormsModule } from '@angular/forms';
import { TranslatePipe, TranslateService } from '@ngx-translate/core';
import { FdUiCardComponent } from 'fd-ui-kit/card/fd-ui-card.component';
import { FdUiInputComponent } from 'fd-ui-kit/input/fd-ui-input.component';
import { FdUiSelectComponent, FdUiSelectOption } from 'fd-ui-kit/select/fd-ui-select.component';
import { FdUiTextareaComponent } from 'fd-ui-kit/textarea/fd-ui-textarea.component';

import { ImageUploadFieldComponent } from '../../../../../components/shared/image-upload-field/image-upload-field.component';
import { RecipeVisibility } from '../../../models/recipe.data';
import { RecipeFormData } from '../recipe-manage.types';

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

    public getFieldError(controlName: keyof RecipeFormData): string | null {
        return this.resolveControlError(this.formGroup().controls[controlName]);
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
