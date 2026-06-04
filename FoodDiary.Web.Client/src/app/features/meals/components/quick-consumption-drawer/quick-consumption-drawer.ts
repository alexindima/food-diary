import { CommonModule } from '@angular/common';
import { ChangeDetectionStrategy, Component, computed, effect, inject, input, signal, untracked } from '@angular/core';
import { TranslatePipe } from '@ngx-translate/core';
import { FdUiHintDirective } from 'fd-ui-kit';
import { FdUiButtonComponent } from 'fd-ui-kit/button/fd-ui-button';
import { FdUiDialogService } from 'fd-ui-kit/dialog/fd-ui-dialog.service';
import { firstValueFrom } from 'rxjs';

import { MealDetailsFieldsComponent } from '../../../../components/shared/meal-details-fields/meal-details-fields';
import { resolveProductImageUrl } from '../../../products/lib/product-image.util';
import { normalizeProductType } from '../../../products/lib/product-type.utils';
import { ProductType } from '../../../products/models/product.data';
import { resolveRecipeImageUrl } from '../../../recipes/lib/recipe-image.util';
import { MealManageFacade } from '../../lib/manage/meal-manage.facade';
import { type QuickMealItem, QuickMealService } from '../../lib/quick/quick-meal.service';
import { ConsumptionSourceType } from '../../models/meal.data';
import type { ConsumptionItemFormValues } from '../manage/meal-manage-lib/meal-manage.types';
import { MealManualItemDialogComponent, type MealManualItemDialogData } from '../manage/meal-manual-item-dialog/meal-manual-item-dialog';

type QuickConsumptionItemView = {
    item: QuickMealItem;
    imageUrl: string;
    name: string;
    amount: number;
    unitKey: string;
    trackingKey: string;
};

type QuickConsumptionToggleView = {
    icon: string;
    labelKey: string;
};

@Component({
    selector: 'fd-quick-consumption-drawer',
    imports: [CommonModule, TranslatePipe, FdUiHintDirective, FdUiButtonComponent, MealDetailsFieldsComponent],
    templateUrl: './quick-consumption-drawer.html',
    styleUrls: ['./quick-consumption-drawer.scss'],
    changeDetection: ChangeDetectionStrategy.OnPush,
})
export class QuickConsumptionDrawerComponent {
    private static nextId = 0;

    private readonly quickService = inject(QuickMealService);
    private readonly fdDialogService = inject(FdUiDialogService);
    private readonly mealManageFacade = inject(MealManageFacade);
    private readonly fallbackImage = 'assets/images/stubs/receipt.png';

    public readonly forceShow = input(false);
    public readonly layout = input<'fixed' | 'inline'>('fixed');
    protected readonly titleId = `fd-quick-consumption-title-${QuickConsumptionDrawerComponent.nextId++}`;

    protected readonly items = this.quickService.items;
    protected readonly details = this.quickService.details;
    protected readonly hasItems = this.quickService.hasItems;
    protected readonly isSaving = this.quickService.isSaving;
    protected readonly isDetailsExpanded = signal(false);
    protected readonly isCollapsed = signal(false);

    protected readonly shouldRender = computed(() => this.forceShow() || this.hasItems());
    protected readonly isInline = computed(() => this.layout() === 'inline');
    protected readonly collapsedToggleView = computed<QuickConsumptionToggleView>(() =>
        this.isCollapsed()
            ? {
                  icon: 'expand_less',
                  labelKey: 'QUICK_CONSUMPTION.EXPAND',
              }
            : {
                  icon: 'expand_more',
                  labelKey: 'QUICK_CONSUMPTION.COLLAPSE',
              },
    );
    protected readonly detailsToggleView = computed<QuickConsumptionToggleView>(() =>
        this.isDetailsExpanded()
            ? {
                  icon: 'expand_less',
                  labelKey: 'MEAL_DETAILS.HIDE',
              }
            : {
                  icon: 'expand_more',
                  labelKey: 'MEAL_DETAILS.ADD',
              },
    );
    protected readonly bodyInert = computed(() => (this.isCollapsed() ? '' : null));
    protected readonly itemViews = computed<QuickConsumptionItemView[]>(() =>
        this.items().map(item => ({
            item,
            imageUrl: this.imageFor(item),
            name: this.itemName(item),
            amount: item.amount,
            unitKey: this.unitKeyFor(item),
            trackingKey: `${item.key}-${item.flashId ?? 0}`,
        })),
    );

