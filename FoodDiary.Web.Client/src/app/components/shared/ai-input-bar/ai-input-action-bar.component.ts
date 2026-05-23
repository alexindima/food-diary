import { ChangeDetectionStrategy, Component, input, output } from '@angular/core';
import { TranslatePipe } from '@ngx-translate/core';
import { FdUiButtonComponent } from 'fd-ui-kit/button/fd-ui-button.component';

import { AiInputBarComponent } from './ai-input-bar.component';
import type { AiInputBarMode, AiInputBarResult } from './ai-input-bar.types';

@Component({
    selector: 'fd-ai-input-action-bar',
    imports: [TranslatePipe, FdUiButtonComponent, AiInputBarComponent],
    templateUrl: './ai-input-action-bar.component.html',
    styleUrl: './ai-input-action-bar.component.scss',
    changeDetection: ChangeDetectionStrategy.OnPush,
})
export class AiInputActionBarComponent {
    public readonly isProcessing = input(false);
    public readonly mode = input<AiInputBarMode>('emit');
    public readonly mealType = input<string | null>(null);
    public readonly manualPrefixLabelKey = input('CONSUMPTION_LIST.MANUAL_ADD_OR');
    public readonly manualActionLabelKey = input('CONSUMPTION_LIST.ADD_MANUALLY_BUTTON');
    public readonly manualActionIcon = input('add');

    public readonly mealRecognized = output<AiInputBarResult>();
    public readonly mealCreateRequested = output<AiInputBarResult>();
    public readonly manualAction = output();
}
