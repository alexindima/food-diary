import { Injectable, computed, inject, signal } from '@angular/core';
import { ConsumptionManageDto, ConsumptionSourceType } from '../types/consumption.data';
import { Product } from '../types/product.data';
import { Recipe } from '../types/recipe.data';
import { ConsumptionService } from './consumption.service';
import { NavigationService } from './navigation.service';
import { FdUiToastService } from 'fd-ui-kit/toast/fd-ui-toast.service';
import { TranslateService } from '@ngx-translate/core';

export type QuickConsumptionItemType = 'product' | 'recipe';

export interface QuickConsumptionItem {
    key: string;
    type: QuickConsumptionItemType;
    product?: Product;
    recipe?: Recipe;
    amount: number;
}

@Injectable({
    providedIn: 'root',
})
export class QuickConsumptionService {
    private readonly consumptionService = inject(ConsumptionService);
    private readonly navigationService = inject(NavigationService);
    private readonly toastService = inject(FdUiToastService);
    private readonly translateService = inject(TranslateService);

    private readonly itemsSignal = signal<QuickConsumptionItem[]>([]);
    private readonly isSavingSignal = signal(false);

    public readonly items = computed(() => this.itemsSignal());
    public readonly hasItems = computed(() => this.itemsSignal().length > 0);
    public readonly isSaving = computed(() => this.isSavingSignal());

    public addProduct(product: Product): void {
        if (!product?.id) {
            return;
        }

        const amount = product.baseAmount ?? 1;
        const key = `product-${product.id}`;
        this.upsertItem({
            key,
            type: 'product',
            product,
            amount,
        });

        this.toastService.open(this.translateService.instant('QUICK_CONSUMPTION.ADDED_PRODUCT'), { appearance: 'positive' });
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

        this.toastService.open(this.translateService.instant('QUICK_CONSUMPTION.ADDED_RECIPE'), { appearance: 'positive' });
    }

    public removeItem(key: string): void {
        this.itemsSignal.update(items => items.filter(item => item.key !== key));
    }

    public clear(): void {
        this.itemsSignal.set([]);
    }

    public async openEditor(): Promise<void> {
        const items = this.consumeItems();
        if (!items.length) {
            return;
        }

        await this.navigationService.navigateToConsumptionAdd(undefined, {
            state: { quickConsumptionItems: items },
        });
    }

    public saveDraft(): void {
        const items = this.itemsSignal();
        if (!items.length || this.isSavingSignal()) {
            return;
        }

        const payload = this.toConsumptionDto(items);
        this.isSavingSignal.set(true);
        this.consumptionService.create(payload).subscribe({
            next: () => {
                this.isSavingSignal.set(false);
                this.toastService.open(this.translateService.instant('QUICK_CONSUMPTION.SAVE_SUCCESS'), {
                    appearance: 'positive',
                });
                this.clear();
            },
            error: () => {
                this.isSavingSignal.set(false);
                this.toastService.open(this.translateService.instant('QUICK_CONSUMPTION.SAVE_ERROR'), {
                    appearance: 'negative',
                });
            },
        });
    }

    public getPrefillItems(): QuickConsumptionItem[] {
        return this.itemsSignal();
    }

    private consumeItems(): QuickConsumptionItem[] {
        const items = this.itemsSignal();
        this.clear();
        return items;
    }

    private upsertItem(newItem: QuickConsumptionItem): void {
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

    private toConsumptionDto(items: QuickConsumptionItem[]): ConsumptionManageDto {
        const mappedItems = items
            .map(item => {
                if (item.type === 'product' && item.product) {
                    return {
                        sourceType: ConsumptionSourceType.Product,
                        productId: item.product.id,
                        recipeId: null,
                        amount: item.amount,
                    };
                }

                if (item.type === 'recipe' && item.recipe) {
                    return {
                        sourceType: ConsumptionSourceType.Recipe,
                        recipeId: item.recipe.id,
                        productId: null,
                        amount: item.amount,
                    };
                }

                return null;
            })
            .filter(Boolean) as ConsumptionManageDto['items'];

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
