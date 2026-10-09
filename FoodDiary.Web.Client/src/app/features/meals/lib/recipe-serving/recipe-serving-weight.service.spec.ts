import { TestBed } from '@angular/core/testing';
import { firstValueFrom, of, throwError } from 'rxjs';
import { describe, expect, it, vi } from 'vitest';

import { RecipeLookupService } from '../../../../shared/api/recipe-lookup.service';
import { MeasurementUnit } from '../../../../shared/models/product.data';
import { type Recipe, RecipeVisibility } from '../../../../shared/models/recipe.data';
import { recipeIngredientFromStored } from '../../../../shared/models/recipe-ingredient';
import type { RecipeLookup } from '../../../../shared/models/recipe-lookup.data';
import { utcInstant } from '../../../../shared/models/semantics/date-value';
import { entityId } from '../../../../shared/models/semantics/entity-id';
import { recipeServingsFromStored } from '../../../../shared/models/semantics/meal-quantity';
import { recipeDisplayToServings, servingMassFromObservation, UNKNOWN_SERVING_MASS } from './recipe-display-amount';
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

        service.loadServingMass(recipe).subscribe(result => {
            expect(result).toEqual(servingMassFromObservation(SERVING_WEIGHT));
        });

        expect(recipeLookupService.getById).not.toHaveBeenCalled();
        expect(service.displayServings(recipe, recipeServingsFromStored(SERVINGS)).value).toBe(TOTAL_WEIGHT);
        expect(recipeDisplayToServings(service.displayAmountFromInput(recipe, TOTAL_WEIGHT))).toBe(SERVINGS);
    });

    it('should load full recipe when list recipe has no ingredient weights', () => {
        setupService(createRecipeLookup());
        const recipe = createRecipe({ steps: [] });

        service.loadServingMass(recipe).subscribe(result => {
            expect(result).toEqual(servingMassFromObservation(SERVING_WEIGHT));
        });

        expect(recipeLookupService.getById).toHaveBeenCalledWith('recipe-1');
    });

    it('should cache lookup result and avoid repeated requests', () => {
        setupService(createRecipeLookup());
        const recipe = createRecipe({ steps: [] });

        service.loadServingMass(recipe).subscribe();
        service.loadServingMass(recipe).subscribe(result => {
            expect(result).toEqual(servingMassFromObservation(SERVING_WEIGHT));
        });

        expect(recipeLookupService.getById).toHaveBeenCalledTimes(1);
    });

    it('keeps an explicit servings display for a missing recipe id', () => {
        setupService(createUnsupportedRecipeLookup());
        const recipe = createRecipe({ id: entityId<'recipe'>('') });

        service.loadServingMass(recipe).subscribe(result => {
            expect(result).toEqual(UNKNOWN_SERVING_MASS);
        });

        expect(service.displayServings(recipe, recipeServingsFromStored(SERVINGS)).value).toBe(SERVINGS);
        expect(recipeDisplayToServings(service.displayAmountFromInput(recipe, TOTAL_WEIGHT))).toBe(TOTAL_WEIGHT);
    });

    it('caches unknown mass when lookup fails', () => {
        setupService(null, true);
        const recipe = createRecipe({ steps: [] });

        service.loadServingMass(recipe).subscribe(result => {
            expect(result).toEqual(UNKNOWN_SERVING_MASS);
        });
        service.loadServingMass(recipe).subscribe(result => {
            expect(result).toEqual(UNKNOWN_SERVING_MASS);
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
                        recipeIngredientFromStored({ id: 'i1', amount: 0, productBaseUnit: MeasurementUnit.G }),
                        recipeIngredientFromStored({ id: 'i2', amount: FIRST_AMOUNT, productBaseUnit: 'PCS' }),
                    ],
                },
            ],
        });

        service.loadServingMass(recipe).subscribe(result => {
            expect(result).toEqual(UNKNOWN_SERVING_MASS);
        });
    });
});

function registerIncompleteMassTests(): void {
    it.each(['ML', 'Pcs', null])('keeps servings when a gram ingredient is mixed with %s without a mass conversion', unit => {
        setupService({
            id: entityId<'recipe'>('recipe-1'),
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
                        recipeIngredientFromStored({ id: 'gram-item', amount: FIRST_AMOUNT, productBaseUnit: 'G' }),
                        recipeIngredientFromStored({ id: 'unknown-item', amount: SECOND_AMOUNT, productBaseUnit: unit }),
                    ],
                },
            ],
        });
        service.loadServingMass(recipe).subscribe(weight => {
            expect(weight).toEqual(UNKNOWN_SERVING_MASS);
        });
        expect(service.hasServingWeight(recipe)).toBe(false);
        expect(service.displayServings(recipe, recipeServingsFromStored(SERVINGS)).value).toBe(SERVINGS);
    });
}

describe('Recipe serving weight after recipe changes', () => {
    it('refreshes the cached mass when ingredient amounts and serving count change', async () => {
        setupService();
        const original = createRecipe();
        service.loadServingMass(original).subscribe();
        const changed = createRecipe({
            servings: 1,
            steps: [
                {
                    ...original.steps[0],
                    ingredients: [recipeIngredientFromStored({ ...original.steps[0].ingredients[0], amount: UPDATED_WEIGHT })],
                },
            ],
        });
        expect(await firstValueFrom(service.loadServingMass(changed))).toEqual(servingMassFromObservation(UPDATED_WEIGHT));
        expect(service.displayServings(changed, recipeServingsFromStored(1)).value).toBe(UPDATED_WEIGHT);
    });

    it('stops offering grams when a cached gram recipe gains an ingredient without known mass', async () => {
        setupService(createUnsupportedRecipeLookup());
        service.loadServingMass(createRecipe()).subscribe();
        const changed = createRecipe({
            steps: [
                {
                    id: 'changed',
                    stepNumber: 1,
                    instruction: '',
                    ingredients: [recipeIngredientFromStored({ id: 'pcs', amount: 1, productBaseUnit: 'Pcs' })],
                },
            ],
        });
        expect(await firstValueFrom(service.loadServingMass(changed))).toEqual(UNKNOWN_SERVING_MASS);
        expect(service.hasServingWeight(changed)).toBe(false);
        expect(service.displayServings(changed, recipeServingsFromStored(1)).value).toBe(1);
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
        id: entityId<'recipe'>('recipe-1'),
        name: 'Soup',
        servings: SERVINGS,
        visibility: RecipeVisibility.Private,
        usageCount: 0,
        createdAt: utcInstant('2026-05-14T00:00:00Z'),
        isOwnedByCurrentUser: true,
        isNutritionAutoCalculated: true,
        steps: [
            {
                id: 'step-1',
                stepNumber: 1,
                instruction: '',
                ingredients: [
                    recipeIngredientFromStored({ id: 'i1', amount: FIRST_AMOUNT, productBaseUnit: MeasurementUnit.G }),
                    recipeIngredientFromStored({ id: 'i2', amount: SECOND_AMOUNT, productBaseUnit: MeasurementUnit.G }),
                ],
            },
        ],
        ...overrides,
    };
}

function createRecipeLookup(): RecipeLookup {
    return {
        id: entityId<'recipe'>('recipe-1'),
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
        id: entityId<'recipe'>('recipe-1'),
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
