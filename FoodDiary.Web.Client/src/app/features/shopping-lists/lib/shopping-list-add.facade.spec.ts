import { HttpErrorResponse, HttpStatusCode } from '@angular/common/http';
import { signal } from '@angular/core';
import { TestBed } from '@angular/core/testing';
import { provideRouter } from '@angular/router';
import { FdUiDialogService } from 'fd-ui-kit/dialog/fd-ui-dialog.service';
import { of, Subject, throwError } from 'rxjs';
import { beforeEach, describe, expect, it, vi } from 'vitest';

import { AuthService } from '../../../services/auth.service';
import { utcInstant } from '../../../shared/models/semantics/date-value';
import { entityId } from '../../../shared/models/semantics/entity-id';
import type { ShoppingList, ShoppingListSummary } from '../../../shared/models/shopping-list.data';
import { PublicAuthDialogService } from '../../public/contracts/auth-dialog';
import { ShoppingListService } from '../api/shopping-list.service';
import type { ShoppingListTarget } from '../dialogs/shopping-list-picker/shopping-list-picker';
import { ShoppingListAddFacade } from './shopping-list-add.facade';

const REVISION_LENGTH = 64;
const authenticated = signal(true);
const api = { getSelectionPage: vi.fn(), getById: vi.fn(), create: vi.fn(), update: vi.fn() };
const dialogs = { open: vi.fn() };
const authDialog = { openAsync: vi.fn() };
const list: ShoppingList = {
    id: entityId<'shopping-list'>('list-1'),
    revision: 'a'.repeat(REVISION_LENGTH),
    name: 'Soup',
    createdAt: utcInstant(''),
    items: [],
};
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
            expectedRevision: list.revision,
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
        expect(api.update).toHaveBeenCalledExactlyOnceWith('list-1', {
            expectedRevision: list.revision,
            items: [{ name: 'Salt', note: 'to taste', sortOrder: 1 }],
        });
        expect(dialogs.open).toHaveBeenCalledTimes(1);
        expect(facade.isAdded(0)).toBe(true);
        expect(facade.isAdded(1)).toBe(true);
    });
});

