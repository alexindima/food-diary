import { HttpStatusCode, provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { TestBed } from '@angular/core/testing';
import { afterEach, beforeEach, describe, expect, it } from 'vitest';

import type { FavoriteProduct } from '../models/product.data';
import { FavoriteProductService } from './favorite-product.service';

const BASE_URL = 'http://localhost:5300/api/v1/favorite-products';
const PRODUCT_CALORIES = 52;
const DEFAULT_PORTION_AMOUNT = 100;

let service: FavoriteProductService;
let httpMock: HttpTestingController;

beforeEach(() => {
    TestBed.configureTestingModule({
        providers: [FavoriteProductService, provideHttpClient(), provideHttpClientTesting()],
    });
    service = TestBed.inject(FavoriteProductService);
    httpMock = TestBed.inject(HttpTestingController);
});

afterEach(() => {
    httpMock.verify();
});

describe('FavoriteProductService', () => {
    it('should get all favorite products', () => {
        const favorites = [createFavoriteProduct()];

        service.getAll().subscribe(result => {
            expect(result).toEqual(favorites);
        });

        const req = httpMock.expectOne(`${BASE_URL}/`);
        expect(req.request.method).toBe('GET');
        req.flush(favorites);
    });

    it('should return an empty list when get all fails', () => {
        service.getAll().subscribe(result => {
            expect(result).toEqual([]);
        });

        const req = httpMock.expectOne(`${BASE_URL}/`);
        req.flush('Server Error', { status: HttpStatusCode.InternalServerError, statusText: 'Internal Server Error' });
    });

    it('should check favorite state', () => {
        service.isFavorite('product-1').subscribe(result => {
            expect(result).toBe(true);
        });

        const req = httpMock.expectOne(`${BASE_URL}/check/product-1`);
        expect(req.request.method).toBe('GET');
        req.flush(true);
    });

    it('should return false when favorite check fails', () => {
        service.isFavorite('product-1').subscribe(result => {
            expect(result).toBe(false);
        });

        const req = httpMock.expectOne(`${BASE_URL}/check/product-1`);
        req.flush('Server Error', { status: HttpStatusCode.InternalServerError, statusText: 'Internal Server Error' });
    });

    it('should add favorite product', () => {
        const favorite = createFavoriteProduct();

        service.add('product-1', 'Apple').subscribe(result => {
            expect(result).toEqual(favorite);
        });

        const req = httpMock.expectOne(`${BASE_URL}/`);
        expect(req.request.method).toBe('POST');
        expect(req.request.body).toEqual({ productId: 'product-1', name: 'Apple' });
        req.flush(favorite);
    });

    it('should remove favorite product', () => {
        service.remove('favorite-1').subscribe();

        const req = httpMock.expectOne(`${BASE_URL}/favorite-1`);
        expect(req.request.method).toBe('DELETE');
        req.flush(null);
    });
});

function createFavoriteProduct(): FavoriteProduct {
    return {
        id: 'favorite-1',
        productId: 'product-1',
        name: 'Apple',
        createdAtUtc: '2026-01-01T00:00:00Z',
        productName: 'Apple',
        brand: 'Garden',
        imageUrl: null,
        caloriesPerBase: PRODUCT_CALORIES,
        baseUnit: 'G',
        defaultPortionAmount: DEFAULT_PORTION_AMOUNT,
    };
}
