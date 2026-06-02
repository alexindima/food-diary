import { TestBed } from '@angular/core/testing';
import { of } from 'rxjs';
import { describe, expect, it, vi } from 'vitest';

import { RecipeService } from '../api/recipe.service';
import type { RecipeFilters } from '../models/recipe.data';
import { RecipeSelectFacade } from './recipe-select.facade';

const FIRST_PAGE = 1;
const FIRST_LIMIT = 10;
const SECOND_PAGE = 2;
const SECOND_LIMIT = 5;

describe('RecipeSelectFacade', () => {
    it('delegates query to recipe service with includePublic default', () => {
        const page = { items: [], page: FIRST_PAGE, pageSize: FIRST_LIMIT, totalCount: 0 };
        const recipeService = { query: vi.fn().mockReturnValue(of(page)) };
        TestBed.configureTestingModule({
            providers: [RecipeSelectFacade, { provide: RecipeService, useValue: recipeService }],
        });

        const facade = TestBed.inject(RecipeSelectFacade);
        const filters: RecipeFilters = { search: 'eggs' };

        facade.query(FIRST_PAGE, FIRST_LIMIT, filters).subscribe(result => {
            expect(result).toBe(page);
        });

        expect(recipeService.query).toHaveBeenCalledWith(FIRST_PAGE, FIRST_LIMIT, filters, true);
    });

    it('passes explicit includePublic flag through to recipe service', () => {
        const page = { items: [], page: SECOND_PAGE, pageSize: SECOND_LIMIT, totalCount: 0 };
        const recipeService = { query: vi.fn().mockReturnValue(of(page)) };
        TestBed.configureTestingModule({
            providers: [RecipeSelectFacade, { provide: RecipeService, useValue: recipeService }],
        });

        const facade = TestBed.inject(RecipeSelectFacade);

        facade.query(SECOND_PAGE, SECOND_LIMIT, undefined, false).subscribe(result => {
            expect(result).toBe(page);
        });

        expect(recipeService.query).toHaveBeenCalledWith(SECOND_PAGE, SECOND_LIMIT, undefined, false);
    });
});
