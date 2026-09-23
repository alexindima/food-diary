import { ChangeDetectionStrategy, Component, computed, input, signal } from '@angular/core';
import { TranslatePipe } from '@ngx-translate/core';

import type { IngredientPreviewItem } from '../recipe-detail-lib/recipe-detail.types';

@Component({
    selector: 'fd-recipe-detail-ingredient-preview',
    imports: [TranslatePipe],
    templateUrl: './recipe-detail-ingredient-preview.html',
    styleUrl: '../recipe-detail/recipe-detail.scss',
    changeDetection: ChangeDetectionStrategy.OnPush,
})
export class RecipeDetailIngredientPreviewComponent {
    protected readonly expanded = signal(false);
    protected readonly previewLimit = 5;
    protected readonly visibleIngredients = computed(() =>
        this.expanded() ? this.ingredients() : this.ingredients().slice(0, this.previewLimit),
    );
    public readonly ingredients = input.required<readonly IngredientPreviewItem[]>();
}
