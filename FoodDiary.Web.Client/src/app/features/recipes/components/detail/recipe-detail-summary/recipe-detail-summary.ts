import { ChangeDetectionStrategy, Component, computed, input } from '@angular/core';
import { TranslatePipe } from '@ngx-translate/core';
import { FdUiHintDirective } from 'fd-ui-kit';

import { injectCurrentLanguage } from '../../../../../shared/i18n/inject-current-language';
import { LocalizedNumberPipe } from '../../../../../shared/i18n/localized-number.pipe';
import type { Recipe } from '../../../../../shared/models/recipe.data';
import { resolveServingsUnitKey } from '../../../lib/recipe-servings.utils';
import { RecipeDetailIngredientPreviewComponent } from '../recipe-detail-ingredient-preview/recipe-detail-ingredient-preview';
import type { IngredientPreviewItem, MacroBlock } from '../recipe-detail-lib/recipe-detail.types';

@Component({
    selector: 'fd-recipe-detail-summary',
    imports: [TranslatePipe, FdUiHintDirective, LocalizedNumberPipe, RecipeDetailIngredientPreviewComponent],
    templateUrl: './recipe-detail-summary.html',
    styleUrl: '../recipe-detail/recipe-detail.scss',
    changeDetection: ChangeDetectionStrategy.OnPush,
})
export class RecipeDetailSummaryComponent {
    protected readonly locale = injectCurrentLanguage();
    public readonly recipe = input.required<Recipe>();
    public readonly calories = input.required<number>();
    public readonly totalTime = input.required<number | null>();
    public readonly qualityGrade = input.required<string>();
    public readonly qualityScore = input.required<number>();
    public readonly macroSummaryBlocks = input.required<readonly MacroBlock[]>();
    public readonly ingredientCount = input.required<number>();
    public readonly ingredientPreview = input.required<readonly IngredientPreviewItem[]>();
    protected readonly qualityHintKey = computed(() => `QUALITY.${this.qualityGrade().toUpperCase()}`);
    protected readonly servingsUnitKey = computed(() => resolveServingsUnitKey(this.recipe().servings));
}
