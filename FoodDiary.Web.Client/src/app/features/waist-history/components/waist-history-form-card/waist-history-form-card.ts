import { ChangeDetectionStrategy, Component, inject, input, output } from '@angular/core';
import { type FieldTree, FormField, FormRoot } from '@angular/forms/signals';
import { TranslatePipe } from '@ngx-translate/core';
import { FdUiButtonComponent } from 'fd-ui-kit/button/fd-ui-button';
import { FdUiDateInputComponent } from 'fd-ui-kit/date-input/fd-ui-date-input';
import { FdUiFormErrorComponent } from 'fd-ui-kit/form-error/fd-ui-form-error';
import { FdUiInputComponent } from 'fd-ui-kit/input/fd-ui-input';

import { MeasurementUnitPipe } from '../../../../shared/measurements/measurement-display.pipe';
import { MeasurementSystemService } from '../../../../shared/measurements/measurement-system.service';

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
}
