import { ChangeDetectionStrategy, Component, input, output } from '@angular/core';
import { TranslatePipe } from '@ngx-translate/core';

import { ImageUploadFieldComponent } from '../../../../../components/shared/image-upload-field/image-upload-field.component';
import type { ImageSelection } from '../../../../../shared/models/image-upload.data';

@Component({
    selector: 'fd-meal-photo-upload-panel',
    imports: [TranslatePipe, ImageUploadFieldComponent],
    templateUrl: './meal-photo-upload-panel.component.html',
    styleUrl: '../meal-photo-recognition-dialog.component.scss',
    changeDetection: ChangeDetectionStrategy.OnPush,
    host: {
        style: 'display: contents',
    },
})
export class MealPhotoUploadPanelComponent {
    public readonly initialSelection = input.required<ImageSelection | null>();
    public readonly statusKey = input.required<string | null>();
    public readonly isLoading = input.required<boolean>();
    public readonly isNutritionLoading = input.required<boolean>();

    public readonly imageChanged = output<ImageSelection | null>();
}
