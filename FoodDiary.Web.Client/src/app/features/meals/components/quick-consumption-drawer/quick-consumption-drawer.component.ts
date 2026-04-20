import { ChangeDetectionStrategy, Component, computed, inject, input } from '@angular/core';
import { CommonModule, NgOptimizedImage } from '@angular/common';
import { TranslatePipe } from '@ngx-translate/core';
import { FdUiHintDirective } from 'fd-ui-kit';

import { FdUiButtonComponent } from 'fd-ui-kit/button/fd-ui-button.component';
import { ProductType } from '../../../products/models/product.data';
import { resolveProductImageUrl } from '../../../products/lib/product-image.util';
import { resolveRecipeImageUrl } from '../../../recipes/lib/recipe-image.util';
import { QuickMealItem, QuickMealService } from '../../lib/quick-meal.service';

@Component({
    selector: 'fd-quick-consumption-drawer',
    standalone: true,
    imports: [CommonModule, TranslatePipe, FdUiHintDirective, FdUiButtonComponent, NgOptimizedImage],
    templateUrl: './quick-consumption-drawer.component.html',
    styleUrls: ['./quick-consumption-drawer.component.scss'],
    changeDetection: ChangeDetectionStrategy.OnPush,
})
export class QuickConsumptionDrawerComponent {
    private readonly quickService = inject(QuickMealService);
    private readonly fallbackImage = 'assets/images/stubs/receipt.png';

    public readonly forceShow = input(false);
    public readonly layout = input<'fixed' | 'inline'>('fixed');

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

    public remove(key: string): void {
        this.quickService.removeItem(key);
    }

    public clear(): void {
        this.quickService.clear();
    }

    public openEditor(): void {
        void this.quickService.openEditor();
    }

    public save(): void {
        this.quickService.saveDraft();
    }
}
