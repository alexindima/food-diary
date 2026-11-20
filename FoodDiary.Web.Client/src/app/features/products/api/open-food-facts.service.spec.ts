import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { TestBed } from '@angular/core/testing';
import { firstValueFrom } from 'rxjs';
import { afterEach, beforeEach, describe, expect, it } from 'vitest';

import { OpenFoodFactsService } from './open-food-facts.service';
import { OPEN_FOOD_FACTS_SEARCH_LIMIT } from './product-api.tokens';

const BASE_URL = 'http://localhost:5300/api/v1/open-food-facts';
const DEFAULT_SEARCH_LIMIT = 10;
const OVERRIDE_SEARCH_LIMIT = 4;

let service: OpenFoodFactsService;
let httpMock: HttpTestingController;

beforeEach(() => {
    TestBed.configureTestingModule({
        providers: [OpenFoodFactsService, provideHttpClient(), provideHttpClientTesting()],
    });
    service = TestBed.inject(OpenFoodFactsService);
    httpMock = TestBed.inject(HttpTestingController);
});

afterEach(() => {
    httpMock.verify();
});

describe('OpenFoodFactsService', () => {
    it('should use injected default search limit', () => {
        service.search('milk').subscribe(result => {
            expect(result).toEqual([]);
        });

        const req = httpMock.expectOne(r => r.url === `${BASE_URL}/products` && r.method === 'GET');
        expect(req.request.params.get('search')).toBe('milk');
        expect(req.request.params.get('limit')).toBe(String(DEFAULT_SEARCH_LIMIT));
        req.flush([]);
    });

    it('should allow search limit to be overridden through DI', () => {
        TestBed.resetTestingModule();
        TestBed.configureTestingModule({
            providers: [
                OpenFoodFactsService,
                provideHttpClient(),
                provideHttpClientTesting(),
                { provide: OPEN_FOOD_FACTS_SEARCH_LIMIT, useValue: OVERRIDE_SEARCH_LIMIT },
            ],
        });
        service = TestBed.inject(OpenFoodFactsService);
        httpMock = TestBed.inject(HttpTestingController);

        service.search('milk').subscribe();

        const req = httpMock.expectOne(r => r.url === `${BASE_URL}/products` && r.method === 'GET');
        expect(req.request.params.get('limit')).toBe(String(OVERRIDE_SEARCH_LIMIT));
        req.flush([]);
    });
});

describe('Open Food Facts failures and barcode lookup', () => {
    it('preserves leading zeroes in a barcode lookup', () => {
        let received: unknown;
        service.searchByBarcode('0012345678905').subscribe(value => {
            received = value;
        });
        const req = httpMock.expectOne(`${BASE_URL}/products/0012345678905`);
        expect(req.request.method).toBe('GET');
        req.flush(null);
        expect(received).toBeNull();
    });
    it('returns null when barcode lookup fails', () => {
        let received: unknown;
        service.searchByBarcode('00123').subscribe(value => {
            received = value;
        });
        httpMock.expectOne(`${BASE_URL}/products/00123`).flush({}, { status: 503, statusText: 'Unavailable' });
        expect(received).toBeNull();
    });
    it('returns an empty search result on failure and honors explicit limit', () => {
        let received: unknown;
        service.search('milk', 2).subscribe(value => {
            received = value;
        });
        const req = httpMock.expectOne(r => r.url === `${BASE_URL}/products`);
        expect(req.request.params.get('limit')).toBe('2');
        req.flush({}, { status: 503, statusText: 'Unavailable' });
        expect(received).toEqual([]);
    });
});

describe('Open Food Facts persisted catalog fields', () => {
    const barcode = '0012345678905';
    const product = { barcode, name: 'Sparkling water', brand: 'Spring', caloriesPer100G: 0 };

    it('preserves a leading-zero barcode and explicit zero calories from a cached lookup', async () => {
        const pending = firstValueFrom(service.searchByBarcode(barcode));
        httpMock.expectOne({ url: `${BASE_URL}/products/${barcode}`, method: 'GET' }).flush(product);

        await expect(pending).resolves.toEqual(product);
    });

    it('preserves search order and sparse fields when both catalog entries are valid', async () => {
        const pending = firstValueFrom(service.search('sparkling water'));
        const matches = [product, { barcode: '0012345678912', name: 'Mineral water' }];
        const request = httpMock.expectOne(req => req.url === `${BASE_URL}/products` && req.method === 'GET');
        expect(request.request.params.get('search')).toBe('sparkling water');
        request.flush(matches);

        await expect(pending).resolves.toEqual(matches);
    });

    it('does not return a partially decoded search page when the provider omits a required barcode', async () => {
        const pending = firstValueFrom(service.search('sparkling water'));
        httpMock.expectOne(req => req.url === `${BASE_URL}/products`).flush([product, { name: 'Unidentified water' }]);

        await expect(pending).resolves.toEqual([]);
    });
});
