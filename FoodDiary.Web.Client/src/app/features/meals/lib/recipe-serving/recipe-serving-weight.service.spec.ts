import { TestBed } from '@angular/core/testing';
import { firstValueFrom, of, throwError } from 'rxjs';
import { describe, expect, it, vi } from 'vitest';

import { RecipeLookupService } from '../../../../shared/api/recipe-lookup.service';
import { MeasurementUnit } from '../../../../shared/models/product.data';
import { type Recipe, RecipeVisibility } from '../../../../shared/models/recipe.data';
import type { RecipeLookup } from '../../../../shared/models/recipe-lookup.data';
import { RecipeServingWeightService } from './recipe-serving-weight.service';

const SERVINGS = 2;
const FIRST_AMOUNT = 100;
const SECOND_AMOUNT = 200;
const TOTAL_WEIGHT = 300;
const SERVING_WEIGHT = 150;
const UPDATED_WEIGHT = 400;

let service: RecipeServingWeightService;
let recipeLookupService: { getById: ReturnType<typeof vi.fn> };

describe('RecipeServingWeightService', () => {
    registerIncompleteMassTests();
    it('should compute serving weight from complete gram ingredient snapshots and cache conversions', () => {
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

function registerIncompleteMassTests(): void {
    it.each(['ML', 'Pcs', null])('keeps servings when a gram ingredient is mixed with %s without a mass conversion', unit => {
        setupService({
            id: 'recipe-1',
            servings: SERVINGS,
            steps: [
                {
                    ingredients: [
                        { amount: FIRST_AMOUNT, productBaseUnit: 'G' },
                        { amount: SECOND_AMOUNT, productBaseUnit: unit },
                    ],
                },
            ],
        });
        const recipe = createRecipe({
            steps: [
                {
                    id: 'mixed-step',
                    stepNumber: 1,
                    instruction: '',
                    ingredients: [
                        { id: 'gram-item', amount: FIRST_AMOUNT, productBaseUnit: 'G' },
                        { id: 'unknown-item', amount: SECOND_AMOUNT, productBaseUnit: unit },
                    ],
                },
            ],
        });
        service.loadServingWeight(recipe).subscribe(weight => {
            expect(weight).toBeNull();
        });
        expect(service.hasServingWeight(recipe)).toBe(false);
        expect(service.convertServingsToGrams(recipe, SERVINGS)).toBe(SERVINGS);
    });
}

describe('Recipe serving weight after recipe changes', () => {
    it('refreshes the cached mass when ingredient amounts and serving count change', async () => {
        setupService();
        const original = createRecipe();
        service.loadServingWeight(original).subscribe();
        const changed = createRecipe({
            servings: 1,
            steps: [{ ...original.steps[0], ingredients: [{ ...original.steps[0].ingredients[0], amount: UPDATED_WEIGHT }] }],
        });
        expect(await firstValueFrom(service.loadServingWeight(changed))).toBe(UPDATED_WEIGHT);
        expect(service.convertServingsToGrams(changed, 1)).toBe(UPDATED_WEIGHT);
    });

    it('stops offering grams when a cached gram recipe gains an ingredient without known mass', async () => {
        setupService(createUnsupportedRecipeLookup());
        service.loadServingWeight(createRecipe()).subscribe();
        const changed = createRecipe({
            steps: [{ id: 'changed', stepNumber: 1, instruction: '', ingredients: [{ id: 'pcs', amount: 1, productBaseUnit: 'Pcs' }] }],
        });
        expect(await firstValueFrom(service.loadServingWeight(changed))).toBeNull();
        expect(service.hasServingWeight(changed)).toBe(false);
        expect(service.convertServingsToGrams(changed, 1)).toBe(1);
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
                    { id: 'i2', amount: SECOND_AMOUNT, productBaseUnit: MeasurementUnit.G },
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
                    { amount: SECOND_AMOUNT, productBaseUnit: 'G' },
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
