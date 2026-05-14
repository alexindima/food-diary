import { DatePipe } from '@angular/common';
import { TestBed } from '@angular/core/testing';
import { TranslateModule } from '@ngx-translate/core';
import { FdUiDialogService } from 'fd-ui-kit/dialog/fd-ui-dialog.service';
import { FdUiDialogRef } from 'fd-ui-kit/dialog/fd-ui-dialog-ref';
import { of, throwError } from 'rxjs';
import { beforeEach, describe, expect, it, vi } from 'vitest';

import { FavoriteMealService } from '../../../api/favorite-meal.service';
import type { FavoriteMeal, Meal } from '../../../models/meal.data';
import { MealDetailFacade } from './meal-detail.facade';

const favoriteMeal: FavoriteMeal = {
    id: 'favorite-1',
    mealId: 'meal-1',
    name: 'Lunch',
    createdAtUtc: '2026-05-14T10:00:00Z',
    mealDate: '2026-05-14T09:00:00Z',
    mealType: 'Lunch',
    totalCalories: 500,
    totalProteins: 30,
    totalFats: 20,
    totalCarbs: 50,
    itemCount: 2,
};

const meal: Meal = {
    id: 'meal-1',
    date: '2026-05-14T09:00:00Z',
    mealType: 'Lunch',
    comment: null,
    imageUrl: null,
    imageAssetId: null,
    totalCalories: 500,
    totalProteins: 30,
    totalFats: 20,
    totalCarbs: 50,
    totalFiber: 5,
    totalAlcohol: 0,
    isNutritionAutoCalculated: true,
    preMealSatietyLevel: null,
    postMealSatietyLevel: null,
    items: [],
    aiSessions: [],
    isFavorite: false,
    favoriteMealId: null,
};

let facade: MealDetailFacade;
let dialogRef: { close: ReturnType<typeof vi.fn> };
let dialogService: { open: ReturnType<typeof vi.fn> };
let favoriteMealService: {
    add: ReturnType<typeof vi.fn>;
    getAll: ReturnType<typeof vi.fn>;
    isFavorite: ReturnType<typeof vi.fn>;
    remove: ReturnType<typeof vi.fn>;
};

beforeEach(() => {
    dialogRef = {
        close: vi.fn(),
    };
    dialogService = {
        open: vi.fn().mockReturnValue({ afterClosed: () => of(true) }),
    };
    favoriteMealService = {
        add: vi.fn().mockReturnValue(of(favoriteMeal)),
        getAll: vi.fn().mockReturnValue(of([favoriteMeal])),
        isFavorite: vi.fn().mockReturnValue(of(false)),
        remove: vi.fn().mockReturnValue(of(undefined)),
    };

    TestBed.configureTestingModule({
        imports: [TranslateModule.forRoot()],
        providers: [
            MealDetailFacade,
            DatePipe,
            { provide: FdUiDialogRef, useValue: dialogRef },
            { provide: FdUiDialogService, useValue: dialogService },
            { provide: FavoriteMealService, useValue: favoriteMealService },
        ],
    });

    facade = TestBed.inject(MealDetailFacade);
});

describe('MealDetailFacade favorite state', () => {
    it('should initialize favorite state from meal and backend check', () => {
        favoriteMealService.isFavorite.mockReturnValue(of(true));

        facade.initialize({ ...meal, isFavorite: false });

        expect(favoriteMealService.isFavorite).toHaveBeenCalledWith('meal-1');
        expect(facade.isFavorite()).toBe(true);
        expect(facade.favoriteIcon()).toBe('star');
        expect(facade.favoriteAriaLabelKey()).toBe('CONSUMPTION_DETAIL.REMOVE_FAVORITE');
    });

    it('should add favorite and store loading state', () => {
        facade.initialize(meal);

        facade.toggleFavorite(meal);

        expect(favoriteMealService.add).toHaveBeenCalledWith('meal-1');
        expect(facade.isFavorite()).toBe(true);
        expect(facade.isFavoriteLoading()).toBe(false);
    });

    it('should reset loading state when add favorite fails', () => {
        favoriteMealService.add.mockReturnValue(throwError(() => new Error('fail')));
        facade.initialize(meal);

        facade.toggleFavorite(meal);

        expect(facade.isFavorite()).toBe(false);
        expect(facade.isFavoriteLoading()).toBe(false);
    });

    it('should remove favorite by known favorite id', () => {
        const favoriteMealData = { ...meal, isFavorite: true, favoriteMealId: 'favorite-1' };
        favoriteMealService.isFavorite.mockReturnValue(of(true));
        facade.initialize(favoriteMealData);

        facade.toggleFavorite(favoriteMealData);

        expect(favoriteMealService.remove).toHaveBeenCalledWith('favorite-1');
        expect(facade.isFavorite()).toBe(false);
        expect(facade.isFavoriteLoading()).toBe(false);
    });

    it('should find favorite id before removing when favorite id is missing', () => {
        const favoriteMealData = { ...meal, isFavorite: true, favoriteMealId: null };
        favoriteMealService.isFavorite.mockReturnValue(of(true));
        facade.initialize(favoriteMealData);

        facade.toggleFavorite(favoriteMealData);

        expect(favoriteMealService.getAll).toHaveBeenCalled();
        expect(favoriteMealService.remove).toHaveBeenCalledWith('favorite-1');
        expect(facade.isFavorite()).toBe(false);
    });

    it('should ignore toggle while favorite request is loading', () => {
        facade.isFavoriteLoading.set(true);

        facade.toggleFavorite(meal);

        expect(favoriteMealService.add).not.toHaveBeenCalled();
        expect(favoriteMealService.remove).not.toHaveBeenCalled();
    });
});

describe('MealDetailFacade actions', () => {
    it('should close with favorite changed result when favorite state changed', () => {
        facade.initialize(meal);
        facade.isFavorite.set(true);

        facade.close(meal);

        expect(dialogRef.close).toHaveBeenCalledWith(
            expect.objectContaining({ id: 'meal-1', action: 'FavoriteChanged', favoriteChanged: true }),
        );
    });

    it('should close without payload when favorite state did not change', () => {
        facade.initialize(meal);

        facade.close(meal);

        expect(dialogRef.close).toHaveBeenCalledWith();
    });

    it('should close with edit and repeat action results', () => {
        facade.initialize(meal);

        facade.edit(meal);
        facade.repeat(meal);

        expect(dialogRef.close).toHaveBeenNthCalledWith(1, expect.objectContaining({ id: 'meal-1', action: 'Edit' }));
        expect(dialogRef.close).toHaveBeenNthCalledWith(2, expect.objectContaining({ id: 'meal-1', action: 'Repeat' }));
    });

    it('should close with delete action after confirmation', () => {
        facade.initialize(meal);

        facade.delete(meal);

        expect(dialogService.open).toHaveBeenCalled();
        expect(dialogRef.close).toHaveBeenCalledWith(expect.objectContaining({ id: 'meal-1', action: 'Delete' }));
    });

    it('should keep dialog open when delete is cancelled', () => {
        dialogService.open.mockReturnValue({ afterClosed: () => of(false) });
        facade.initialize(meal);

        facade.delete(meal);

        expect(dialogRef.close).not.toHaveBeenCalled();
    });
});
