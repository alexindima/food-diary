import { HttpStatusCode, provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { TestBed } from '@angular/core/testing';
import { firstValueFrom } from 'rxjs';
import { afterEach, beforeEach, describe, expect, it, vi } from 'vitest';

import { environment } from '../../../environments/environment';
import type { RecipeLookup } from '../models/recipe-lookup.data';
import { entityId } from '../models/semantics/entity-id';
import { RecipeLookupService } from './recipe-lookup.service';
import type { RecipeHttpResponse } from './sdk/generated/model/recipe-http-response';

const SERVINGS = 4;
const PUBLIC_RECIPE_ID = entityId<'recipe'>('3c49eb8c-a30e-4c07-955e-b576e44ba08d');
const PUBLIC_RECIPE_RESPONSE = {
    id: PUBLIC_RECIPE_ID,
    name: 'Roasted vegetables with rice',
    servings: SERVINGS,
    visibility: 'Public',
    isOwnedByCurrentUser: false,
    steps: [
        {
            stepNumber: 1,
            instruction: 'Combine the vegetables and prepared rice.',
            ingredients: [
                { amount: 200, productId: 'vegetable-mix', productBaseUnit: 'g' },
                { amount: 2, nestedRecipeId: 'prepared-rice', nestedRecipeServings: 4, productBaseUnit: null },
            ],
        },
    ],
} satisfies RecipeHttpResponse;

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

describe('RecipeLookupService', () => {
    it('should be created', () => {
        expect(service).toBeTruthy();
    });

    it('should get recipe by id', () => {
        const recipeId = 'recipe-123';

        const mockResponse: RecipeLookup = {
            id: entityId<'recipe'>(recipeId),
            servings: SERVINGS,
            steps: [
                {
                    ingredients: [{ amount: 200, productBaseUnit: 'g' }],
                },
            ],
        };

        service.getById(entityId<'recipe'>(recipeId)).subscribe(response => {
            expect(response.id).toBe(recipeId);
            expect(response.servings).toBe(SERVINGS);
            expect(response.steps.length).toBe(1);
        });

        const req = httpMock.expectOne(r => r.url === `${baseUrl}/${recipeId}`);
        expect(req.request.method).toBe('GET');
        req.flush(mockResponse);
    });

    it('should include includePublic param when provided', () => {
        const recipeId = 'recipe-456';

        const mockResponse: RecipeLookup = {
            id: entityId<'recipe'>(recipeId),
            servings: 2,
            steps: [],
        };

        service.getById(entityId<'recipe'>(recipeId), false).subscribe(response => {
            expect(response.id).toBe(recipeId);
        });

        const req = httpMock.expectOne(r => r.url === `${baseUrl}/${recipeId}` && r.params.get('includePublic') === 'false');
        expect(req.request.method).toBe('GET');
        expect(req.request.params.get('includePublic')).toBe('false');
        req.flush(mockResponse);
    });

    it('keeps owner-only access on failure and returns a public recipe without inferring units for its nested recipe', async () => {
        const path = `${baseUrl}/${PUBLIC_RECIPE_ID}`;
        const logError = vi.spyOn(console, 'error').mockImplementation(() => {});
        try {
            const ownerOnly = expect(firstValueFrom(service.getById(PUBLIC_RECIPE_ID, false))).rejects.toMatchObject({
                status: HttpStatusCode.NotFound,
                error: { error: 'Recipe.NotFound' },
            });
            const privateRequest = httpMock.expectOne(request => request.url === path && request.method === 'GET');
            expect(privateRequest.request.params.get('includePublic')).toBe('false');
            privateRequest.flush({ error: 'Recipe.NotFound' }, { status: HttpStatusCode.NotFound, statusText: 'Not Found' });
            await ownerOnly;
            httpMock.expectNone(request => request.url === path);
        } finally {
            logError.mockRestore();
        }

        const publicRecipe = firstValueFrom(service.getById(PUBLIC_RECIPE_ID, true));
        const publicRequest = httpMock.expectOne(request => request.url === path && request.method === 'GET');
        expect(publicRequest.request.params.get('includePublic')).toBe('true');
        publicRequest.flush(PUBLIC_RECIPE_RESPONSE);

        await expect(publicRecipe).resolves.toEqual({
            id: PUBLIC_RECIPE_ID,
            servings: SERVINGS,
            steps: [
                {
                    ingredients: [
                        { amount: 200, productBaseUnit: 'g' },
                        { amount: 2, productBaseUnit: null },
                    ],
                },
            ],
        });
    });
});
