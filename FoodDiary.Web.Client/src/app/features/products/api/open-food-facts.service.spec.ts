import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { TestBed } from '@angular/core/testing';
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
