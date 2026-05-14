import { ChangeDetectionStrategy, Component, input } from '@angular/core';
import { TranslatePipe } from '@ngx-translate/core';

import type { RecognizedItemView } from '../meal-photo-recognition-dialog-lib/meal-photo-recognition-dialog.types';

@Component({
    selector: 'fd-meal-photo-result-table',
    imports: [TranslatePipe],
    templateUrl: './meal-photo-result-table.component.html',
    styleUrl: '../meal-photo-recognition-dialog.component.scss',
    changeDetection: ChangeDetectionStrategy.OnPush,
    host: {
        style: 'display: contents',
    },
})
export class MealPhotoResultTableComponent {
    public readonly resultViews = input.required<RecognizedItemView[]>();
}
