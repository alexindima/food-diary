import { ChangeDetectionStrategy, Component, EventEmitter, Input, Output } from '@angular/core';
import { CommonModule } from '@angular/common';
import { TranslatePipe } from '@ngx-translate/core';
import { FdUiButtonComponent } from 'fd-ui-kit/button/fd-ui-button.component';
import { FdUiIconModule } from 'fd-ui-kit/material';
import { Product } from '../../../types/product.data';

@Component({
    selector: 'fd-product-card',
    standalone: true,
    imports: [CommonModule, TranslatePipe, FdUiButtonComponent, FdUiIconModule],
    templateUrl: './product-card.component.html',
    styleUrl: './product-card.component.scss',
    changeDetection: ChangeDetectionStrategy.OnPush,
})
export class ProductCardComponent {
    @Input({ required: true }) product!: Product;
    @Input() imageUrl?: string;
    @Output() open = new EventEmitter<void>();

    public handleOpen(): void {
        this.open.emit();
    }
}
