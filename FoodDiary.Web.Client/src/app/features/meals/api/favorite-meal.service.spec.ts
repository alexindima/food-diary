import { HttpStatusCode, provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { TestBed } from '@angular/core/testing';
import { afterEach, beforeEach, describe, expect, it } from 'vitest';

import { getNumberProperty } from '../../../shared/lib/unknown-value.utils';
import type { FavoriteMeal } from '../models/meal.data';
import { FavoriteMealService } from './favorite-meal.service';

const BASE_URL = 'http://localhost:5300/api/v1/favorite-meals';

const favoriteMeal: FavoriteMeal = {
    id: 'favorite-1',
    mealId: 'meal-1',
    name: 'Lunch',
    createdAtUtc: '2026-05-14T10:00:00Z',
    mealDate: '2026-05-14T09:00:00Z',
    mealType: 'Lunch',
    totalCalories: 500,
    totalProteins: 30,
    totalFats: 20,
    totalCarbs: 50,
    itemCount: 2,
};

let service: FavoriteMealService;
let httpMock: HttpTestingController;

beforeEach(() => {
    TestBed.configureTestingModule({
        providers: [FavoriteMealService, provideHttpClient(), provideHttpClientTesting()],
    });
    service = TestBed.inject(FavoriteMealService);
    httpMock = TestBed.inject(HttpTestingController);
});

afterEach(() => {
    httpMock.verify();
});

describe('FavoriteMealService', () => {
    it('should get all favorite meals', () => {
        service.getAll().subscribe(result => {
            expect(result).toEqual([favoriteMeal]);
        });

        const req = httpMock.expectOne(`${BASE_URL}/`);
        expect(req.request.method).toBe('GET');
        req.flush([favoriteMeal]);
    });

    it('should return empty array when favorite list request fails', () => {
        service.getAll().subscribe(result => {
            expect(result).toEqual([]);
        });

        const req = httpMock.expectOne(`${BASE_URL}/`);
        req.flush('Server Error', { status: HttpStatusCode.InternalServerError, statusText: 'Internal Server Error' });
    });

    it('should check favorite state by meal id', () => {
        service.isFavorite('meal-1').subscribe(result => {
            expect(result).toBe(true);
        });

        const req = httpMock.expectOne(`${BASE_URL}/check/meal-1`);
        expect(req.request.method).toBe('GET');
        req.flush(true);
    });

    it('should return false when favorite check fails', () => {
        service.isFavorite('meal-1').subscribe(result => {
            expect(result).toBe(false);
        });

        const req = httpMock.expectOne(`${BASE_URL}/check/meal-1`);
        req.flush('Server Error', { status: HttpStatusCode.InternalServerError, statusText: 'Internal Server Error' });
    });

    it('should add favorite meal with optional name', () => {
        service.add('meal-1', 'Lunch').subscribe(result => {
            expect(result).toEqual(favoriteMeal);
        });

        const req = httpMock.expectOne(`${BASE_URL}/`);
        expect(req.request.method).toBe('POST');
        expect(req.request.body).toEqual({ mealId: 'meal-1', name: 'Lunch' });
        req.flush(favoriteMeal);
    });

    it('should rethrow add favorite errors', () => {
        service.add('meal-1').subscribe({
            next: () => {
                expect.fail('Expected add to fail');
            },
            error: (error: unknown) => {
                expect(getNumberProperty(error, 'status')).toBe(HttpStatusCode.InternalServerError);
            },
        });

        const req = httpMock.expectOne(`${BASE_URL}/`);
        req.flush('Server Error', { status: HttpStatusCode.InternalServerError, statusText: 'Internal Server Error' });
    });

    it('should remove favorite meal by favorite id', () => {
        service.remove('favorite-1').subscribe();

        const req = httpMock.expectOne(`${BASE_URL}/favorite-1`);
        expect(req.request.method).toBe('DELETE');
        req.flush(null);
    });

    it('should rethrow remove favorite errors', () => {
        service.remove('favorite-1').subscribe({
            next: () => {
                expect.fail('Expected remove to fail');
            },
            error: (error: unknown) => {
                expect(getNumberProperty(error, 'status')).toBe(HttpStatusCode.InternalServerError);
            },
        });

        const req = httpMock.expectOne(`${BASE_URL}/favorite-1`);
        req.flush('Server Error', { status: HttpStatusCode.InternalServerError, statusText: 'Internal Server Error' });
    });
});
