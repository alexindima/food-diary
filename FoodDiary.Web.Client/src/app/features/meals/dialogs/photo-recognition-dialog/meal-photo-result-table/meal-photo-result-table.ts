import { ChangeDetectionStrategy, Component, input } from '@angular/core';
import { TranslatePipe } from '@ngx-translate/core';

import { injectCurrentLanguage } from '../../../../../shared/i18n/inject-current-language';
import { LocalizedNumberPipe } from '../../../../../shared/i18n/localized-number.pipe';
import type { RecognizedItemView } from '../meal-photo-recognition-dialog-lib/meal-photo-recognition-dialog.types';

@Component({
    selector: 'fd-meal-photo-result-table',
    imports: [TranslatePipe, LocalizedNumberPipe],
    templateUrl: './meal-photo-result-table.html',
    styleUrl: '../meal-photo-recognition-dialog.scss',
    changeDetection: ChangeDetectionStrategy.OnPush,
    host: {
        style: 'display: contents',
    },
})
export class MealPhotoResultTableComponent {
    protected readonly locale = injectCurrentLanguage();
    public readonly resultViews = input.required<RecognizedItemView[]>();
}
