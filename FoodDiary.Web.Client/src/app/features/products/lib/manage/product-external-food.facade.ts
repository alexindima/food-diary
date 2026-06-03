import { inject, Service } from '@angular/core';
import type { Observable } from 'rxjs';

import { UsdaService } from '../../../usda/api/usda.service';
import type { UsdaFoodDetail } from '../../../usda/models/usda.data';
import { OpenFoodFactsService } from '../../api/open-food-facts.service';
import type { OpenFoodFactsProduct } from '../../models/open-food-facts.data';

@Service()
export class ProductExternalFoodFacade {
    private readonly openFoodFactsService = inject(OpenFoodFactsService);
    private readonly usdaService = inject(UsdaService);

    public searchByBarcode(barcode: string): Observable<OpenFoodFactsProduct | null> {
        return this.openFoodFactsService.searchByBarcode(barcode);
    }

    public getUsdaFoodDetail(fdcId: number): Observable<UsdaFoodDetail> {
        return this.usdaService.getFoodDetail(fdcId);
    }

    public linkUsdaProduct(productId: string, fdcId: number): Observable<void> {
        return this.usdaService.linkProduct(productId, fdcId);
    }

    public unlinkUsdaProduct(productId: string): Observable<void> {
        return this.usdaService.unlinkProduct(productId);
    }
}
