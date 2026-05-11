import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { TestBed } from '@angular/core/testing';
import { afterEach, beforeEach, describe, expect, it } from 'vitest';

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

const BASE_AMOUNT_GRAMS = 100;
const CHICKEN_CALORIES = 165;
const CHICKEN_PROTEINS = 31;
const CHICKEN_FATS = 3.6;
const QUALITY_SCORE_GREEN = 80;
const DEFAULT_PAGE_LIMIT = 10;
const FILTERED_PAGE_LIMIT = 20;
const RECENT_PRODUCTS_LIMIT = 5;
const NEW_PRODUCT_CALORIES = 100;
const NEW_PRODUCT_PROTEINS = 10;
const NEW_PRODUCT_FATS = 2;
const NEW_PRODUCT_CARBS = 12;
const HTTP_INTERNAL_SERVER_ERROR = 500;
const HTTP_NOT_FOUND = 404;

describe('ProductService', () => {
    let service: ProductService;
    let httpMock: HttpTestingController;
    const baseUrl = 'http://localhost:5300/api/v1/products';

    const mockProduct: Product = {
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
        baseAmount: BASE_AMOUNT_GRAMS,
        defaultPortionAmount: BASE_AMOUNT_GRAMS,
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

    const mockPage: PageOf<Product> = {
        data: [mockProduct],
        page: 1,
        limit: DEFAULT_PAGE_LIMIT,
        totalPages: 1,
        totalItems: 1,
    };

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

    it('should be created', () => {
        expect(service).toBeTruthy();
    });

    it('should query products with pagination params', () => {
        service.query(1, DEFAULT_PAGE_LIMIT).subscribe(result => {
            expect(result).toEqual(mockPage);
        });

        const req = httpMock.expectOne(r => r.url === `${baseUrl}/` && r.method === 'GET');
        expect(req.request.params.get('page')).toBe('1');
        expect(req.request.params.get('limit')).toBe(String(DEFAULT_PAGE_LIMIT));
        expect(req.request.params.get('includePublic')).toBe('true');
        req.flush(mockPage);
    });

    it('should include filters in query params', () => {
        const filters = { search: 'chicken', productTypes: [ProductType.Meat, ProductType.Dairy] };

        service.query(1, FILTERED_PAGE_LIMIT, filters, false).subscribe(result => {
            expect(result.data.length).toBe(1);
        });

        const req = httpMock.expectOne(r => r.url === `${baseUrl}/` && r.method === 'GET');
        expect(req.request.params.get('search')).toBe('chicken');
        expect(req.request.params.get('productTypes')).toBe('Meat,Dairy');
        expect(req.request.params.get('includePublic')).toBe('false');
        req.flush(mockPage);
    });

    it('should not include empty search filter', () => {
        const filters = { search: '  ' };

        service.query(1, DEFAULT_PAGE_LIMIT, filters).subscribe();

        const req = httpMock.expectOne(r => r.url === `${baseUrl}/` && r.method === 'GET');
        expect(req.request.params.has('search')).toBe(false);
        req.flush(mockPage);
    });

    it('should get product by id', () => {
        service.getById('p1').subscribe(result => {
            expect(result).toEqual(mockProduct);
        });

        const req = httpMock.expectOne(`${baseUrl}/p1`);
        expect(req.request.method).toBe('GET');
        req.flush(mockProduct);
    });

    it('should create product', () => {
        const createData: CreateProductRequest = {
            name: 'New Product',
            productType: ProductType.Other,
            baseUnit: MeasurementUnit.G,
            baseAmount: BASE_AMOUNT_GRAMS,
            defaultPortionAmount: BASE_AMOUNT_GRAMS,
            caloriesPerBase: NEW_PRODUCT_CALORIES,
            proteinsPerBase: NEW_PRODUCT_PROTEINS,
            fatsPerBase: NEW_PRODUCT_FATS,
            carbsPerBase: NEW_PRODUCT_CARBS,
            fiberPerBase: 1,
            alcoholPerBase: 0,
            visibility: ProductVisibility.Private,
        };

        service.create(createData).subscribe(result => {
            expect(result).toEqual(mockProduct);
        });

        const req = httpMock.expectOne(`${baseUrl}/`);
        expect(req.request.method).toBe('POST');
        expect(req.request.body).toEqual(createData);
        req.flush(mockProduct);
    });

    it('should update product via PATCH', () => {
        const updateData: UpdateProductRequest = { name: 'Updated Product' };

        service.update('p1', updateData).subscribe(result => {
            expect(result).toEqual(mockProduct);
        });

        const req = httpMock.expectOne(`${baseUrl}/p1`);
        expect(req.request.method).toBe('PATCH');
        expect(req.request.body).toEqual(updateData);
        req.flush(mockProduct);
    });

    it('should delete product by id', () => {
        service.deleteById('p1').subscribe();

        const req = httpMock.expectOne(`${baseUrl}/p1`);
        expect(req.request.method).toBe('DELETE');
        req.flush(null);
    });

    it('should duplicate product', () => {
        service.duplicate('p1').subscribe(result => {
            expect(result).toEqual(mockProduct);
        });

        const req = httpMock.expectOne(`${baseUrl}/p1/duplicate`);
        expect(req.request.method).toBe('POST');
        expect(req.request.body).toEqual({});
        req.flush(mockProduct);
    });

    it('should get recent products with default params', () => {
        const recentProducts = [mockProduct];

        service.getRecent().subscribe(result => {
            expect(result).toEqual(recentProducts);
        });

        const req = httpMock.expectOne(r => r.url === `${baseUrl}/recent` && r.method === 'GET');
        expect(req.request.params.get('limit')).toBe(String(DEFAULT_PAGE_LIMIT));
        expect(req.request.params.get('includePublic')).toBe('true');
        req.flush(recentProducts);
    });

    it('should get recent products with custom params', () => {
        service.getRecent(RECENT_PRODUCTS_LIMIT, false).subscribe();

        const req = httpMock.expectOne(r => r.url === `${baseUrl}/recent` && r.method === 'GET');
        expect(req.request.params.get('limit')).toBe(String(RECENT_PRODUCTS_LIMIT));
        expect(req.request.params.get('includePublic')).toBe('false');
        req.flush([]);
    });

    it('should return empty PageOf on query failure', () => {
        service.query(1, DEFAULT_PAGE_LIMIT).subscribe(result => {
            expect(result).toEqual({ data: [], page: 1, limit: DEFAULT_PAGE_LIMIT, totalPages: 0, totalItems: 0 });
        });

        const req = httpMock.expectOne(r => r.url === `${baseUrl}/` && r.method === 'GET');
        req.flush('Server Error', { status: HTTP_INTERNAL_SERVER_ERROR, statusText: 'Internal Server Error' });
    });

    it('should return null on getById failure', () => {
        service.getById('p1').subscribe(result => {
            expect(result).toBeNull();
        });

        const req = httpMock.expectOne(`${baseUrl}/p1`);
        req.flush('Not Found', { status: HTTP_NOT_FOUND, statusText: 'Not Found' });
    });

    it('should return empty array on getRecent failure', () => {
        service.getRecent().subscribe(result => {
            expect(result).toEqual([]);
        });

        const req = httpMock.expectOne(r => r.url === `${baseUrl}/recent` && r.method === 'GET');
        req.flush('Server Error', { status: HTTP_INTERNAL_SERVER_ERROR, statusText: 'Internal Server Error' });
    });
});
