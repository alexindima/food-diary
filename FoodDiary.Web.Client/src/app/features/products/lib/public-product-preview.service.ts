import { inject, Service } from '@angular/core';
import { FdUiDialogService } from 'fd-ui-kit/dialog/fd-ui-dialog.service';

import type { ProductId } from '../../../shared/models/semantics/entity-id';

@Service()
export class PublicProductPreviewService {
    private readonly dialogs = inject(FdUiDialogService);

    public async openAsync(id: ProductId): Promise<void> {
        const { PublicProductPreviewComponent } = await import('../dialogs/public-product-preview/public-product-preview');
        this.dialogs.open(PublicProductPreviewComponent, { size: 'lg', data: id });
    }
}
