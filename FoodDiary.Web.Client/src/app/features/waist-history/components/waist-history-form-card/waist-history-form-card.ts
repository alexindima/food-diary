import { ChangeDetectionStrategy, Component, input, output } from '@angular/core';
import { type FormGroup, ReactiveFormsModule } from '@angular/forms';
import { TranslatePipe } from '@ngx-translate/core';
import { FdUiButtonComponent } from 'fd-ui-kit/button/fd-ui-button';
import { FdUiCardComponent } from 'fd-ui-kit/card/fd-ui-card';
import { FdUiDateInputComponent } from 'fd-ui-kit/date-input/fd-ui-date-input';
import { FdUiInputComponent } from 'fd-ui-kit/input/fd-ui-input';

@Component({
    selector: 'fd-waist-history-form-card',
    imports: [ReactiveFormsModule, FdUiButtonComponent, FdUiCardComponent, FdUiDateInputComponent, FdUiInputComponent, TranslatePipe],
    templateUrl: './waist-history-form-card.html',
    styleUrl: '../../pages/waist-history-page/waist-history-page.scss',
    changeDetection: ChangeDetectionStrategy.OnPush,
})
export class WaistHistoryFormCardComponent {
    public readonly form = input.required<FormGroup>();
    public readonly isSaving = input.required<boolean>();
    public readonly isEditing = input.required<boolean>();

    public readonly formSubmit = output();
    public readonly editCancel = output();
}
