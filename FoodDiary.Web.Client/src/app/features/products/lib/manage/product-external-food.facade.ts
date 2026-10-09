import { inject, Service } from '@angular/core';
import type { Observable } from 'rxjs';

import type { ProductId } from '../../../../shared/models/semantics/entity-id';
import type { UsdaFoodId } from '../../../../shared/models/semantics/usda-food-id';
import type { UsdaFoodDetail } from '../../../../shared/models/usda.data';
import { USDA_PRODUCT_LINK } from '../../../usda/contracts/usda-product-link';
import { OpenFoodFactsService } from '../../api/open-food-facts.service';
import type { OpenFoodFactsProduct } from '../../models/open-food-facts.data';

@Service()
export class ProductExternalFoodFacade {
    private readonly openFoodFactsService = inject(OpenFoodFactsService);
    private readonly usdaService = inject(USDA_PRODUCT_LINK);

    public searchByBarcode(barcode: string): Observable<OpenFoodFactsProduct | null> {
        return this.openFoodFactsService.searchByBarcode(barcode);
    }

    public getUsdaFoodDetail(fdcId: UsdaFoodId): Observable<UsdaFoodDetail> {
        return this.usdaService.getFoodDetail(fdcId);
    }

    public linkUsdaProduct(productId: ProductId, fdcId: UsdaFoodId): Observable<void> {
        return this.usdaService.linkProduct(productId, fdcId);
    }

    public unlinkUsdaProduct(productId: ProductId): Observable<void> {
        return this.usdaService.unlinkProduct(productId);
    }
}
