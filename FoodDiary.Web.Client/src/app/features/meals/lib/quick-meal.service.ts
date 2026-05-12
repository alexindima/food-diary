import { computed, inject, Injectable, signal } from '@angular/core';
import { TranslateService } from '@ngx-translate/core';
import { FdUiToastService } from 'fd-ui-kit/toast/fd-ui-toast.service';

import { DEFAULT_SATIETY_LEVEL, normalizeSatietyLevel } from '../../../shared/lib/satiety-level.utils';
import type { Product } from '../../products/models/product.data';
import type { Recipe } from '../../recipes/models/recipe.data';
import { MealService } from '../api/meal.service';
import type { ConsumptionItemManageDto, MealManageDto } from '../models/meal.data';

const DEFAULT_ITEM_AMOUNT = 1;
const NEXT_MONTH_OFFSET = 1;
const PADDED_DATE_PART_LENGTH = 2;

export type QuickMealItemType = 'product' | 'recipe';

export type QuickMealItem = {
    key: string;
    type: QuickMealItemType;
    product?: Product;
    recipe?: Recipe;
    amount: number;
    flashId?: number;
};

export type QuickMealDetails = {
    date: string;
    time: string;
    comment: string;
    preMealSatietyLevel: number;
    postMealSatietyLevel: number;
};

@Injectable({
    providedIn: 'root',
})
export class QuickMealService {
    private readonly mealService = inject(MealService);
    private readonly toastService = inject(FdUiToastService);
    private readonly translateService = inject(TranslateService);

    private readonly itemsSignal = signal<QuickMealItem[]>([]);
    private readonly detailsSignal = signal<QuickMealDetails>(this.createDefaultDetails());
    private readonly isSavingSignal = signal(false);
    private nextFlashId = 0;
    private isPreviewMode = false;

    public readonly items = computed(() => this.itemsSignal());
    public readonly details = computed(() => this.detailsSignal());
    public readonly hasItems = computed(() => this.itemsSignal().length > 0);
    public readonly isSaving = computed(() => this.isSavingSignal());

    public addProduct(product: Product): void {
        if (product.id.length === 0) {
            return;
        }

        const amount = this.resolveProductAmount(product);
        const key = `product-${product.id}`;
        this.refreshDetailsForFirstItem();
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
        if (recipe.id.length === 0) {
            return;
        }

        const key = `recipe-${recipe.id}`;
        this.refreshDetailsForFirstItem();
        this.upsertItem({
            key,
            type: 'recipe',
            recipe,
            amount: DEFAULT_ITEM_AMOUNT,
        });

        if (this.isPreviewMode) {
            return;
        }

        this.toastService.success(this.translateService.instant('QUICK_CONSUMPTION.ADDED_RECIPE'));
    }

    public removeItem(key: string): void {
        this.itemsSignal.update(items => items.filter(item => item.key !== key));
    }

    public updateItem(key: string, item: Omit<QuickMealItem, 'flashId'>): void {
        this.itemsSignal.update(items => {
            const flashId = ++this.nextFlashId;
            const withoutCurrent = items.filter(current => current.key !== key);
            const existingIndex = withoutCurrent.findIndex(current => current.key === item.key);

            if (existingIndex >= 0) {
                const updated = [...withoutCurrent];
                updated[existingIndex] = {
                    ...updated[existingIndex],
                    amount: updated[existingIndex].amount + item.amount,
                    flashId,
                };
                return updated;
            }

            const originalIndex = items.findIndex(current => current.key === key);
            const updatedItem = { ...item, flashId };
            if (originalIndex < 0) {
                return [...withoutCurrent, updatedItem];
            }

            const insertIndex = Math.min(originalIndex, withoutCurrent.length);
            return [...withoutCurrent.slice(0, insertIndex), updatedItem, ...withoutCurrent.slice(insertIndex)];
        });
    }

    public clear(): void {
        this.itemsSignal.set([]);
        this.detailsSignal.set(this.createDefaultDetails());
    }

    public updateDetails(details: Partial<QuickMealDetails>): void {
        this.detailsSignal.update(current => ({
            ...current,
            ...details,
        }));
    }

