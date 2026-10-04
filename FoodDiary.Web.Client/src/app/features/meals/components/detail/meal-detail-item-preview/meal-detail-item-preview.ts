import { ChangeDetectionStrategy, Component, computed, inject, input, output } from '@angular/core';
import { TranslatePipe, TranslateService } from '@ngx-translate/core';

import { LocalizedNumberPipe } from '../../../../../shared/i18n/localized-number.pipe';
import { MEAL_DETAIL_ITEM_PREVIEW_MAX_ITEMS } from '../meal-detail-lib/meal-detail.config';
import type { MealDetailItemPreview } from '../meal-detail-lib/meal-detail.types';

@Component({
    selector: 'fd-meal-detail-item-preview',
    imports: [LocalizedNumberPipe, TranslatePipe],
    templateUrl: './meal-detail-item-preview.html',
    styleUrl: '../meal-detail/meal-detail.scss',
    changeDetection: ChangeDetectionStrategy.OnPush,
})
export class MealDetailItemPreviewComponent {
    protected readonly translateService = inject(TranslateService);
    public readonly items = input.required<readonly MealDetailItemPreview[]>();
    public readonly isItemPreviewExpanded = input.required<boolean>();

    protected readonly visibleItems = computed(() =>
        this.isItemPreviewExpanded() ? this.items() : this.items().slice(0, MEAL_DETAIL_ITEM_PREVIEW_MAX_ITEMS),
    );
    protected readonly hiddenItemPreviewCount = computed(() => Math.max(0, this.items().length - MEAL_DETAIL_ITEM_PREVIEW_MAX_ITEMS));

    public readonly itemPreviewExpandedToggle = output();
}
