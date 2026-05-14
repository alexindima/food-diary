import { DecimalPipe } from '@angular/common';
import { ChangeDetectionStrategy, Component, input, output } from '@angular/core';
import { TranslatePipe } from '@ngx-translate/core';

import type { MealDetailItemPreview } from '../meal-detail-lib/meal-detail.types';

@Component({
    selector: 'fd-meal-detail-item-preview',
    imports: [DecimalPipe, TranslatePipe],
    templateUrl: './meal-detail-item-preview.component.html',
    styleUrl: '../meal-detail/meal-detail.component.scss',
    changeDetection: ChangeDetectionStrategy.OnPush,
})
export class MealDetailItemPreviewComponent {
    public readonly items = input.required<readonly MealDetailItemPreview[]>();
    public readonly itemsCount = input.required<number>();
    public readonly hiddenItemPreviewCount = input.required<number>();
    public readonly isItemPreviewExpanded = input.required<boolean>();

    public readonly itemPreviewExpandedToggle = output();
}
