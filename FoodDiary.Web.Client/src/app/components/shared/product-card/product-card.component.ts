import { ChangeDetectionStrategy, Component, EventEmitter, Input, Output } from '@angular/core';
import { CommonModule } from '@angular/common';
import { TranslatePipe } from '@ngx-translate/core';
import { FdUiButtonComponent } from 'fd-ui-kit/button/fd-ui-button.component';
import { FdUiIconModule } from 'fd-ui-kit/material';
import { NutrientBadgesComponent } from '../nutrient-badges/nutrient-badges.component';

export interface ProductCardItem {
    name: string;
    brand?: string | null;
    barcode?: string | null;
    isOwnedByCurrentUser: boolean;
    proteinsPerBase: number;
    fatsPerBase: number;
    carbsPerBase: number;
    fiberPerBase: number;
    alcoholPerBase: number;
    caloriesPerBase: number;
}

@Component({
    selector: 'fd-product-card',
    standalone: true,
    imports: [CommonModule, TranslatePipe, FdUiButtonComponent, FdUiIconModule, NutrientBadgesComponent],
    templateUrl: './product-card.component.html',
    styleUrl: './product-card.component.scss',
    changeDetection: ChangeDetectionStrategy.OnPush,
})
export class ProductCardComponent {
    @Input({ required: true }) public product!: ProductCardItem;
    @Input() public imageUrl?: string;
    @Output() public open = new EventEmitter<void>();
    @Output() public addToMeal = new EventEmitter<void>();

    public handleOpen(): void {
        this.open.emit();
    }

    public handleAdd(event: Event): void {
        event.stopPropagation();
        this.addToMeal.emit();
    }
}
