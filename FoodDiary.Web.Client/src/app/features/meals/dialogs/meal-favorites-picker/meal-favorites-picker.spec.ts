import { TestBed } from '@angular/core/testing';
import { FD_UI_DIALOG_DATA } from 'fd-ui-kit/dialog/fd-ui-dialog-data';
import { FdUiDialogRef } from 'fd-ui-kit/dialog/fd-ui-dialog-ref';
import { of, Subject, throwError } from 'rxjs';
import { beforeEach, describe, expect, it, vi } from 'vitest';

import { provideTranslateTesting } from '../../../../../testing/translate-testing.module';
import { FavoriteMealService } from '../../api/favorite-meal.service';
import type { FavoriteMeal } from '../../models/meal.data';
import { MealFavoritesPickerComponent } from './meal-favorites-picker';

const PAGE_SIZE = 10;
const LAST_PAGE = 3;
const SINGLE_LAST_PAGE_TOTAL = 21;

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
    registerUndoRenderingTests();
    registerUndoPositionTests();
});

function registerUndoPositionTests(): void {
    it('keeps a tombstone on the last page without fetching replacement rows', () => {
        const component = TestBed.createComponent(MealFavoritesPickerComponent).componentInstance;
        component['facade'].load(LAST_PAGE, 'rice');
        component['facade'].total.set(SINGLE_LAST_PAGE_TOTAL);
        component['remove'](favorite);
        expect(component['facade'].page()).toBe(LAST_PAGE);
        expect(component['facade'].total()).toBe(SINGLE_LAST_PAGE_TOTAL - 1);
        expect(component['facade'].paginationTotal()).toBe(SINGLE_LAST_PAGE_TOTAL);
        expect(api.getPage).toHaveBeenCalledTimes(2);
        component['undoRemoval'](favorite);
        expect(component['facade'].total()).toBe(SINGLE_LAST_PAGE_TOTAL);
        expect(api.getPage).toHaveBeenCalledTimes(2);
    });
    it('clears undo rows on navigation and clamps a page that no longer exists', () => {
        const component = TestBed.createComponent(MealFavoritesPickerComponent).componentInstance;
        component['facade'].total.set(1);
        component['remove'](favorite);
        component['load'](2);
        expect(component['facade'].removedIds().size).toBe(0);
        expect(api.getPage).toHaveBeenLastCalledWith(1, PAGE_SIZE, '');
        component['undoRemoval'](favorite);
        expect(restore).not.toHaveBeenCalled();
    });
    it('ignores a removal response arriving after a debounced search has changed', () => {
        const pending = new Subject<boolean>();
        remove.mockReturnValue(pending);
        const component = TestBed.createComponent(MealFavoritesPickerComponent).componentInstance;
        component['remove'](favorite);
        component['load'](1, 'chicken');
        pending.next(true);
        pending.complete();
        expect(component['facade'].removedIds().size).toBe(0);
        expect(component['facade'].total()).toBe(1);
    });
}

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
    it('removes once, blocks adding during removal, without reloading or closing', () => {
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
    it('restores any removed row independently and preserves every original slot', () => {
        const component = TestBed.createComponent(MealFavoritesPickerComponent).componentInstance;
        const second = { ...favorite, id: 'f2', mealId: 'm2', name: 'Saved dinner' };
        component['facade'].items.set([favorite, second]);
        component['facade'].total.set(2);
        component['remove'](favorite);
        component['remove'](second);
        expect([...component['facade'].removedIds()]).toEqual(['f1', 'f2']);
        component['undoRemoval'](favorite);
        expect(restore).toHaveBeenLastCalledWith(favorite);
        expect([...component['facade'].removedIds()]).toEqual(['f2']);
        component['undoRemoval'](second);
        expect(component['facade'].removedIds().size).toBe(0);
        expect(component['facade'].items()).toEqual([favorite, second]);
        expect(component['facade'].total()).toBe(2);
        expect(api.getPage).toHaveBeenCalledTimes(1);
    });
    it('keeps a failed row available for retry and blocks competing mutations', () => {
        const component = TestBed.createComponent(MealFavoritesPickerComponent).componentInstance;
        component['remove'](favorite);
        const pending = new Subject<boolean>();
        restore.mockReturnValueOnce(pending);
        component['undoRemoval'](favorite);
        component['undoRemoval'](favorite);
        component['remove'](favorite);
        component['add'](favorite);
        component['changePage'](2);
        expect(restore).toHaveBeenCalledTimes(1);
        expect(remove).toHaveBeenCalledTimes(1);
        expect(repeat).not.toHaveBeenCalled();
        expect(api.getPage).toHaveBeenCalledTimes(1);
        pending.next(false);
        pending.complete();
        expect(component['restoreErrors']().has(favorite.id)).toBe(true);
        expect(component['facade'].removedIds().has(favorite.id)).toBe(true);
        component['undoRemoval'](favorite);
        expect(component['facade'].removedIds().size).toBe(0);
        expect(component['restoreErrors']().size).toBe(0);
    });
    it('isolates network errors to the affected row and clears errors on search', () => {
        const component = TestBed.createComponent(MealFavoritesPickerComponent).componentInstance;
        component['remove'](favorite);
        restore.mockReturnValueOnce(throwError(() => new Error('offline')));
        component['undoRemoval'](favorite);
        expect([...component['restoreErrors']()]).toEqual([favorite.id]);
        expect(component['busy']()).toBe(false);
        component['load'](1, 'new search');
        expect(component['restoreErrors']().size).toBe(0);
        expect(component['facade'].removedIds().size).toBe(0);
    });
    it('cancels a pending restore when the dialog is destroyed', () => {
        const fixture = TestBed.createComponent(MealFavoritesPickerComponent);
        const pending = new Subject<boolean>();
        restore.mockReturnValue(pending);
        fixture.componentInstance['remove'](favorite);
        fixture.componentInstance['undoRemoval'](favorite);
        fixture.destroy();
        expect(pending.observed).toBe(false);
    });
}

function registerUndoRenderingTests(): void {
    it('renders several inline undo rows and restores the clicked row in place', async () => {
        const second = { ...favorite, id: 'f2', name: 'Dinner' };
        api.getPage.mockReturnValue(of({ data: [favorite, second], totalItems: 2 }));
        const fixture = TestBed.createComponent(MealFavoritesPickerComponent);
        fixture.detectChanges();
        fixture.componentInstance['remove'](favorite);
        fixture.componentInstance['remove'](second);
        fixture.detectChanges();
        await fixture.whenStable();
        const element = fixture.nativeElement as HTMLElement;
        expect(element.querySelectorAll('.favorite-row__undo')).toHaveLength(2);
        expect(element.querySelector('footer .favorite-row__undo')).toBeNull();
        element.querySelector<HTMLButtonElement>('.favorite-row__undo button')?.click();
        fixture.detectChanges();
        await fixture.whenStable();
        const rows = element.querySelectorAll('fd-favorite-meal-row');
        expect(rows[0].querySelector('.favorite-row__undo')).toBeNull();
        expect(rows[1].querySelector('.favorite-row__undo')).not.toBeNull();
        expect(restore).toHaveBeenCalledExactlyOnceWith(favorite);
    });
}
