import { InjectionToken } from '@angular/core';
import type { Observable } from 'rxjs';

import type { UsdaFoodDetail } from '../../../shared/models/usda.data';

export type UsdaProductLink = {
    getFoodDetail: (fdcId: number) => Observable<UsdaFoodDetail>;
    linkProduct: (productId: string, fdcId: number) => Observable<void>;
    unlinkProduct: (productId: string) => Observable<void>;
};

export const USDA_PRODUCT_LINK = new InjectionToken<UsdaProductLink>('UsdaProductLink');