describe('recipe shopping conditional append', () => {
    it.each([HttpStatusCode.Conflict, HttpStatusCode.TooManyRequests])(
        'does not mark a failed append (%s) as added and rebuilds the merge from a fresh detail on explicit retry',
        async status => {
            dialogs.open.mockReturnValue({ afterClosed: () => of({ id: list.id, name: list.name }) });
            api.update.mockReturnValueOnce(throwError(() => new HttpErrorResponse({ status })));
            await facade.addAsync(0, { name: 'Rice' });
            expect(facade.isAdded(0)).toBe(false);
            expect(api.update).toHaveBeenCalledExactlyOnceWith(list.id, {
                expectedRevision: list.revision,
                items: [{ name: 'Rice', sortOrder: 1 }],
            });
            const fresh = {
                ...list,
                revision: 'b'.repeat(REVISION_LENGTH),
                items: [{ id: 'new-server-item', shoppingListId: list.id, name: 'Milk', isChecked: true, sortOrder: 1 }],
            };
            api.getById.mockReturnValueOnce(of(fresh));
            api.update.mockReturnValueOnce(of({ ...fresh, revision: 'c'.repeat(REVISION_LENGTH) }));
            await facade.addAsync(0, { name: 'Rice' });
            expect(api.getById).toHaveBeenCalledTimes(2);
            expect(api.update).toHaveBeenLastCalledWith(list.id, {
                expectedRevision: fresh.revision,
                items: [expect.objectContaining({ id: 'new-server-item', name: 'Milk', isChecked: true }), { name: 'Rice', sortOrder: 2 }],
            });
            expect(facade.isAdded(0)).toBe(true);
        },
    );
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
            expectedRevision: list.revision,
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

describe('shopping list selection and request lifetime', () => {
    it('chooses an existing destination without writing items and can retry a failed lookup', async () => {
        api.getSelectionPage.mockReturnValueOnce(throwError(() => new Error('offline')));
        await facade.chooseAsync();
        expect(facade.message()).toBe('PUBLIC_RECIPES.SHOPPING_ERROR');
        expect(facade.busy()).toBe(false);

        dialogs.open.mockReturnValueOnce({ afterClosed: () => of({ id: list.id, name: list.name }) });
        await facade.chooseAsync();
        expect(facade.target()).toEqual({ id: list.id, name: list.name });
        expect(facade.message()).toBeNull();
        expect(api.create).not.toHaveBeenCalled();
        expect(api.update).not.toHaveBeenCalled();
    });

    it.each(['navigation', 'logout'])('keeps stale save errors out of the current view after %s', async transition => {
        const response = new Subject<ShoppingList>();
        api.create.mockReturnValueOnce(response);
        const pending = facade.addAsync(0, { name: 'Rice' });
        await vi.waitFor(() => {
            expect(api.create).toHaveBeenCalledOnce();
        });

        if (transition === 'navigation') {
            facade.setScope('recipe-2', 'Salad');
        } else {
            authenticated.set(false);
            TestBed.tick();
        }
        response.error(new Error('offline'));
        await pending;

        expect(facade.message()).toBeNull();
        expect(facade.target()).toBeNull();
        expect(facade.isAdded(0)).toBe(false);
        expect(facade.busy()).toBe(false);
    });
});

describe('shopping list add recovery', () => {
    it('waits for the sign-in dialog to finish before creating a destination', async () => {
        authenticated.set(false);
        TestBed.tick();
        const closed = new Subject<void>();
        authDialog.openAsync.mockResolvedValueOnce({ afterClosed: () => closed });
        const adding = facade.addAsync(0, { name: 'Rice' });
        await vi.waitFor(() => {
            expect(closed.observed).toBe(true);
        });
        expect(authDialog.openAsync).toHaveBeenCalledExactlyOnceWith(expect.objectContaining({ mode: 'login' }));
        expect(api.getSelectionPage).not.toHaveBeenCalled();
        expect(api.create).not.toHaveBeenCalled();

        authenticated.set(true);
        TestBed.tick();
        closed.next();
        closed.complete();
        await adding;

        expect(api.getSelectionPage).toHaveBeenCalledOnce();
        expect(api.create).toHaveBeenCalledExactlyOnceWith({ name: list.name, items: [{ name: 'Rice', sortOrder: 1 }] });
        expect(facade.isAdded(0)).toBe(true);
    });

    it('clears an unavailable destination and uses a newly selected one without losing the ingredient', async () => {
        const replaced = { ...list, id: entityId<'shopping-list'>('replacement'), name: 'Market' };
        const ingredient = { name: 'Rice', note: 'Long grain' };
        dialogs.open.mockReturnValueOnce({ afterClosed: () => of({ id: list.id, name: list.name }) });
        api.getById.mockReturnValueOnce(of(null));
        await facade.addAsync(0, ingredient);

        expect(api.getById).toHaveBeenCalledExactlyOnceWith(list.id);
        expect(api.update).not.toHaveBeenCalled();
        expect(facade.target()).toBeNull();
        expect(facade.isAdded(0)).toBe(false);
        expect(facade.message()).toBe('PUBLIC_RECIPES.SHOPPING_ERROR');

        dialogs.open.mockReturnValueOnce({ afterClosed: () => of({ id: null, name: replaced.name }) });
        api.create.mockReturnValueOnce(of(replaced));
        await facade.addAsync(0, ingredient);

        expect(api.create).toHaveBeenCalledExactlyOnceWith({ name: replaced.name, items: [{ ...ingredient, sortOrder: 1 }] });
        expect(facade.target()).toEqual({ id: replaced.id, name: replaced.name });
        expect(facade.isAdded(0)).toBe(true);
        expect(facade.message()).toBe('PUBLIC_RECIPES.SHOPPING_SUCCESS');
    });

    it('keeps a delayed destination lookup error off a newly opened recipe', async () => {
        const pending = new Subject<ShoppingListSummary[]>();
        api.getSelectionPage.mockReturnValueOnce(pending);
        const selecting = facade.chooseAsync();
        await vi.waitFor(() => {
            expect(pending.observed).toBe(true);
        });
        facade.setScope('recipe-2', 'Salad');
        pending.error(new Error('old recipient lookup failed'));
        await selecting;

        expect(facade.message()).toBeNull();
        expect(facade.target()).toBeNull();
        expect(dialogs.open).not.toHaveBeenCalled();
        expect(facade.busy()).toBe(false);
    });
});
