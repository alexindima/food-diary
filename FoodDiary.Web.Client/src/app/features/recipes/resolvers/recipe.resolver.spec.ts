import { beforeEach, describe, expect, it, vi } from 'vitest';
import { TestBed } from '@angular/core/testing';
import { ActivatedRouteSnapshot, RouterStateSnapshot } from '@angular/router';
import { Observable, of, throwError } from 'rxjs';
import { recipeResolver } from './recipe.resolver';
import { RecipeService } from '../api/recipe.service';
import { NavigationService } from '../../../services/navigation.service';
import { Recipe } from '../models/recipe.data';

describe('recipeResolver', () => {
    let recipeServiceSpy: { getById: ReturnType<typeof vi.fn> };
    let navSpy: { navigateToRecipeList: ReturnType<typeof vi.fn> };

    const mockRecipe: Partial<Recipe> = { id: 'recipe-1' };

    const mockState = {} as unknown as RouterStateSnapshot;

    beforeEach(() => {
        recipeServiceSpy = { getById: vi.fn() };
        navSpy = { navigateToRecipeList: vi.fn().mockResolvedValue(undefined) };

        TestBed.configureTestingModule({
            providers: [
                { provide: RecipeService, useValue: recipeServiceSpy },
                { provide: NavigationService, useValue: navSpy },
            ],
        });
    });

    it('should resolve recipe by id', () => {
        recipeServiceSpy.getById.mockReturnValue(of(mockRecipe));

        let resolved: Recipe | null = null;

        const mockRoute = {
            paramMap: { get: vi.fn().mockReturnValue('recipe-1') },
        } as unknown as ActivatedRouteSnapshot;

        TestBed.runInInjectionContext(() => {
            const result$ = recipeResolver(mockRoute, mockState) as Observable<Recipe | null>;
            result$.subscribe(result => {
                resolved = result;
            });
        });

        expect(resolved).toEqual(mockRecipe);
        expect(recipeServiceSpy.getById).toHaveBeenCalledWith('recipe-1', false);
    });

    it('should navigate to recipe list when id is missing', () => {
        const mockRoute = {
            paramMap: { get: vi.fn().mockReturnValue(null) },
        } as unknown as ActivatedRouteSnapshot;

        let resolved: Recipe | null | undefined;

        TestBed.runInInjectionContext(() => {
            const result$ = recipeResolver(mockRoute, mockState) as Observable<Recipe | null>;
            result$.subscribe(result => {
                resolved = result;
            });
        });

        expect(resolved).toBeNull();
        expect(navSpy.navigateToRecipeList).toHaveBeenCalled();
    });

    it('should navigate to recipe list when service throws error', () => {
        recipeServiceSpy.getById.mockReturnValue(throwError(() => new Error('not found')));

        const mockRoute = {
            paramMap: { get: vi.fn().mockReturnValue('recipe-1') },
        } as unknown as ActivatedRouteSnapshot;

        let resolved: Recipe | null | undefined;

        TestBed.runInInjectionContext(() => {
            const result$ = recipeResolver(mockRoute, mockState) as Observable<Recipe | null>;
            result$.subscribe(result => {
                resolved = result;
            });
        });

        expect(resolved).toBeNull();
        expect(navSpy.navigateToRecipeList).toHaveBeenCalled();
    });
});
