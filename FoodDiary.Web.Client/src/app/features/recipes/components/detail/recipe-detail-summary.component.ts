import { ChangeDetectionStrategy, Component, input } from '@angular/core';
import { TranslatePipe } from '@ngx-translate/core';
import { FdUiHintDirective } from 'fd-ui-kit';
import { FdUiAccentSurfaceComponent } from 'fd-ui-kit/accent-surface/fd-ui-accent-surface.component';

import type { Recipe } from '../../models/recipe.data';
import type { IngredientPreviewItem, MacroBlock } from './recipe-detail.types';
import { RecipeDetailIngredientPreviewComponent } from './recipe-detail-ingredient-preview.component';

@Component({
    selector: 'fd-recipe-detail-summary',
    imports: [TranslatePipe, FdUiHintDirective, FdUiAccentSurfaceComponent, RecipeDetailIngredientPreviewComponent],
    templateUrl: './recipe-detail-summary.component.html',
    styleUrl: './recipe-detail.component.scss',
    changeDetection: ChangeDetectionStrategy.OnPush,
})
export class RecipeDetailSummaryComponent {
    public readonly recipe = input.required<Recipe>();
    public readonly calories = input.required<number>();
    public readonly totalTime = input.required<number | null>();
    public readonly qualityGrade = input.required<string>();
    public readonly qualityScore = input.required<number>();
    public readonly qualityHintKey = input.required<string>();
    public readonly macroSummaryBlocks = input.required<readonly MacroBlock[]>();
    public readonly ingredientCount = input.required<number>();
    public readonly ingredientPreview = input.required<readonly IngredientPreviewItem[]>();
}
