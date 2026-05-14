import { CommonModule } from '@angular/common';
import { ChangeDetectionStrategy, Component, computed, effect, inject, input, signal, untracked } from '@angular/core';
import type { FormGroup } from '@angular/forms';
import { TranslatePipe } from '@ngx-translate/core';
import { FdUiHintDirective } from 'fd-ui-kit';
import { FdUiButtonComponent } from 'fd-ui-kit/button/fd-ui-button.component';
import { FdUiDialogService } from 'fd-ui-kit/dialog/fd-ui-dialog.service';
import { firstValueFrom } from 'rxjs';

import { MealDetailsFieldsComponent } from '../../../../components/shared/meal-details-fields/meal-details-fields.component';
import { resolveProductImageUrl } from '../../../products/lib/product-image.util';
import { normalizeProductType } from '../../../products/lib/product-type.utils';
import { ProductType } from '../../../products/models/product.data';
import { resolveRecipeImageUrl } from '../../../recipes/lib/recipe-image.util';
import { MealManageFacade } from '../../lib/manage/meal-manage.facade';
import { type QuickMealItem, QuickMealService } from '../../lib/quick/quick-meal.service';
import { ConsumptionSourceType } from '../../models/meal.data';
import type { ConsumptionItemFormData } from '../manage/meal-manage-lib/meal-manage.types';
import {
    MealManualItemDialogComponent,
    type MealManualItemDialogData,
} from '../manage/meal-manual-item-dialog/meal-manual-item-dialog.component';

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
    standalone: true,
    imports: [CommonModule, TranslatePipe, FdUiHintDirective, FdUiButtonComponent, MealDetailsFieldsComponent],
    templateUrl: './quick-consumption-drawer.component.html',
    styleUrls: ['./quick-consumption-drawer.component.scss'],
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
    public readonly titleId = `fd-quick-consumption-title-${QuickConsumptionDrawerComponent.nextId++}`;

    public readonly items = this.quickService.items;
    public readonly details = this.quickService.details;
    public readonly hasItems = this.quickService.hasItems;
    public readonly isSaving = this.quickService.isSaving;
    public readonly isDetailsExpanded = signal(false);
    public readonly isCollapsed = signal(false);

    public readonly shouldRender = computed(() => this.forceShow() || this.hasItems());
    public readonly isInline = computed(() => this.layout() === 'inline');
    public readonly collapsedToggleView = computed<QuickConsumptionToggleView>(() =>
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
    public readonly detailsToggleView = computed<QuickConsumptionToggleView>(() =>
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
    public readonly bodyInert = computed(() => (this.isCollapsed() ? '' : null));
    public readonly itemViews = computed<QuickConsumptionItemView[]>(() =>
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

    public updateDate(value: string): void {
        this.quickService.updateDetails({ date: value });
    }

    public updateTime(value: string): void {
        this.quickService.updateDetails({ time: value });
    }

    public updateComment(value: string): void {
        this.quickService.updateDetails({ comment: value });
    }

    public toggleDetails(): void {
        this.isDetailsExpanded.update(value => !value);
    }

    public toggleCollapsed(): void {
        this.isCollapsed.update(value => !value);
    }

    public updatePreMealSatietyLevel(value: number | null): void {
        if (value === null) {
            return;
        }

        this.quickService.updateDetails({ preMealSatietyLevel: value });
    }

    public updatePostMealSatietyLevel(value: number | null): void {
        if (value === null) {
            return;
        }

        this.quickService.updateDetails({ postMealSatietyLevel: value });
    }

    public edit(item: QuickMealItem): void {
        void this.openEditDialogAsync(item);
    }

    public remove(key: string): void {
        this.quickService.removeItem(key);
    }

    public clear(): void {
        this.quickService.clear();
    }

    public save(): void {
        this.quickService.saveDraft();
    }

    private resetUiState(): void {
        this.isCollapsed.set(false);
        this.isDetailsExpanded.set(false);
    }

    private async openEditDialogAsync(item: QuickMealItem): Promise<void> {
        const group = await this.createFormGroupAsync(item);
        const saved = await firstValueFrom(
            this.fdDialogService
                .open<MealManualItemDialogComponent, MealManualItemDialogData, boolean>(MealManualItemDialogComponent, {
                    preset: 'form',
                    data: { group },
                })
                .afterClosed(),
        );

        if (saved !== true) {
            return;
        }

        const updatedItem = this.toQuickMealItem(group);
        if (updatedItem === null) {
            return;
        }

        this.quickService.updateItem(item.key, updatedItem);
    }

    private async createFormGroupAsync(item: QuickMealItem): Promise<FormGroup<ConsumptionItemFormData>> {
        const sourceType = item.type === 'recipe' ? ConsumptionSourceType.Recipe : ConsumptionSourceType.Product;
        const amount =
            sourceType === ConsumptionSourceType.Recipe
                ? await this.mealManageFacade.resolveRecipeServingsToGramsAsync(item.recipe ?? null, item.amount)
                : item.amount;

        return this.mealManageFacade.createConsumptionItem(
            sourceType === ConsumptionSourceType.Product ? (item.product ?? null) : null,
            sourceType === ConsumptionSourceType.Recipe ? (item.recipe ?? null) : null,
            amount,
            sourceType,
        );
    }

    private toQuickMealItem(group: FormGroup<ConsumptionItemFormData>): Omit<QuickMealItem, 'flashId'> | null {
        const sourceType = group.controls.sourceType.value;
        const amount = group.controls.amount.value ?? 0;

        if (sourceType === ConsumptionSourceType.Product) {
            const product = group.controls.product.value;
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

        const recipe = group.controls.recipe.value;
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