    public saveDraft(): void {
        if (this.isPreviewMode) {
            return;
        }

        const items = this.itemsSignal();
        if (items.length === 0 || this.isSavingSignal()) {
            return;
        }

        const payload = this.toMealDto(items);
        if (payload.items.length === 0) {
            this.toastService.error(this.translateService.instant('QUICK_CONSUMPTION.SAVE_ERROR'));
            return;
        }

        this.isSavingSignal.set(true);
        this.mealService.create(payload).subscribe({
            next: meal => {
                this.isSavingSignal.set(false);
                if (meal === null) {
                    this.toastService.error(this.translateService.instant('QUICK_CONSUMPTION.SAVE_ERROR'));
                    return;
                }

                this.toastService.success(this.translateService.instant('QUICK_CONSUMPTION.SAVE_SUCCESS'));
                this.clear();
            },
            error: () => {
                this.isSavingSignal.set(false);
                this.toastService.error(this.translateService.instant('QUICK_CONSUMPTION.SAVE_ERROR'));
            },
        });
    }

    public setPreviewItems(items: QuickMealItem[]): void {
        this.isPreviewMode = true;
        this.isSavingSignal.set(false);
        this.itemsSignal.set(items);
        this.detailsSignal.set(this.createDefaultDetails());
    }

    public exitPreview(): void {
        if (!this.isPreviewMode) {
            return;
        }

        this.clear();
        this.isPreviewMode = false;
    }

    private resolveProductAmount(product: Product): number {
        if (product.defaultPortionAmount > 0) {
            return product.defaultPortionAmount;
        }

        if (product.baseAmount > 0) {
            return product.baseAmount;
        }

        return DEFAULT_ITEM_AMOUNT;
    }

    private refreshDetailsForFirstItem(): void {
        if (this.itemsSignal().length === 0) {
            this.detailsSignal.set(this.createDefaultDetails());
        }
    }

    private upsertItem(newItem: QuickMealItem): void {
        this.itemsSignal.update(items => {
            const flashId = ++this.nextFlashId;
            const existingIndex = items.findIndex(item => item.key === newItem.key);
            if (existingIndex >= 0) {
                const updated = [...items];
                updated[existingIndex] = {
                    ...updated[existingIndex],
                    amount: updated[existingIndex].amount + newItem.amount,
                    flashId,
                };
                return updated;
            }

            return [...items, { ...newItem, flashId }];
        });
    }

    private toMealDto(items: QuickMealItem[]): MealManageDto {
        const mappedItems = items.reduce<ConsumptionItemManageDto[]>((result, item) => {
            if (item.type === 'product' && item.product !== undefined) {
                result.push({
                    productId: item.product.id,
                    recipeId: null,
                    amount: item.amount,
                });
                return result;
            }

            if (item.type === 'recipe' && item.recipe !== undefined) {
                result.push({
                    recipeId: item.recipe.id,
                    productId: null,
                    amount: item.amount,
                });
            }

            return result;
        }, []);

        return {
            date: this.getDetailsDateTime(),
            mealType: undefined,
            comment: this.resolveComment(),
            imageUrl: undefined,
            imageAssetId: undefined,
            items: mappedItems,
            isNutritionAutoCalculated: true,
            preMealSatietyLevel: normalizeSatietyLevel(this.detailsSignal().preMealSatietyLevel),
            postMealSatietyLevel: normalizeSatietyLevel(this.detailsSignal().postMealSatietyLevel),
        };
    }

    private createDefaultDetails(): QuickMealDetails {
        const now = new Date();
        return {
            date: this.getDateInputValue(now),
            time: this.getTimeInputValue(now),
            comment: '',
            preMealSatietyLevel: DEFAULT_SATIETY_LEVEL,
            postMealSatietyLevel: DEFAULT_SATIETY_LEVEL,
        };
    }

    private getDateInputValue(date: Date): string {
        const year = date.getFullYear();
        const month = this.padNumber(date.getMonth() + NEXT_MONTH_OFFSET);
        const day = this.padNumber(date.getDate());
        return `${year}-${month}-${day}`;
    }

    private getTimeInputValue(date: Date): string {
        const hours = this.padNumber(date.getHours());
        const minutes = this.padNumber(date.getMinutes());
        return `${hours}:${minutes}`;
    }

    private getDetailsDateTime(): Date {
        const details = this.detailsSignal();
        const parsed = new Date(`${details.date}T${details.time}`);
        return Number.isNaN(parsed.getTime()) ? new Date() : parsed;
    }

    private resolveComment(): string | undefined {
        const comment = this.detailsSignal().comment.trim();
        return comment.length > 0 ? comment : undefined;
    }

    private padNumber(value: number): string {
        return value.toString().padStart(PADDED_DATE_PART_LENGTH, '0');
    }
}
