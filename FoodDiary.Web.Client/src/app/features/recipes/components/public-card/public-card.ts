import { DecimalPipe } from '@angular/common';
import { ChangeDetectionStrategy, Component, computed, input } from '@angular/core';
import { RouterLink } from '@angular/router';
import { TranslatePipe } from '@ngx-translate/core';
import { FdUiButtonComponent, FdUiHintDirective, FdUiIconComponent } from 'fd-ui-kit';

import type { PublicRecipe } from '../../models/public-recipe.data';

@Component({
    selector: 'fd-public-recipe-card',
    changeDetection: ChangeDetectionStrategy.OnPush,
    templateUrl: './public-card.html',
    styleUrl: './public-card.scss',
    imports: [DecimalPipe, RouterLink, TranslatePipe, FdUiButtonComponent, FdUiHintDirective, FdUiIconComponent],
})
export class PublicRecipeCardComponent {
    public readonly recipe = input.required<PublicRecipe>();
    protected readonly calories = computed(() => {
        const recipe = this.recipe();
        return recipe.totalCalories === null ? null : recipe.totalCalories / Math.max(1, recipe.servings);
    });
}
