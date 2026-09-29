import { signal } from '@angular/core';
import { TestBed } from '@angular/core/testing';
import { provideRouter } from '@angular/router';
import { FdUiDialogService } from 'fd-ui-kit/dialog/fd-ui-dialog.service';
import { of, Subject, throwError } from 'rxjs';
import { beforeEach, describe, expect, it, vi } from 'vitest';

import { AuthService } from '../../../services/auth.service';
import { PublicAuthDialogService } from '../../public/lib/public-auth-dialog.service';
import { ShoppingListService } from '../api/shopping-list.service';
import type { ShoppingListTarget } from '../dialogs/shopping-list-picker/shopping-list-picker';
import type { ShoppingList } from '../models/shopping-list.data';
import { ShoppingListAddFacade } from './shopping-list-add.facade';

const authenticated = signal(true);
const api = { getSelectionPage: vi.fn(), getById: vi.fn(), create: vi.fn(), update: vi.fn() };
const dialogs = { open: vi.fn() };
const authDialog = { openAsync: vi.fn() };
const list: ShoppingList = { id: 'list-1', name: 'Soup', createdAt: '', items: [] };
let facade: ShoppingListAddFacade;
beforeEach(() => {
    vi.resetAllMocks();
    authenticated.set(true);
    TestBed.configureTestingModule({
        providers: [
            ShoppingListAddFacade,
            provideRouter([]),
            { provide: ShoppingListService, useValue: api },
            { provide: FdUiDialogService, useValue: dialogs },
            { provide: AuthService, useValue: { isAuthenticated: authenticated } },
            { provide: PublicAuthDialogService, useValue: authDialog },
        ],
    });
    api.getSelectionPage.mockReturnValue(of([]));
    api.create.mockReturnValue(of(list));
    api.getById.mockReturnValue(of(list));
    api.update.mockReturnValue(of(list));
    dialogs.open.mockReturnValue({ afterClosed: () => of({ id: null, name: 'Soup' }) });
    facade = TestBed.inject(ShoppingListAddFacade);
    TestBed.tick();
    facade.setScope('recipe-1', 'Soup');
});
describe('recipe shopping additions', () => {
    it('adds remaining ingredients in one request and keeps earlier additions', async () => {
        await facade.addAsync(0, { name: 'Rice', amount: 200 });
        dialogs.open.mockReturnValue({ afterClosed: () => of({ id: 'list-1', name: 'Soup' }) });
        api.getById.mockReturnValue(
            of({ ...list, items: [{ id: 'rice', shoppingListId: 'list-1', name: 'Rice', amount: 200, isChecked: true, sortOrder: 1 }] }),
        );
        const entries = [
            { index: 0, item: { name: 'Rice' } },
            { index: 1, item: { name: 'Salt', note: 'to taste' } },
            { index: 2, item: { name: 'Water', amount: 600, unit: 'Ml' } },
        ];
        await facade.addAllAsync(entries);
        expect(api.update).toHaveBeenCalledExactlyOnceWith('list-1', {
            items: [
                expect.objectContaining({ id: 'rice', isChecked: true }),
                { name: 'Salt', note: 'to taste', sortOrder: 2 },
                { name: 'Water', amount: 600, unit: 'Ml', sortOrder: 3 },
            ],
        });
        const options = dialogs.open.mock.calls[1][1] as { data: { count: number } };
        expect(options.data.count).toBe(2);
        expect(facade.isAdded(1)).toBe(true);
        expect(facade.isAdded(2)).toBe(true);
        await facade.addAllAsync(entries);
        expect(api.update).toHaveBeenCalledTimes(1);
    });
    it('does not write a cancelled bulk addition', async () => {
        dialogs.open.mockReturnValue({ afterClosed: () => of(undefined) });
        await facade.addAllAsync([{ index: 0, item: { name: 'Rice' } }]);
        expect(api.create).not.toHaveBeenCalled();
        expect(facade.isAdded(0)).toBe(false);
    });

    it('creates one list with the first item, appends following items and prevents repeat clicks', async () => {
        await facade.addAsync(0, { name: 'Rice', amount: 200, unit: 'g' });
        await facade.addAsync(1, { name: 'Salt', note: 'to taste' });
        await facade.addAsync(1, { name: 'Salt', note: 'to taste' });
        expect(api.create).toHaveBeenCalledExactlyOnceWith({
            name: 'Soup',
            items: [{ name: 'Rice', amount: 200, unit: 'g', sortOrder: 1 }],
        });
        expect(api.update).toHaveBeenCalledExactlyOnceWith('list-1', { items: [{ name: 'Salt', note: 'to taste', sortOrder: 1 }] });
        expect(dialogs.open).toHaveBeenCalledTimes(1);
        expect(facade.isAdded(0)).toBe(true);
        expect(facade.isAdded(1)).toBe(true);
    });
});