    public constructor() {
        let hadItems = this.hasItems();
        effect(() => {
            const hasItems = this.hasItems();
            untracked(() => {
                if (!hasItems) {
                    this.resetUiState();
                } else if (!hadItems) {
                    this.isCollapsed.set(false);
                }
                hadItems = hasItems;
            });
        });
    }

    private imageFor(item: QuickMealItem): string {
        if (item.type === 'product') {
            const product = item.product;
            const type = normalizeProductType(product?.productType) ?? ProductType.Unknown;
            return resolveProductImageUrl(product?.imageUrl ?? undefined, type) ?? this.fallbackImage;
        }

        return resolveRecipeImageUrl(item.recipe?.imageUrl ?? undefined) ?? this.fallbackImage;
    }

    private itemName(item: QuickMealItem): string {
        return item.type === 'product' ? (item.product?.name ?? '') : (item.recipe?.name ?? '');
    }

    private unitKeyFor(item: QuickMealItem): string {
        if (item.type === 'product') {
            return `GENERAL.UNITS.${item.product?.baseUnit ?? 'G'}`;
        }

        return 'QUICK_CONSUMPTION.SERVINGS';
    }

    protected updateDate(value: string): void {
        this.quickService.updateDetails({ date: value });
    }

    protected updateTime(value: string): void {
        this.quickService.updateDetails({ time: value });
    }

    protected updateComment(value: string): void {
        this.quickService.updateDetails({ comment: value });
    }

    protected toggleDetails(): void {
        this.isDetailsExpanded.update(value => !value);
    }

    protected toggleCollapsed(): void {
        this.isCollapsed.update(value => !value);
    }

    protected updatePreMealSatietyLevel(value: number | null): void {
        if (value === null) {
            return;
        }

        this.quickService.updateDetails({ preMealSatietyLevel: value });
    }

    protected updatePostMealSatietyLevel(value: number | null): void {
        if (value === null) {
            return;
        }

        this.quickService.updateDetails({ postMealSatietyLevel: value });
    }

    protected edit(item: QuickMealItem): void {
        void this.openEditDialogAsync(item);
    }

    protected remove(key: string): void {
        this.quickService.removeItem(key);
    }

    protected clear(): void {
        this.quickService.clear();
    }

    protected save(): void {
        this.quickService.saveDraft();
    }

    private resetUiState(): void {
        this.isCollapsed.set(false);
        this.isDetailsExpanded.set(false);
    }

    private async openEditDialogAsync(item: QuickMealItem): Promise<void> {
        const dialogItem = await this.createDialogItemAsync(item);
        const result = await firstValueFrom(
            this.fdDialogService
                .open<MealManualItemDialogComponent, MealManualItemDialogData, ConsumptionItemFormValues | null>(
                    MealManualItemDialogComponent,
                    {
                        preset: 'form',
                        data: { item: dialogItem },
                    },
                )
                .afterClosed(),
        );

        if (result === null || result === undefined) {
            return;
        }

        const updatedItem = this.toQuickMealItem(result);
        if (updatedItem === null) {
            return;
        }

        this.quickService.updateItem(item.key, updatedItem);
    }

    private async createDialogItemAsync(item: QuickMealItem): Promise<ConsumptionItemFormValues> {
        const sourceType = item.type === 'recipe' ? ConsumptionSourceType.Recipe : ConsumptionSourceType.Product;
        const amount =
            sourceType === ConsumptionSourceType.Recipe
                ? await this.mealManageFacade.resolveRecipeServingsToGramsAsync(item.recipe ?? null, item.amount)
                : item.amount;

        return {
            sourceType,
            product: sourceType === ConsumptionSourceType.Product ? (item.product ?? null) : null,
            recipe: sourceType === ConsumptionSourceType.Recipe ? (item.recipe ?? null) : null,
            amount,
        };
    }

    private toQuickMealItem(item: ConsumptionItemFormValues): Omit<QuickMealItem, 'flashId'> | null {
        const amount = item.amount ?? 0;

        if (item.sourceType === ConsumptionSourceType.Product) {
            const product = item.product;
            if (product?.id === undefined || product.id.length === 0) {
                return null;
            }

            return {
                key: `product-${product.id}`,
                type: 'product',
                product,
                amount,
            };
        }

        const recipe = item.recipe;
        if (recipe?.id === undefined || recipe.id.length === 0) {
            return null;
        }

        return {
            key: `recipe-${recipe.id}`,
            type: 'recipe',
            recipe,
            amount: this.mealManageFacade.convertRecipeGramsToServings(recipe, amount),
        };
    }
}
