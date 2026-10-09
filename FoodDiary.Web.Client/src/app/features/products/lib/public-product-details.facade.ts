import { inject, Service } from '@angular/core';
import type { Observable } from 'rxjs';

import type { ProductId } from '../../../shared/models/semantics/entity-id';
import { PublicProductService } from '../api/public-product.service';
import type { PublicProduct } from '../models/public-product.data';

@Service()
export class PublicProductDetailsFacade {
    private readonly api = inject(PublicProductService);
    public getById(id: ProductId): Observable<PublicProduct> {
        return this.api.getById(id);
    }
}
