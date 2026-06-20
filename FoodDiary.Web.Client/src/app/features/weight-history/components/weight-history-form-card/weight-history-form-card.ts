import { ChangeDetectionStrategy, Component, input, output } from '@angular/core';
import { type FieldTree, FormField, FormRoot } from '@angular/forms/signals';
import { TranslatePipe } from '@ngx-translate/core';
import { FdUiButtonComponent } from 'fd-ui-kit/button/fd-ui-button';
import { FdUiCardComponent } from 'fd-ui-kit/card/fd-ui-card';
import { FdUiDateInputComponent } from 'fd-ui-kit/date-input/fd-ui-date-input';
import { FdUiFormErrorComponent } from 'fd-ui-kit/form-error/fd-ui-form-error';
import { FdUiInputComponent } from 'fd-ui-kit/input/fd-ui-input';

@Component({
    selector: 'fd-weight-history-form-card',
    imports: [
        FormField,
        FormRoot,
        FdUiButtonComponent,
        FdUiCardComponent,
        FdUiDateInputComponent,
        FdUiFormErrorComponent,
        FdUiInputComponent,
        TranslatePipe,
    ],
    templateUrl: './weight-history-form-card.html',
    styleUrl: '../../pages/weight-history-page/weight-history-page.scss',
    changeDetection: ChangeDetectionStrategy.OnPush,
})
export class WeightHistoryFormCardComponent {
    public readonly form = input.required<FieldTree<{ date: string; weight: string }>>();
    public readonly isSaving = input.required<boolean>();
    public readonly isEditing = input.required<boolean>();
    public readonly error = input<string | null>(null);

    public readonly editCancel = output();
}
