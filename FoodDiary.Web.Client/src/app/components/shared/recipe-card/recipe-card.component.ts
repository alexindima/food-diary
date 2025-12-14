import { ChangeDetectionStrategy, Component, EventEmitter, Input, Output } from '@angular/core';
import { CommonModule } from '@angular/common';
import { TranslatePipe } from '@ngx-translate/core';
import { FdUiIconModule } from 'fd-ui-kit/material';
import { FdUiButtonComponent } from 'fd-ui-kit/button/fd-ui-button.component';
import { Recipe } from '../../../types/recipe.data';
import { resolveProductImageUrl } from '../../../utils/product-stub.utils';
import { resolveRecipeImageUrl } from '../../../utils/recipe-stub.utils';
import { ProductType } from '../../../types/product.data';

@Component({
    selector: 'fd-recipe-card',
    standalone: true,
    imports: [CommonModule, TranslatePipe, FdUiIconModule, FdUiButtonComponent],
    templateUrl: './recipe-card.component.html',
    styleUrl: './recipe-card.component.scss',
    changeDetection: ChangeDetectionStrategy.OnPush,
})
export class RecipeCardComponent {
    private readonly fallbackIngredientImage = 'assets/images/stubs/receipt.png';
    private readonly previewLimit = 3;

    @Input({ required: true }) recipe!: Recipe;
    @Input() imageUrl?: string;
    @Output() open = new EventEmitter<void>();
    @Output() addToMeal = new EventEmitter<Recipe>();

    public handleOpen(): void {
        this.open.emit();
    }

    public handleAdd(event: Event): void {
        event.stopPropagation();
        this.addToMeal.emit(this.recipe);
    }

    public getTotalTime(): number | null {
        const prep = this.recipe?.prepTime ?? 0;
        const cook = this.recipe?.cookTime ?? 0;
        const total = prep + cook;
        return total > 0 ? total : null;
    }

    public getIngredientCount(): number {
        if (!this.recipe?.steps?.length) {
            return 0;
        }

        return this.recipe.steps.reduce((total, step) => total + (step.ingredients?.length ?? 0), 0);
    }

    public previewImages(): string[] {
        const images = this.ingredientImages();

        if (images.length === 0) {
            return [this.fallbackIngredientImage];
        }

        return images.slice(0, this.previewLimit);
    }

    public extraCount(): number {
        const total = this.ingredientImages().length;
        return Math.max(0, total - this.previewLimit);
    }

    private ingredientImages(): string[] {
        if (!this.recipe?.steps?.length) {
            return [];
        }

        const images: string[] = [];

        for (const step of this.recipe.steps) {
            for (const ingredient of step.ingredients ?? []) {
                if (ingredient.productId) {
                    const image = resolveProductImageUrl(undefined, ProductType.Unknown) ?? this.fallbackIngredientImage;
                    images.push(image);
                    continue;
                }

                if (ingredient.nestedRecipeId) {
                    const image = resolveRecipeImageUrl(undefined) ?? this.fallbackIngredientImage;
                    images.push(image);
                    continue;
                }

                images.push(this.fallbackIngredientImage);
            }
        }

        return images;
    }
}
