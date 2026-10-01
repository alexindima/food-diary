import { ChangeDetectionStrategy, Component, computed, inject, input, output } from '@angular/core';
import { type FieldTree, FormField, FormRoot } from '@angular/forms/signals';
import { TranslatePipe } from '@ngx-translate/core';
import { FdUiButtonComponent } from 'fd-ui-kit/button/fd-ui-button';
import { FdUiDateInputComponent } from 'fd-ui-kit/date-input/fd-ui-date-input';
import { FdUiFormErrorComponent } from 'fd-ui-kit/form-error/fd-ui-form-error';
import { FdUiInputComponent } from 'fd-ui-kit/input/fd-ui-input';

import { MeasurementUnitPipe } from '../../../../shared/measurements/measurement-display.pipe';
import { MeasurementSystemService } from '../../../../shared/measurements/measurement-system.service';
import { MAX_WAIST_CM, MIN_WAIST_CM } from '../../lib/waist-history.constants';

@Component({
    selector: 'fd-waist-history-form-card',
    imports: [
        FormField,
        FormRoot,
        FdUiButtonComponent,
        FdUiDateInputComponent,
        FdUiFormErrorComponent,
        FdUiInputComponent,
        MeasurementUnitPipe,
        TranslatePipe,
    ],
    templateUrl: './waist-history-form-card.html',
    styleUrl: '../../pages/waist-history-page/waist-history-page.scss',
    changeDetection: ChangeDetectionStrategy.OnPush,
})
export class WaistHistoryFormCardComponent {
    protected readonly measurements = inject(MeasurementSystemService);
    public readonly form = input.required<FieldTree<{ date: string; circumference: string }>>();
    public readonly isSaving = input.required<boolean>();
    public readonly isEditing = input.required<boolean>();
    public readonly error = input<string | null>(null);

    public readonly editCancel = output();

    protected readonly dateError = computed(() => {
        const field = this.form().date();
        return field.invalid() && field.touched() ? 'FORM_ERRORS.REQUIRED' : null;
    });
    protected readonly circumferenceError = computed(() => {
        const field = this.form().circumference();
        if (!field.invalid() || !(field.touched() || field.dirty())) {
            return null;
        }
        return field.errors().some(error => error.kind === 'required') ? 'FORM_ERRORS.REQUIRED' : 'WAIST_HISTORY.VALUE_RANGE';
    });
    protected readonly circumferenceRange = computed(() => ({
        min: this.measurements.displayLength(MIN_WAIST_CM),
        max: this.measurements.displayLength(MAX_WAIST_CM),
    }));
}
