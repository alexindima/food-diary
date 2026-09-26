import { DecimalPipe } from '@angular/common';
import { ChangeDetectionStrategy, Component, computed, input, signal } from '@angular/core';
import { RouterLink } from '@angular/router';
import { TranslatePipe } from '@ngx-translate/core';
import { FdUiButtonComponent } from 'fd-ui-kit';

import { scaleIngredient } from '../../lib/public-recipe.utils';
import type { PublicRecipe, PublicRecipeIngredient } from '../../models/public-recipe.data';

@Component({
    selector: 'fd-public-ingredients',
    changeDetection: ChangeDetectionStrategy.OnPush,
    templateUrl: './public-ingredients.html',
    styleUrl: './public-ingredients.scss',
    imports: [DecimalPipe, RouterLink, TranslatePipe, FdUiButtonComponent],
})
export class PublicIngredientsComponent {
    public readonly recipe = input.required<PublicRecipe>();
    protected readonly maxServings = 1000;
    protected readonly offset = signal(0);
    protected readonly servings = computed(() => this.recipe().servings + this.offset());
    protected readonly ingredients = computed(() => this.recipe().steps.flatMap(step => step.ingredients));
    protected amount(ingredient: PublicRecipeIngredient): number | null {
        return scaleIngredient(ingredient, this.servings(), this.recipe().servings);
    }
    protected unitKey(unit: string | null): string {
        const keys = new Map([
            ['g', 'GRAMS'],
            ['Gram', 'GRAMS'],
            ['G', 'GRAMS'],
            ['ml', 'ML'],
            ['Milliliter', 'ML'],
            ['Ml', 'ML'],
            ['ML', 'ML'],
            ['serving', 'SERVINGS'],
            ['Piece', 'PIECES'],
            ['piece', 'PIECES'],
            ['PCS', 'PIECES'],
        ]);
        const key = keys.get(unit ?? '');
        return key === undefined ? '' : `PUBLIC_RECIPES.${key}`;
    }
    protected changeServings(delta: number): void {
        this.offset.set(Math.min(this.maxServings, Math.max(1, this.servings() + delta)) - this.recipe().servings);
    }
}
