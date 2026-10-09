import { inject, Service } from '@angular/core';
import { FdUiDialogService } from 'fd-ui-kit/dialog/fd-ui-dialog.service';
import type { Observable } from 'rxjs';

import type { ProductId } from '../../../shared/models/semantics/entity-id';
import { PublicProductService } from '../api/public-product.service';
import type { PublicProduct } from '../models/public-product.data';

@Service()
export class PublicProductPreviewService {
    private readonly api = inject(PublicProductService);
    private readonly dialogs = inject(FdUiDialogService);
    public getById(id: ProductId): Observable<PublicProduct> {
        return this.api.getById(id);
    }
    public async openAsync(id: ProductId): Promise<void> {
        const { PublicProductPreviewComponent } = await import('../dialogs/public-product-preview/public-product-preview');
        this.dialogs.open(PublicProductPreviewComponent, { size: 'lg', data: id });
    }
}
