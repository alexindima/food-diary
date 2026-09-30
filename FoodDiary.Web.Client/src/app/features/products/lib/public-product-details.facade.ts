import { inject, Service } from '@angular/core';
import type { Observable } from 'rxjs';

import { PublicProductService } from '../api/public-product.service';
import type { PublicProduct } from '../models/public-product.data';

@Service()
export class PublicProductDetailsFacade {
    private readonly api = inject(PublicProductService);
    public getById(id: string): Observable<PublicProduct> {
        return this.api.getById(id);
    }
}
