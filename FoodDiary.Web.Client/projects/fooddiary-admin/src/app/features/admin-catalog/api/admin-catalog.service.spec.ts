import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { TestBed } from '@angular/core/testing';
import { retry } from 'rxjs';
import { afterEach, beforeEach, describe, expect, it } from 'vitest';

import type { CatalogProduct, CatalogResult } from '../models/catalog-transfer';
import { AdminCatalogService } from './admin-catalog.service';

const product: CatalogProduct = {
    id: 'product',
    name: 'Product',
    barcode: null,
    brand: null,
    productType: 'Food',
    category: null,
    description: null,
    imageUrl: null,
    baseUnit: 'g',
    baseAmount: 100,
    defaultPortionAmount: 0,
    caloriesPerBase: 0,
    proteinsPerBase: 1.23456789,
    fatsPerBase: 0,
    carbsPerBase: 0,
    fiberPerBase: 0,
    alcoholPerBase: 0,
};
let http: HttpTestingController;
let service: AdminCatalogService;

beforeEach(() => {
    TestBed.configureTestingModule({ providers: [provideHttpClient(), provideHttpClientTesting()] });
    http = TestBed.inject(HttpTestingController);
    service = TestBed.inject(AdminCatalogService);
});
afterEach(() => {
    http.verify();
});

describe('AdminCatalogService generated requests', () => {
    it('retains the import idempotency key and original nutrient payload across retry', () => {
        let result: CatalogResult | undefined;
        service
            .importItem('products', product)
            .pipe(retry(1))
            .subscribe(value => {
                result = value;
            });
        const first = http.expectOne(request => request.url.endsWith('/admin/catalog/products/import'));
        const key = first.request.headers.get('Idempotency-Key');
        expect(key).toBeTruthy();
        expect(first.request.body).toEqual(product);
        first.flush('temporary', { status: 503, statusText: 'Unavailable' });
        const second = http.expectOne(request => request.url.endsWith('/admin/catalog/products/import'));
        expect(second.request.headers.get('Idempotency-Key')).toBe(key);
        second.flush({ id: 'product', status: 'imported', errors: [] });
        expect(result?.status).toBe('imported');
    });
});
