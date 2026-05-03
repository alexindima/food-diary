import { computed, inject, Injectable, signal } from '@angular/core';
import { TranslateService } from '@ngx-translate/core';
import { FdUiToastService } from 'fd-ui-kit/toast/fd-ui-toast.service';

import { NavigationService } from '../../../services/navigation.service';
import { Product } from '../../products/models/product.data';
import { Recipe } from '../../recipes/models/recipe.data';
import { MealService } from '../api/meal.service';
import { MealManageDto, MealSourceType } from '../models/meal.data';

export type QuickMealItemType = 'product' | 'recipe';

export interface QuickMealItem {
    key: string;
    type: QuickMealItemType;
    product?: Product;
    recipe?: Recipe;
    amount: number;
}

@Injectable({
    providedIn: 'root',
})
export class QuickMealService {
    private readonly mealService = inject(MealService);
    private readonly navigationService = inject(NavigationService);
    private readonly toastService = inject(FdUiToastService);
    private readonly translateService = inject(TranslateService);

    private readonly itemsSignal = signal<QuickMealItem[]>([]);
    private readonly isSavingSignal = signal(false);
    private isPreviewMode = false;

    public readonly items = computed(() => this.itemsSignal());
    public readonly hasItems = computed(() => this.itemsSignal().length > 0);
    public readonly isSaving = computed(() => this.isSavingSignal());

    public addProduct(product: Product): void {
        if (!product?.id) {
            return;
        }

        const amount = this.resolveProductAmount(product);
        const key = `product-${product.id}`;
        this.upsertItem({
            key,
            type: 'product',
            product,
            amount,
        });

        if (this.isPreviewMode) {
            return;
        }

        this.toastService.success(this.translateService.instant('QUICK_CONSUMPTION.ADDED_PRODUCT'));
    }

    public addRecipe(recipe: Recipe): void {
        if (!recipe?.id) {
            return;
        }

        const key = `recipe-${recipe.id}`;
        this.upsertItem({
            key,
            type: 'recipe',
            recipe,
            amount: 1,
        });

        if (this.isPreviewMode) {
            return;
        }

        this.toastService.success(this.translateService.instant('QUICK_CONSUMPTION.ADDED_RECIPE'));
    }

    public removeItem(key: string): void {
        this.itemsSignal.update(items => items.filter(item => item.key !== key));
    }

    public clear(): void {
        this.itemsSignal.set([]);
    }

    public async openEditor(): Promise<void> {
        if (this.isPreviewMode) {
            return;
        }

        const items = this.consumeItems();
        if (!items.length) {
            return;
        }

        await this.navigationService.navigateToConsumptionAdd(undefined, {
            state: { quickConsumptionItems: items },
        });
    }

    public saveDraft(): void {
        if (this.isPreviewMode) {
            return;
        }

        const items = this.itemsSignal();
        if (!items.length || this.isSavingSignal()) {
            return;
        }

        const payload = this.toMealDto(items);
        this.isSavingSignal.set(true);
        this.mealService.create(payload).subscribe({
            next: () => {
                this.isSavingSignal.set(false);
                this.toastService.success(this.translateService.instant('QUICK_CONSUMPTION.SAVE_SUCCESS'));
                this.clear();
            },
            error: () => {
                this.isSavingSignal.set(false);
                this.toastService.error(this.translateService.instant('QUICK_CONSUMPTION.SAVE_ERROR'));
            },
        });
    }

    public getPrefillItems(): QuickMealItem[] {
        return this.itemsSignal();
    }

    public setPreviewItems(items: QuickMealItem[]): void {
        this.isPreviewMode = true;
        this.isSavingSignal.set(false);
        this.itemsSignal.set(items);
    }

    public exitPreview(): void {
        if (!this.isPreviewMode) {
            return;
        }

        this.clear();
        this.isPreviewMode = false;
    }

    private consumeItems(): QuickMealItem[] {
        const items = this.itemsSignal();
        this.clear();
        return items;
    }

    private resolveProductAmount(product: Product): number {
        if (product.defaultPortionAmount > 0) {
            return product.defaultPortionAmount;
        }

        if (product.baseAmount > 0) {
            return product.baseAmount;
        }

        return 1;
    }

    private upsertItem(newItem: QuickMealItem): void {
        this.itemsSignal.update(items => {
            const existingIndex = items.findIndex(item => item.key === newItem.key);
            if (existingIndex >= 0) {
                const updated = [...items];
                updated[existingIndex] = {
                    ...updated[existingIndex],
                    amount: updated[existingIndex].amount + newItem.amount,
                };
                return updated;
            }

            return [...items, newItem];
        });
    }

    private toMealDto(items: QuickMealItem[]): MealManageDto {
        const mappedItems = items
            .map(item => {
                if (item.type === 'product' && item.product) {
                    return {
                        sourceType: MealSourceType.Product,
                        productId: item.product.id,
                        recipeId: null,
                        amount: item.amount,
                    };
                }

                if (item.type === 'recipe' && item.recipe) {
                    return {
                        sourceType: MealSourceType.Recipe,
                        recipeId: item.recipe.id,
                        productId: null,
                        amount: item.amount,
                    };
                }

                return null;
            })
            .filter(Boolean) as MealManageDto['items'];

        return {
            date: new Date(),
            mealType: undefined,
            comment: undefined,
            imageUrl: undefined,
            imageAssetId: undefined,
            items: mappedItems,
            isNutritionAutoCalculated: true,
        };
    }
}
