import { ChangeDetectionStrategy, Component, input, output } from '@angular/core';
import { type FormGroup, ReactiveFormsModule } from '@angular/forms';
import { TranslatePipe } from '@ngx-translate/core';
import { FdUiButtonComponent } from 'fd-ui-kit/button/fd-ui-button.component';
import { FdUiCardComponent } from 'fd-ui-kit/card/fd-ui-card.component';
import { FdUiDateInputComponent } from 'fd-ui-kit/date-input/fd-ui-date-input.component';
import { FdUiInputComponent } from 'fd-ui-kit/input/fd-ui-input.component';

@Component({
    selector: 'fd-weight-history-form-card',
    imports: [ReactiveFormsModule, FdUiButtonComponent, FdUiCardComponent, FdUiDateInputComponent, FdUiInputComponent, TranslatePipe],
    templateUrl: './weight-history-form-card.component.html',
    styleUrl: '../../pages/weight-history-page/weight-history-page.component.scss',
    changeDetection: ChangeDetectionStrategy.OnPush,
})
export class WeightHistoryFormCardComponent {
    public readonly form = input.required<FormGroup>();
    public readonly isSaving = input.required<boolean>();
    public readonly isEditing = input.required<boolean>();

    public readonly formSubmit = output();
    public readonly editCancel = output();
}
