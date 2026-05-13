import { ChangeDetectionStrategy, Component, input } from '@angular/core';

import { ProductManageFormComponent } from '../../components/manage/product-manage-form.component';
import type { Product } from '../../models/product.data';

@Component({
    selector: 'fd-product-edit',
    templateUrl: './product-edit.component.html',
    styleUrls: ['./product-edit.component.scss', '../../components/manage/product-manage-form.component.scss'],
    changeDetection: ChangeDetectionStrategy.OnPush,
    imports: [ProductManageFormComponent],
})
export class ProductEditComponent {
    public readonly product = input<Product | null>(null);
}
