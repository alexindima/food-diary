import { TestBed } from '@angular/core/testing';
import { FD_UI_DIALOG_DATA } from 'fd-ui-kit/dialog/fd-ui-dialog-data';
import { FdUiDialogRef } from 'fd-ui-kit/dialog/fd-ui-dialog-ref';
import { type Observable,of, Subject } from 'rxjs';
import { beforeEach, describe, expect, it, vi } from 'vitest';

import { provideTranslateTesting } from '../../../../../testing/translate-testing.module';
import { FavoriteMealService } from '../../api/favorite-meal.service';
import type { FavoriteMeal } from '../../models/meal.data';
import { MealFavoritesPickerComponent } from './meal-favorites-picker';

const PAGE_SIZE = 10;
const favorite: FavoriteMeal = {
    id: 'f1',
    mealId: 'm1',
    name: null,
    itemNames: ['Rice', 'Chicken'],
    createdAtUtc: '',
    mealDate: '',
    mealType: null,
    totalCalories: 500,
    totalProteins: 30,
    totalFats: 10,
    totalCarbs: 50,
    itemCount: 2,
};
const api = { getPage: vi.fn() };
const ref = { close: vi.fn() };
const repeat = vi.fn();
const remove = vi.fn();
const restore = vi.fn();

describe('MealFavoritesPickerComponent', () => {
    beforeEach(() => {
        api.getPage.mockReset().mockReturnValue(of({ data: [favorite], totalItems: 1, page: 1, limit: 10, totalPages: 1 }));
        ref.close.mockReset();
        repeat.mockReset().mockReturnValue(of(true));
        remove.mockReset().mockReturnValue(of(true));
        restore.mockReset().mockReturnValue(of(true));
        TestBed.configureTestingModule({
            imports: [MealFavoritesPickerComponent],
            providers: [
                provideTranslateTesting(),
                { provide: FavoriteMealService, useValue: api },
                { provide: FD_UI_DIALOG_DATA, useValue: { repeat, remove, restore } },
                { provide: FdUiDialogRef, useValue: ref },
            ],
        });
    });
    registerPickerTests();
    registerUndoTests();
});

function registerPickerTests(): void {
    it('loads only one page on opening', () => {
        const fixture = TestBed.createComponent(MealFavoritesPickerComponent);
        expect(fixture.componentInstance).toBeTruthy();
        expect(api.getPage).toHaveBeenCalledExactlyOnceWith(1, PAGE_SIZE, '');
    });
    it('blocks double submission and closes only after successful adding', () => {
        const pending = new Subject<boolean>();
        repeat.mockReturnValue(pending);
        const component = TestBed.createComponent(MealFavoritesPickerComponent).componentInstance;
        component['add'](favorite);
        component['add'](favorite);
        expect(repeat).toHaveBeenCalledTimes(1);
        expect(ref.close).not.toHaveBeenCalled();
        pending.next(true);
        pending.complete();
        expect(ref.close).toHaveBeenCalledWith(true);
        expect(component['savingId']()).toBeNull();
    });
    it('retains the dialog and allows retry after an add failure', () => {
        repeat.mockReturnValueOnce(of(false));
        const component = TestBed.createComponent(MealFavoritesPickerComponent).componentInstance;
        component['add'](favorite);
        expect(component['saveFailed']()).toBe(true);
        expect(ref.close).not.toHaveBeenCalled();
        component['add'](favorite);
        expect(component['saveFailed']()).toBe(false);
        expect(ref.close).toHaveBeenCalledWith(true);
    });
    it('removes once, blocks adding during removal, and refreshes without closing', () => {
        const pending = new Subject<boolean>();
        remove.mockReturnValue(pending);
        const component = TestBed.createComponent(MealFavoritesPickerComponent).componentInstance;
        component['remove'](favorite);
        component['remove'](favorite);
        component['add'](favorite);
        expect(remove).toHaveBeenCalledExactlyOnceWith(favorite);
        expect(repeat).not.toHaveBeenCalled();
        pending.next(true);
        pending.complete();
        expect(api.getPage).toHaveBeenLastCalledWith(1, PAGE_SIZE, '');
        expect(ref.close).not.toHaveBeenCalled();
        expect(component['busy']()).toBe(false);
    });
    it('preserves the row and offers retry after removal fails', () => {
        remove.mockReturnValueOnce(of(false));
        const component = TestBed.createComponent(MealFavoritesPickerComponent).componentInstance;
        component['remove'](favorite);
        expect(component['removeFailed']()).toBe(true);
        expect(api.getPage).toHaveBeenCalledTimes(1);
        component['remove'](favorite);
        expect(component['removeFailed']()).toBe(false);
        expect(ref.close).not.toHaveBeenCalled();
    });
}