describe('recipe shopping target state', () => {
    it('preserves existing items, IDs and checked state when selecting an existing list', async () => {
        dialogs.open.mockReturnValue({ afterClosed: () => of({ id: 'list-1', name: 'Soup' }) });
        api.getById.mockReturnValue(
            of({
                ...list,
                items: [{ id: 'existing', shoppingListId: 'list-1', name: 'Milk', isChecked: true, sortOrder: 1, note: 'Keep' }],
            }),
        );
        await facade.addAsync(0, { name: 'Rice' });
        expect(api.create).not.toHaveBeenCalled();
        expect(api.update).toHaveBeenCalledWith('list-1', {
            items: [
                expect.objectContaining({ id: 'existing', name: 'Milk', isChecked: true, note: 'Keep' }),
                { name: 'Rice', sortOrder: 2 },
            ],
        });
    });
    it('cancels selection without creating an empty list', async () => {
        dialogs.open.mockReturnValue({ afterClosed: () => of(undefined) });
        await facade.addAsync(0, { name: 'Rice' });
        expect(api.create).not.toHaveBeenCalled();
        expect(api.update).not.toHaveBeenCalled();
    });
    it('does not mark a failed addition as added and permits retry', async () => {
        api.create.mockReturnValueOnce(throwError(() => new Error('offline')));
        await facade.addAsync(0, { name: 'Rice' });
        expect(facade.isAdded(0)).toBe(false);
        expect(facade.message()).toBe('PUBLIC_RECIPES.SHOPPING_ERROR');
        await facade.addAsync(0, { name: 'Rice' });
        expect(facade.isAdded(0)).toBe(true);
    });
    it('does not fetch or write lists when a guest cancels sign-in', async () => {
        authenticated.set(false);
        TestBed.tick();
        authDialog.openAsync.mockResolvedValue(null);
        await facade.addAsync(0, { name: 'Rice' });
        expect(api.getSelectionPage).not.toHaveBeenCalled();
        expect(api.create).not.toHaveBeenCalled();
    });
    it('ignores double clicks while selecting and ignores selection after navigating away', async () => {
        const selection = new Subject<ShoppingListTarget | undefined>();
        dialogs.open.mockReturnValue({ afterClosed: () => selection });
        const pending = facade.addAsync(0, { name: 'Rice' });
        await vi.waitFor(() => {
            expect(dialogs.open).toHaveBeenCalledTimes(1);
        });
        await facade.addAsync(1, { name: 'Salt' });
        facade.setScope('recipe-2', 'Salad');
        selection.next({ id: null, name: 'Soup' });
        await pending;
        expect(api.create).not.toHaveBeenCalled();
        expect(facade.target()).toBeNull();
    });
    it('clears selected list and added state on logout', async () => {
        await facade.addAsync(0, { name: 'Rice' });
        authenticated.set(false);
        TestBed.tick();
        expect(facade.target()).toBeNull();
        expect(facade.isAdded(0)).toBe(false);
    });
});
