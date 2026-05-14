import { DecimalPipe } from '@angular/common';
import { ChangeDetectionStrategy, Component, computed, input, output } from '@angular/core';
import { TranslatePipe } from '@ngx-translate/core';

import { MEAL_DETAIL_ITEM_PREVIEW_MAX_ITEMS } from '../meal-detail-lib/meal-detail.config';
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
    public readonly isItemPreviewExpanded = input.required<boolean>();

    public readonly visibleItems = computed(() =>
        this.isItemPreviewExpanded() ? this.items() : this.items().slice(0, MEAL_DETAIL_ITEM_PREVIEW_MAX_ITEMS),
    );
    public readonly hiddenItemPreviewCount = computed(() => Math.max(0, this.items().length - MEAL_DETAIL_ITEM_PREVIEW_MAX_ITEMS));

    public readonly itemPreviewExpandedToggle = output();
}
