import { ChangeDetectionStrategy, Component, inject, Input } from '@angular/core';
import { CommonModule, NgOptimizedImage } from '@angular/common';
import { TranslatePipe } from '@ngx-translate/core';

import { FdUiButtonComponent } from 'fd-ui-kit/button/fd-ui-button.component';
import { FdUiIconModule } from 'fd-ui-kit/material';
import { ProductType } from '../../../products/models/product.data';
import { resolveProductImageUrl } from '../../../products/lib/product-image.util';
import { resolveRecipeImageUrl } from '../../../recipes/lib/recipe-image.util';
import { QuickMealItem, QuickMealService } from '../../lib/quick-meal.service';

@Component({
    selector: 'fd-quick-consumption-drawer',
    standalone: true,
    imports: [CommonModule, TranslatePipe, FdUiButtonComponent, FdUiIconModule, NgOptimizedImage],
    templateUrl: './quick-consumption-drawer.component.html',
    styleUrls: ['./quick-consumption-drawer.component.scss'],
    changeDetection: ChangeDetectionStrategy.OnPush,
})
export class QuickConsumptionDrawerComponent {
    private readonly quickService = inject(QuickMealService);
    private readonly fallbackImage = 'assets/images/stubs/receipt.png';

    @Input() public forceShow = false;
    @Input() public layout: 'fixed' | 'inline' = 'fixed';

    public readonly items = this.quickService.items;
    public readonly hasItems = this.quickService.hasItems;
    public readonly isSaving = this.quickService.isSaving;

    public get shouldRender(): boolean {
        return this.forceShow || this.hasItems();
    }

    public get isInline(): boolean {
        return this.layout === 'inline';
    }

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
