import { InjectionToken } from '@angular/core';
import type { Observable } from 'rxjs';

import type { ProductId } from '../../../shared/models/semantics/entity-id';
import type { UsdaFoodId } from '../../../shared/models/semantics/usda-food-id';
import type { UsdaFoodDetail } from '../../../shared/models/usda.data';

export type UsdaProductLink = {
    getFoodDetail: (fdcId: UsdaFoodId) => Observable<UsdaFoodDetail>;
    linkProduct: (productId: ProductId, fdcId: UsdaFoodId) => Observable<void>;
    unlinkProduct: (productId: ProductId) => Observable<void>;
};

export const USDA_PRODUCT_LINK = new InjectionToken<UsdaProductLink>('UsdaProductLink');
