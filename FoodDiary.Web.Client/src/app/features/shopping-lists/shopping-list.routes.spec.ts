import { HttpErrorResponse, HttpStatusCode } from '@angular/common/http';
import { Component, signal } from '@angular/core';
import { TestBed } from '@angular/core/testing';
import { provideRouter, Router } from '@angular/router';
import { RouterTestingHarness } from '@angular/router/testing';
import { of, Subject, throwError } from 'rxjs';
import { afterEach, beforeEach, describe, expect, it, vi } from 'vitest';

import { provideTranslateTesting } from '../../../testing/translate-testing.module';
import { utcInstant } from '../../shared/models/semantics/date-value';
import { entityId } from '../../shared/models/semantics/entity-id';
import type { ShoppingList } from '../../shared/models/shopping-list.data';
import { ViewportService } from '../../shared/platform/viewport.service';
import { ShoppingListService } from './api/shopping-list.service';
import shoppingListRoutes from './shopping-list.routes';

@Component({ template: '<main>Home</main>' })
class ShoppingTestHomeComponent {}

const LIST_ID = entityId<'shopping-list'>('list-1');
const CREATED_AT = utcInstant('2026-11-19T08:00:00Z');
const REVISION_LENGTH = 64;
const ORIGINAL_REVISION = 'a'.repeat(REVISION_LENGTH);
const OTHER_TAB_REVISION = 'b'.repeat(REVISION_LENGTH);
const SAVED_REVISION = 'c'.repeat(REVISION_LENGTH);
const SHOPPING_URL = '/shopping-lists';
const LOCAL_DRAFT = 'Local draft tomatoes';
const REMOTE_ITEM = 'Other tab added apples';
const NEW_ITEM = 'Newly added broccoli';

const getOverview = vi.fn<ShoppingListService['getOverview']>();
const getById = vi.fn<ShoppingListService['getById']>();
const update = vi.fn<ShoppingListService['update']>();

beforeEach(() => {
    vi.resetAllMocks();
    vi.stubGlobal(
        'matchMedia',
        vi.fn(() => ({ matches: false })),
    );
    const selectedList = createList(ORIGINAL_REVISION, []);
    getOverview.mockReturnValue(
        of({
            selectedList,
            lists: {
                items: [{ id: LIST_ID, name: selectedList.name, itemsCount: 0, createdAt: CREATED_AT }],
                hasMore: false,
                nextPage: null,
            },
        }),
    );
    TestBed.configureTestingModule({
        providers: [
            provideTranslateTesting(),
            provideRouter([
                { path: 'shopping-lists', children: shoppingListRoutes },
                { path: 'home', component: ShoppingTestHomeComponent },
            ]),
            { provide: ShoppingListService, useValue: { getOverview, getById, update } },
            { provide: ViewportService, useValue: { isMobile: signal(false) } },
        ],
    });
});

afterEach(() => vi.unstubAllGlobals());

