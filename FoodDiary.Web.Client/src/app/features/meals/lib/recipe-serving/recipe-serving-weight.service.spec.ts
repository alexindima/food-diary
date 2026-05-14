import { TestBed } from '@angular/core/testing';
import { of, throwError } from 'rxjs';
import { describe, expect, it, vi } from 'vitest';

import { RecipeLookupService } from '../../../../shared/api/recipe-lookup.service';
import type { RecipeLookup } from '../../../../shared/models/recipe-lookup.data';
import { MeasurementUnit } from '../../../products/models/product.data';
import { type Recipe, RecipeVisibility } from '../../../recipes/models/recipe.data';
import { RecipeServingWeightService } from './recipe-serving-weight.service';

const SERVINGS = 2;
const FIRST_AMOUNT = 100;
const SECOND_AMOUNT = 200;
const TOTAL_WEIGHT = 300;
const SERVING_WEIGHT = 150;

let service: RecipeServingWeightService;
let recipeLookupService: { getById: ReturnType<typeof vi.fn> };

describe('RecipeServingWeightService', () => {
    it('should compute serving weight from recipe steps and cache conversions', () => {
        setupService();
        const recipe = createRecipe();

        service.loadServingWeight(recipe).subscribe(result => {
            expect(result).toBe(SERVING_WEIGHT);
        });

        expect(recipeLookupService.getById).not.toHaveBeenCalled();
        expect(service.convertServingsToGrams(recipe, SERVINGS)).toBe(TOTAL_WEIGHT);
        expect(service.convertGramsToServings(recipe, TOTAL_WEIGHT)).toBe(SERVINGS);
    });

    it('should load full recipe when list recipe has no ingredient weights', () => {
        setupService(createRecipeLookup());
        const recipe = createRecipe({ steps: [] });

        service.loadServingWeight(recipe).subscribe(result => {
            expect(result).toBe(SERVING_WEIGHT);
        });

        expect(recipeLookupService.getById).toHaveBeenCalledWith('recipe-1');
    });

    it('should cache lookup result and avoid repeated requests', () => {
        setupService(createRecipeLookup());
        const recipe = createRecipe({ steps: [] });

        service.loadServingWeight(recipe).subscribe();
        service.loadServingWeight(recipe).subscribe(result => {
            expect(result).toBe(SERVING_WEIGHT);
        });

        expect(recipeLookupService.getById).toHaveBeenCalledTimes(1);
    });

    it('should return null for missing recipe id and keep conversions unchanged', () => {
        setupService(createUnsupportedRecipeLookup());
        const recipe = createRecipe({ id: '' });

        service.loadServingWeight(recipe).subscribe(result => {
            expect(result).toBeNull();
        });

        expect(service.convertServingsToGrams(recipe, SERVINGS)).toBe(SERVINGS);
        expect(service.convertGramsToServings(recipe, TOTAL_WEIGHT)).toBe(TOTAL_WEIGHT);
    });

    it('should cache null when lookup fails', () => {
        setupService(null, true);
        const recipe = createRecipe({ steps: [] });

        service.loadServingWeight(recipe).subscribe(result => {
            expect(result).toBeNull();
        });
        service.loadServingWeight(recipe).subscribe(result => {
            expect(result).toBeNull();
        });

        expect(recipeLookupService.getById).toHaveBeenCalledTimes(1);
    });

    it('should ignore unsupported or non-positive ingredient amounts', () => {
        setupService(createUnsupportedRecipeLookup());
        const recipe = createRecipe({
            steps: [
                {
                    id: 'step-1',
                    stepNumber: 1,
                    instruction: '',
                    ingredients: [
                        { id: 'i1', amount: 0, productBaseUnit: MeasurementUnit.G },
                        { id: 'i2', amount: FIRST_AMOUNT, productBaseUnit: 'PCS' },
                    ],
                },
            ],
        });

        service.loadServingWeight(recipe).subscribe(result => {
            expect(result).toBeNull();
        });
    });
});

function setupService(lookup: RecipeLookup | null = null, shouldFail = false): void {
    recipeLookupService = {
        getById: vi.fn().mockReturnValue(shouldFail ? throwError(() => new Error('fail')) : of(lookup ?? createRecipeLookup())),
    };

    TestBed.resetTestingModule();
    TestBed.configureTestingModule({
        providers: [RecipeServingWeightService, { provide: RecipeLookupService, useValue: recipeLookupService }],
    });
    service = TestBed.inject(RecipeServingWeightService);
}

function createRecipe(overrides: Partial<Recipe> = {}): Recipe {
    return {
        id: 'recipe-1',
        name: 'Soup',
        servings: SERVINGS,
        visibility: RecipeVisibility.Private,
        usageCount: 0,
        createdAt: '2026-05-14T00:00:00Z',
        isOwnedByCurrentUser: true,
        isNutritionAutoCalculated: true,
        steps: [
            {
                id: 'step-1',
                stepNumber: 1,
                instruction: '',
                ingredients: [
                    { id: 'i1', amount: FIRST_AMOUNT, productBaseUnit: MeasurementUnit.G },
                    { id: 'i2', amount: SECOND_AMOUNT, productBaseUnit: MeasurementUnit.ML },
                ],
            },
        ],
        ...overrides,
    };
}

function createRecipeLookup(): RecipeLookup {
    return {
        id: 'recipe-1',
        servings: SERVINGS,
        steps: [
            {
                ingredients: [
                    { amount: FIRST_AMOUNT, productBaseUnit: 'G' },
                    { amount: SECOND_AMOUNT, productBaseUnit: 'ML' },
                ],
            },
        ],
    };
}

function createUnsupportedRecipeLookup(): RecipeLookup {
    return {
        id: 'recipe-1',
        servings: SERVINGS,
        steps: [
            {
                ingredients: [
                    { amount: 0, productBaseUnit: 'G' },
                    { amount: FIRST_AMOUNT, productBaseUnit: 'PCS' },
                ],
            },
        ],
    };
}
