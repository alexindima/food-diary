import { ChangeDetectionStrategy, Component, input, viewChild } from '@angular/core';

import type { UnsavedEditor } from '../../../../../shared/forms/unsaved-editor';
import type { Product } from '../../../../../shared/models/product.data';
import { ProductManageFormComponent } from '../../../components/manage/product-manage-form/product-manage-form';

@Component({
    selector: 'fd-product-edit',
    templateUrl: './product-edit.html',
    styleUrls: ['./product-edit.scss', '../../../components/manage/product-manage-form/product-manage-form.scss'],
    changeDetection: ChangeDetectionStrategy.OnPush,
    imports: [ProductManageFormComponent],
})
export class ProductEditComponent implements UnsavedEditor {
    private readonly editor = viewChild(ProductManageFormComponent);
    public readonly product = input<Product | null>(null);

    // eslint-disable-next-line local/prefer-protected-template-members -- Public editor departure contract used by the router and parent view queries.
    public async canLeaveAsync(): Promise<boolean> {
        return this.editor()?.canLeaveAsync() ?? true;
    }
}
