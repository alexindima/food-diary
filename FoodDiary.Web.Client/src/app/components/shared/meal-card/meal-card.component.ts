import { ChangeDetectionStrategy, Component, computed, input, output } from '@angular/core';
import { TranslatePipe } from '@ngx-translate/core';
import { CommonModule } from '@angular/common';
import { Consumption } from '../../../types/consumption.data';
import { resolveMealImageUrl } from '../../../utils/meal-stub.utils';
import { ProductType } from '../../../types/product.data';
import { resolveProductImageUrl } from '../../../utils/product-stub.utils';
import { resolveRecipeImageUrl } from '../../../utils/recipe-stub.utils';
import { NgOptimizedImage } from '@angular/common';

@Component({
    selector: 'fd-meal-card',
    standalone: true,
    imports: [CommonModule, TranslatePipe, NgOptimizedImage],
    templateUrl: './meal-card.component.html',
    styleUrls: ['./meal-card.component.scss'],
    changeDetection: ChangeDetectionStrategy.OnPush,
})
export class MealCardComponent {
    public readonly meal = input.required<Consumption>();
    public readonly open = output<Consumption>();
    private readonly fallbackMealImage = 'assets/images/stubs/meals/other.svg';
    private readonly previewLimit = 3;

    public readonly coverImage = computed(() => {
        const image = this.meal().imageUrl?.trim();
        const resolved = resolveMealImageUrl(image ?? undefined, this.meal().mealType ?? undefined) ?? image;
        return resolved ?? this.fallbackMealImage;
    });

    private readonly itemImages = computed(() => {
        const images: string[] = [];
        const items = this.meal().items ?? [];

        for (const item of items) {
            if (item.recipe) {
                const recipeImage = resolveRecipeImageUrl(item.recipe.imageUrl ?? undefined) ?? this.fallbackMealImage;
                images.push(recipeImage);
                continue;
            }

            if (item.product) {
                const type = (item.product.productType as ProductType | undefined) ?? ProductType.Unknown;
                const productImage = resolveProductImageUrl(item.product.imageUrl ?? undefined, type) ?? this.fallbackMealImage;
                images.push(productImage);
                continue;
            }
        }

        return images;
    });

    public readonly galleryImages = computed(() => {
        const items = this.itemImages();
        if (items.length) {
            return items;
        }

        return [this.fallbackMealImage];
    });

    public readonly previewImages = computed(() => this.galleryImages().slice(0, this.previewLimit));
    public readonly extraCount = computed(() => Math.max(0, this.galleryImages().length - this.previewLimit));

    public readonly macroChips = computed(() => [
        {
            label: 'GENERAL.NUTRIENTS.PROTEIN',
            value: this.meal().totalProteins,
            color: '#4f46e5',
        },
        {
            label: 'GENERAL.NUTRIENTS.CARB',
            value: this.meal().totalCarbs,
            color: '#0ea5e9',
        },
        {
            label: 'GENERAL.NUTRIENTS.FAT',
            value: this.meal().totalFats,
            color: '#f59e0b',
        },
        {
            label: 'GENERAL.NUTRIENTS.FIBER',
            value: this.meal().totalFiber,
            color: '#94a3b8',
        },
    ]);

    public handleOpen(): void {
        this.open.emit(this.meal());
    }
}
