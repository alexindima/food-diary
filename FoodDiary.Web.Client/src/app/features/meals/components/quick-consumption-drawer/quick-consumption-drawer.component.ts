import { CommonModule, NgOptimizedImage } from '@angular/common';
import { ChangeDetectionStrategy, Component, computed, inject, input, signal } from '@angular/core';
import { type FormGroup } from '@angular/forms';
import { TranslatePipe, TranslateService } from '@ngx-translate/core';
import { FdUiHintDirective } from 'fd-ui-kit';
import { FdUiButtonComponent } from 'fd-ui-kit/button/fd-ui-button.component';
import { FdUiDialogService } from 'fd-ui-kit/dialog/fd-ui-dialog.service';
import {
    FdUiEmojiPickerComponent,
    type FdUiEmojiPickerOption,
    type FdUiEmojiPickerValue,
} from 'fd-ui-kit/emoji-picker/fd-ui-emoji-picker.component';
import { DEFAULT_HUNGER_LEVELS, DEFAULT_SATIETY_LEVELS } from 'fd-ui-kit/satiety-scale/fd-ui-satiety-scale.component';
import { firstValueFrom } from 'rxjs';

import { resolveProductImageUrl } from '../../../products/lib/product-image.util';
import { ProductType } from '../../../products/models/product.data';
import { resolveRecipeImageUrl } from '../../../recipes/lib/recipe-image.util';
import { MealManageFacade } from '../../lib/meal-manage.facade';
import { type QuickMealDetails, type QuickMealItem, QuickMealService } from '../../lib/quick-meal.service';
import { ConsumptionSourceType } from '../../models/meal.data';
import { type ConsumptionItemFormData } from '../manage/base-meal-manage.types';
import {
    MealManualItemDialogComponent,
    type MealManualItemDialogData,
} from '../manage/meal-manual-item-dialog/meal-manual-item-dialog.component';

@Component({
    selector: 'fd-quick-consumption-drawer',
    standalone: true,
    imports: [CommonModule, TranslatePipe, FdUiHintDirective, FdUiButtonComponent, FdUiEmojiPickerComponent, NgOptimizedImage],
    templateUrl: './quick-consumption-drawer.component.html',
    styleUrls: ['./quick-consumption-drawer.component.scss'],
    changeDetection: ChangeDetectionStrategy.OnPush,
})
export class QuickConsumptionDrawerComponent {
    private static nextId = 0;

    private readonly quickService = inject(QuickMealService);
    private readonly translateService = inject(TranslateService);
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
    public readonly hungerEmojiOptions: FdUiEmojiPickerOption<number>[] = this.buildEmojiOptions(DEFAULT_HUNGER_LEVELS);
    public readonly satietyEmojiOptions: FdUiEmojiPickerOption<number>[] = this.buildEmojiOptions(DEFAULT_SATIETY_LEVELS);

    public imageFor(item: QuickMealItem): string {
        if (item.type === 'product' && item.product) {
            const type = (item.product.productType as ProductType | undefined) ?? ProductType.Unknown;
            return resolveProductImageUrl(item.product.imageUrl ?? undefined, type) ?? this.fallbackImage;
        }

        if (item.type === 'recipe' && item.recipe) {
            return resolveRecipeImageUrl(item.recipe.imageUrl ?? undefined) ?? this.fallbackImage;
        }

        return this.fallbackImage;
    }

    public itemName(item: QuickMealItem): string {
        return item.type === 'product' ? (item.product?.name ?? '') : (item.recipe?.name ?? '');
    }

    public removeItemAriaLabel(item: QuickMealItem): string {
        return this.translateService.instant('QUICK_CONSUMPTION.REMOVE_ITEM_NAMED', {
            name: this.itemName(item),
        });
    }

    public editItemAriaLabel(item: QuickMealItem): string {
        return this.translateService.instant('CONSUMPTION_MANAGE.MANUAL_ITEM_EDIT') + ': ' + this.itemName(item);
    }

    public updateDetails(field: keyof Pick<QuickMealDetails, 'date' | 'time' | 'comment'>, value: string): void {
        this.quickService.updateDetails({ [field]: value });
    }

    public toggleDetails(): void {
        this.isDetailsExpanded.update(value => !value);
    }

