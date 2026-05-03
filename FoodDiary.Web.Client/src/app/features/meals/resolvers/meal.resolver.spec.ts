import { TestBed } from '@angular/core/testing';
import { type ActivatedRouteSnapshot, type RouterStateSnapshot } from '@angular/router';
import { type Observable, of, throwError } from 'rxjs';
import { beforeEach, describe, expect, it, vi } from 'vitest';

import { NavigationService } from '../../../services/navigation.service';
import { MealService } from '../api/meal.service';
import { type Meal } from '../models/meal.data';
import { mealResolver } from './meal.resolver';

describe('mealResolver', () => {
    let mealServiceSpy: { getById: ReturnType<typeof vi.fn> };
    let navSpy: { navigateToConsumptionList: ReturnType<typeof vi.fn> };

    const mockMeal: Partial<Meal> = { id: 'meal-1' };

    const mockRoute = {
        paramMap: {
            get: vi.fn().mockReturnValue('meal-1'),
        },
    } as unknown as ActivatedRouteSnapshot;

    const mockState = {} as unknown as RouterStateSnapshot;

    beforeEach(() => {
        mealServiceSpy = { getById: vi.fn() };
        navSpy = { navigateToConsumptionList: vi.fn().mockResolvedValue(undefined) };

        TestBed.configureTestingModule({
            providers: [
                { provide: MealService, useValue: mealServiceSpy },
                { provide: NavigationService, useValue: navSpy },
            ],
        });
    });

    it('should resolve meal by id', () => {
        mealServiceSpy.getById.mockReturnValue(of(mockMeal));

        let resolved: Meal | null = null;

        TestBed.runInInjectionContext(() => {
            const result$ = mealResolver(mockRoute, mockState) as Observable<Meal | null>;
            result$.subscribe(result => {
                resolved = result;
            });
        });

        expect(resolved).toEqual(mockMeal);
        expect(mealServiceSpy.getById).toHaveBeenCalledWith('meal-1');
    });

    it('should navigate to consumption list when meal is null', () => {
        mealServiceSpy.getById.mockReturnValue(of(null));

        let resolved: Meal | null | undefined;

        TestBed.runInInjectionContext(() => {
            const result$ = mealResolver(mockRoute, mockState) as Observable<Meal | null>;
            result$.subscribe(result => {
                resolved = result;
            });
        });

        expect(resolved).toBeNull();
        expect(navSpy.navigateToConsumptionList).toHaveBeenCalled();
    });

    it('should navigate to consumption list when service throws error', () => {
        mealServiceSpy.getById.mockReturnValue(throwError(() => new Error('not found')));

        let resolved: Meal | null | undefined;

        TestBed.runInInjectionContext(() => {
            const result$ = mealResolver(mockRoute, mockState) as Observable<Meal | null>;
            result$.subscribe(result => {
                resolved = result;
            });
        });

        expect(resolved).toBeNull();
        expect(navSpy.navigateToConsumptionList).toHaveBeenCalled();
    });
});
