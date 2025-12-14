import { ChangeDetectionStrategy, Component, computed, inject } from '@angular/core';
import { CommonModule, NgOptimizedImage } from '@angular/common';
import { TranslatePipe } from '@ngx-translate/core';
import { FdUiButtonComponent } from 'fd-ui-kit/button/fd-ui-button.component';
import { FdUiIconModule } from 'fd-ui-kit/material';
import { QuickConsumptionService } from '../../../services/quick-consumption.service';
import { resolveProductImageUrl } from '../../../utils/product-stub.utils';
import { resolveRecipeImageUrl } from '../../../utils/recipe-stub.utils';
import { ProductType } from '../../../types/product.data';
import { QuickConsumptionItem } from '../../../services/quick-consumption.service';

@Component({
    selector: 'fd-quick-consumption-drawer',
    standalone: true,
    imports: [CommonModule, TranslatePipe, FdUiButtonComponent, FdUiIconModule, NgOptimizedImage],
    templateUrl: './quick-consumption-drawer.component.html',
    styleUrls: ['./quick-consumption-drawer.component.scss'],
    changeDetection: ChangeDetectionStrategy.OnPush,
})
export class QuickConsumptionDrawerComponent {
    private readonly quickService = inject(QuickConsumptionService);
    private readonly fallbackImage = 'assets/images/stubs/receipt.png';

    public readonly items = this.quickService.items;
    public readonly hasItems = this.quickService.hasItems;
    public readonly isSaving = this.quickService.isSaving;

    public imageFor(item: QuickConsumptionItem): string {
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