describe('Shopping list route protection', () => {
    it('preserves a conflicted draft until the user confirms reload and saves the next edit against the new revision', async () => {
        update.mockReturnValueOnce(throwError(() => new HttpErrorResponse({ status: HttpStatusCode.Conflict })));
        getById.mockReturnValue(of(createList(OTHER_TAB_REVISION, [REMOTE_ITEM])));
        const harness = await RouterTestingHarness.create(SHOPPING_URL);
        const router = TestBed.inject(Router);
        const page = harness.routeNativeElement as HTMLElement;

        addItem(harness, page, LOCAL_DRAFT);
        await expect(router.navigateByUrl('/home')).resolves.toBe(false);
        harness.detectChanges();
        expect(router.url).toBe(SHOPPING_URL);
        expect(page.textContent).toContain(LOCAL_DRAFT);
        expect(page.textContent).toContain('SHOPPING_LIST.CONFLICT_MESSAGE');
        expect(update).toHaveBeenCalledExactlyOnceWith(LIST_ID, expect.objectContaining({ expectedRevision: ORIGINAL_REVISION }));

        findButton(page, 'SHOPPING_LIST.CONFLICT_RELOAD').click();
        harness.detectChanges();
        const firstDialog = requireElement(document.body, 'fd-confirm-delete-dialog');
        findButton(firstDialog, 'CONFIRM_DELETE.CANCEL').click();
        harness.detectChanges();
        expect(getById).not.toHaveBeenCalled();
        expect(page.textContent).toContain(LOCAL_DRAFT);

        findButton(page, 'SHOPPING_LIST.CONFLICT_RELOAD').click();
        harness.detectChanges();
        const secondDialog = requireElement(document.body, 'fd-confirm-delete-dialog');
        findButton(secondDialog, 'SHOPPING_LIST.CONFLICT_RELOAD').click();
        harness.detectChanges();
        expect(getById).toHaveBeenCalledExactlyOnceWith(LIST_ID);
        expect(page.textContent).toContain(REMOTE_ITEM);
        expect(page.textContent).not.toContain(LOCAL_DRAFT);

        update.mockReturnValueOnce(of(createList(SAVED_REVISION, [REMOTE_ITEM, NEW_ITEM])));
        addItem(harness, page, NEW_ITEM);
        await expect(router.navigateByUrl('/home')).resolves.toBe(true);
        expect(update).toHaveBeenCalledTimes(2);
        expect(update).toHaveBeenLastCalledWith(
            LIST_ID,
            expect.objectContaining({
                expectedRevision: OTHER_TAB_REVISION,
                items: [expect.objectContaining({ name: REMOTE_ITEM }), expect.objectContaining({ name: NEW_ITEM })],
            }),
        );
        expect(router.url).toBe('/home');
    });

    it('waits for the actual save response before allowing navigation away from an edited list', async () => {
        const pending = new Subject<ShoppingList>();
        update.mockReturnValueOnce(pending);
        const harness = await RouterTestingHarness.create(SHOPPING_URL);
        const router = TestBed.inject(Router);
        addItem(harness, harness.routeNativeElement as HTMLElement, LOCAL_DRAFT);

        const navigation = router.navigateByUrl('/home');
        await vi.waitFor(() => {
            expect(update).toHaveBeenCalledOnce();
        });
        expect(router.url).toBe(SHOPPING_URL);
        expect(harness.routeNativeElement?.textContent).toContain(LOCAL_DRAFT);

        pending.next(createList(SAVED_REVISION, [LOCAL_DRAFT]));
        pending.complete();
        await expect(navigation).resolves.toBe(true);
        expect(router.url).toBe('/home');
        expect(update).toHaveBeenCalledOnce();
    });
});

function addItem(harness: RouterTestingHarness, root: HTMLElement, name: string): void {
    const input = root.querySelector<HTMLInputElement>('input[aria-label="SHOPPING_LIST.ADD_ITEM_NAME"]');
    if (input === null) {
        throw new Error('The item name input is missing.');
    }
    input.value = name;
    input.dispatchEvent(new Event('input', { bubbles: true }));
    harness.detectChanges();
    findButton(root, 'SHOPPING_LIST.ADD_ITEM_BUTTON').click();
    harness.detectChanges();
}

function requireElement(root: HTMLElement, selector: string): HTMLElement {
    const element = root.querySelector<HTMLElement>(selector);
    if (element === null) {
        throw new Error(`Expected ${selector}.`);
    }
    return element;
}

function findButton(root: HTMLElement, key: string): HTMLButtonElement {
    const button = [...root.querySelectorAll('button')].find(
        candidate => candidate.getAttribute('aria-label') === key || candidate.textContent.trim().endsWith(key),
    );
    if (button === undefined) {
        throw new Error(`The ${key} button is missing.`);
    }
    return button;
}

function createList(revision: string, names: readonly string[]): ShoppingList {
    return {
        id: LIST_ID,
        revision,
        name: 'Groceries',
        createdAt: CREATED_AT,
        items: names.map((name, index) => ({
            id: entityId<'shopping-list-item'>(`item-${index}`),
            shoppingListId: LIST_ID,
            name,
            isChecked: false,
            sortOrder: index,
        })),
    };
}
