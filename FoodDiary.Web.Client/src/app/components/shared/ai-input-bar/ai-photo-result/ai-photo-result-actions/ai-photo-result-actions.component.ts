import { ChangeDetectionStrategy, Component, input, output } from '@angular/core';
import { TranslatePipe } from '@ngx-translate/core';
import { FdUiButtonComponent, FdUiHintDirective } from 'fd-ui-kit';

import type { AiEditActionView } from '../ai-photo-result-lib/ai-photo-result.types';

@Component({
    selector: 'fd-ai-photo-result-actions',
    imports: [TranslatePipe, FdUiButtonComponent, FdUiHintDirective],
    templateUrl: './ai-photo-result-actions.component.html',
    styleUrl: '../ai-photo-result.component.scss',
    changeDetection: ChangeDetectionStrategy.OnPush,
    host: {
        style: 'display: contents',
    },
})
export class AiPhotoResultActionsComponent {
    public readonly isAnalyzing = input.required<boolean>();
    public readonly isEditing = input.required<boolean>();
    public readonly editActionView = input.required<AiEditActionView>();

    public readonly reanalyze = output();
    public readonly editAction = output();
    public readonly editCancel = output();
}
