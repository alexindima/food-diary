import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { TestBed } from '@angular/core/testing';
import { afterEach, beforeEach, describe, expect, it } from 'vitest';

import { environment } from '../../../environments/environment';
import { RecipeLookup } from '../models/recipe-lookup.data';
import { RecipeLookupService } from './recipe-lookup.service';

describe('RecipeLookupService', () => {
    let service: RecipeLookupService;
    let httpMock: HttpTestingController;

    const baseUrl = environment.apiUrls.recipes;

    beforeEach(() => {
        TestBed.configureTestingModule({
            providers: [RecipeLookupService, provideHttpClient(), provideHttpClientTesting()],
        });

        service = TestBed.inject(RecipeLookupService);
        httpMock = TestBed.inject(HttpTestingController);
    });

    afterEach(() => {
        httpMock.verify();
    });

    it('should be created', () => {
        expect(service).toBeTruthy();
    });

    it('should get recipe by id', () => {
        const recipeId = 'recipe-123';

        const mockResponse: RecipeLookup = {
            id: recipeId,
            servings: 4,
            steps: [
                {
                    ingredients: [{ amount: 200, productBaseUnit: 'g' }],
                },
            ],
        };

        service.getById(recipeId).subscribe(response => {
            expect(response.id).toBe(recipeId);
            expect(response.servings).toBe(4);
            expect(response.steps.length).toBe(1);
        });

        const req = httpMock.expectOne(r => r.url === `${baseUrl}/${recipeId}`);
        expect(req.request.method).toBe('GET');
        req.flush(mockResponse);
    });

    it('should include includePublic param when provided', () => {
        const recipeId = 'recipe-456';

        const mockResponse: RecipeLookup = {
            id: recipeId,
            servings: 2,
            steps: [],
        };

        service.getById(recipeId, false).subscribe(response => {
            expect(response.id).toBe(recipeId);
        });

        const req = httpMock.expectOne(r => r.url === `${baseUrl}/${recipeId}` && r.params.get('includePublic') === 'false');
        expect(req.request.method).toBe('GET');
        expect(req.request.params.get('includePublic')).toBe('false');
        req.flush(mockResponse);
    });
});
