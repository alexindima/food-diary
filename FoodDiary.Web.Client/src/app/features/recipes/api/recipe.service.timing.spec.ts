import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { TestBed } from '@angular/core/testing';
import { afterEach, beforeEach, describe, expect, it } from 'vitest';

import { type RecipeDto, RecipeVisibility } from '../../../shared/models/recipe.data';
import { entityId } from '../../../shared/models/semantics/entity-id';
import { RecipeService } from './recipe.service';

const KNOWN_PREPARATION_MINUTES = 12;
const KNOWN_COOKING_MINUTES = 15;

describe('RecipeService timing update intent', () => {
    let service: RecipeService;
    let http: HttpTestingController;

    beforeEach(() => {
        TestBed.configureTestingModule({ providers: [RecipeService, provideHttpClient(), provideHttpClientTesting()] });
        service = TestBed.inject(RecipeService);
        http = TestBed.inject(HttpTestingController);
    });

    afterEach(() => {
        http.verify();
    });

    it.each([
        [null, null, true, true],
        [0, KNOWN_COOKING_MINUTES, false, false],
        [undefined, undefined, false, false],
        [KNOWN_PREPARATION_MINUTES, null, false, true],
    ])('sends distinct clearing intent for prep %s and cook %s', (prepTime, cookTime, clearPrepTime, clearCookTime) => {
        const body: RecipeDto = {
            name: 'Own timing recipe',
            servings: 2,
            visibility: RecipeVisibility.Private,
            calculateNutritionAutomatically: true,
            prepTime,
            cookTime,
            steps: [],
        };

        service.update(entityId<'recipe'>('recipe-id'), body).subscribe();

        const request = http.expectOne(r => r.method === 'PATCH' && r.url.endsWith('/recipes/recipe-id'));
        expect(request.request.body).toMatchObject({ ...body, clearPrepTime, clearCookTime });
        request.flush({
            id: 'recipe-id',
            name: body.name,
            servings: body.servings,
            visibility: body.visibility,
            usageCount: 0,
            createdAt: '2026-10-01T00:00:00Z',
            isOwnedByCurrentUser: true,
            isNutritionAutoCalculated: true,
            steps: [],
        });
    });
});
