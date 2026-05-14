import { DecimalPipe } from '@angular/common';
import { ChangeDetectionStrategy, Component, input, output } from '@angular/core';
import { TranslatePipe } from '@ngx-translate/core';
import { FdUiHintDirective } from 'fd-ui-kit';
import { FdUiAccentSurfaceComponent } from 'fd-ui-kit/accent-surface/fd-ui-accent-surface.component';

import type { Meal } from '../../../models/meal.data';
import { MealDetailItemPreviewComponent } from '../meal-detail-item-preview/meal-detail-item-preview.component';
import type { MealDetailItemPreview, MealMacroBlock, MealSatietyMeta } from '../meal-detail-lib/meal-detail.types';

@Component({
    selector: 'fd-meal-detail-summary',
    imports: [DecimalPipe, TranslatePipe, FdUiHintDirective, FdUiAccentSurfaceComponent, MealDetailItemPreviewComponent],
    templateUrl: './meal-detail-summary.component.html',
    styleUrl: '../meal-detail/meal-detail.component.scss',
    changeDetection: ChangeDetectionStrategy.OnPush,
})
export class MealDetailSummaryComponent {
    public readonly consumption = input.required<Meal>();
    public readonly calories = input.required<number>();
    public readonly qualityGrade = input.required<string>();
    public readonly qualityScore = input.required<number>();
    public readonly qualityHintKey = input.required<string>();
    public readonly macroSummaryBlocks = input.required<readonly MealMacroBlock[]>();
    public readonly preMealSatietyMeta = input.required<MealSatietyMeta>();
    public readonly postMealSatietyMeta = input.required<MealSatietyMeta>();
    public readonly visibleItemPreview = input.required<readonly MealDetailItemPreview[]>();
    public readonly itemsCount = input.required<number>();
    public readonly hiddenItemPreviewCount = input.required<number>();
    public readonly isItemPreviewExpanded = input.required<boolean>();

    public readonly itemPreviewExpandedToggle = output();
}