function registerUndoTests(): void {
    it('restores the last removed favorite without resetting search and supports successive undos', () => {
        const component = TestBed.createComponent(MealFavoritesPickerComponent).componentInstance;
        const second = { ...favorite, id: 'f2', mealId: 'm2', name: 'Saved dinner' };
        component['facade'].load(2, 'rice');
        component['remove'](favorite);
        component['remove'](second);
        expect(component['undoItem']()).toEqual(second);
        component['undoRemoval']();
        expect(restore).toHaveBeenLastCalledWith(second);
        expect(api.getPage).toHaveBeenLastCalledWith(1, PAGE_SIZE, 'rice');
        expect(component['undoItem']()).toEqual(favorite);
        component['undoRemoval']();
        expect(restore).toHaveBeenLastCalledWith(favorite);
        expect(component['undoItem']()).toBeUndefined();
    });
    it('keeps undo available on restore failure and blocks duplicate or competing mutations', () => {
        const component = TestBed.createComponent(MealFavoritesPickerComponent).componentInstance;
        component['remove'](favorite);
        const pending = new Subject<boolean>();
        restore.mockReturnValueOnce(pending);
        component['undoRemoval']();
        component['undoRemoval']();
        component['remove'](favorite);
        component['add'](favorite);
        expect(restore).toHaveBeenCalledTimes(1);
        expect(remove).toHaveBeenCalledTimes(1);
        expect(repeat).not.toHaveBeenCalled();
        pending.next(false);
        pending.complete();
        expect(component['restoreFailed']()).toBe(true);
        expect(component['undoItem']()).toEqual(favorite);
        component['undoRemoval']();
        expect(component['undoItem']()).toBeUndefined();
        expect(component['restoreFailed']()).toBe(false);
    });
    it('cancels a pending restore when the dialog is destroyed', () => {
        const fixture = TestBed.createComponent(MealFavoritesPickerComponent);
        const pending = new Subject<boolean>();
        restore.mockReturnValue(pending);
        fixture.componentInstance['remove'](favorite);
        fixture.componentInstance['undoRemoval']();
        fixture.destroy();
        expect(pending.observed).toBe(false);
    });
}

describe('MealFavoritesPicker undo notification rendering', () => {
    it('keeps the notification in the dialog and removes it after restoration', async () => {
        TestBed.configureTestingModule({
            imports: [MealFavoritesPickerComponent],
            providers: [
                provideTranslateTesting(),
                {
                    provide: FavoriteMealService,
                    useValue: {
                        getPage: vi.fn().mockReturnValue(of({ data: [], totalItems: 0, page: 1, limit: PAGE_SIZE, totalPages: 0 })),
                    },
                },
                {
                    provide: FD_UI_DIALOG_DATA,
                    useValue: {
                        remove: (): Observable<boolean> => of(true),
                        restore: (): Observable<boolean> => of(true),
                    },
                },
                { provide: FdUiDialogRef, useValue: ref },
            ],
        });
        const fixture = TestBed.createComponent(MealFavoritesPickerComponent);
        fixture.detectChanges();
        const element = fixture.nativeElement as HTMLElement;
        expect(element.querySelector('.favorites-picker__undo')).toBeNull();
        fixture.componentInstance['remove'](favorite);
        fixture.detectChanges();
        await fixture.whenStable();
        expect(element.querySelector('footer .favorites-picker__undo')?.textContent).toContain('MEAL_FAVORITES.UNDO');
        fixture.componentInstance['undoRemoval']();
        fixture.detectChanges();
        await fixture.whenStable();
        expect(element.querySelector('.favorites-picker__undo')).toBeNull();
    });
});
