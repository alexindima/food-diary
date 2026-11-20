import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { TestBed } from '@angular/core/testing';
import { firstValueFrom } from 'rxjs';
import { afterEach, beforeEach, describe, expect, it, vi } from 'vitest';

import { environment } from '../../../../environments/environment';
import type { PublicRecipeHttpResponse } from '../../../shared/api/sdk/generated/model/public-recipe-http-response';
import { entityId } from '../../../shared/models/semantics/entity-id';
import { PublicRecipeService } from './public-recipe.service';

const publicRecipeId = entityId<'recipe'>('00000000-0000-4000-8000-000000000110');
const yogurtId = entityId<'product'>('00000000-0000-4000-8000-000000000220');
const dressingId = entityId<'recipe'>('00000000-0000-4000-8000-000000000330');
const unavailableIngredient = {
    isAvailable: false,
    productId: null,
    recipeId: null,
    name: null,
    amount: null,
    unit: null,
    amountText: null,
};

const categorizedRecipe: PublicRecipeHttpResponse = {
    id: publicRecipeId,
    name: 'Yogurt salad',
    category: 'salads',
    language: 'en',
    images: [],
    prepTime: 10,
    cookTime: null,
    servings: 2,
    totalCalories: null,
    missingIngredientCount: 1,
    steps: [
        {
            stepNumber: 1,
            instruction: 'Mix and serve.',
            images: [],
            ingredients: [
                { isAvailable: true, productId: yogurtId, name: 'Yogurt', amount: 180, unit: 'Gram' },
                { isAvailable: true, recipeId: dressingId, name: 'Dressing', amount: 1, unit: 'serving' },
                unavailableIngredient,
            ],
        },
    ],
};

describe('PublicRecipeService', () => {
    let service: PublicRecipeService;
    let http: HttpTestingController;
    beforeEach(() => {
        TestBed.configureTestingModule({ providers: [provideHttpClient(), provideHttpClientTesting()] });
        service = TestBed.inject(PublicRecipeService);
        http = TestBed.inject(HttpTestingController);
    });
    afterEach(() => {
        http.verify();
    });
    it('requests an explicitly public paginated catalog', () => {
        service.query({ page: 2, search: 'Soup', category: 'Dinner', maxTotalTime: 30, sortBy: 'fastest' }).subscribe();
        const request = http.expectOne(req => req.url === `${environment.apiUrls.recipes}/public`);
        expect(request.request.params.get('limit')).toBe('20');
        expect(request.request.params.get('page')).toBe('2');
        expect(request.request.params.get('sortBy')).toBe('fastest');
        expect(request.request.params.get('maxTotalTime')).toBe('30');
        expect(request.request.params.has('includePublic')).toBe(false);
        request.flush({ data: [], totalItems: 0, totalPages: 0, page: 2, limit: 20 });
    });
    it('propagates not-found instead of pretending a private recipe is public', () => {
        const error = vi.fn();
        service.getById(entityId<'recipe'>('private')).subscribe({ error });
        http.expectOne(`${environment.apiUrls.recipes}/public/private`).flush({}, { status: 404, statusText: 'Not Found' });
        expect(error).toHaveBeenCalled();
    });
    it('decodes categorized public details without inventing nutrition or identities for private ingredients', async () => {
        const result = firstValueFrom(service.getById(publicRecipeId));
        http.expectOne({ method: 'GET', url: `${environment.apiUrls.recipes}/public/${publicRecipeId}` }).flush(categorizedRecipe);

        await expect(result).resolves.toMatchObject({
            id: publicRecipeId,
            category: 'salads',
            prepTime: 10,
            cookTime: null,
            totalCalories: null,
            missingIngredientCount: 1,
            steps: [
                {
                    stepNumber: 1,
                    title: null,
                    ingredients: [
                        { productId: yogurtId, recipeId: null, amount: 180, unit: 'Gram', isAvailable: true },
                        { recipeId: dressingId, amount: 1, unit: 'serving', isAvailable: true },
                        unavailableIngredient,
                    ],
                },
            ],
        });
    });
    it('searches public categories in the selected recipe language', () => {
        service.getCategories('Soup', 'en').subscribe();
        const request = http.expectOne(req => req.url === `${environment.apiUrls.recipes}/public/categories`);
        expect(request.request.params.get('search')).toBe('Soup');
        expect(request.request.params.get('language')).toBe('en');
        request.flush(['Soups']);
    });
});
