import { HttpClient } from '@angular/common/http';
import { inject, Service } from '@angular/core';
import { TranslateService } from '@ngx-translate/core';
import { map, type Observable } from 'rxjs';

import { environment } from '../../../../environments/environment';
import { createProductsSdk } from '../../../shared/api/sdk/products-sdk';
import type { ProductId } from '../../../shared/models/semantics/entity-id';
import type { PublicProduct } from '../models/public-product.data';
import { productLanguageRequestOptions } from './product-language-request';
import { publicProductFromSdk } from './product-sdk.mapper';

@Service()
export class PublicProductService {
    private readonly sdk = createProductsSdk(environment.apiUrls.products, inject(HttpClient));
    private readonly translate = inject(TranslateService);

    public getById(id: ProductId): Observable<PublicProduct> {
        return this.sdk.client
            .getPublicProductById({ version: this.sdk.version, id }, 'body', false, productLanguageRequestOptions(this.translate))
            .pipe(map(publicProductFromSdk));
    }
}
