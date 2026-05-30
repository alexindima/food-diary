import { ChangeDetectionStrategy, Component, input } from '@angular/core';
import { type FormGroup, ReactiveFormsModule } from '@angular/forms';
import { TranslatePipe } from '@ngx-translate/core';
import { FdUiCardComponent } from 'fd-ui-kit/card/fd-ui-card';
import { FdUiDateInputComponent } from 'fd-ui-kit/date-input/fd-ui-date-input';
import type { FdUiSelectOption } from 'fd-ui-kit/select/fd-ui-select';
import { FdUiSelectComponent } from 'fd-ui-kit/select/fd-ui-select';
import { FdUiTextareaComponent } from 'fd-ui-kit/textarea/fd-ui-textarea';
import { FdUiTimeInputComponent } from 'fd-ui-kit/time-input/fd-ui-time-input';

import { ImageUploadFieldComponent } from '../../../../../components/shared/image-upload-field/image-upload-field';
import type { ConsumptionFormData } from '../meal-manage-lib/meal-manage.types';

export type MealGeneralFieldErrors = {
    date: string | null;
    time: string | null;
    mealType: string | null;
};

@Component({
    selector: 'fd-meal-general-info',
    templateUrl: './meal-general-info.html',
    styleUrls: ['../meal-manage-form.scss'],
    changeDetection: ChangeDetectionStrategy.OnPush,
    imports: [
        ReactiveFormsModule,
        TranslatePipe,
        FdUiCardComponent,
        FdUiDateInputComponent,
        FdUiTimeInputComponent,
        FdUiSelectComponent,
        FdUiTextareaComponent,
        ImageUploadFieldComponent,
    ],
})
export class MealGeneralInfoComponent {
    public readonly consumptionForm = input.required<FormGroup<ConsumptionFormData>>();
    public readonly mealTypeSelectOptions = input.required<Array<FdUiSelectOption<string>>>();
    public readonly generalErrors = input.required<MealGeneralFieldErrors>();
}
