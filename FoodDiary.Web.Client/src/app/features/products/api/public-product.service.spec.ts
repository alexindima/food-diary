import { HttpStatusCode, provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { TestBed } from '@angular/core/testing';
import { TranslateService } from '@ngx-translate/core';
import { firstValueFrom } from 'rxjs';
import { afterEach, beforeEach, describe, expect, it } from 'vitest';

import { environment } from '../../../../environments/environment';
import { entityId } from '../../../shared/models/semantics/entity-id';
import { PublicProductDetailsFacade } from '../lib/public-product-details.facade';

const baseUrl = `${environment.apiUrls.products}/public`;
const product = {
    id: entityId<'product'>('oat-drink'),
    name: 'Овсяный напиток',
    baseAmount: 100,
    baseUnit: 'Ml',
    calories: 45,
    proteins: 1,
    fats: 1.5,
    carbs: 7,
    fiber: 0,
    alcohol: 0,
};

let httpMock: HttpTestingController;
let details: PublicProductDetailsFacade;

beforeEach(() => {
    TestBed.configureTestingModule({
        providers: [
            provideHttpClient(),
            provideHttpClientTesting(),
            { provide: TranslateService, useValue: { getCurrentLang: (): string => 'ru' } },
        ],
    });
    httpMock = TestBed.inject(HttpTestingController);
    details = TestBed.inject(PublicProductDetailsFacade);
});

afterEach(() => {
    httpMock.verify();
});

describe('Public product details through the catalog API', () => {
    it('requests the current language and preserves the published photo order and nutrition basis', async () => {
        const imageUrl = 'https://cdn.example.test/oat/front.jpg';
        const published = { ...product, brand: 'Овёс', imageUrl, images: [imageUrl, 'https://cdn.example.test/oat/back.jpg'] };
        const pending = firstValueFrom(details.getById(product.id));
        const request = httpMock.expectOne({ url: `${baseUrl}/${product.id}`, method: 'GET' });
        expect(request.request.headers.get('Accept-Language')).toBe('ru');
        request.flush(published);

        await expect(pending).resolves.toEqual(published);
    });

    it('preserves an older public product without optional brand or photo data', async () => {
        const pending = firstValueFrom(details.getById(product.id));
        httpMock.expectOne({ url: `${baseUrl}/${product.id}`, method: 'GET' }).flush(product);

        await expect(pending).resolves.toEqual({ ...product, brand: null, imageUrl: null, images: undefined });
    });

    it('returns a not-found response for an unpublished product', async () => {
        const unpublished = entityId<'product'>('unpublished');
        const pending = firstValueFrom(details.getById(unpublished));
        const error = { title: 'Product not found' };
        httpMock.expectOne({ url: `${baseUrl}/${unpublished}`, method: 'GET' }).flush(error, {
            status: HttpStatusCode.NotFound,
            statusText: 'Not Found',
        });

        await expect(pending).rejects.toMatchObject({ status: HttpStatusCode.NotFound, error });
    });
});
