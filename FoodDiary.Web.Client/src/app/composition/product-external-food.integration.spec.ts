import { HttpStatusCode, provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting, type TestRequest } from '@angular/common/http/testing';
import { TestBed } from '@angular/core/testing';
import { firstValueFrom } from 'rxjs';
import { afterEach, beforeEach, expect, it, vi } from 'vitest';

import { environment } from '../../environments/environment';
import { ProductExternalFoodFacade } from '../features/products/lib/manage/product-external-food.facade';
import type { UsdaFoodDetailHttpResponse } from '../shared/api/sdk/generated/model/usda-food-detail-http-response';
import { entityId } from '../shared/models/semantics/entity-id';
import { usdaFoodId } from '../shared/models/semantics/usda-food-id';
import { FEATURE_ACTION_PROVIDERS } from './feature-action.providers';

const BARCODE = '0012345678905';
const PRODUCT_ID = entityId<'product'>('12345678-1234-1234-1234-123456789abc');
const FDC_NUMBER = 17_000;
const FDC_ID = usdaFoodId(FDC_NUMBER);
const BARCODE_URL = `${environment.apiUrls.openFoodFacts}/products/${BARCODE}`;
const DETAIL_URL = `${environment.apiUrls.usda}/foods/${FDC_ID}`;
const LINK_URL = `${environment.apiUrls.usda}/products/${PRODUCT_ID}/link`;
const FOOD_DETAIL: UsdaFoodDetailHttpResponse = {
    fdcId: FDC_ID,
    description: 'Plain yogurt',
    foodCategory: 'Dairy',
    nutrients: [],
    portions: [],
    healthScores: null,
};

let http: HttpTestingController;
let products: ProductExternalFoodFacade;

beforeEach(() => {
    TestBed.configureTestingModule({
        providers: [...FEATURE_ACTION_PROVIDERS, provideHttpClient(), provideHttpClientTesting()],
    });
    products = TestBed.inject(ProductExternalFoodFacade);
    http = TestBed.inject(HttpTestingController);
});

afterEach(() => {
    try {
        http.verify();
    } finally {
        TestBed.resetTestingModule();
    }
});

it('preserves a barcode with leading zeroes when finding food for the product editor', async () => {
    const response = { barcode: BARCODE, name: 'Sparkling water', caloriesPer100G: 0 };
    const lookup = firstValueFrom(products.searchByBarcode(BARCODE));
    http.expectOne({ method: 'GET', url: BARCODE_URL }).flush(response);

    await expect(lookup).resolves.toEqual(response);
});

it('uses the real lazy USDA binding to read, link and remove a micronutrient source', async () => {
    const detail = firstValueFrom(products.getUsdaFoodDetail(FDC_ID));
    const detailRequest = await activeRequestAsync('GET', DETAIL_URL);
    detailRequest.flush(FOOD_DETAIL);
    await expect(detail).resolves.toEqual(FOOD_DETAIL);

    const link = firstValueFrom(products.linkUsdaProduct(PRODUCT_ID, FDC_ID));
    const linkRequest = await activeRequestAsync('PUT', LINK_URL);
    expect(linkRequest.request.body).toEqual({ fdcId: FDC_ID });
    linkRequest.flush(null, { status: HttpStatusCode.NoContent, statusText: 'No Content' });
    await link;

    const unlink = firstValueFrom(products.unlinkUsdaProduct(PRODUCT_ID));
    const unlinkRequest = await activeRequestAsync('DELETE', LINK_URL);
    expect(unlinkRequest.request.body).toBeNull();
    unlinkRequest.flush(null, { status: HttpStatusCode.NoContent, statusText: 'No Content' });
    await unlink;
});

async function activeRequestAsync(method: string, url: string): Promise<TestRequest> {
    return vi.waitFor(() => http.expectOne({ method, url }));
}
