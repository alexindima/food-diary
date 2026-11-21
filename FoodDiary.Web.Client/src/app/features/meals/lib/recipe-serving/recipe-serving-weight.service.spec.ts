import { TestBed } from '@angular/core/testing';
import { defer, firstValueFrom, type Observable, of, Subject, throwError } from 'rxjs';
import { describe, expect, it, vi } from 'vitest';

import { RecipeLookupService } from '../../../../shared/api/recipe-lookup.service';
import { MeasurementUnit } from '../../../../shared/models/product.data';
import { type Recipe, RecipeVisibility } from '../../../../shared/models/recipe.data';
import { recipeIngredientFromStored } from '../../../../shared/models/recipe-ingredient';
import type { RecipeLookup } from '../../../../shared/models/recipe-lookup.data';
import { utcInstant } from '../../../../shared/models/semantics/date-value';
import { entityId } from '../../../../shared/models/semantics/entity-id';
import { recipeServingsFromStored } from '../../../../shared/models/semantics/meal-quantity';
import { displayRecipeServings, recipeServingsFromInput, servingMassFromObservation, UNKNOWN_SERVING_MASS } from './recipe-display-amount';
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
    it('provides a round-trip quantity basis from complete gram ingredient snapshots', async () => {
        setupService();
        const recipe = createRecipe();

        const mass = await firstValueFrom(service.loadServingMass(recipe));
        expect(mass).toEqual(servingMassFromObservation(SERVING_WEIGHT));

        expect(recipeLookupService.getById).not.toHaveBeenCalled();
        expect(displayRecipeServings(recipeServingsFromStored(SERVINGS), mass).value).toBe(TOTAL_WEIGHT);
        expect(recipeServingsFromInput(TOTAL_WEIGHT, mass)).toBe(SERVINGS);
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

    it('keeps an explicit servings display for a missing recipe id', async () => {
        setupService(createUnsupportedRecipeLookup());
        const recipe = createRecipe({ id: entityId<'recipe'>('') });

        const mass = await firstValueFrom(service.loadServingMass(recipe));
        expect(mass).toEqual(UNKNOWN_SERVING_MASS);
        expect(recipeLookupService.getById).not.toHaveBeenCalled();

        expect(displayRecipeServings(recipeServingsFromStored(SERVINGS), mass).value).toBe(SERVINGS);
        expect(recipeServingsFromInput(TOTAL_WEIGHT, mass)).toBe(TOTAL_WEIGHT);
    });

    it('retries a failed lookup on the next load and caches the recovered mass', async () => {
        setupService(null, true);
        const recipe = createRecipe({ steps: [] });

        expect(await firstValueFrom(service.loadServingMass(recipe))).toEqual(UNKNOWN_SERVING_MASS);
        recipeLookupService.getById.mockReturnValue(of(createRecipeLookup()));
        expect(await firstValueFrom(service.loadServingMass(recipe))).toEqual(servingMassFromObservation(SERVING_WEIGHT));
        expect(await firstValueFrom(service.loadServingMass(recipe))).toEqual(servingMassFromObservation(SERVING_WEIGHT));

        expect(recipeLookupService.getById).toHaveBeenCalledTimes(2);
    });

    it('caches a successful lookup when the ingredients have no gram equivalent', async () => {
        setupService(createUnsupportedRecipeLookup());
        const recipe = createRecipe({ steps: [] });

        expect(await firstValueFrom(service.loadServingMass(recipe))).toEqual(UNKNOWN_SERVING_MASS);
        expect(await firstValueFrom(service.loadServingMass(recipe))).toEqual(UNKNOWN_SERVING_MASS);
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
    it.each(['ML', 'Pcs', null])('keeps servings when a gram ingredient is mixed with %s without a mass conversion', async unit => {
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
        const mass = await firstValueFrom(service.loadServingMass(recipe));
        expect(mass).toEqual(UNKNOWN_SERVING_MASS);
        expect(displayRecipeServings(recipeServingsFromStored(SERVINGS), mass).value).toBe(SERVINGS);
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
        const mass = await firstValueFrom(service.loadServingMass(changed));
        expect(mass).toEqual(servingMassFromObservation(UPDATED_WEIGHT));
        expect(displayRecipeServings(recipeServingsFromStored(1), mass).value).toBe(UPDATED_WEIGHT);
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
        const mass = await firstValueFrom(service.loadServingMass(changed));
        expect(mass).toEqual(UNKNOWN_SERVING_MASS);
        expect(displayRecipeServings(recipeServingsFromStored(1), mass).value).toBe(1);
    });
});

describe('Recipe serving weight observation order', () => {
    it('retains the latest successful unknown mass and orders reads by subscription', async () => {
        setupService();
        const recipe = createRecipe({ steps: [] });
        const earlierRead = new Subject<RecipeLookup>();
        const laterRead = new Subject<RecipeLookup>();
        const startLookup = vi.fn<() => Observable<RecipeLookup>>().mockReturnValueOnce(earlierRead).mockReturnValueOnce(laterRead);
        recipeLookupService.getById.mockReturnValue(defer(startLookup));

        const queuedRead = service.loadServingMass(recipe);
        const earlier = firstValueFrom(service.loadServingMass(recipe));
        const later = firstValueFrom(queuedRead);
        laterRead.next(createUnsupportedRecipeLookup());
        earlierRead.next(createRecipeLookup());

        expect(await later).toEqual(UNKNOWN_SERVING_MASS);
        expect(await earlier).toEqual(servingMassFromObservation(SERVING_WEIGHT));
        expect(await firstValueFrom(service.loadServingMass(recipe))).toEqual(UNKNOWN_SERVING_MASS);
        expect(recipeLookupService.getById).toHaveBeenCalledTimes(2);
    });

    it('retains a newer recipe snapshot alongside a separately observed recipe', async () => {
        setupService();
        const oldRecipe = createRecipe({ steps: [] });
        const revisedRecipe = { ...oldRecipe, servings: 1 };
        const otherRecipe = { ...oldRecipe, id: entityId<'recipe'>('other-recipe') };
        const olderRead = new Subject<RecipeLookup>();
        const revisedRead = new Subject<RecipeLookup>();
        const otherRead = new Subject<RecipeLookup>();
        recipeLookupService.getById.mockReturnValueOnce(olderRead).mockReturnValueOnce(revisedRead).mockReturnValueOnce(otherRead);

        const older = firstValueFrom(service.loadServingMass(oldRecipe));
        const revised = firstValueFrom(service.loadServingMass(revisedRecipe));
        const other = firstValueFrom(service.loadServingMass(otherRecipe));
        revisedRead.next({ ...createRecipeLookup(), servings: revisedRecipe.servings });
        olderRead.next(createRecipeLookup());
        otherRead.next({ ...createRecipeLookup(), id: otherRecipe.id });

        expect(await older).toEqual(servingMassFromObservation(SERVING_WEIGHT));
        expect(await revised).toEqual(servingMassFromObservation(TOTAL_WEIGHT));
        expect(await other).toEqual(servingMassFromObservation(SERVING_WEIGHT));
        expect(await firstValueFrom(service.loadServingMass(revisedRecipe))).toEqual(servingMassFromObservation(TOTAL_WEIGHT));
        expect(await firstValueFrom(service.loadServingMass(otherRecipe))).toEqual(servingMassFromObservation(SERVING_WEIGHT));
        expect(recipeLookupService.getById.mock.calls).toEqual([[oldRecipe.id], [revisedRecipe.id], [otherRecipe.id]]);
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
