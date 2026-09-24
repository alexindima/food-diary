import { HttpStatusCode, provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { TestBed } from '@angular/core/testing';
import type { Observable } from 'rxjs';
import { afterEach, beforeEach, describe, expect, it } from 'vitest';

import { DEFAULT_NUTRITION_BASE_AMOUNT } from '../../../shared/lib/nutrition.constants';
import type { PageOf } from '../../../shared/models/page-of.data';
import {
    type CreateProductRequest,
    MeasurementUnit,
    type Product,
    ProductType,
    ProductVisibility,
    type UpdateProductRequest,
} from '../models/product.data';
import { ProductService } from './product.service';
import { PRODUCT_API_LIMITS } from './product-api.tokens';

const BASE_URL = 'http://localhost:5300/api/v1/products';
const CHICKEN_CALORIES = 165;
const CHICKEN_PROTEINS = 31;
const CHICKEN_FATS = 3.6;
const QUALITY_SCORE_GREEN = 80;
const DEFAULT_PAGE_LIMIT = 10;
const DEFAULT_RECENT_LIMIT = 10;
const DEFAULT_FAVORITE_LIMIT = 10;
const DEFAULT_SUGGESTIONS_LIMIT = 5;
const FILTERED_PAGE_LIMIT = 20;
const RECENT_PRODUCTS_LIMIT = 5;
const OVERRIDE_RECENT_LIMIT = 7;
const OVERRIDE_FAVORITE_LIMIT = 8;
const OVERRIDE_SUGGESTIONS_LIMIT = 3;
const NEW_PRODUCT_CALORIES = 100;
const NEW_PRODUCT_PROTEINS = 10;
const NEW_PRODUCT_FATS = 2;
const NEW_PRODUCT_CARBS = 12;
const MOCK_PRODUCT: Product = {
    id: 'p1',
    name: 'Chicken Breast',
    barcode: null,
    brand: null,
    productType: ProductType.Meat,
    category: null,
    description: null,
    comment: null,
    imageUrl: null,
    imageAssetId: null,
    baseUnit: MeasurementUnit.G,
    baseAmount: DEFAULT_NUTRITION_BASE_AMOUNT,
    defaultPortionAmount: DEFAULT_NUTRITION_BASE_AMOUNT,
    caloriesPerBase: CHICKEN_CALORIES,
    proteinsPerBase: CHICKEN_PROTEINS,
    fatsPerBase: CHICKEN_FATS,
    carbsPerBase: 0,
    fiberPerBase: 0,
    alcoholPerBase: 0,
    usageCount: 0,
    visibility: ProductVisibility.Private,
    createdAt: new Date('2026-01-01'),
    isOwnedByCurrentUser: true,
    qualityScore: QUALITY_SCORE_GREEN,
    qualityGrade: 'green',
};
const MOCK_PAGE: PageOf<Product> = {
    data: [MOCK_PRODUCT],
    page: 1,
    limit: DEFAULT_PAGE_LIMIT,
    totalPages: 1,
    totalItems: 1,
};

let service: ProductService;
let httpMock: HttpTestingController;

beforeEach(() => {
    TestBed.configureTestingModule({
        providers: [ProductService, provideHttpClient(), provideHttpClientTesting()],
    });
    service = TestBed.inject(ProductService);
    httpMock = TestBed.inject(HttpTestingController);
});

afterEach(() => {
    httpMock.verify();
});

describe('ProductService', () => {
    it('should be created', () => {
        expect(service).toBeTruthy();
    });
});

describe('ProductService query', () => {
    it('should query products with pagination params', () => {
        service.query(1, DEFAULT_PAGE_LIMIT).subscribe(result => {
            expect(result).toEqual(MOCK_PAGE);
        });

        const req = httpMock.expectOne(r => r.url === `${BASE_URL}/` && r.method === 'GET');
        expect(req.request.params.get('page')).toBe('1');
        expect(req.request.params.get('limit')).toBe(String(DEFAULT_PAGE_LIMIT));
        expect(req.request.params.get('includePublic')).toBe('true');
        req.flush(MOCK_PAGE);
    });

    it('should include filters in query params', () => {
        const filters = { search: 'chicken', productTypes: [ProductType.Meat, ProductType.Dairy] };

        service.query(1, FILTERED_PAGE_LIMIT, filters, false).subscribe(result => {
            expect(result.data.length).toBe(1);
        });

        const req = httpMock.expectOne(r => r.url === `${BASE_URL}/` && r.method === 'GET');
        expect(req.request.params.get('search')).toBe('chicken');
        expect(req.request.params.get('productTypes')).toBe('Meat,Dairy');
        expect(req.request.params.get('includePublic')).toBe('false');
        req.flush(MOCK_PAGE);
    });

    it('should not include empty search filter', () => {
        const filters = { search: '  ' };

        service.query(1, DEFAULT_PAGE_LIMIT, filters).subscribe();

        const req = httpMock.expectOne(r => r.url === `${BASE_URL}/` && r.method === 'GET');
        expect(req.request.params.has('search')).toBe(false);
        req.flush(MOCK_PAGE);
    });

    it('should return empty PageOf on query failure', () => {
        service.query(1, DEFAULT_PAGE_LIMIT).subscribe(result => {
            expect(result).toEqual({ data: [], page: 1, limit: DEFAULT_PAGE_LIMIT, totalPages: 0, totalItems: 0 });
        });

        const req = httpMock.expectOne(r => r.url === `${BASE_URL}/` && r.method === 'GET');
        req.flush('Server Error', { status: HttpStatusCode.InternalServerError, statusText: 'Internal Server Error' });
    });
});

describe('ProductService reads', () => {
    it('should get product by id', () => {
        service.getById('p1').subscribe(result => {
            expect(result).toEqual(MOCK_PRODUCT);
        });

        const req = httpMock.expectOne(`${BASE_URL}/p1`);
        expect(req.request.method).toBe('GET');
        req.flush(MOCK_PRODUCT);
    });

    it('should return null on getById failure', () => {
        service.getById('p1').subscribe(result => {
            expect(result).toBeNull();
        });

        const req = httpMock.expectOne(`${BASE_URL}/p1`);
        req.flush('Not Found', { status: HttpStatusCode.NotFound, statusText: 'Not Found' });
    });
});

describe('ProductService mutations', () => {
    it('should create product', () => {
        const createData = createProductRequest();

        service.create(createData).subscribe(result => {
            expect(result).toEqual(MOCK_PRODUCT);
        });

        const req = httpMock.expectOne(`${BASE_URL}/`);
        expect(req.request.method).toBe('POST');
        expect(req.request.body).toEqual(createData);
        req.flush(MOCK_PRODUCT);
    });

    it('should update product via PATCH', () => {
        const updateData: UpdateProductRequest = { name: 'Updated Product' };

        service.update('p1', updateData).subscribe(result => {
            expect(result).toEqual(MOCK_PRODUCT);
        });

        const req = httpMock.expectOne(`${BASE_URL}/p1`);
        expect(req.request.method).toBe('PATCH');
        expect(req.request.body).toEqual(updateData);
        req.flush(MOCK_PRODUCT);
    });

    it('should delete product by id', () => {
        service.deleteById('p1').subscribe();

        const req = httpMock.expectOne(`${BASE_URL}/p1`);
        expect(req.request.method).toBe('DELETE');
        req.flush(null);
    });

    it('should duplicate product', () => {
        service.duplicate('p1').subscribe(result => {
            expect(result).toEqual(MOCK_PRODUCT);
        });

        const req = httpMock.expectOne(`${BASE_URL}/p1/duplicate`);
        expect(req.request.method).toBe('POST');
        expect(req.request.body).toEqual({});
        req.flush(MOCK_PRODUCT);
    });
});

describe('ProductService recent', () => {
    it('should get recent products with default params', () => {
        const recentProducts = [MOCK_PRODUCT];

        service.getRecent().subscribe(result => {
            expect(result).toEqual(recentProducts);
        });

        const req = httpMock.expectOne(r => r.url === `${BASE_URL}/recent` && r.method === 'GET');
        expect(req.request.params.get('limit')).toBe(String(DEFAULT_RECENT_LIMIT));
        expect(req.request.params.get('includePublic')).toBe('true');
        req.flush(recentProducts);
    });

    it('should get recent products with custom params', () => {
        service.getRecent(RECENT_PRODUCTS_LIMIT, false).subscribe();

        const req = httpMock.expectOne(r => r.url === `${BASE_URL}/recent` && r.method === 'GET');
        expect(req.request.params.get('limit')).toBe(String(RECENT_PRODUCTS_LIMIT));
        expect(req.request.params.get('includePublic')).toBe('false');
        req.flush([]);
    });

    it('should return empty array on getRecent failure', () => {
        service.getRecent().subscribe(result => {
            expect(result).toEqual([]);
        });

        const req = httpMock.expectOne(r => r.url === `${BASE_URL}/recent` && r.method === 'GET');
        req.flush('Server Error', { status: HttpStatusCode.InternalServerError, statusText: 'Internal Server Error' });
    });
});

describe('ProductService defaults', () => {
    it('should use injected overview and suggestions limits by default', () => {
        service.queryOverview({ page: 1, limit: DEFAULT_PAGE_LIMIT }).subscribe(result => {
            expect(result.recentItems).toEqual([]);
        });

        const overviewReq = httpMock.expectOne(r => r.url === `${BASE_URL}/overview` && r.method === 'GET');
        expect(overviewReq.request.params.get('recentLimit')).toBe(String(DEFAULT_RECENT_LIMIT));
        expect(overviewReq.request.params.get('favoriteLimit')).toBe(String(DEFAULT_FAVORITE_LIMIT));
        overviewReq.flush({
            recentItems: [],
            favoriteItems: [],
            favoriteTotalCount: 0,
            allProducts: MOCK_PAGE,
        });

        service.searchSuggestions('chi').subscribe(result => {
            expect(result).toEqual([]);
        });

        const suggestionsReq = httpMock.expectOne(r => r.url === `${BASE_URL}/suggestions` && r.method === 'GET');
        expect(suggestionsReq.request.params.get('limit')).toBe(String(DEFAULT_SUGGESTIONS_LIMIT));
        suggestionsReq.flush([]);
    });

    it('should allow product API limits to be overridden through DI', () => {
        TestBed.resetTestingModule();
        TestBed.configureTestingModule({
            providers: [
                ProductService,
                provideHttpClient(),
                provideHttpClientTesting(),
                {
                    provide: PRODUCT_API_LIMITS,
                    useValue: {
                        recent: OVERRIDE_RECENT_LIMIT,
                        favorite: OVERRIDE_FAVORITE_LIMIT,
                        suggestions: OVERRIDE_SUGGESTIONS_LIMIT,
                    },
                },
            ],
        });
        service = TestBed.inject(ProductService);
        httpMock = TestBed.inject(HttpTestingController);

        service.queryOverview({ page: 1, limit: DEFAULT_PAGE_LIMIT }).subscribe();
        const overviewReq = httpMock.expectOne(r => r.url === `${BASE_URL}/overview` && r.method === 'GET');
        expect(overviewReq.request.params.get('recentLimit')).toBe(String(OVERRIDE_RECENT_LIMIT));
        expect(overviewReq.request.params.get('favoriteLimit')).toBe(String(OVERRIDE_FAVORITE_LIMIT));
        overviewReq.flush({
            recentItems: [],
            favoriteItems: [],
            favoriteTotalCount: 0,
            allProducts: MOCK_PAGE,
        });

        service.searchSuggestions('chi').subscribe();
        const suggestionsReq = httpMock.expectOne(r => r.url === `${BASE_URL}/suggestions` && r.method === 'GET');
        expect(suggestionsReq.request.params.get('limit')).toBe(String(OVERRIDE_SUGGESTIONS_LIMIT));
        suggestionsReq.flush([]);
    });
});

function createProductRequest(): CreateProductRequest {
    return {
        name: 'New Product',
        productType: ProductType.Other,
        baseUnit: MeasurementUnit.G,
        baseAmount: DEFAULT_NUTRITION_BASE_AMOUNT,
        defaultPortionAmount: DEFAULT_NUTRITION_BASE_AMOUNT,
        caloriesPerBase: NEW_PRODUCT_CALORIES,
        proteinsPerBase: NEW_PRODUCT_PROTEINS,
        fatsPerBase: NEW_PRODUCT_FATS,
        carbsPerBase: NEW_PRODUCT_CARBS,
        fiberPerBase: 1,
        alcoholPerBase: 0,
        visibility: ProductVisibility.Private,
    };
}

describe('Product mutation failures', () => {
    it.each(['create', 'update', 'delete', 'duplicate'] as const)('propagates %s failure to the caller', action => {
        const payload: CreateProductRequest = { ...MOCK_PRODUCT, productType: ProductType.Meat };
        const result: Observable<unknown> =
            action === 'create'
                ? service.create(payload)
                : action === 'update'
                  ? service.update('p1', payload)
                  : action === 'delete'
                    ? service.deleteById('p1')
                    : service.duplicate('p1');
        let received: unknown;
        let emitted = false;
        result.subscribe({
            next: () => {
                emitted = true;
            },
            error: (error: unknown) => {
                received = error;
            },
        });
        httpMock.expectOne(r => r.url.startsWith(BASE_URL)).flush({ message: 'rejected' }, { status: 409, statusText: 'Conflict' });
        expect(emitted).toBe(false);
        expect(received).toMatchObject({ status: 409, error: { message: 'rejected' } });
    });
    it('returns empty suggestions after a request failure', () => {
        let received: unknown;
        service.searchSuggestions('rice').subscribe(value => {
            received = value;
        });
        httpMock.expectOne(r => r.url === `${BASE_URL}/suggestions`).flush({}, { status: 503, statusText: 'Unavailable' });
        expect(received).toEqual([]);
    });
    it('retains requested pagination in the overview fallback', () => {
        let received: unknown;
        service.queryOverview({ page: 3, limit: 7, favoriteLimit: 0 }).subscribe(value => {
            received = value;
        });
        const req = httpMock.expectOne(r => r.url === `${BASE_URL}/overview`);
        expect(req.request.params.get('favoriteLimit')).toBe('0');
        req.flush({}, { status: 503, statusText: 'Unavailable' });
        expect(received).toEqual({
            recentItems: [],
            favoriteItems: [],
            favoriteTotalCount: 0,
            allProducts: { data: [], page: 3, limit: 7, totalPages: 0, totalItems: 0 },
        });
    });
});

describe('ProductService API measurement units', () => {
    it.each([
        ['G', MeasurementUnit.G],
        ['Ml', MeasurementUnit.ML],
        ['Pcs', MeasurementUnit.PCS],
    ])('normalizes %s when reopening a saved product', (wireUnit, expected) => {
        let received: Product | null = null;
        service.getById('p1').subscribe(value => {
            received = value;
        });
        httpMock.expectOne(`${BASE_URL}/p1`).flush({ ...MOCK_PRODUCT, baseUnit: wireUnit });
        expect(received).toEqual({ ...MOCK_PRODUCT, baseUnit: expected });
    });

    it('normalizes units in every overview section', () => {
        let received: unknown;
        service.queryOverview({ page: 1, limit: DEFAULT_PAGE_LIMIT }).subscribe(value => {
            received = value;
        });
        httpMock
            .expectOne(request => request.url === `${BASE_URL}/overview`)
            .flush({
                recentItems: [{ ...MOCK_PRODUCT, baseUnit: 'Ml' }],
                favoriteItems: [{ ...MOCK_PRODUCT, baseUnit: 'Pcs' }],
                favoriteTotalCount: 1,
                allProducts: { ...MOCK_PAGE, data: [{ ...MOCK_PRODUCT, baseUnit: 'Ml' }] },
            });
        expect(received).toMatchObject({
            recentItems: [{ baseUnit: MeasurementUnit.ML }],
            favoriteItems: [{ baseUnit: MeasurementUnit.PCS }],
            allProducts: { data: [{ baseUnit: MeasurementUnit.ML }] },
        });
    });

    it('normalizes search results and recent products', () => {
        let page: unknown;
        let recent: unknown;
        service.query(1, DEFAULT_PAGE_LIMIT).subscribe(value => {
            page = value;
        });
        httpMock
            .expectOne(request => request.url === `${BASE_URL}/`)
            .flush({
                ...MOCK_PAGE,
                data: [{ ...MOCK_PRODUCT, baseUnit: 'Pcs' }],
            });
        service.getRecent().subscribe(value => {
            recent = value;
        });
        httpMock.expectOne(request => request.url === `${BASE_URL}/recent`).flush([{ ...MOCK_PRODUCT, baseUnit: 'Ml' }]);
        expect(page).toMatchObject({ data: [{ baseUnit: MeasurementUnit.PCS }] });
        expect(recent).toMatchObject([{ baseUnit: MeasurementUnit.ML }]);
    });
});