    public toggleCollapsed(): void {
        this.isCollapsed.update(value => !value);
    }

    public onSatietyLevelChange(field: 'preMealSatietyLevel' | 'postMealSatietyLevel', value: FdUiEmojiPickerValue | null): void {
        if (typeof value !== 'number') {
            return;
        }

        this.quickService.updateDetails({ [field]: this.normalizeSatietyLevel(value) });
    }

    public getSatietyButtonAriaLabel(kind: 'before' | 'after'): string {
        const value = kind === 'before' ? this.details().preMealSatietyLevel : this.details().postMealSatietyLevel;
        const meta = this.getSatietyLevelMeta(kind, value);
        const fieldLabel = this.translateService.instant(
            kind === 'before' ? 'AI_INPUT_BAR.DETAIL_SATIETY_BEFORE' : 'AI_INPUT_BAR.DETAIL_SATIETY_AFTER',
        );
        return `${fieldLabel}. ${meta.label}. ${meta.description}`.trim();
    }

    public edit(item: QuickMealItem): void {
        const group = this.createFormGroup(item);
        void firstValueFrom(
            this.fdDialogService
                .open<MealManualItemDialogComponent, MealManualItemDialogData, boolean>(MealManualItemDialogComponent, {
                    preset: 'form',
                    data: { group },
                })
                .afterClosed(),
        ).then(saved => {
            if (!saved) {
                return;
            }

            const updatedItem = this.toQuickMealItem(group);
            if (!updatedItem) {
                return;
            }

            this.quickService.updateItem(item.key, updatedItem);
        });
    }

    public remove(key: string): void {
        this.quickService.removeItem(key);
    }

    public clear(): void {
        this.quickService.clear();
    }

    public openEditor(): void {
        void this.quickService.openEditorAsync();
    }

    public save(): void {
        this.quickService.saveDraft();
    }

    private buildEmojiOptions(levels: typeof DEFAULT_SATIETY_LEVELS): FdUiEmojiPickerOption<number>[] {
        return levels.map(level => {
            const label = this.translateService.instant(level.titleKey);
            const description = this.translateService.instant(level.descriptionKey);
            return {
                value: level.value,
                emoji: level.emoji,
                label,
                description,
                ariaLabel: `${label}. ${description}`,
                hint: `${label}. ${description}`,
            };
        });
    }

    private getSatietyLevelMeta(kind: 'before' | 'after', value: number | null): { label: string; description: string } {
        const normalizedValue = this.normalizeSatietyLevel(value);
        const levels = kind === 'before' ? DEFAULT_HUNGER_LEVELS : DEFAULT_SATIETY_LEVELS;
        const config = levels.find(level => level.value === normalizedValue);
        return {
            label: this.translateService.instant(config?.titleKey ?? ''),
            description: this.translateService.instant(config?.descriptionKey ?? ''),
        };
    }

    private normalizeSatietyLevel(value: number | null): number {
        if (!value) {
            return 3;
        }

        if (value > 5) {
            return Math.min(5, Math.max(1, Math.round(value / 2)));
        }

        return Math.max(1, value);
    }

    private createFormGroup(item: QuickMealItem): FormGroup<ConsumptionItemFormData> {
        const sourceType = item.type === 'recipe' ? ConsumptionSourceType.Recipe : ConsumptionSourceType.Product;
        return this.mealManageFacade.createConsumptionItem(
            sourceType === ConsumptionSourceType.Product ? (item.product ?? null) : null,
            sourceType === ConsumptionSourceType.Recipe ? (item.recipe ?? null) : null,
            item.amount,
            sourceType,
        );
    }

    private toQuickMealItem(group: FormGroup<ConsumptionItemFormData>): Omit<QuickMealItem, 'flashId'> | null {
        const sourceType = group.controls.sourceType.value;
        const amount = group.controls.amount.value ?? 0;

        if (sourceType === ConsumptionSourceType.Product) {
            const product = group.controls.product.value;
            if (!product?.id) {
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
        if (!recipe?.id) {
            return null;
        }

        return {
            key: `recipe-${recipe.id}`,
            type: 'recipe',
            recipe,
            amount,
        };
    }
}
