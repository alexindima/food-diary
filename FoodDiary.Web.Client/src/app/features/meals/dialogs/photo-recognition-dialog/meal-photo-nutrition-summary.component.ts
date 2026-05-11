import { DecimalPipe } from '@angular/common';
import { ChangeDetectionStrategy, Component, input } from '@angular/core';
import { TranslatePipe } from '@ngx-translate/core';

import type { MacroSummaryItem } from './meal-photo-recognition-dialog.types';

@Component({
    selector: 'fd-meal-photo-nutrition-summary',
    imports: [DecimalPipe, TranslatePipe],
    templateUrl: './meal-photo-nutrition-summary.component.html',
    styleUrl: './meal-photo-recognition-dialog.component.scss',
    changeDetection: ChangeDetectionStrategy.OnPush,
    host: {
        style: 'display: contents',
    },
})
export class MealPhotoNutritionSummaryComponent {
    public readonly isNutritionLoading = input.required<boolean>();
    public readonly items = input.required<MacroSummaryItem[]>();
    public readonly errorKey = input.required<string | null>();
}
