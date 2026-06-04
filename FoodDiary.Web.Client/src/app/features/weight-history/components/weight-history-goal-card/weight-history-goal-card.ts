import { ChangeDetectionStrategy, Component, input, output } from '@angular/core';
import { type FieldTree, FormField } from '@angular/forms/signals';
import { TranslatePipe } from '@ngx-translate/core';
import { FdUiButtonComponent } from 'fd-ui-kit/button/fd-ui-button';
import { FdUiCardComponent } from 'fd-ui-kit/card/fd-ui-card';
import { FdUiInputComponent } from 'fd-ui-kit/input/fd-ui-input';

@Component({
    selector: 'fd-weight-history-goal-card',
    imports: [FormField, FdUiButtonComponent, FdUiCardComponent, FdUiInputComponent, TranslatePipe],
    templateUrl: './weight-history-goal-card.html',
    styleUrl: '../../pages/weight-history-page/weight-history-page.scss',
    changeDetection: ChangeDetectionStrategy.OnPush,
})
export class WeightHistoryGoalCardComponent {
    public readonly weightField = input.required<FieldTree<string>>();
    public readonly isSaving = input.required<boolean>();

    public readonly saveGoal = output();
}
