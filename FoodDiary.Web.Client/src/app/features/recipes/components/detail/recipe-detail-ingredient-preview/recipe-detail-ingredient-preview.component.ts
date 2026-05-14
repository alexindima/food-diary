import { ChangeDetectionStrategy, Component, input } from '@angular/core';
import { TranslatePipe } from '@ngx-translate/core';

import type { IngredientPreviewItem } from '../recipe-detail-lib/recipe-detail.types';

@Component({
    selector: 'fd-recipe-detail-ingredient-preview',
    imports: [TranslatePipe],
    templateUrl: './recipe-detail-ingredient-preview.component.html',
    styleUrl: '../recipe-detail/recipe-detail.component.scss',
    changeDetection: ChangeDetectionStrategy.OnPush,
})
export class RecipeDetailIngredientPreviewComponent {
    public readonly ingredients = input.required<readonly IngredientPreviewItem[]>();
}
