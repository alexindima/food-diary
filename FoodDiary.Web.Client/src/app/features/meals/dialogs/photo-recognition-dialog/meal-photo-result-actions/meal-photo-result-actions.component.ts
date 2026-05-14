import { ChangeDetectionStrategy, Component, computed, input, output } from '@angular/core';
import { TranslatePipe } from '@ngx-translate/core';
import { FdUiButtonComponent, FdUiHintDirective } from 'fd-ui-kit';

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
    public readonly hasSelectionAsset = input.required<boolean>();
    public readonly isEditing = input.required<boolean>();
    public readonly editActionState = computed(() =>
        this.isEditing()
            ? {
                  variant: 'primary' as const,
                  fill: 'solid' as const,
                  labelKey: 'CONSUMPTION_MANAGE.PHOTO_AI_DIALOG.SAVE',
              }
            : {
                  variant: 'secondary' as const,
                  fill: 'outline' as const,
                  labelKey: 'CONSUMPTION_MANAGE.PHOTO_AI_DIALOG.EDIT_BUTTON',
              },
    );

    public readonly reanalyze = output();
    public readonly editAction = output();
}
