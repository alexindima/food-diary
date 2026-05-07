import { CommonModule, NgOptimizedImage } from '@angular/common';
import { ChangeDetectionStrategy, Component, computed, inject, input } from '@angular/core';
import { TranslatePipe, TranslateService } from '@ngx-translate/core';
import { FdUiHintDirective } from 'fd-ui-kit';
import { FdUiButtonComponent } from 'fd-ui-kit/button/fd-ui-button.component';

import { resolveProductImageUrl } from '../../../products/lib/product-image.util';
import { ProductType } from '../../../products/models/product.data';
import { resolveRecipeImageUrl } from '../../../recipes/lib/recipe-image.util';
import { type QuickMealItem, QuickMealService } from '../../lib/quick-meal.service';

@Component({
    selector: 'fd-quick-consumption-drawer',
    standalone: true,
    imports: [CommonModule, TranslatePipe, FdUiHintDirective, FdUiButtonComponent, NgOptimizedImage],
    templateUrl: './quick-consumption-drawer.component.html',
    styleUrls: ['./quick-consumption-drawer.component.scss'],
    changeDetection: ChangeDetectionStrategy.OnPush,
})
export class QuickConsumptionDrawerComponent {
    private static nextId = 0;

    private readonly quickService = inject(QuickMealService);
    private readonly translateService = inject(TranslateService);
    private readonly fallbackImage = 'assets/images/stubs/receipt.png';

    public readonly forceShow = input(false);
    public readonly layout = input<'fixed' | 'inline'>('fixed');
    public readonly titleId = `fd-quick-consumption-title-${QuickConsumptionDrawerComponent.nextId++}`;

    public readonly items = this.quickService.items;
    public readonly hasItems = this.quickService.hasItems;
    public readonly isSaving = this.quickService.isSaving;

    public readonly shouldRender = computed(() => this.forceShow() || this.hasItems());
    public readonly isInline = computed(() => this.layout() === 'inline');

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
}
