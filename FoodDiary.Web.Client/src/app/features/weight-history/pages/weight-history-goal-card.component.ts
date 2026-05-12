import { ChangeDetectionStrategy, Component, input, output } from '@angular/core';
import { type FormControl, ReactiveFormsModule } from '@angular/forms';
import { TranslatePipe } from '@ngx-translate/core';
import { FdUiButtonComponent } from 'fd-ui-kit/button/fd-ui-button.component';
import { FdUiCardComponent } from 'fd-ui-kit/card/fd-ui-card.component';
import { FdUiInputComponent } from 'fd-ui-kit/input/fd-ui-input.component';

@Component({
    selector: 'fd-weight-history-goal-card',
    imports: [ReactiveFormsModule, FdUiButtonComponent, FdUiCardComponent, FdUiInputComponent, TranslatePipe],
    templateUrl: './weight-history-goal-card.component.html',
    styleUrl: './weight-history-page.component.scss',
    changeDetection: ChangeDetectionStrategy.OnPush,
})
export class WeightHistoryGoalCardComponent {
    public readonly control = input.required<FormControl<string | null>>();
    public readonly isSaving = input.required<boolean>();

    public readonly saveGoal = output();
}
