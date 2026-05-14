import { ChangeDetectionStrategy, Component, input, output } from '@angular/core';
import { TranslatePipe } from '@ngx-translate/core';
import { FdUiButtonComponent, FdUiHintDirective } from 'fd-ui-kit';

import type { PhotoAiEditActionState } from '../meal-photo-recognition-dialog-lib/meal-photo-recognition-dialog.types';

@Component({
    selector: 'fd-meal-photo-result-actions',
    imports: [TranslatePipe, FdUiButtonComponent, FdUiHintDirective],
    templateUrl: './meal-photo-result-actions.component.html',
    styleUrl: '../meal-photo-recognition-dialog.component.scss',
    changeDetection: ChangeDetectionStrategy.OnPush,
    host: {
        style: 'display: contents',
    },
})
export class MealPhotoResultActionsComponent {
    public readonly isLoading = input.required<boolean>();
    public readonly hasSelectionAsset = input.required<boolean>();
    public readonly isEditing = input.required<boolean>();
    public readonly editActionState = input.required<PhotoAiEditActionState>();

    public readonly reanalyze = output();
    public readonly editAction = output();
}
