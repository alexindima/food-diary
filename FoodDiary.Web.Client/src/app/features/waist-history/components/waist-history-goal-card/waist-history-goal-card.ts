import { ChangeDetectionStrategy, Component, input, output } from '@angular/core';
import { type FormControl, ReactiveFormsModule } from '@angular/forms';
import { TranslatePipe } from '@ngx-translate/core';
import { FdUiButtonComponent } from 'fd-ui-kit/button/fd-ui-button';
import { FdUiCardComponent } from 'fd-ui-kit/card/fd-ui-card';
import { FdUiInputComponent } from 'fd-ui-kit/input/fd-ui-input';

@Component({
    selector: 'fd-waist-history-goal-card',
    imports: [ReactiveFormsModule, FdUiButtonComponent, FdUiCardComponent, FdUiInputComponent, TranslatePipe],
    templateUrl: './waist-history-goal-card.html',
    styleUrl: '../../pages/waist-history-page/waist-history-page.scss',
    changeDetection: ChangeDetectionStrategy.OnPush,
})
export class WaistHistoryGoalCardComponent {
    public readonly control = input.required<FormControl<string | null>>();
    public readonly isSaving = input.required<boolean>();

    public readonly saveGoal = output();
}
