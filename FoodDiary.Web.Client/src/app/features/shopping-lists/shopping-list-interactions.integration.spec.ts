import { HttpErrorResponse, HttpStatusCode } from '@angular/common/http';
import { ChangeDetectionStrategy, Component, signal } from '@angular/core';
import { TestBed } from '@angular/core/testing';
import { GuardsCheckStart, provideRouter, type ResolveFn, ResolveStart, Router } from '@angular/router';
import { RouterTestingHarness } from '@angular/router/testing';
import { TranslateService } from '@ngx-translate/core';
import { FdTourService } from 'fd-tour';
import { FdUiDialogService } from 'fd-ui-kit/dialog/fd-ui-dialog.service';
import { FdUiToastService } from 'fd-ui-kit/toast/fd-ui-toast.service';
import { filter, firstValueFrom, isObservable, type Observable, of, Subject, throwError } from 'rxjs';
import { afterEach, describe, expect, it, type Mock, vi } from 'vitest';

import { provideTranslateTesting } from '../../../testing/translate-testing.module';
import { MeasurementUnit, ProductType } from '../../shared/models/product.data';
import { utcInstant } from '../../shared/models/semantics/date-value';
import { entityId } from '../../shared/models/semantics/entity-id';
import type { ShoppingList, ShoppingListOverview, ShoppingListSummary } from '../../shared/models/shopping-list.data';
import { ViewportService } from '../../shared/platform/viewport.service';
import { ShoppingListService } from './api/shopping-list.service';
import shoppingListRoutes from './shopping-list.routes';

const SHOPPING_URL = '/shopping-lists';
const HOME_URL = '/home';
const FIRST_PAGE_SIZE = 20;
const REVISION_LENGTH = 64;
const PAGE_CONTENT_HEIGHT = 600;
const PAGE_SCROLL_OFFSET = 380;
const PAGE_VIEWPORT_HEIGHT = 200;
const FIRST_REVISION = 'a'.repeat(REVISION_LENGTH);
const SAVED_REVISION = 'b'.repeat(REVISION_LENGTH);
const CREATED_AT = utcInstant('2026-11-19T08:00:00Z');
const GROCERIES = makeList('groceries', 'Groceries', ['Red apples']);
const TRAVEL = makeList('travel', 'Travel', ['Trail mix']);
const REVIEW_BROWSER = 'fd-shopping-list-browser-dialog';
const SHOPPING_AUTOSAVE_WAIT_MS = 3000;
const CREATED_SHOPPING_ITEM_NAME = 'Broad beans';
const SHOPPING_ITEM_EDITOR = 'fd-shopping-item-edit';

@Component({ template: '<h1>Home</h1>', changeDetection: ChangeDetectionStrategy.OnPush })
class ShoppingHomeComponent {}

type ShoppingApi = {
    [Method in 'getOverview' | 'getById' | 'getPage' | 'create' | 'update' | 'deleteById']: Mock<ShoppingListService[Method]>;
};

type ShoppingFixture = {
    harness: RouterTestingHarness;
    api: ShoppingApi;
    toastError: Mock<FdUiToastService['error']>;
};

afterEach(() => {
    TestBed.inject(FdUiDialogService).closeAll();
    vi.unstubAllGlobals();
});

async function renderShoppingAsync(
    overview: ShoppingListOverview | Observable<ShoppingListOverview> = firstOverview(),
    mobile = false,
    resolveHome?: ResolveFn<boolean>,
): Promise<ShoppingFixture> {
    vi.stubGlobal(
        'matchMedia',
        vi.fn(() => ({ matches: false })),
    );
    const api: ShoppingApi = {
        getOverview: vi.fn<ShoppingListService['getOverview']>().mockReturnValue(isObservable(overview) ? overview : of(overview)),
        getPage: vi.fn<ShoppingListService['getPage']>().mockReturnValue(of([])),
        getById: vi
            .fn<ShoppingListService['getById']>()
            .mockImplementation(id => of([GROCERIES, TRAVEL].find(list => list.id === id) ?? null)),
        create: vi.fn<ShoppingListService['create']>(),
        update: vi.fn<ShoppingListService['update']>(),
        deleteById: vi.fn<ShoppingListService['deleteById']>(),
    };
    const toastError = vi.fn<FdUiToastService['error']>();
    TestBed.configureTestingModule({
        providers: [
            provideTranslateTesting(),
            provideRouter([
                { path: 'shopping-lists', children: shoppingListRoutes },
                { path: 'home', component: ShoppingHomeComponent, resolve: resolveHome === undefined ? undefined : { ready: resolveHome } },
            ]),
            { provide: ShoppingListService, useValue: api },
            { provide: ViewportService, useValue: { isMobile: signal(mobile) } },
            { provide: FdUiToastService, useValue: { error: toastError } },
        ],
    });
    const translate = TestBed.inject(TranslateService);
    translate.setFallbackLang('en');
    translate.use('en');
    const harness = await RouterTestingHarness.create(SHOPPING_URL);
    return { harness, api, toastError };
}

function makeList(key: string, name: string, itemNames: string[] = []): ShoppingList {
    const id = entityId<'shopping-list'>(key);
    return {
        id,
        name,
        createdAt: CREATED_AT,
        revision: FIRST_REVISION,
        items: itemNames.map((itemName, index) => ({
            id: entityId<'shopping-list-item'>(`${key}-item-${index}`),
            shoppingListId: id,
            name: itemName,
            isChecked: false,
            sortOrder: index,
        })),
    };
}

function summary(list: ShoppingList): ShoppingListSummary {
    return {
        id: list.id,
        name: list.name,
        createdAt: list.createdAt,
        itemsCount: list.items.length,
        remainingCount: list.items.filter(item => !item.isChecked).length,
    };
}

function firstOverview(): ShoppingListOverview {
    return { selectedList: GROCERIES, lists: { items: [summary(GROCERIES), summary(TRAVEL)], hasMore: false, nextPage: null } };
}

function pagedOverview(): ShoppingListOverview {
    const otherLists = Array.from({ length: FIRST_PAGE_SIZE - 1 }, (_, index) => summary(makeList(`list-${index}`, `List ${index + 1}`)));
    return { selectedList: GROCERIES, lists: { items: [summary(GROCERIES), ...otherLists], hasMore: true, nextPage: 2 } };
}

function page(harness: RouterTestingHarness): HTMLElement {
    const host = harness.routeNativeElement;
    if (host === null) {
        throw new Error('Expected the shopping route');
    }
    return host;
}

function nativeButton(root: ParentNode, label: string): HTMLButtonElement {
    const button = Array.from(root.querySelectorAll('button')).find(
        candidate => candidate.getAttribute('aria-label') === label || candidate.textContent.includes(label),
    );
    if (button === undefined) {
        throw new Error(`Expected shopping action ${label}`);
    }
    return button;
}

function requiredElement(root: ParentNode, selector: string): HTMLElement {
    const found = root.querySelector<HTMLElement>(selector);
    if (found === null) {
        throw new Error(`Expected ${selector} on the shopping route`);
    }
    return found;
}

function requiredInput(root: ParentNode, selector: string): HTMLInputElement {
    const found = requiredElement(root, selector);
    if (!(found instanceof HTMLInputElement)) {
        throw new TypeError(`Expected ${selector} to select an input`);
    }
    return found;
}

function inputText(input: HTMLInputElement, value: string): void {
    input.value = value;
    input.dispatchEvent(new Event('input', { bubbles: true }));
}

function openMobilePicker(harness: RouterTestingHarness): HTMLElement {
    nativeButton(page(harness), 'SHOPPING_LIST.ALL_LISTS').click();
    harness.detectChanges();
    return requiredElement(document, REVIEW_BROWSER);
}

describe('Shopping overview error recovery', () => {
    it.each(['existing lists', 'an empty account'] as const)('loads %s after retrying from the error screen', async expected => {
        const initial = new Subject<ShoppingListOverview>();
        const retry = new Subject<ShoppingListOverview>();
        const { harness, api } = await renderShoppingAsync(initial);
        const host = page(harness);
        expect(host.querySelector('.shopping-list__loader')).not.toBeNull();
        expect(host.textContent).not.toContain('SHOPPING_LIST.NO_LISTS_TITLE');

        initial.error(new Error('Overview unavailable'));
        harness.detectChanges();
        expect(host.querySelector('.shopping-list__loader')).toBeNull();
        expect(host.textContent).not.toContain('SHOPPING_LIST.NO_LISTS_TITLE');
        const alert = requiredElement(host, 'fd-ui-inline-alert[role="alert"]');
        expect(alert.textContent).toContain('SHOPPING_LIST.LOAD_ERROR');
        expect(api.create).not.toHaveBeenCalled();

        api.getOverview.mockReturnValueOnce(retry);
        nativeButton(alert, 'ERRORS.RETRY').click();
        harness.detectChanges();
        expect(api.getOverview).toHaveBeenCalledTimes(2);
        expect(host.querySelector('fd-ui-inline-alert[role="alert"]')).toBeNull();
        expect(host.querySelector('.shopping-list__loader')).not.toBeNull();

        const overview =
            expected === 'existing lists' ? firstOverview() : { selectedList: null, lists: { items: [], hasMore: false, nextPage: null } };
        retry.next(overview);
        retry.complete();
        harness.detectChanges();
        expect(host.querySelector('.shopping-list__loader')).toBeNull();
        expect(host.querySelector('fd-ui-inline-alert[role="alert"]')).toBeNull();
        if (expected === 'existing lists') {
            expect(host.textContent).toContain('Red apples');
            expect(host.textContent).not.toContain('SHOPPING_LIST.NO_LISTS_TITLE');
        } else {
            expect(host.textContent).toContain('SHOPPING_LIST.NO_LISTS_TITLE');
            expect(nativeButton(host, 'SHOPPING_LIST.NEW_LIST_BUTTON').disabled).toBe(false);
        }
    });
});

describe('Shopping route list creation', () => {
    it('creates the first list once and saves its trimmed inline name using Enter', async () => {
        const { harness, api } = await renderShoppingAsync({ selectedList: null, lists: { items: [], hasMore: false, nextPage: null } });
        const creation = new Subject<ShoppingList>();
        const created = makeList('first', 'New list');
        api.create.mockReturnValueOnce(creation);
        api.getPage.mockReturnValueOnce(of([summary(created)]));
        api.getById.mockReturnValueOnce(of(created));
        api.update.mockReturnValueOnce(of({ ...created, name: 'Weekly market', revision: SAVED_REVISION }));
        const host = page(harness);
        expect(host.textContent).toContain('SHOPPING_LIST.NO_LISTS_TITLE');

        const create = nativeButton(host, 'SHOPPING_LIST.NEW_LIST_BUTTON');
        create.click();
        harness.detectChanges();
        expect(create.disabled).toBe(true);
        create.click();
        expect(api.create).toHaveBeenCalledOnce();
        expect(api.create.mock.calls[0][0].name).toContain('SHOPPING_LIST.NEW_LIST');
        creation.next(created);
        creation.complete();
        harness.detectChanges();

        const rename = requiredInput(host, '.shopping-list__list-card-rename-input');
        expect(rename.value).toBe('New list');
        inputText(rename, '  ');
        rename.dispatchEvent(new KeyboardEvent('keydown', { key: 'Enter', bubbles: true, cancelable: true }));
        expect(api.update).not.toHaveBeenCalled();
        inputText(rename, '  Weekly market  ');
        rename.dispatchEvent(new KeyboardEvent('keydown', { key: 'Enter', bubbles: true, cancelable: true }));
        harness.detectChanges();

        expect(api.update).toHaveBeenCalledExactlyOnceWith(created.id, { expectedRevision: FIRST_REVISION, name: 'Weekly market' });
        expect(requiredElement(host, '.shopping-list__summary-title').textContent).toBe('Weekly market');
        expect(host.querySelector('.shopping-list__list-card-rename-input')).toBeNull();
    });

    it('keeps a newly created list selected when the refreshed first page omits it', async () => {
        const initial = pagedOverview();
        const { harness, api } = await renderShoppingAsync(initial);
        const created = makeList('new-outside-first-page', 'Newly created list');
        const pageRefresh = new Subject<ShoppingListSummary[]>();
        api.create.mockReturnValueOnce(of(created));
        api.getPage.mockReturnValueOnce(pageRefresh);
        api.getById.mockReturnValueOnce(of(created));
        const host = page(harness);

        nativeButton(host, 'SHOPPING_LIST.NEW_LIST_BUTTON').click();
        harness.detectChanges();
        expect(api.getPage).toHaveBeenCalledOnce();
        expect(api.getById).not.toHaveBeenCalled();
        expect(requiredElement(host, '.shopping-list__workspace').getAttribute('aria-busy')).toBe('true');

        pageRefresh.next(initial.lists.items);
        pageRefresh.complete();
        harness.detectChanges();

        expect(api.getById).toHaveBeenCalledExactlyOnceWith(created.id);
        const selectedTab = requiredElement(host, '.shopping-list__list-tab--active button');
        expect(selectedTab.getAttribute('aria-current')).toBe('true');
        expect(selectedTab.textContent).toContain(created.name);
        expect(requiredInput(host, '.shopping-list__list-card-rename-input').value).toBe(created.name);
        expect(host.textContent).not.toContain('Red apples');
    });
});

describe('Shopping list creation without a cached overview', () => {
    it('retries an explicit creation and can delete the saved list even when refreshing its overview fails', async () => {
        const initial = throwError(() => new Error('Initial overview unavailable'));
        const { harness, api, toastError } = await renderShoppingAsync(initial);
        const saved = makeList('recovered', 'Recovered list');
        api.create
            .mockReturnValueOnce(throwError(() => new Error('Creation unavailable')))
            .mockImplementationOnce(({ name }) => of({ ...saved, name }));
        api.getPage.mockReturnValueOnce(throwError(() => new Error('Index unavailable'))).mockReturnValueOnce(of([]));
        api.deleteById.mockReturnValueOnce(of(void 0));
        const host = page(harness);
        const create = nativeButton(host, 'SHOPPING_LIST.NEW_LIST_BUTTON');
        const loadError = 'SHOPPING_LIST.LOAD_ERROR';
        expect(requiredElement(host, 'fd-ui-inline-alert[role="alert"]').textContent).toContain(loadError);

        create.click();
        harness.detectChanges();
        expect(api.create).toHaveBeenCalledOnce();
        expect(api.getPage).not.toHaveBeenCalled();
        expect(toastError).toHaveBeenCalledExactlyOnceWith('SHOPPING_LIST.CREATE_ERROR');
        expect(requiredElement(host, 'fd-ui-inline-alert[role="alert"]').textContent).toContain(loadError);
        expect(create.disabled).toBe(false);

        create.click();
        harness.detectChanges();
        expect(api.create).toHaveBeenCalledTimes(2);
        expect(api.getPage).toHaveBeenCalledOnce();
        expect(toastError).toHaveBeenLastCalledWith(loadError);
        expect(host.querySelector('fd-ui-inline-alert[role="alert"]')).toBeNull();
        requiredInput(host, '.shopping-list__list-card-rename-input').dispatchEvent(
            new KeyboardEvent('keydown', { key: 'Escape', bubbles: true }),
        );
        harness.detectChanges();
        expect(requiredElement(host, '.shopping-list__summary-title').textContent).toBe(api.create.mock.calls[1][0].name);

        chooseSummaryAction(harness, 'SHOPPING_LIST.DELETE_LIST_BUTTON');
        nativeButton(requiredElement(document, 'fd-confirm-delete-dialog'), 'CONFIRM_DELETE.CONFIRM').click();
        harness.detectChanges();
        expect(api.deleteById).toHaveBeenCalledExactlyOnceWith(saved.id);
        expect(api.getPage).toHaveBeenCalledTimes(2);
        expect(api.getById).not.toHaveBeenCalled();
        expect(host.querySelector('fd-ui-inline-alert[role="alert"]')).toBeNull();
        expect(host.textContent).toContain('SHOPPING_LIST.NO_LISTS_TITLE');
    });
});

describe('Shopping route list selection', () => {
    it('unlocks the current list after a failed selection and can retry that tab', async () => {
        const { harness, api, toastError } = await renderShoppingAsync();
        const pending = new Subject<ShoppingList | null>();
        api.getById.mockReturnValueOnce(pending);
        const host = page(harness);
        const travel = (): HTMLButtonElement => nativeButton(host, TRAVEL.name);
        travel().click();
        await vi.waitFor(() => {
            harness.detectChanges();
            expect(api.getById).toHaveBeenCalledExactlyOnceWith(TRAVEL.id);
        });
        expect(requiredInput(host, 'input[aria-label="SHOPPING_LIST.ADD_ITEM_NAME"]').closest('fieldset')?.disabled).toBe(true);

        pending.next(null);
        pending.complete();
        harness.detectChanges();
        expect(toastError).toHaveBeenCalledExactlyOnceWith('SHOPPING_LIST.LOAD_ERROR');
        expect(requiredElement(host, '.shopping-list__summary-title').textContent).toBe(GROCERIES.name);
        expect(host.textContent).toContain('Red apples');
        expect(travel().disabled).toBe(false);
        travel().click();
        await vi.waitFor(() => {
            harness.detectChanges();
            expect(requiredElement(host, '.shopping-list__summary-title').textContent).toBe(TRAVEL.name);
        });

        expect(host.textContent).toContain('Trail mix');
        expect(host.textContent).not.toContain('Red apples');
        expect(api.getById.mock.calls).toEqual([[TRAVEL.id], [TRAVEL.id]]);
    });
});

describe('Shopping existing list rename', () => {
    it.each(['Enter', 'blur'] as const)('leaves an unchanged list alone on %s and saves a later explicit rename', async finish => {
        const { harness, api } = await renderShoppingAsync();
        const host = page(harness);
        api.update.mockReturnValue(throwError(() => new HttpErrorResponse({ status: 409 })));
        chooseSummaryAction(harness, 'SHOPPING_LIST.RENAME_LIST_BUTTON');
        const name = requiredInput(host, '.shopping-list__list-card-rename-input');
        expect(name.value).toBe(GROCERIES.name);
        inputText(name, `  ${GROCERIES.name}  `);
        if (finish === 'Enter') {
            name.dispatchEvent(new KeyboardEvent('keydown', { key: 'Enter', bubbles: true, cancelable: true }));
        } else {
            name.focus();
            name.blur();
        }
        harness.detectChanges();

        expect(api.update).not.toHaveBeenCalled();
        expect(host.querySelector('fd-ui-inline-alert')).toBeNull();
        expect(host.querySelector('.shopping-list__list-card-rename-input')).toBeNull();
        expect(requiredElement(host, '.shopping-list__summary-title').textContent).toBe(GROCERIES.name);

        const newName = 'Weekend market';
        api.update.mockReturnValueOnce(of({ ...GROCERIES, name: newName, revision: SAVED_REVISION }));
        chooseSummaryAction(harness, 'SHOPPING_LIST.RENAME_LIST_BUTTON');
        const changed = requiredInput(host, '.shopping-list__list-card-rename-input');
        inputText(changed, `  ${newName}  `);
        changed.dispatchEvent(new KeyboardEvent('keydown', { key: 'Enter', bubbles: true, cancelable: true }));
        harness.detectChanges();

        expect(api.update).toHaveBeenCalledExactlyOnceWith(GROCERIES.id, { expectedRevision: FIRST_REVISION, name: newName });
        expect(requiredElement(host, '.shopping-list__summary-title').textContent).toBe(newName);
    });
});

describe('Shopping inline name validation', () => {
    it('keeps the name editor open when Enter submits whitespace and saves a corrected name', async () => {
        const { harness, api } = await renderShoppingAsync();
        const host = page(harness);
        chooseSummaryAction(harness, 'SHOPPING_LIST.RENAME_LIST_BUTTON');
        const name = requiredInput(host, '.shopping-list__list-card-rename-input');

        inputText(name, '   ');
        harness.detectChanges();
        name.dispatchEvent(new KeyboardEvent('keydown', { key: 'Enter', bubbles: true, cancelable: true }));
        harness.detectChanges();

        expect(name.isConnected).toBe(true);
        expect(name.value).toBe('   ');
        expect(nativeButton(host, 'SHOPPING_LIST.SAVE_NAME_BUTTON').disabled).toBe(true);
        expect(api.update).not.toHaveBeenCalled();

        const correctedName = 'Market groceries';
        api.update.mockReturnValueOnce(of({ ...GROCERIES, name: correctedName, revision: SAVED_REVISION }));
        inputText(name, `  ${correctedName}  `);
        harness.detectChanges();
        name.dispatchEvent(new KeyboardEvent('keydown', { key: 'Enter', bubbles: true, cancelable: true }));
        harness.detectChanges();

        expect(api.update).toHaveBeenCalledExactlyOnceWith(GROCERIES.id, {
            expectedRevision: FIRST_REVISION,
            name: correctedName,
        });
        expect(name.isConnected).toBe(false);
        expect(requiredElement(host, '.shopping-list__summary-title').textContent).toBe(correctedName);
    });
});

describe('Shopping list rename while an item autosave is pending', () => {
    it.each(['Enter', 'blur'] as const)('retains the typed list name after %s and lets the user save after autosave', async finish => {
        const { harness, api } = await renderShoppingAsync();
        const host = page(harness);
        const itemSave = new Subject<ShoppingList>();
        const saved = shoppingListWithAddedItem(GROCERIES, 'market-snacks', 'Trail snacks', SAVED_REVISION);
        const newName = 'Weekend market';
        const renamed = { ...saved, name: newName, revision: 'c'.repeat(REVISION_LENGTH) };
        const editorSelector = '.shopping-list__list-card-rename-input';
        const saveNameLabel = 'SHOPPING_LIST.SAVE_NAME_BUTTON';
        api.update.mockReturnValueOnce(itemSave).mockReturnValueOnce(of(renamed));

        addNativeItem(harness, 'Trail snacks');
        chooseSummaryAction(harness, 'SHOPPING_LIST.RENAME_LIST_BUTTON');
        const editor = requiredInput(host, editorSelector);
        const draft = '  Weekend market  ';
        inputText(editor, draft);
        editor.focus();
        await waitForShoppingAutosaveAsync(harness, api.update, 1);
        expect(api.update.mock.calls[0][1].name).toBe(GROCERIES.name);

        if (finish === 'Enter') {
            editor.dispatchEvent(new KeyboardEvent('keydown', { key: 'Enter', bubbles: true, cancelable: true }));
        } else {
            editor.blur();
        }
        harness.detectChanges();

        expect(host.querySelector<HTMLInputElement>(editorSelector)?.value).toBe(draft);
        expect(nativeButton(host, saveNameLabel).disabled).toBe(true);
        expect(api.update).toHaveBeenCalledOnce();

        itemSave.next(saved);
        itemSave.complete();
        harness.detectChanges();
        expect(requiredInput(host, editorSelector).value).toBe(draft);
        const saveName = nativeButton(host, saveNameLabel);
        expect(saveName.disabled).toBe(false);
        saveName.click();
        harness.detectChanges();

        expect(api.update.mock.calls).toHaveLength(2);
        expect(api.update).toHaveBeenNthCalledWith(2, saved.id, { expectedRevision: saved.revision, name: newName });
        expect(host.querySelector(editorSelector)).toBeNull();
        expect(requiredElement(host, '.shopping-list__summary-title').textContent).toBe(newName);
        expect(host.textContent).toContain('Trail snacks');
    });
});

describe('Shopping item changes while a list name is saving', () => {
    it.each(['accepted', 'rejected'] as const)('saves the item edit after the server %s the pending rename', async outcome => {
        const { harness, api, toastError } = await renderShoppingAsync();
        const host = page(harness);
        const pendingRename = new Subject<ShoppingList>();
        const renamed = { ...GROCERIES, name: 'Market run', revision: SAVED_REVISION };
        const resultingList = outcome === 'accepted' ? renamed : GROCERIES;
        const checkedItems = resultingList.items.map(item => ({ ...item, isChecked: true }));
        api.update
            .mockReturnValueOnce(pendingRename)
            .mockReturnValueOnce(of({ ...resultingList, revision: SAVED_REVISION, items: checkedItems }));

        chooseSummaryAction(harness, 'SHOPPING_LIST.RENAME_LIST_BUTTON');
        const name = requiredInput(host, '.shopping-list__list-card-rename-input');
        inputText(name, renamed.name);
        name.dispatchEvent(new KeyboardEvent('keydown', { key: 'Enter', bubbles: true, cancelable: true }));
        harness.detectChanges();
        expect(api.update).toHaveBeenCalledExactlyOnceWith(GROCERIES.id, {
            expectedRevision: GROCERIES.revision,
            name: renamed.name,
        });

        const check = requiredInput(host, 'fd-shopping-list-items-panel input[type="checkbox"]');
        expect(check.matches(':enabled')).toBe(true);
        check.click();
        harness.detectChanges();
        expect(api.update).toHaveBeenCalledOnce();

        if (outcome === 'accepted') {
            pendingRename.next(renamed);
            pendingRename.complete();
        } else {
            pendingRename.error(new Error('Name could not be saved'));
        }
        await vi.waitFor(
            () => {
                expect(api.update).toHaveBeenNthCalledWith(2, GROCERIES.id, {
                    expectedRevision: resultingList.revision,
                    name: resultingList.name,
                    items: [expect.objectContaining({ name: GROCERIES.items[0].name, isChecked: true })],
                });
            },
            { timeout: SHOPPING_AUTOSAVE_WAIT_MS },
        );
        harness.detectChanges();

        expect(requiredElement(host, '.shopping-list__summary-title').textContent).toBe(resultingList.name);
        expect(requiredInput(host, 'fd-shopping-list-items-panel input[type="checkbox"]').checked).toBe(true);
        expect(toastError).toHaveBeenCalledTimes(Number(outcome === 'rejected'));
    });
});

describe('Shopping departure while a list name is saving', () => {
    it('retains a queued item edit until the rename settles and uses its new revision to leave safely', async () => {
        const { harness, api } = await renderShoppingAsync();
        const rename = new Subject<ShoppingList>();
        const saving = new Subject<ShoppingList>();
        const renamed = { ...GROCERIES, name: 'Renamed market list', revision: SAVED_REVISION };
        api.update.mockReturnValueOnce(rename).mockReturnValueOnce(saving);
        const host = page(harness);
        chooseSummaryAction(harness, 'SHOPPING_LIST.RENAME_LIST_BUTTON');
        const name = requiredInput(host, '.shopping-list__list-card-rename-input');
        inputText(name, renamed.name);
        name.dispatchEvent(new KeyboardEvent('keydown', { key: 'Enter', bubbles: true, cancelable: true }));
        harness.detectChanges();
        expect(api.update).toHaveBeenCalledExactlyOnceWith(GROCERIES.id, { expectedRevision: FIRST_REVISION, name: renamed.name });
        const purchased = requiredInput(host, 'fd-shopping-list-items-panel input[type="checkbox"]');
        purchased.click();
        harness.detectChanges();

        const router = TestBed.inject(Router);
        await expect(router.navigateByUrl(HOME_URL)).resolves.toBe(false);
        expect(router.url).toBe(SHOPPING_URL);
        expect(purchased.checked).toBe(true);
        expect(api.update).toHaveBeenCalledOnce();

        rename.next(renamed);
        rename.complete();
        const departure = router.navigateByUrl(HOME_URL);
        await waitForShoppingAutosaveAsync(harness, api.update, 2);
        expect(router.url).toBe(SHOPPING_URL);
        expect(api.update).toHaveBeenLastCalledWith(
            GROCERIES.id,
            expect.objectContaining({
                expectedRevision: SAVED_REVISION,
                name: renamed.name,
                items: [expect.objectContaining({ id: GROCERIES.items[0].id, isChecked: true })],
            }),
        );
        saving.next({ ...renamed, items: renamed.items.map(item => ({ ...item, isChecked: true })) });
        saving.complete();
        await expect(departure).resolves.toBe(true);
        expect(router.url).toBe(HOME_URL);
    });
});

describe('Shopping list picker while list changes are pending', () => {
    it.each([
        { viewport: 'desktop', mobile: false },
        { viewport: 'mobile', mobile: true },
    ])('unlocks the $viewport list picker after an inline rename settles', async ({ mobile }) => {
        const overview = firstOverview();
        overview.lists.items = [
            summary(GROCERIES),
            summary(makeList('camping', 'Camping')),
            summary(makeList('pantry', 'Pantry')),
            summary(TRAVEL),
        ];
        const { harness, api } = await renderShoppingAsync(overview, mobile);
        const rename = new Subject<ShoppingList>();
        const host = page(harness);
        const allLists = nativeButton(host, 'SHOPPING_LIST.ALL_LISTS');
        const browserSelector = mobile ? REVIEW_BROWSER : '[role="menu"]';
        expect(allLists.disabled).toBe(false);
        expect(host.querySelector('.shopping-list__list-tabs')?.textContent).not.toContain(TRAVEL.name);

        api.update.mockReturnValueOnce(rename);
        chooseSummaryAction(harness, 'SHOPPING_LIST.RENAME_LIST_BUTTON');
        const name = requiredInput(host, '.shopping-list__list-card-rename-input');
        const newName = 'Weekend groceries';
        inputText(name, newName);
        name.dispatchEvent(new KeyboardEvent('keydown', { key: 'Enter', bubbles: true, cancelable: true }));
        harness.detectChanges();
        expect(api.update).toHaveBeenCalledExactlyOnceWith(GROCERIES.id, { expectedRevision: FIRST_REVISION, name: newName });
        expect(allLists.disabled).toBe(true);
        allLists.click();
        harness.detectChanges();
        expect(document.querySelector(browserSelector)).toBeNull();
        expect(api.getById).not.toHaveBeenCalled();

        rename.next({ ...GROCERIES, name: newName, revision: SAVED_REVISION });
        rename.complete();
        harness.detectChanges();
        expect(allLists.disabled).toBe(false);
        expect(requiredElement(host, '.shopping-list__summary-title').textContent).toBe(newName);
        allLists.click();
        harness.detectChanges();
        nativeButton(requiredElement(document, browserSelector), TRAVEL.name).click();
        await vi.waitFor(() => {
            harness.detectChanges();
            expect(requiredElement(host, '.shopping-list__summary-title').textContent).toBe(TRAVEL.name);
        });
        expect(api.getById).toHaveBeenCalledExactlyOnceWith(TRAVEL.id);
    });
});

describe('Shopping list picker during autosave', () => {
    it.each([
        { viewport: 'desktop', mobile: false },
        { viewport: 'mobile', mobile: true },
    ])('opens a list chosen from the $viewport picker once the existing save completes', async ({ mobile }) => {
        const overview = firstOverview();
        overview.lists.items = [
            summary(GROCERIES),
            summary(makeList('camping', 'Camping')),
            summary(makeList('pantry', 'Pantry')),
            summary(TRAVEL),
        ];
        const { harness, api } = await renderShoppingAsync(overview, mobile);
        const saving = new Subject<ShoppingList>();
        api.update.mockReturnValueOnce(saving);
        const host = page(harness);
        const allLists = nativeButton(host, 'SHOPPING_LIST.ALL_LISTS');

        addNativeItem(harness, CREATED_SHOPPING_ITEM_NAME);
        expect(allLists.disabled).toBe(false);
        allLists.click();
        harness.detectChanges();
        const picker = requiredElement(document, mobile ? REVIEW_BROWSER : '[role="menu"]');
        const chooseTravel = nativeButton(picker, TRAVEL.name);
        await waitForShoppingAutosaveAsync(harness, api.update, 1);
        expect(allLists.disabled).toBe(true);
        expect(chooseTravel.disabled).toBe(false);
        chooseTravel.click();
        await vi.waitFor(() => {
            harness.detectChanges();
            expect(picker.isConnected).toBe(false);
        });

        expect(api.getById).not.toHaveBeenCalled();
        expect(requiredElement(host, '.shopping-list__summary-title').textContent).toBe(GROCERIES.name);
        saving.next(shoppingListWithAddedItem(GROCERIES, 'saved-before-travel', CREATED_SHOPPING_ITEM_NAME, SAVED_REVISION));
        saving.complete();
        await vi.waitFor(() => {
            harness.detectChanges();
            expect(api.getById).toHaveBeenCalledExactlyOnceWith(TRAVEL.id);
            expect(requiredElement(host, '.shopping-list__summary-title').textContent).toBe(TRAVEL.name);
        });
        expect(api.update).toHaveBeenCalledExactlyOnceWith(GROCERIES.id, expect.objectContaining({ expectedRevision: FIRST_REVISION }));
        expect(api.update.mock.calls[0][1].items?.map(item => item.name)).toEqual([GROCERIES.items[0].name, CREATED_SHOPPING_ITEM_NAME]);
        expect(host.textContent).toContain(TRAVEL.items[0].name);
    });
});

describe('Shopping open picker during a revision conflict', () => {
    it.each([
        { viewport: 'desktop', mobile: false },
        { viewport: 'mobile', mobile: true },
    ])('preserves the draft when the open $viewport picker cannot switch lists before a confirmed reload', async ({ mobile }) => {
        const overview = firstOverview();
        overview.lists.items = [
            summary(GROCERIES),
            summary(makeList('camping', 'Camping')),
            summary(makeList('pantry', 'Pantry')),
            summary(TRAVEL),
        ];
        const { harness, api } = await renderShoppingAsync(overview, mobile);
        const saving = new Subject<ShoppingList>();
        api.update.mockReturnValueOnce(saving);
        const host = page(harness);
        const allLists = nativeButton(host, 'SHOPPING_LIST.ALL_LISTS');
        const browserSelector = mobile ? REVIEW_BROWSER : '[role="menu"]';

        addNativeItem(harness, CREATED_SHOPPING_ITEM_NAME);
        allLists.click();
        harness.detectChanges();
        const picker = requiredElement(document, browserSelector);
        await waitForShoppingAutosaveAsync(harness, api.update, 1);
        saving.error(new HttpErrorResponse({ status: 409 }));
        harness.detectChanges();

        expect(requiredElement(host, 'fd-ui-inline-alert').textContent).toContain('SHOPPING_LIST.CONFLICT_MESSAGE');
        expect(allLists.disabled).toBe(true);
        nativeButton(picker, TRAVEL.name).click();
        await vi.waitFor(() => {
            harness.detectChanges();
            expect(picker.isConnected).toBe(false);
        });

        expect(api.getById).not.toHaveBeenCalled();
        expect(requiredElement(host, '.shopping-list__summary-title').textContent).toBe(GROCERIES.name);
        expect(host.textContent).toContain(CREATED_SHOPPING_ITEM_NAME);

        const reloaded = { ...makeList(GROCERIES.id, GROCERIES.name, ['Fresh pears']), revision: SAVED_REVISION };
        api.getById.mockReturnValueOnce(of(reloaded));
        nativeButton(openConflictConfirmation(harness), 'SHOPPING_LIST.CONFLICT_RELOAD').click();
        harness.detectChanges();
        expect(host.querySelector('fd-ui-inline-alert')).toBeNull();
        expect(host.textContent).toContain(reloaded.items[0].name);
        expect(host.textContent).not.toContain(CREATED_SHOPPING_ITEM_NAME);
        expect(allLists.disabled).toBe(false);

        allLists.click();
        harness.detectChanges();
        nativeButton(requiredElement(document, browserSelector), TRAVEL.name).click();
        await vi.waitFor(() => {
            harness.detectChanges();
            expect(requiredElement(host, '.shopping-list__summary-title').textContent).toBe(TRAVEL.name);
        });
        expect(api.getById.mock.calls).toEqual([[GROCERIES.id], [TRAVEL.id]]);
        expect(api.update).toHaveBeenCalledTimes(1);
    });
});

describe('Shopping linked item editor', () => {
    it('validates the amount and saves the corrected amount without overwriting the product identity or aisle', async () => {
        const item = {
            ...GROCERIES.items[0],
            productId: entityId<'product'>('apple-product'),
            unit: MeasurementUnit.PCS,
            amount: 2,
            category: ProductType.Fruit,
            aisle: 'Aisle 7',
        };
        const selected = { ...GROCERIES, items: [item] };
        const { harness, api } = await renderShoppingAsync({
            selectedList: selected,
            lists: { items: [summary(selected)], hasMore: false, nextPage: null },
        });
        TestBed.inject(TranslateService).setTranslation(
            'en',
            { PRODUCT_MANAGE: { PRODUCT_TYPE_OPTIONS: { FRUIT: 'Fruit section' } } },
            true,
        );
        const host = page(harness);
        nativeButton(host, `COMMON.MORE_ACTIONS: ${item.name}`).click();
        harness.detectChanges();
        nativeButton(requiredElement(document, '[role="menu"]'), 'SHOPPING_LIST.EDIT_ITEM').click();
        harness.detectChanges();
        const dialog = requiredElement(document, 'fd-shopping-item-edit');
        const amount = requiredInput(dialog, 'input[aria-label="SHOPPING_LIST.ADD_ITEM_AMOUNT"]');
        expect(requiredInput(dialog, 'input[aria-label="SHOPPING_LIST.ADD_ITEM_NAME"]').readOnly).toBe(true);
        expect(requiredInput(dialog, 'input[aria-label="SHOPPING_LIST.ADD_ITEM_CATEGORY"]').value).toBe('Fruit section');
        expect(requiredElement(dialog, 'fd-ui-select button[aria-haspopup="listbox"]').matches(':disabled')).toBe(true);

        inputText(amount, '0');
        nativeButton(dialog, 'SHOPPING_LIST.SAVE_ITEM').click();
        harness.detectChanges();
        expect(document.querySelector('fd-shopping-item-edit')).toBe(dialog);
        expect(dialog.textContent).toContain('SHOPPING_LIST.AMOUNT_ERROR');
        expect(api.update).not.toHaveBeenCalled();

        const amountAfterEdit = 3;
        const note = 'Very ripe';
        inputText(amount, String(amountAfterEdit));
        inputText(requiredInput(dialog, 'input[aria-label="SHOPPING_LIST.ADD_ITEM_NOTE"]'), `  ${note}  `);
        api.update.mockReturnValueOnce(of({ ...selected, revision: SAVED_REVISION, items: [{ ...item, amount: amountAfterEdit, note }] }));
        nativeButton(dialog, 'SHOPPING_LIST.SAVE_ITEM').click();
        harness.detectChanges();
        expect(document.querySelector('fd-shopping-item-edit')).toBeNull();
        expect(host.querySelector('.shopping-list__item-meta')?.textContent).toContain(note);
        await expect(TestBed.inject(Router).navigateByUrl(HOME_URL)).resolves.toBe(true);

        expect(api.update).toHaveBeenCalledExactlyOnceWith(
            selected.id,
            expect.objectContaining({
                expectedRevision: FIRST_REVISION,
                items: [
                    expect.objectContaining({
                        id: item.id,
                        productId: item.productId,
                        name: item.name,
                        category: item.category,
                        unit: item.unit,
                        aisle: item.aisle,
                        amount: amountAfterEdit,
                        note,
                    }),
                ],
            }),
        );
    });
});

it('lets the user clear a failed first draft without replaying the addition', async () => {
    const empty = makeList('empty-pantry', 'Pantry');
    const { harness, api } = await renderShoppingAsync({
        selectedList: empty,
        lists: { items: [summary(empty)], hasMore: false, nextPage: null },
    });
    const draftName = 'Dried lentils';
    const saving = new Subject<ShoppingList>();
    api.getById.mockReturnValue(of(empty));
    api.update.mockReturnValueOnce(saving).mockReturnValueOnce(of(empty));

    addNativeItem(harness, draftName);
    await waitForShoppingAutosaveAsync(harness, api.update, 1);
    expect(saving.observed).toBe(true);
    expect(api.update).toHaveBeenCalledExactlyOnceWith(
        empty.id,
        expect.objectContaining({
            expectedRevision: empty.revision,
            items: [expect.objectContaining({ id: null, name: draftName })],
        }),
    );
    saving.error(new HttpErrorResponse({ status: HttpStatusCode.ServiceUnavailable }));

    const host = page(harness);
    harness.detectChanges();
    expect(requiredElement(host, 'fd-ui-inline-alert').textContent).toContain('SHOPPING_LIST.SAVE_ERROR');
    expect(host.textContent).toContain(draftName);
    nativeButton(requiredElement(host, '.shopping-list__summary'), 'COMMON.MORE_ACTIONS').click();
    harness.detectChanges();
    const clear = nativeButton(requiredElement(document, '[role="menu"]'), 'SHOPPING_LIST.CLEAR_LIST_BUTTON');
    expect(clear.disabled).toBe(false);
    const count = requiredElement(host, '.shopping-list__list-tab--active .shopping-list__tab-count');
    expect(count.hidden).toBe(false);
    expect(count.textContent.trim()).toBe('1');

    clear.click();
    harness.detectChanges();
    const confirmation = requiredElement(document, 'fd-confirm-delete-dialog');
    nativeButton(confirmation, 'SHOPPING_LIST.CLEAR_LIST_BUTTON').click();
    harness.detectChanges();

    expect(api.update).toHaveBeenCalledTimes(2);
    expect(api.update).toHaveBeenLastCalledWith(empty.id, { expectedRevision: empty.revision, name: empty.name, items: [] });
    expect(api.getById).not.toHaveBeenCalled();
    expect(host.querySelector('fd-ui-inline-alert')).toBeNull();
    expect(host.textContent).not.toContain(draftName);
    expect(requiredElement(host, '.shopping-list__empty-state').textContent).toContain('SHOPPING_LIST.EMPTY_TITLE');
    await expect(TestBed.inject(Router).navigateByUrl(HOME_URL)).resolves.toBe(true);
    expect(api.update).toHaveBeenCalledTimes(2);
});

describe('Shopping whole-list clear', () => {
    it('keeps the active list and locks its item controls until a clear succeeds or fails', async () => {
        const { harness, api, toastError } = await renderShoppingAsync();
        const host = page(harness);
        const pending = new Subject<ShoppingList>();
        api.update.mockReturnValueOnce(pending).mockReturnValueOnce(of({ ...GROCERIES, revision: SAVED_REVISION, items: [] }));

        chooseSummaryAction(harness, 'SHOPPING_LIST.CLEAR_LIST_BUTTON');
        nativeButton(requiredElement(document, 'fd-confirm-delete-dialog'), 'CONFIRM_DELETE.CANCEL').click();
        harness.detectChanges();
        expect(api.update).not.toHaveBeenCalled();
        expect(host.textContent).toContain('Red apples');

        chooseSummaryAction(harness, 'SHOPPING_LIST.CLEAR_LIST_BUTTON');
        nativeButton(requiredElement(document, 'fd-confirm-delete-dialog'), 'SHOPPING_LIST.CLEAR_LIST_BUTTON').click();
        harness.detectChanges();
        const content = requiredElement(host, 'fieldset.shopping-list__content');
        expect(content.hasAttribute('disabled')).toBe(true);
        expect(requiredElement(host, '.shopping-list__workspace').getAttribute('aria-busy')).toBe('true');
        expect(api.update).toHaveBeenCalledExactlyOnceWith(GROCERIES.id, {
            expectedRevision: FIRST_REVISION,
            name: GROCERIES.name,
            items: [],
        });

        pending.error(new Error('Clear unavailable'));
        harness.detectChanges();
        expect(toastError).toHaveBeenCalledExactlyOnceWith('SHOPPING_LIST.CLEAR_ERROR');
        expect(content.hasAttribute('disabled')).toBe(false);
        expect(host.textContent).toContain('Red apples');
        chooseSummaryAction(harness, 'SHOPPING_LIST.CLEAR_LIST_BUTTON');
        nativeButton(requiredElement(document, 'fd-confirm-delete-dialog'), 'SHOPPING_LIST.CLEAR_LIST_BUTTON').click();
        harness.detectChanges();

        expect(api.update).toHaveBeenCalledTimes(2);
        expect(host.textContent).not.toContain('Red apples');
        expect(requiredElement(host, '.shopping-list__summary-title').textContent).toBe(GROCERIES.name);
        expect(host.querySelector('.shopping-list__empty-state')?.textContent).toContain('SHOPPING_LIST.EMPTY_TITLE');
    });

    it('starts the shopping guide from its visible help button', async () => {
        const { harness } = await renderShoppingAsync();
        const start = vi.spyOn(TestBed.inject(FdTourService), 'start').mockReturnValue(true);
        expect(start).not.toHaveBeenCalled();
        nativeButton(page(harness), 'SHOPPING_LIST.TOUR.START_TOUR').click();
        expect(start).toHaveBeenCalledExactlyOnceWith(expect.any(Object), { force: true });
    });
});

describe('Shopping navigation after deliberately discarding a failed draft', () => {
    it.each([
        { action: 'clear', label: 'SHOPPING_LIST.CLEAR_LIST_BUTTON', confirm: 'SHOPPING_LIST.CLEAR_LIST_BUTTON' },
        { action: 'delete', label: 'SHOPPING_LIST.DELETE_LIST_BUTTON', confirm: 'CONFIRM_DELETE.CONFIRM' },
    ] as const)('leaves immediately after a confirmed $action supersedes a failed autosave', async scenario => {
        const { harness, api } = await renderShoppingAsync();
        const host = page(harness);
        const draft = 'Draft almonds';
        api.update.mockReturnValueOnce(throwError(() => new Error('Autosave unavailable')));
        addNativeItem(harness, draft);
        await vi.waitFor(
            () => {
                harness.detectChanges();
                expect(requiredElement(host, 'fd-ui-inline-alert').textContent).toContain('SHOPPING_LIST.SAVE_ERROR');
            },
            { timeout: 3000 },
        );
        expect(api.update).toHaveBeenCalledTimes(1);
        expect(host.textContent).toContain(draft);

        if (scenario.action === 'clear') {
            api.update.mockReturnValueOnce(of({ ...GROCERIES, revision: SAVED_REVISION, items: [] }));
        } else {
            api.deleteById.mockReturnValueOnce(of(void 0));
            api.getPage.mockReturnValueOnce(of([summary(TRAVEL)]));
        }
        chooseSummaryAction(harness, scenario.label);
        nativeButton(requiredElement(document, 'fd-confirm-delete-dialog'), scenario.confirm).click();
        harness.detectChanges();
        expect(host.textContent).not.toContain(draft);
        expect(host.querySelector('fd-ui-inline-alert')).toBeNull();
        expect(requiredElement(host, '.shopping-list__summary-title').textContent).toBe(
            scenario.action === 'clear' ? GROCERIES.name : TRAVEL.name,
        );

        const router = TestBed.inject(Router);
        await expect(router.navigateByUrl(HOME_URL)).resolves.toBe(true);

        expect(router.url).toBe(HOME_URL);
        expect(page(harness).textContent).toContain('Home');
        expect(api.update).toHaveBeenCalledTimes(scenario.action === 'clear' ? 2 : 1);
    });
});

describe('Shopping whole-clear confirmation during autosave', () => {
    it('uses the new revision when autosave starts before an open clear confirmation is approved', async () => {
        const { harness, api } = await renderShoppingAsync();
        const draftName = 'New tin of tomatoes';
        const saving = new Subject<ShoppingList>();
        const saved = shoppingListWithAddedItem(GROCERIES, 'saved-tinned-tomatoes', draftName, SAVED_REVISION);
        api.update.mockReturnValueOnce(saving).mockReturnValueOnce(of({ ...saved, revision: 'c'.repeat(REVISION_LENGTH), items: [] }));

        addNativeItem(harness, draftName);
        chooseSummaryAction(harness, 'SHOPPING_LIST.CLEAR_LIST_BUTTON');
        const confirmation = requiredElement(document, 'fd-confirm-delete-dialog');
        await waitForShoppingAutosaveAsync(harness, api.update, 1);
        expect(api.update).toHaveBeenCalledExactlyOnceWith(GROCERIES.id, expect.objectContaining({ expectedRevision: FIRST_REVISION }));
        expect(api.update.mock.calls[0][1].items?.map(item => item.name)).toEqual([GROCERIES.items[0].name, draftName]);
        nativeButton(confirmation, 'SHOPPING_LIST.CLEAR_LIST_BUTTON').click();
        harness.detectChanges();
        expect(api.update).toHaveBeenCalledOnce();
        expect(page(harness).textContent).toContain(draftName);

        saving.next(saved);
        saving.complete();
        await waitForShoppingAutosaveAsync(harness, api.update, 2);
        expect(api.update).toHaveBeenLastCalledWith(GROCERIES.id, {
            expectedRevision: SAVED_REVISION,
            name: GROCERIES.name,
            items: [],
        });
        expect(page(harness).textContent).not.toContain(draftName);
        expect(page(harness).querySelector('.shopping-list__empty-state')?.textContent).toContain('SHOPPING_LIST.EMPTY_TITLE');
    });

    it('keeps the items after the prerequisite save fails and can retry the draft from the rendered alert', async () => {
        const { harness, api } = await renderShoppingAsync();
        const draftName = 'Canned chickpeas';
        const saving = new Subject<ShoppingList>();
        const saved = shoppingListWithAddedItem(GROCERIES, 'saved-chickpeas', draftName, SAVED_REVISION);
        api.update.mockReturnValueOnce(saving).mockReturnValueOnce(of(saved));

        addNativeItem(harness, draftName);
        chooseSummaryAction(harness, 'SHOPPING_LIST.CLEAR_LIST_BUTTON');
        const confirmation = requiredElement(document, 'fd-confirm-delete-dialog');
        await waitForShoppingAutosaveAsync(harness, api.update, 1);
        nativeButton(confirmation, 'SHOPPING_LIST.CLEAR_LIST_BUTTON').click();
        harness.detectChanges();
        saving.error(new Error('Autosave unavailable'));

        const host = page(harness);
        const alert = await vi.waitFor(() => {
            harness.detectChanges();
            return requiredElement(host, 'fd-ui-inline-alert');
        });
        expect(alert.textContent).toContain('SHOPPING_LIST.SAVE_ERROR');
        expect(host.textContent).toContain(GROCERIES.items[0].name);
        expect(host.textContent).toContain(draftName);
        expect(api.update).toHaveBeenCalledOnce();

        nativeButton(alert, 'ERRORS.RETRY').click();
        harness.detectChanges();
        expect(api.update).toHaveBeenCalledTimes(2);
        expect(api.update).toHaveBeenLastCalledWith(GROCERIES.id, api.update.mock.calls[0][1]);
        expect(host.textContent).toContain(draftName);
        expect(host.querySelector('fd-ui-inline-alert')).toBeNull();
    });
});

describe('Shopping whole-clear conflict and unavailable server revision', () => {
    it('keeps the local draft after an unavailable confirmed reload and clears it against the recovered revision', async () => {
        const { harness, api, toastError } = await renderShoppingAsync();
        const host = page(harness);
        const localItem = 'Unsaved tea';
        const currentServerList = { ...makeList(GROCERIES.id, GROCERIES.name, ['Server pears']), revision: SAVED_REVISION };
        api.update.mockReturnValueOnce(throwError(() => new HttpErrorResponse({ status: 409 })));
        addNativeItem(harness, localItem);
        chooseSummaryAction(harness, 'SHOPPING_LIST.CLEAR_LIST_BUTTON');
        nativeButton(requiredElement(document, 'fd-confirm-delete-dialog'), 'SHOPPING_LIST.CLEAR_LIST_BUTTON').click();
        harness.detectChanges();
        expect(api.update).toHaveBeenCalledExactlyOnceWith(GROCERIES.id, {
            expectedRevision: FIRST_REVISION,
            name: GROCERIES.name,
            items: [],
        });
        expect(requiredElement(host, 'fd-ui-inline-alert').textContent).toContain('SHOPPING_LIST.CONFLICT_MESSAGE');
        expect(host.textContent).toContain(localItem);
        await expect(TestBed.inject(Router).navigateByUrl(HOME_URL)).resolves.toBe(false);

        api.getById.mockReturnValueOnce(of(null)).mockReturnValueOnce(of(currentServerList));
        nativeButton(openConflictConfirmation(harness), 'SHOPPING_LIST.CONFLICT_RELOAD').click();
        harness.detectChanges();
        expect(api.getById).toHaveBeenCalledExactlyOnceWith(GROCERIES.id);
        expect(toastError).toHaveBeenCalledExactlyOnceWith('SHOPPING_LIST.LOAD_ERROR');
        expect(host.textContent).toContain(localItem);
        expect(requiredElement(host, 'fd-ui-inline-alert').textContent).toContain('SHOPPING_LIST.CONFLICT_MESSAGE');

        nativeButton(openConflictConfirmation(harness), 'SHOPPING_LIST.CONFLICT_RELOAD').click();
        harness.detectChanges();
        expect(api.getById.mock.calls).toEqual([[GROCERIES.id], [GROCERIES.id]]);
        expect(host.querySelector('fd-ui-inline-alert')).toBeNull();
        expect(host.textContent).toContain('Server pears');
        expect(host.textContent).not.toContain(localItem);

        const emptyRevision = 'c'.repeat(REVISION_LENGTH);
        api.update.mockReturnValueOnce(of({ ...currentServerList, revision: emptyRevision, items: [] }));
        chooseSummaryAction(harness, 'SHOPPING_LIST.CLEAR_LIST_BUTTON');
        nativeButton(requiredElement(document, 'fd-confirm-delete-dialog'), 'SHOPPING_LIST.CLEAR_LIST_BUTTON').click();
        harness.detectChanges();
        expect(api.update).toHaveBeenLastCalledWith(GROCERIES.id, {
            expectedRevision: SAVED_REVISION,
            name: GROCERIES.name,
            items: [],
        });
        expect(host.querySelector('.shopping-list__empty-state')?.textContent).toContain('SHOPPING_LIST.EMPTY_TITLE');
    });
});

describe('Shopping list creation with a full first page', () => {
    it('keeps the new list active and can open a completed list from the next mobile page', async () => {
        const overview = pagedOverview();
        const { harness, api, toastError } = await renderShoppingAsync(overview, true);
        const created = { ...makeList('dinner-party', 'Dinner Party'), createdAt: utcInstant('2026-11-20T08:00:00Z') };
        const displaced = overview.lists.items[FIRST_PAGE_SIZE - 1];
        const older = {
            ...TRAVEL,
            createdAt: utcInstant('2026-11-18T08:00:00Z'),
            items: TRAVEL.items.map(item => ({ ...item, isChecked: true, checkedOnUtc: CREATED_AT })),
        };
        api.create.mockReturnValueOnce(of(created));
        api.getPage
            .mockReturnValueOnce(of([summary(created), ...overview.lists.items.slice(0, -1)]))
            .mockReturnValueOnce(of([displaced, summary(older)]));
        api.getById.mockReturnValueOnce(of(created)).mockReturnValueOnce(of(older));
        const host = page(harness);

        nativeButton(host, 'SHOPPING_LIST.NEW_LIST_BUTTON').click();
        harness.detectChanges();
        requiredInput(host, '.shopping-list__list-card-rename-input').dispatchEvent(
            new KeyboardEvent('keydown', { key: 'Escape', bubbles: true }),
        );
        harness.detectChanges();
        expect(api.getPage).toHaveBeenCalledOnce();
        expect(api.getById).toHaveBeenCalledExactlyOnceWith(created.id);
        expect(requiredElement(host, '.shopping-list__summary-title').textContent).toBe(created.name);

        const dialog = openMobilePicker(harness);
        expect(dialog.querySelectorAll('.list-browser__row')).toHaveLength(FIRST_PAGE_SIZE);
        expect(nativeButton(dialog, created.name).getAttribute('aria-pressed')).toBe('true');
        expect(dialog.textContent).not.toContain(displaced.name);
        expect(api.getPage).toHaveBeenCalledOnce();

        nativeButton(dialog, 'SHOPPING_LIST.LOAD_MORE').click();
        harness.detectChanges();
        expect(api.getPage.mock.calls).toEqual([[], [2, '']]);
        expect(nativeButton(dialog, displaced.name).getAttribute('aria-pressed')).toBe('false');
        const completed = nativeButton(dialog, 'SHOPPING_LIST.COMPLETED_GROUP');
        expect(completed.getAttribute('aria-expanded')).toBe('false');
        expect(dialog.textContent).not.toContain(older.name);
        completed.click();
        harness.detectChanges();
        const olderRow = nativeButton(dialog, older.name);
        expect(olderRow.textContent).toContain('SHOPPING_LIST.DONE_STATUS');
        olderRow.click();
        await vi.waitFor(() => {
            harness.detectChanges();
            expect(requiredElement(host, '.shopping-list__summary-title').textContent).toBe(older.name);
        });

        const tabs = requiredElement(host, '.shopping-list__list-tabs');
        const selected = nativeButton(tabs, older.name);
        expect(selected.getAttribute('aria-current')).toBe('true');
        expect(selected.querySelector('.shopping-list__tab-count')?.textContent).toBe('✓');
        expect(api.getById.mock.calls).toEqual([[created.id], [older.id]]);
        expect(api.update).not.toHaveBeenCalled();
        expect(toastError).not.toHaveBeenCalled();
    });
});

describe('Shopping directory totals after creating another list', () => {
    const cases = [
        { change: 'remotely completed', initiallyChecked: false, itemCount: 1, remainingCount: 0, tabCount: '✓' },
        { change: 'remotely reopened', initiallyChecked: true, itemCount: 1, remainingCount: 1, tabCount: '1' },
        {
            change: 'legacy summary after the item count changes',
            initiallyChecked: true,
            itemCount: 2,
            remainingCount: undefined,
            tabCount: '…',
        },
        {
            change: 'legacy summary with the same item count',
            initiallyChecked: false,
            itemCount: 1,
            remainingCount: undefined,
            tabCount: '1',
        },
    ];
    it.each(cases)(
        'shows the expected count for a $change list without reloading its rows',
        async ({ initiallyChecked, itemCount, remainingCount, tabCount }) => {
            const initial = {
                ...GROCERIES,
                items: GROCERIES.items.map(item => ({
                    ...item,
                    isChecked: initiallyChecked,
                    checkedOnUtc: initiallyChecked ? CREATED_AT : null,
                })),
            };
            const pantry = makeList('pantry', 'Pantry');
            const created = makeList('spring-market', 'Spring Market');
            const initialOverview: ShoppingListOverview = {
                selectedList: initial,
                lists: { items: [summary(initial), summary(TRAVEL), summary(pantry)], hasMore: false, nextPage: null },
            };
            const { harness, api } = await renderShoppingAsync(initialOverview, true);
            const host = page(harness);
            const groceryTab = (): HTMLButtonElement => nativeButton(requiredElement(host, '.shopping-list__list-tabs'), initial.name);
            expect(groceryTab().querySelector('.shopping-list__tab-count')?.textContent).toBe(initiallyChecked ? '✓' : '1');

            api.create.mockReturnValueOnce(of(created));
            api.getById.mockReturnValueOnce(of(created));
            api.getPage.mockReturnValueOnce(
                of([
                    summary(created),
                    { ...summary(initial), itemsCount: itemCount, remainingCount },
                    { ...summary(TRAVEL), remainingCount: undefined },
                    summary(pantry),
                ]),
            );
            nativeButton(host, 'SHOPPING_LIST.NEW_LIST_BUTTON').click();
            harness.detectChanges();
            const name = requiredInput(host, '.shopping-list__list-card-rename-input');
            name.dispatchEvent(new KeyboardEvent('keydown', { key: 'Escape', bubbles: true }));
            harness.detectChanges();

            expect(requiredElement(host, '.shopping-list__summary-title').textContent).toBe(created.name);
            expect(groceryTab().querySelector('.shopping-list__tab-count')?.textContent).toBe(tabCount);
            expect(api.getById).toHaveBeenCalledExactlyOnceWith(created.id);

            const picker = openMobilePicker(harness);
            expect(nativeButton(picker, TRAVEL.name).querySelector('.list-browser__status')?.textContent).toBe('…');
            if (tabCount === '✓') {
                expect(picker.textContent).not.toContain(initial.name);
                nativeButton(picker, 'SHOPPING_LIST.COMPLETED_GROUP').click();
                harness.detectChanges();
            }
            const status = tabCount === '✓' ? 'SHOPPING_LIST.DONE_STATUS' : tabCount;
            expect(nativeButton(picker, initial.name).querySelector('.list-browser__status')?.textContent).toBe(status);
            expect(api.getPage).toHaveBeenCalledOnce();
            expect(api.update).not.toHaveBeenCalled();
        },
    );
});

describe('Shopping list directory recovery', () => {
    it('keeps an editable first list when the post-create directory is still empty', async () => {
        const overview: ShoppingListOverview = { selectedList: null, lists: { items: [], hasMore: false, nextPage: null } };
        const { harness, api, toastError } = await renderShoppingAsync(overview);
        const directory = new Subject<ShoppingListSummary[]>();
        const created = makeList('new-picnic', 'New list');
        const renamed = { ...created, name: 'Picnic list', revision: SAVED_REVISION };
        api.create.mockReturnValueOnce(of(created));
        api.getPage.mockReturnValueOnce(directory);
        api.update.mockReturnValueOnce(of(renamed));
        const host = page(harness);

        nativeButton(host, 'SHOPPING_LIST.NEW_LIST_BUTTON').click();
        harness.detectChanges();
        expect(api.create).toHaveBeenCalledOnce();
        expect(api.getPage).toHaveBeenCalledOnce();
        directory.next([]);
        directory.complete();
        harness.detectChanges();

        expect(host.textContent).not.toContain('SHOPPING_LIST.NO_LISTS_TITLE');
        const name = requiredInput(host, '.shopping-list__list-card-rename-input');
        expect(name.value).toBe(created.name);
        inputText(name, renamed.name);
        name.dispatchEvent(new KeyboardEvent('keydown', { key: 'Enter', bubbles: true, cancelable: true }));
        harness.detectChanges();

        expect(api.update).toHaveBeenCalledExactlyOnceWith(created.id, { expectedRevision: FIRST_REVISION, name: renamed.name });
        expect(requiredElement(host, '.shopping-list__summary-title').textContent).toBe(renamed.name);
        expect(toastError).not.toHaveBeenCalled();
    });

    it('keeps the current list visible when another list fails to load and retries that selection', async () => {
        const { harness, api, toastError } = await renderShoppingAsync();
        const pending = new Subject<ShoppingList>();
        api.getById.mockReturnValueOnce(pending);
        const host = page(harness);
        const travel = nativeButton(requiredElement(host, '.shopping-list__list-tabs'), TRAVEL.name);

        travel.click();
        await vi.waitFor(() => {
            harness.detectChanges();
            expect(api.getById).toHaveBeenCalledExactlyOnceWith(TRAVEL.id);
            expect(travel.disabled).toBe(true);
        });
        pending.error(new Error('List unavailable'));
        harness.detectChanges();

        expect(toastError).toHaveBeenCalledExactlyOnceWith('SHOPPING_LIST.LOAD_ERROR');
        expect(requiredElement(host, '.shopping-list__summary-title').textContent).toBe(GROCERIES.name);
        expect(host.textContent).toContain(GROCERIES.items[0].name);
        expect(travel.disabled).toBe(false);

        travel.click();
        await vi.waitFor(() => {
            harness.detectChanges();
            expect(requiredElement(host, '.shopping-list__summary-title').textContent).toBe(TRAVEL.name);
        });
        expect(host.textContent).toContain(TRAVEL.items[0].name);
        expect(api.getById.mock.calls).toEqual([[TRAVEL.id], [TRAVEL.id]]);
        expect(api.update).not.toHaveBeenCalled();
    });
});

describe('Shopping mobile list browser', () => {
    it('loads near the scroll end, waits during loading, and requires explicit retry after failure', async () => {
        const { harness, api } = await renderShoppingAsync(pagedOverview(), true);
        const pending = new Subject<ShoppingListSummary[]>();
        api.getPage.mockReturnValueOnce(pending).mockReturnValueOnce(of([summary(TRAVEL)]));
        const dialog = openMobilePicker(harness);
        const results = requiredElement(dialog, '.list-browser__results');
        Object.defineProperties(results, {
            scrollHeight: { value: PAGE_CONTENT_HEIGHT },
            scrollTop: { value: 0, writable: true },
            clientHeight: { value: PAGE_VIEWPORT_HEIGHT },
        });
        results.dispatchEvent(new Event('scroll'));
        expect(api.getPage).not.toHaveBeenCalled();
        results.scrollTop = PAGE_SCROLL_OFFSET;
        results.dispatchEvent(new Event('scroll'));
        results.dispatchEvent(new Event('scroll'));
        expect(api.getPage).toHaveBeenCalledExactlyOnceWith(2, '');
        pending.error(new Error('The next page was unavailable'));
        harness.detectChanges();
        results.dispatchEvent(new Event('scroll'));
        expect(api.getPage).toHaveBeenCalledOnce();

        nativeButton(dialog, 'SHOPPING_LIST.RETRY_LOAD').click();
        harness.detectChanges();
        const travel = Array.from(results.querySelectorAll('button')).find(button => button.textContent.includes(TRAVEL.name));
        if (travel === undefined) {
            throw new Error('Expected the next page to contain the Travel list');
        }
        travel.click();
        await vi.waitFor(() => {
            harness.detectChanges();
            expect(requiredElement(page(harness), '.shopping-list__summary-title').textContent).toBe(TRAVEL.name);
        });
        expect(api.getPage.mock.calls).toEqual([
            [2, ''],
            [2, ''],
        ]);
        expect(document.querySelector(REVIEW_BROWSER)).toBeNull();
    });

    it('creates the trimmed search text as a name after the browser reports no matches', async () => {
        const { harness, api } = await renderShoppingAsync(pagedOverview(), true);
        const result = new Subject<ShoppingListSummary[]>();
        const created = makeList('camping', 'Camping Supplies');
        api.getPage.mockReturnValueOnce(result).mockReturnValueOnce(of([summary(created)]));
        api.getById.mockReturnValueOnce(of(created));
        api.create.mockReturnValueOnce(of(created));
        const dialog = openMobilePicker(harness);
        inputText(requiredInput(dialog, 'input[aria-label="SHOPPING_LIST.SEARCH_LISTS"]'), '  Camping Supplies  ');
        harness.detectChanges();
        await vi.waitFor(() => {
            expect(api.getPage).toHaveBeenCalledExactlyOnceWith(1, 'camping supplies');
        });
        expect(dialog.textContent).not.toContain('SHOPPING_LIST.CREATE_NAMED_LIST');

        result.next([]);
        result.complete();
        harness.detectChanges();
        expect(dialog.textContent).toContain('SHOPPING_LIST.NO_MATCHES');
        nativeButton(dialog, 'SHOPPING_LIST.CREATE_NAMED_LIST').click();
        harness.detectChanges();
        expect(api.create).toHaveBeenCalledExactlyOnceWith({ name: 'Camping Supplies' });
        expect(requiredElement(page(harness), '.shopping-list__summary-title').textContent).toBe('Camping Supplies');
        expect(page(harness).querySelector('.shopping-list__list-card-rename-input')).toBeNull();
    });
});

describe('Shopping mobile picker dismissal', () => {
    it('cancels its pending search from the close button and opens a different list after reopening', async () => {
        const overview = pagedOverview();
        overview.lists.items[1] = summary(TRAVEL);
        const { harness, api } = await renderShoppingAsync(overview, true);
        const searchResult = new Subject<ShoppingListSummary[]>();
        api.getPage.mockReturnValueOnce(searchResult);
        const picker = openMobilePicker(harness);
        const searchSelector = 'input[aria-label="SHOPPING_LIST.SEARCH_LISTS"]';
        inputText(requiredInput(picker, searchSelector), 'Vacation');
        harness.detectChanges();
        await vi.waitFor(() => {
            expect(api.getPage).toHaveBeenCalledExactlyOnceWith(1, 'vacation');
        });
        expect(searchResult.observed).toBe(true);

        nativeButton(picker, 'COMMON.CLOSE').click();
        harness.detectChanges();
        expect(document.querySelector(REVIEW_BROWSER)).toBeNull();
        expect(searchResult.observed).toBe(false);
        expect(page(harness).textContent).toContain(GROCERIES.items[0].name);
        expect(api.getById).not.toHaveBeenCalled();
        expect(api.create).not.toHaveBeenCalled();

        const reopened = openMobilePicker(harness);
        expect(requiredInput(reopened, searchSelector).value).toBe('');
        nativeButton(reopened, TRAVEL.name).click();
        await vi.waitFor(() => {
            harness.detectChanges();
            expect(page(harness).textContent).toContain(TRAVEL.items[0].name);
        });
        expect(document.querySelector(REVIEW_BROWSER)).toBeNull();
        expect(api.getById).toHaveBeenCalledExactlyOnceWith(TRAVEL.id);
        expect(api.update).not.toHaveBeenCalled();
    });
});

describe('Shopping mobile picker request ownership', () => {
    it.each(['search', 'pagination'] as const)('closes the picker and cancels pending %s on departure', async operation => {
        const pending = new Subject<ShoppingListSummary[]>();
        const { harness, api } = await renderShoppingAsync(pagedOverview(), true);
        api.getPage.mockReturnValueOnce(pending);
        const dialog = openMobilePicker(harness);
        if (operation === 'search') {
            inputText(requiredInput(dialog, 'input[aria-label="SHOPPING_LIST.SEARCH_LISTS"]'), '  Travel  ');
            harness.detectChanges();
            await vi.waitFor(() => {
                expect(api.getPage).toHaveBeenCalledExactlyOnceWith(1, 'travel');
            });
        } else {
            const results = requiredElement(dialog, '.list-browser__results');
            Object.defineProperties(results, {
                scrollHeight: { value: PAGE_CONTENT_HEIGHT },
                scrollTop: { value: PAGE_SCROLL_OFFSET },
                clientHeight: { value: PAGE_VIEWPORT_HEIGHT },
            });
            results.dispatchEvent(new Event('scroll'));
            expect(api.getPage).toHaveBeenCalledExactlyOnceWith(2, '');
        }
        expect(pending.observed).toBe(true);

        await harness.navigateByUrl(HOME_URL, ShoppingHomeComponent);
        expect(page(harness).querySelector('h1')?.textContent).toBe('Home');
        expect(dialog.isConnected).toBe(false);
        expect(pending.observed).toBe(false);
        pending.next([summary(TRAVEL)]);
        pending.complete();
        expect(api.create).not.toHaveBeenCalled();
        expect(api.getById).not.toHaveBeenCalled();

        await harness.navigateByUrl(SHOPPING_URL);
        const reopened = openMobilePicker(harness);
        expect(requiredInput(reopened, 'input[aria-label="SHOPPING_LIST.SEARCH_LISTS"]').value).toBe('');
        expect(reopened.textContent).toContain(GROCERIES.name);
        expect(reopened.textContent).not.toContain(TRAVEL.name);
    });
});

describe('Shopping purchased items', () => {
    it('moves a newly checked item into the purchased section and keeps it checked when the section is collapsed', async () => {
        const { harness, api } = await renderShoppingAsync();
        const saved = { ...GROCERIES, revision: SAVED_REVISION, items: GROCERIES.items.map(item => ({ ...item, isChecked: true })) };
        api.update.mockReturnValueOnce(of(saved));
        const host = page(harness);
        requiredInput(host, 'fd-shopping-list-items-panel input[type="checkbox"]').click();
        harness.detectChanges();
        expect(host.querySelector('.shopping-list__purchased')).toBeNull();
        await vi.waitFor(() => {
            harness.detectChanges();
            expect(requiredElement(host, '.shopping-list__purchased').textContent).toContain('Red apples');
        });

        requiredElement(host, '.shopping-list__purchased-toggle button').click();
        harness.detectChanges();
        expect(requiredElement(host, '.shopping-list__purchased').textContent).not.toContain('Red apples');
        requiredElement(host, '.shopping-list__purchased-toggle button').click();
        harness.detectChanges();
        expect(requiredInput(host, '.shopping-list__purchased input[type="checkbox"]').checked).toBe(true);

        await expect(TestBed.inject(Router).navigateByUrl(HOME_URL)).resolves.toBe(true);
        expect(api.update).toHaveBeenCalledExactlyOnceWith(
            GROCERIES.id,
            expect.objectContaining({
                expectedRevision: FIRST_REVISION,
                items: [expect.objectContaining({ name: 'Red apples', isChecked: true })],
            }),
        );
    });

    it('keeps purchased items when the confirmation is cancelled and persists only remaining items after approval', async () => {
        const list = makeList('market', 'Market groceries', ['Red apples', 'Unpurchased pears']);
        const items = list.items.map((item, index) => ({ ...item, isChecked: index === 0 }));
        const selected = { ...list, items };
        const overview: ShoppingListOverview = {
            selectedList: selected,
            lists: { items: [summary(selected)], hasMore: false, nextPage: null },
        };
        const { harness, api } = await renderShoppingAsync(overview);
        const host = page(harness);
        const acquired = requiredElement(host, '.shopping-list__purchased');
        expect(acquired.textContent).toContain('Red apples');

        openPurchasedConfirmation(harness);
        nativeButton(requiredElement(document, 'fd-confirm-delete-dialog'), 'CONFIRM_DELETE.CANCEL').click();
        harness.detectChanges();
        expect(api.update).not.toHaveBeenCalled();
        expect(acquired.textContent).toContain('Red apples');

        const remaining = items.filter(item => !item.isChecked);
        api.update.mockReturnValueOnce(of({ ...selected, revision: SAVED_REVISION, items: remaining }));
        openPurchasedConfirmation(harness);
        nativeButton(requiredElement(document, 'fd-confirm-delete-dialog'), 'SHOPPING_LIST.CLEAR_PURCHASED').click();
        harness.detectChanges();
        expect(host.querySelector('.shopping-list__purchased')).toBeNull();
        expect(host.textContent).toContain('Unpurchased pears');

        await expect(TestBed.inject(Router).navigateByUrl(HOME_URL)).resolves.toBe(true);
        expect(api.update).toHaveBeenCalledExactlyOnceWith(
            selected.id,
            expect.objectContaining({
                expectedRevision: FIRST_REVISION,
                items: [expect.objectContaining({ id: remaining[0].id, name: 'Unpurchased pears', isChecked: false })],
            }),
        );
    });
});

function openPurchasedConfirmation(harness: RouterTestingHarness): void {
    nativeButton(requiredElement(page(harness), '.shopping-list__summary'), 'COMMON.MORE_ACTIONS').click();
    harness.detectChanges();
    nativeButton(requiredElement(document, '[role="menu"]'), 'SHOPPING_LIST.CLEAR_PURCHASED').click();
    harness.detectChanges();
}

describe('Shopping purchased confirmation during the first item save', () => {
    it.each([true, false])('uses the persisted ID when confirmation is accepted=%s', async accept => {
        const { harness, api } = await renderShoppingAsync();
        const write = new Subject<ShoppingList>();
        const saved = purchasedDraftResult();
        const cleared = { ...GROCERIES, revision: 'c'.repeat(REVISION_LENGTH) };
        api.update.mockReturnValueOnce(write).mockReturnValueOnce(of(cleared));
        addAndCheckShoppingDraft(harness);
        openPurchasedConfirmation(harness);
        await waitForShoppingAutosaveAsync(harness, api.update, 1);
        expect(api.update).toHaveBeenCalledExactlyOnceWith(GROCERIES.id, expect.objectContaining({ expectedRevision: FIRST_REVISION }));
        expect(api.update.mock.calls[0][1].items?.find(item => item.name === CREATED_SHOPPING_ITEM_NAME)).toMatchObject({
            id: null,
            isChecked: true,
        });

        write.next(saved);
        write.complete();
        const dialog = await waitForPurchasedConfirmationAsync(harness);
        nativeButton(dialog, accept ? 'SHOPPING_LIST.CLEAR_PURCHASED' : 'CONFIRM_DELETE.CANCEL').click();
        harness.detectChanges();

        expect(page(harness).textContent.includes(CREATED_SHOPPING_ITEM_NAME)).toBe(!accept);
        expect(page(harness).textContent).toContain(GROCERIES.items[0].name);
        await expect(TestBed.inject(Router).navigateByUrl(HOME_URL)).resolves.toBe(true);
        expect(api.update).toHaveBeenCalledTimes(accept ? 2 : 1);
        if (accept) {
            expect(api.update).toHaveBeenLastCalledWith(
                GROCERIES.id,
                expect.objectContaining({
                    expectedRevision: SAVED_REVISION,
                    items: [expect.objectContaining({ id: GROCERIES.items[0].id, isChecked: false })],
                }),
            );
        }
    });

    it('keeps the failed draft without showing a stale confirmation and can retry the clear action', async () => {
        const { harness, api } = await renderShoppingAsync();
        const write = new Subject<ShoppingList>();
        api.update.mockReturnValueOnce(write).mockReturnValueOnce(of(purchasedDraftResult()));
        addAndCheckShoppingDraft(harness);
        openPurchasedConfirmation(harness);
        await waitForShoppingAutosaveAsync(harness, api.update, 1);
        write.error(new Error('Purchase status not saved'));
        await vi.waitFor(() => {
            harness.detectChanges();
            expect(page(harness).querySelector('fd-ui-inline-alert')?.textContent).toContain('SHOPPING_LIST.SAVE_ERROR');
        });
        expect(page(harness).textContent).toContain(CREATED_SHOPPING_ITEM_NAME);
        expect(document.querySelector('fd-confirm-delete-dialog')).toBeNull();

        openPurchasedConfirmation(harness);
        const dialog = await waitForPurchasedConfirmationAsync(harness);
        nativeButton(dialog, 'CONFIRM_DELETE.CANCEL').click();
        harness.detectChanges();
        expect(api.update).toHaveBeenCalledTimes(2);
        expect(api.update).toHaveBeenLastCalledWith(GROCERIES.id, api.update.mock.calls[0][1]);
        expect(page(harness).textContent).toContain(CREATED_SHOPPING_ITEM_NAME);
    });
});

describe('Shopping purchased confirmation after undoing the last check', () => {
    it('saves the unchecked item and leaves the list intact without asking to clear it', async () => {
        const { harness, api } = await renderShoppingAsync();
        const firstWrite = new Subject<ShoppingList>();
        const secondWrite = new Subject<ShoppingList>();
        const checked = purchasedDraftResult();
        const unchecked = {
            ...checked,
            revision: 'c'.repeat(REVISION_LENGTH),
            items: checked.items.map(item => ({ ...item, isChecked: false, checkedOnUtc: null })),
        };
        api.update.mockReturnValueOnce(firstWrite).mockReturnValueOnce(secondWrite);
        addAndCheckShoppingDraft(harness);
        openPurchasedConfirmation(harness);
        await waitForShoppingAutosaveAsync(harness, api.update, 1);
        expect(api.update.mock.calls[0][1].items?.find(item => item.name === CREATED_SHOPPING_ITEM_NAME)).toMatchObject({
            isChecked: true,
        });

        const checkbox = draftItemCheckbox(harness);
        expect(checkbox.checked).toBe(true);
        expect(checkbox.matches(':enabled')).toBe(true);
        checkbox.click();
        harness.detectChanges();
        expect(draftItemCheckbox(harness).checked).toBe(false);
        expect(document.querySelector('fd-confirm-delete-dialog')).toBeNull();

        firstWrite.next(checked);
        firstWrite.complete();
        await waitForShoppingAutosaveAsync(harness, api.update, 2);
        const [listId, payload] = api.update.mock.calls[1];
        expect(listId).toBe(checked.id);
        expect(payload.expectedRevision).toBe(checked.revision);
        expect(payload.items?.find(item => item.name === CREATED_SHOPPING_ITEM_NAME)).toMatchObject({
            id: checked.items[1].id,
            isChecked: false,
        });
        expect(document.querySelector('fd-confirm-delete-dialog')).toBeNull();

        secondWrite.next(unchecked);
        secondWrite.complete();
        await harness.fixture.whenStable();
        harness.detectChanges();

        expect(document.querySelector('fd-confirm-delete-dialog')).toBeNull();
        expect(page(harness).textContent).toContain(GROCERIES.items[0].name);
        expect(draftItemCheckbox(harness).checked).toBe(false);
        nativeButton(requiredElement(page(harness), '.shopping-list__summary'), 'COMMON.MORE_ACTIONS').click();
        harness.detectChanges();
        expect(nativeButton(requiredElement(document, '[role="menu"]'), 'SHOPPING_LIST.CLEAR_PURCHASED').disabled).toBe(true);
        expect(api.update).toHaveBeenCalledTimes(2);
    });
});

function addAndCheckShoppingDraft(harness: RouterTestingHarness): void {
    addNativeItem(harness, CREATED_SHOPPING_ITEM_NAME);
    draftItemCheckbox(harness).click();
    harness.detectChanges();
}

function draftItemCheckbox(harness: RouterTestingHarness): HTMLInputElement {
    const row = Array.from(page(harness).querySelectorAll<HTMLElement>('.shopping-list__item')).find(item =>
        item.textContent.includes(CREATED_SHOPPING_ITEM_NAME),
    );
    if (row === undefined) {
        throw new Error('Expected a new shopping item');
    }
    return requiredInput(row, 'input[type="checkbox"]');
}

describe('Shopping purchased confirmation route ownership', () => {
    it('leaves no purchased confirmation or follow-up write when navigation waits for the same pending save', async () => {
        const { harness, api } = await renderShoppingAsync();
        const write = new Subject<ShoppingList>();
        api.update.mockReturnValueOnce(write);
        addAndCheckShoppingDraft(harness);
        openPurchasedConfirmation(harness);
        await waitForShoppingAutosaveAsync(harness, api.update, 1);

        const router = TestBed.inject(Router);
        const guardStarted = firstValueFrom(router.events.pipe(filter(event => event instanceof GuardsCheckStart)));
        const navigation = router.navigateByUrl(HOME_URL);
        await guardStarted;
        expect(router.url).toBe(SHOPPING_URL);
        write.next(purchasedDraftResult());
        write.complete();
        await expect(navigation).resolves.toBe(true);
        harness.detectChanges();

        expect(TestBed.inject(Router).url).toBe(HOME_URL);
        expect(document.querySelector('fd-confirm-delete-dialog')).toBeNull();
        expect(api.update).toHaveBeenCalledOnce();
    });
});

describe('Shopping purchased confirmation while creating another list', () => {
    it('keeps the new list action active without opening a confirmation for the previous list', async () => {
        const { harness, api } = await renderShoppingAsync();
        const save = new Subject<ShoppingList>();
        const creation = new Subject<ShoppingList>();
        const saved = purchasedDraftResult();
        const created = makeList('weekend', 'Weekend market');
        api.update.mockReturnValueOnce(save);
        api.create.mockReturnValueOnce(creation);
        api.getPage.mockReturnValueOnce(of([summary(created), summary(saved), summary(TRAVEL)]));

        addAndCheckShoppingDraft(harness);
        openPurchasedConfirmation(harness);
        await waitForShoppingAutosaveAsync(harness, api.update, 1);
        expect(document.querySelector('fd-confirm-delete-dialog')).toBeNull();

        const newList = nativeButton(page(harness), 'SHOPPING_LIST.NEW_LIST_BUTTON');
        expect(newList.disabled).toBe(false);
        newList.click();
        harness.detectChanges();
        expect(api.create).not.toHaveBeenCalled();
        expect(newList.disabled).toBe(true);

        save.next(saved);
        save.complete();
        await vi.waitFor(() => {
            harness.detectChanges();
            expect(api.create).toHaveBeenCalledOnce();
        });
        await harness.fixture.whenStable();
        harness.detectChanges();
        expect(creation.observed).toBe(true);
        const confirmationDuringCreation = document.querySelector('fd-confirm-delete-dialog');

        creation.next(created);
        creation.complete();
        await vi.waitFor(() => {
            harness.detectChanges();
            const activeList = requiredElement(page(harness), '.shopping-list__list-tab button[aria-current="true"]');
            expect(activeList.textContent).toContain(created.name);
        });
        expect(api.update).toHaveBeenCalledOnce();
        const [updatedListId, update] = api.update.mock.calls[0];
        expect(updatedListId).toBe(saved.id);
        expect(update.expectedRevision).toBe(FIRST_REVISION);
        expect(update.items?.find(item => item.name === CREATED_SHOPPING_ITEM_NAME)?.isChecked).toBe(true);
        expect(confirmationDuringCreation).toBeNull();
        expect(document.querySelector('fd-confirm-delete-dialog')).toBeNull();
    });
});

function purchasedDraftResult(): ShoppingList {
    const saved = shoppingListWithAddedItem(GROCERIES, 'purchased-beans', CREATED_SHOPPING_ITEM_NAME, SAVED_REVISION);
    return { ...saved, items: saved.items.map(item => ({ ...item, isChecked: item.name === CREATED_SHOPPING_ITEM_NAME })) };
}

async function waitForPurchasedConfirmationAsync(harness: RouterTestingHarness): Promise<HTMLElement> {
    return vi.waitFor(() => {
        harness.detectChanges();
        return requiredElement(document, 'fd-confirm-delete-dialog');
    });
}

describe('Shopping revision conflict recovery', () => {
    it('keeps the draft through cancellation and a network failure, then saves with the newly confirmed server revision', async () => {
        const { harness, api, toastError } = await renderShoppingAsync();
        const host = page(harness);
        const router = TestBed.inject(Router);
        const serverList = { ...makeList('groceries', 'Groceries', ['Server pears']), revision: SAVED_REVISION };
        api.update.mockReturnValueOnce(throwError(() => new HttpErrorResponse({ status: 409 })));
        addNativeItem(harness, 'Local peaches');
        await expect(router.navigateByUrl(HOME_URL)).resolves.toBe(false);
        harness.detectChanges();
        expect(router.url).toBe(SHOPPING_URL);
        expect(requiredElement(host, 'fd-ui-inline-alert').textContent).toContain('SHOPPING_LIST.CONFLICT_MESSAGE');
        expect(nativeButton(host, 'SHOPPING_LIST.NEW_LIST_BUTTON').disabled).toBe(true);
        expect(host.textContent).toContain('Local peaches');

        const cancel = openConflictConfirmation(harness);
        nativeButton(cancel, 'CONFIRM_DELETE.CANCEL').click();
        harness.detectChanges();
        expect(api.getById).not.toHaveBeenCalled();
        expect(host.textContent).toContain('Local peaches');

        api.getById.mockReturnValueOnce(throwError(() => new Error('Reload unavailable')));
        nativeButton(openConflictConfirmation(harness), 'SHOPPING_LIST.CONFLICT_RELOAD').click();
        harness.detectChanges();
        expect(toastError).toHaveBeenCalledWith('SHOPPING_LIST.LOAD_ERROR');
        expect(requiredElement(host, 'fd-ui-inline-alert').textContent).toContain('SHOPPING_LIST.CONFLICT_MESSAGE');
        expect(host.textContent).toContain('Local peaches');

        api.getById.mockReturnValueOnce(of(serverList));
        nativeButton(openConflictConfirmation(harness), 'SHOPPING_LIST.CONFLICT_RELOAD').click();
        harness.detectChanges();
        expect(api.getById.mock.calls).toEqual([[GROCERIES.id], [GROCERIES.id]]);
        expect(host.querySelector('fd-ui-inline-alert')).toBeNull();
        expect(host.textContent).toContain('Server pears');
        expect(host.textContent).not.toContain('Local peaches');

        addNativeItem(harness, 'Fresh oranges');
        api.update.mockReturnValueOnce(of(serverList));
        await expect(router.navigateByUrl(HOME_URL)).resolves.toBe(true);
        expect(api.update).toHaveBeenLastCalledWith(
            GROCERIES.id,
            expect.objectContaining({
                expectedRevision: SAVED_REVISION,
                items: [expect.objectContaining({ name: 'Server pears' }), expect.objectContaining({ name: 'Fresh oranges' })],
            }),
        );
    });
});

describe('Shopping confirmation while a conflict reload is pending', () => {
    it('does not open a duplicate confirmation and keeps the draft until a retry reloads the list', async () => {
        const { harness, api, toastError } = await renderShoppingAsync();
        const pending = new Subject<ShoppingList>();
        const serverList = { ...makeList('groceries', 'Groceries', ['Server pears']), revision: SAVED_REVISION };
        api.update.mockReturnValueOnce(throwError(() => new HttpErrorResponse({ status: 409 })));
        api.getById.mockReturnValueOnce(pending).mockReturnValueOnce(of(serverList));
        addNativeItem(harness, 'Local peaches');
        await expect(TestBed.inject(Router).navigateByUrl(HOME_URL)).resolves.toBe(false);
        harness.detectChanges();

        nativeButton(openConflictConfirmation(harness), 'SHOPPING_LIST.CONFLICT_RELOAD').click();
        harness.detectChanges();
        expect(pending.observed).toBe(true);
        const host = page(harness);
        const alert = requiredElement(host, 'fd-ui-inline-alert');
        nativeButton(alert, 'SHOPPING_LIST.CONFLICT_RELOAD').click();
        harness.detectChanges();

        expect(document.querySelector('fd-confirm-delete-dialog')).toBeNull();
        expect(api.getById).toHaveBeenCalledExactlyOnceWith(GROCERIES.id);
        expect(host.textContent).toContain('Local peaches');

        pending.error(new Error('Reload unavailable'));
        harness.detectChanges();
        expect(toastError).toHaveBeenCalledWith('SHOPPING_LIST.LOAD_ERROR');
        expect(host.textContent).toContain('Local peaches');

        nativeButton(openConflictConfirmation(harness), 'SHOPPING_LIST.CONFLICT_RELOAD').click();
        harness.detectChanges();
        expect(api.getById.mock.calls).toEqual([[GROCERIES.id], [GROCERIES.id]]);
        expect(host.querySelector('fd-ui-inline-alert')).toBeNull();
        expect(host.textContent).toContain('Server pears');
        expect(host.textContent).not.toContain('Local peaches');
        expect(api.update).toHaveBeenCalledOnce();
    });
});

describe('Shopping list rename draft during conflict recovery', () => {
    it('keeps both drafts through cancellation and reload failure, then discards them after a successful reload', async () => {
        const { harness, api, toastError } = await renderShoppingAsync();
        const host = page(harness);
        const saving = new Subject<ShoppingList>();
        api.update.mockReturnValueOnce(saving);
        addNativeItem(harness, CREATED_SHOPPING_ITEM_NAME);
        chooseSummaryAction(harness, 'SHOPPING_LIST.RENAME_LIST_BUTTON');

        const nameDraft = 'Local new list name';
        const editorSelector = '.shopping-list__list-card-rename-input';
        const editor = requiredInput(host, editorSelector);
        inputText(editor, nameDraft);
        editor.focus();
        await waitForShoppingAutosaveAsync(harness, api.update, 1);
        expect(api.update).toHaveBeenCalledExactlyOnceWith(GROCERIES.id, expect.objectContaining({ name: GROCERIES.name }));

        saving.error(new HttpErrorResponse({ status: 409 }));
        harness.detectChanges();
        editor.blur();
        harness.detectChanges();
        expect(requiredInput(host, editorSelector).value).toBe(nameDraft);
        expect(host.textContent).toContain(CREATED_SHOPPING_ITEM_NAME);
        expect(api.update).toHaveBeenCalledTimes(1);

        nativeButton(openConflictConfirmation(harness), 'CONFIRM_DELETE.CANCEL').click();
        harness.detectChanges();
        expect(requiredInput(host, editorSelector).value).toBe(nameDraft);
        expect(api.getById).not.toHaveBeenCalled();

        api.getById.mockReturnValueOnce(throwError(() => new Error('Reload unavailable')));
        nativeButton(openConflictConfirmation(harness), 'SHOPPING_LIST.CONFLICT_RELOAD').click();
        harness.detectChanges();
        expect(toastError).toHaveBeenCalledExactlyOnceWith('SHOPPING_LIST.LOAD_ERROR');
        expect(requiredInput(host, editorSelector).value).toBe(nameDraft);
        expect(host.textContent).toContain(CREATED_SHOPPING_ITEM_NAME);

        const serverList = { ...makeList(GROCERIES.id, 'Saved market list', ['Fresh pears']), revision: SAVED_REVISION };
        api.getById.mockReturnValueOnce(of(serverList));
        nativeButton(openConflictConfirmation(harness), 'SHOPPING_LIST.CONFLICT_RELOAD').click();
        harness.detectChanges();

        expect(api.getById.mock.calls).toEqual([[GROCERIES.id], [GROCERIES.id]]);
        expect(host.querySelector('fd-ui-inline-alert')).toBeNull();
        expect(host.querySelector(editorSelector)).toBeNull();
        expect(requiredElement(host, '.shopping-list__summary-title').textContent).toBe(serverList.name);
        expect(host.textContent).toContain(serverList.items[0].name);
        expect(host.textContent).not.toContain(CREATED_SHOPPING_ITEM_NAME);

        chooseSummaryAction(harness, 'SHOPPING_LIST.RENAME_LIST_BUTTON');
        expect(requiredInput(host, editorSelector).value).toBe(serverList.name);
        expect(api.update).toHaveBeenCalledTimes(1);
    });
});

describe('Shopping list menu when autosave starts', () => {
    it.each([
        { action: 'SHOPPING_LIST.MERGE_TITLE', dialog: 'fd-shopping-merge-preview' },
        { action: 'SHOPPING_LIST.CLEAR_LIST_BUTTON', dialog: 'fd-confirm-delete-dialog' },
        { action: 'SHOPPING_LIST.DELETE_LIST_BUTTON', dialog: 'fd-confirm-delete-dialog' },
        { action: 'SHOPPING_LIST.CLEAR_PURCHASED', dialog: 'fd-confirm-delete-dialog' },
    ])('ignores $action from the open menu until the current save settles', async ({ action, dialog }) => {
        const list = mergeableList();
        const { harness, api } = await renderShoppingAsync({
            selectedList: list,
            lists: { items: [summary(list)], hasMore: false, nextPage: null },
        });
        const save = new Subject<ShoppingList>();
        api.update.mockReturnValueOnce(save);
        const host = page(harness);
        const autosaveDebounceMs = 500;

        vi.useFakeTimers({ toFake: ['setInterval', 'clearInterval'] });
        try {
            requiredInput(host, '.shopping-list__item:nth-child(3) input[type="checkbox"]').click();
            harness.detectChanges();
            nativeButton(requiredElement(host, '.shopping-list__summary'), 'COMMON.MORE_ACTIONS').click();
            harness.detectChanges();
            const originalAction = nativeButton(requiredElement(document, '[role="menu"]'), action);
            expect(api.update).not.toHaveBeenCalled();

            vi.advanceTimersByTime(autosaveDebounceMs);
            expect(api.update).toHaveBeenCalledOnce();
            expect(save.observed).toBe(true);
            expect(originalAction.isConnected).toBe(true);
            expect(originalAction.disabled).toBe(false);
            originalAction.click();
            harness.detectChanges();
            expect(document.querySelector(dialog)).toBeNull();
            expect(api.update).toHaveBeenCalledOnce();

            save.next({ ...list, revision: SAVED_REVISION, items: list.items.map((item, index) => ({ ...item, isChecked: index === 2 })) });
            save.complete();
        } finally {
            vi.useRealTimers();
        }

        harness.detectChanges();
        chooseSummaryAction(harness, action);
        expect(document.querySelector(dialog)).not.toBeNull();
        expect(api.update).toHaveBeenCalledOnce();
    });
});

describe('Shopping consolidation preview', () => {
    it('offers dismissal only when no items can be combined', async () => {
        const { harness, api } = await renderShoppingAsync();
        const dialog = openMergePreview(harness);
        expect(dialog.textContent).toContain('SHOPPING_LIST.MERGE_EMPTY');
        expect(dialog.textContent).not.toContain('SHOPPING_LIST.MERGE_CONFIRM');
        nativeButton(dialog, 'PUBLIC_RECIPES.SHOPPING_CANCEL').click();
        harness.detectChanges();
        expect(document.querySelector('fd-shopping-merge-preview')).toBeNull();
        expect(api.update).not.toHaveBeenCalled();
    });

    it('asks for a fresh preview if an autosave finishes while it is open, then persists the newly confirmed merge', async () => {
        const list = mergeableList();
        const { harness, api, toastError } = await renderShoppingAsync({
            selectedList: list,
            lists: { items: [summary(list)], hasMore: false, nextPage: null },
        });
        const host = page(harness);
        const firstSave = new Subject<ShoppingList>();
        api.update.mockReturnValueOnce(firstSave);
        requiredInput(host, '.shopping-list__item:nth-child(3) input[type="checkbox"]').click();
        harness.detectChanges();
        const unconfirmed = openMergePreview(harness);
        expect(requiredElement(unconfirmed, 'li').textContent).toContain('2 + 3');
        expect(requiredElement(unconfirmed, 'li').textContent).toContain('→ 5');
        await vi.waitFor(
            () => {
                expect(api.update).toHaveBeenCalledOnce();
            },
            { timeout: 3000 },
        );

        const saved = { ...list, revision: SAVED_REVISION, items: list.items.map((item, index) => ({ ...item, isChecked: index === 2 })) };
        firstSave.next(saved);
        firstSave.complete();
        harness.detectChanges();
        nativeButton(unconfirmed, 'SHOPPING_LIST.MERGE_CONFIRM').click();
        harness.detectChanges();
        expect(toastError).toHaveBeenCalledExactlyOnceWith('SHOPPING_LIST.MERGE_STALE');
        await vi.waitFor(() => {
            harness.detectChanges();
            expect(requiredElement(host, '.shopping-list__purchased').textContent).toContain('Oat milk');
        });
        const pendingRows = '.shopping-list__items:not(.shopping-list__purchased) > .shopping-list__item';
        expect(host.querySelectorAll(pendingRows)).toHaveLength(2);
        expect(api.update).toHaveBeenCalledOnce();

        const refreshed = openMergePreview(harness);
        nativeButton(refreshed, 'SHOPPING_LIST.MERGE_CONFIRM').click();
        harness.detectChanges();
        expect(host.querySelectorAll(pendingRows)).toHaveLength(1);
        expect(requiredElement(host, '.shopping-list__purchased').textContent).toContain('Oat milk');
        api.update.mockReturnValueOnce(of(saved));
        await expect(TestBed.inject(Router).navigateByUrl(HOME_URL)).resolves.toBe(true);
        expect(api.update).toHaveBeenCalledTimes(2);
        expect(api.update).toHaveBeenLastCalledWith(
            list.id,
            expect.objectContaining({
                expectedRevision: SAVED_REVISION,
                items: [
                    expect.objectContaining({ name: 'Pear', amount: 5 }),
                    expect.objectContaining({ name: 'Oat milk', isChecked: true }),
                ],
            }),
        );
    });
});

describe('Shopping freeform entries through the native list', () => {
    it('keeps blank quick adds out of autosave and persists a corrected trimmed name', async () => {
        const { harness, api } = await renderShoppingAsync();
        const host = page(harness);
        const name = requiredInput(host, 'input[aria-label="SHOPPING_LIST.ADD_ITEM_NAME"]');
        const submit = nativeButton(host, 'SHOPPING_LIST.ADD_ITEM_BUTTON');
        inputText(name, '   ');
        submit.click();
        harness.detectChanges();
        expect(name.value).toBe('   ');
        expect(host.querySelectorAll('.shopping-list__items > .shopping-list__item')).toHaveLength(1);
        expect(api.update).not.toHaveBeenCalled();

        const corrected = 'Green pears';
        inputText(name, `  ${corrected}  `);
        submit.click();
        harness.detectChanges();
        expect(host.textContent).toContain(corrected);
        const saved = { ...makeList(GROCERIES.id, GROCERIES.name, [GROCERIES.items[0].name, corrected]), revision: SAVED_REVISION };
        api.update.mockReturnValueOnce(of(saved));
        await expect(TestBed.inject(Router).navigateByUrl(HOME_URL)).resolves.toBe(true);
        expect(api.update).toHaveBeenCalledOnce();
        expect(api.update.mock.calls[0][1].items?.map(item => item.name)).toEqual([GROCERIES.items[0].name, corrected]);
    });

    it('merges duplicate text quantities without merging a different quantity note', async () => {
        const draft = makeList(GROCERIES.id, GROCERIES.name, ['Sea salt', 'Sea salt', 'Sea salt']);
        const [first, duplicate, different] = draft.items;
        const list = {
            ...draft,
            items: [
                { ...first, note: 'to taste' },
                { ...duplicate, note: 'to taste' },
                { ...different, note: 'optional' },
            ],
        };
        const { harness, api } = await renderShoppingAsync({
            selectedList: list,
            lists: { items: [summary(list)], hasMore: false, nextPage: null },
        });
        const preview = openMergePreview(harness);
        expect(preview.querySelectorAll('li')).toHaveLength(1);
        expect(requiredElement(preview, 'li').textContent).toContain('to taste + to taste → to taste');
        const saved = { ...list, items: [list.items[0], list.items[2]], revision: SAVED_REVISION };
        api.update.mockReturnValueOnce(of(saved));
        nativeButton(preview, 'SHOPPING_LIST.MERGE_CONFIRM').click();
        harness.detectChanges();
        expect(page(harness).querySelectorAll('.shopping-list__items > .shopping-list__item')).toHaveLength(2);
        await expect(TestBed.inject(Router).navigateByUrl(HOME_URL)).resolves.toBe(true);
        expect(api.update).toHaveBeenCalledOnce();
        expect(api.update.mock.calls[0][1].items?.map(({ id, amount, note }) => ({ id, amount, note }))).toEqual([
            { id: first.id, amount: null, note: 'to taste' },
            { id: different.id, amount: null, note: 'optional' },
        ]);
    });
});

describe('Shopping existing confirmations after a revision conflict', () => {
    const actions = [
        { action: 'merge', open: openMergePreview, confirm: 'SHOPPING_LIST.MERGE_CONFIRM' },
        {
            action: 'clear',
            open: (harness: RouterTestingHarness): HTMLElement => openShoppingListConfirmation(harness, 'SHOPPING_LIST.CLEAR_LIST_BUTTON'),
            confirm: 'SHOPPING_LIST.CLEAR_LIST_BUTTON',
        },
        {
            action: 'delete',
            open: (harness: RouterTestingHarness): HTMLElement => openShoppingListConfirmation(harness, 'SHOPPING_LIST.DELETE_LIST_BUTTON'),
            confirm: 'CONFIRM_DELETE.CONFIRM',
        },
    ];

    it.each(actions)('does not $action the unresolved list from an already open confirmation', async ({ open, confirm }) => {
        const list = mergeableList();
        const { harness, api } = await renderShoppingAsync({
            selectedList: list,
            lists: { items: [summary(list)], hasMore: false, nextPage: null },
        });
        const host = page(harness);
        const saving = new Subject<ShoppingList>();
        api.update.mockReturnValueOnce(saving);
        requiredInput(host, '.shopping-list__item:nth-child(3) input[type="checkbox"]').click();
        harness.detectChanges();

        const confirmation = open(harness);
        await waitForShoppingAutosaveAsync(harness, api.update, 1);
        expect(confirmation.isConnected).toBe(true);
        saving.error(new HttpErrorResponse({ status: 409 }));
        harness.detectChanges();
        expect(requiredElement(host, 'fd-ui-inline-alert').textContent).toContain('SHOPPING_LIST.CONFLICT_MESSAGE');

        nativeButton(confirmation, confirm).click();
        harness.detectChanges();
        expect(confirmation.isConnected).toBe(false);
        expect(api.update).toHaveBeenCalledExactlyOnceWith(list.id, expect.objectContaining({ expectedRevision: FIRST_REVISION }));
        expect(api.deleteById).not.toHaveBeenCalled();
        expect(requiredElement(host, '.shopping-list__summary-title').textContent).toBe(list.name);
        expect(requiredElement(host, '.shopping-list__purchased').textContent).toContain('Oat milk');
        expect(host.querySelectorAll('.shopping-list__item')).toHaveLength(list.items.length);
        expect(requiredElement(host, 'fd-ui-inline-alert').textContent).toContain('SHOPPING_LIST.CONFLICT_MESSAGE');
    });
});

it('shows localized fractional quantities and persists a combined quantity only after confirmation', async () => {
    const source = makeList('dried-fruit', 'Market', ['Raisins', 'Raisins']);
    const firstAmount = 1.25;
    const secondAmount = 2.5;
    const combinedAmount = 3.75;
    const first = { ...source.items[0], amount: firstAmount, unit: MeasurementUnit.G };
    const second = { ...source.items[1], amount: secondAmount, unit: MeasurementUnit.G };
    const list = { ...source, items: [first, second] };
    const { harness, api } = await renderShoppingAsync({
        selectedList: list,
        lists: { items: [summary(list)], hasMore: false, nextPage: null },
    });
    const translate = TestBed.inject(TranslateService);
    translate.setTranslation('ru', { GENERAL: { UNITS: { G: 'г' } } });
    await firstValueFrom(translate.use('ru'));
    harness.detectChanges();

    const preview = openMergePreview(harness);
    expect(requiredElement(preview, 'li div').textContent.trim()).toBe('1,25 г + 2,5 г → 3,75 г');
    nativeButton(preview, 'PUBLIC_RECIPES.SHOPPING_CANCEL').click();
    harness.detectChanges();

    expect(document.querySelector('fd-shopping-merge-preview')).toBeNull();
    expect(page(harness).querySelectorAll('.shopping-list__item')).toHaveLength(2);
    expect(api.update).not.toHaveBeenCalled();

    const saved = { ...list, revision: SAVED_REVISION, items: [{ ...first, amount: combinedAmount }] };
    api.update.mockReturnValueOnce(of(saved));
    nativeButton(openMergePreview(harness), 'SHOPPING_LIST.MERGE_CONFIRM').click();
    await waitForShoppingAutosaveAsync(harness, api.update, 1);

    expect(api.update).toHaveBeenCalledExactlyOnceWith(
        list.id,
        expect.objectContaining({
            expectedRevision: FIRST_REVISION,
            items: [expect.objectContaining({ id: first.id, amount: combinedAmount, unit: MeasurementUnit.G })],
        }),
    );
    expect(page(harness).querySelectorAll('.shopping-list__item')).toHaveLength(1);
});

describe('Shopping list deletion recovery', () => {
    it('keeps a successfully deleted list out of both menus when the overview refresh fails', async () => {
        const overview = pagedOverview();
        const { harness, api, toastError } = await renderShoppingAsync(overview, true);
        const shifted = makeList('next-shift', 'Pantry supplies', ['Bread']);
        api.deleteById.mockReturnValueOnce(of(void 0));
        api.getPage
            .mockReturnValueOnce(throwError(() => new Error('Overview refresh unavailable')))
            .mockReturnValueOnce(of([...overview.lists.items.filter(item => item.id !== GROCERIES.id), summary(shifted)]));
        api.getById.mockReturnValueOnce(of(shifted));
        const host = page(harness);

        nativeButton(requiredElement(host, '.shopping-list__summary'), 'COMMON.MORE_ACTIONS').click();
        harness.detectChanges();
        nativeButton(requiredElement(document, '[role="menu"]'), 'SHOPPING_LIST.DELETE_LIST_BUTTON').click();
        harness.detectChanges();
        nativeButton(requiredElement(document, 'fd-confirm-delete-dialog'), 'CONFIRM_DELETE.CONFIRM').click();
        harness.detectChanges();
        expect(api.deleteById).toHaveBeenCalledExactlyOnceWith(GROCERIES.id);
        expect(toastError).toHaveBeenCalledWith('SHOPPING_LIST.LOAD_ERROR');
        expect(requiredElement(host, '.shopping-list__list-tabs').textContent).not.toContain(GROCERIES.name);

        const browser = openMobilePicker(harness);
        expect(browser.textContent).not.toContain(GROCERIES.name);
        expect(browser.textContent).toContain('List 1');
        expect(browser.textContent).not.toContain(shifted.name);
        nativeButton(browser, 'SHOPPING_LIST.LOAD_MORE').click();
        harness.detectChanges();
        expect(api.getPage).toHaveBeenLastCalledWith(1, '');
        nativeButton(browser, shifted.name).click();
        await vi.waitFor(() => {
            harness.detectChanges();
            expect(requiredElement(host, '.shopping-list__summary-title').textContent).toBe(shifted.name);
        });
        expect(host.textContent).toContain('Bread');
        expect(api.getById).toHaveBeenCalledExactlyOnceWith(shifted.id);
    });
});

describe('Shopping item workspace while deleting the selected list', () => {
    it('does not carry an item entered without a list into the next list after deletion', async () => {
        const { harness, api } = await renderShoppingAsync();
        const deleting = new Subject<void>();
        api.deleteById.mockReturnValueOnce(deleting);
        api.getPage.mockReturnValueOnce(of([summary(TRAVEL)]));
        const host = page(harness);
        const name = requiredInput(host, '.shopping-list__quick-add-input input');
        name.focus();
        expect(document.activeElement).toBe(name);

        const confirmation = openShoppingListConfirmation(harness, 'SHOPPING_LIST.DELETE_LIST_BUTTON');
        nativeButton(confirmation, 'CONFIRM_DELETE.CONFIRM').click();
        harness.detectChanges();
        expect(api.deleteById).toHaveBeenCalledExactlyOnceWith(GROCERIES.id);
        expect(deleting.observed).toBe(true);
        expect(host.querySelector('.shopping-list__summary-title')).toBeNull();

        name.focus();
        if (document.activeElement === name) {
            addNativeItem(harness, 'Was this meant for the deleted list?');
        }
        expect(api.update).not.toHaveBeenCalled();

        deleting.next();
        deleting.complete();
        harness.detectChanges();
        expect(requiredElement(host, '.shopping-list__summary-title').textContent).toBe(TRAVEL.name);
        expect(name.value).toBe('');
        expect(name.matches(':disabled')).toBe(false);

        api.update.mockReturnValueOnce(of(shoppingListWithAddedItem(TRAVEL, 'travel-new', CREATED_SHOPPING_ITEM_NAME, SAVED_REVISION)));
        addNativeItem(harness, CREATED_SHOPPING_ITEM_NAME);
        await waitForShoppingAutosaveAsync(harness, api.update, 1);
        expect(api.update).toHaveBeenCalledExactlyOnceWith(
            TRAVEL.id,
            expect.objectContaining({
                items: [
                    expect.objectContaining({ name: TRAVEL.items[0].name }),
                    expect.objectContaining({ name: CREATED_SHOPPING_ITEM_NAME }),
                ],
            }),
        );
    });

    it('preserves an unsubmitted item when a failed deletion restores the original list', async () => {
        const { harness, api, toastError } = await renderShoppingAsync();
        const deleting = new Subject<void>();
        api.deleteById.mockReturnValueOnce(deleting);
        const host = page(harness);
        const workspace = requiredElement(host, '.shopping-list__workspace');
        const name = requiredInput(workspace, '.shopping-list__quick-add-input input');
        const add = nativeButton(workspace, 'SHOPPING_LIST.ADD_ITEM_BUTTON');
        const checkbox = requiredInput(workspace, '.shopping-list__item input[type="checkbox"]');
        const draft = 'Rice for tomorrow';
        inputText(name, draft);
        harness.detectChanges();

        const confirmation = openShoppingListConfirmation(harness, 'SHOPPING_LIST.DELETE_LIST_BUTTON');
        nativeButton(confirmation, 'CONFIRM_DELETE.CONFIRM').click();
        expect(api.deleteById).toHaveBeenCalledExactlyOnceWith(GROCERIES.id);
        expect(add.isConnected && !add.matches(':disabled')).toBe(true);
        add.click();
        expect(checkbox.isConnected && !checkbox.matches(':disabled')).toBe(true);
        checkbox.click();
        harness.detectChanges();
        expect([name, add].every(control => control.matches(':disabled'))).toBe(true);
        expect(workspace.getAttribute('aria-busy')).toBe('true');

        deleting.error(new Error('Delete interrupted'));
        harness.detectChanges();
        expect(toastError).toHaveBeenCalledExactlyOnceWith('SHOPPING_LIST.DELETE_ERROR');
        expect(requiredElement(host, '.shopping-list__summary-title').textContent).toBe(GROCERIES.name);
        expect(workspace.textContent).toContain(GROCERIES.items[0].name);
        expect(requiredInput(workspace, '.shopping-list__item input[type="checkbox"]').checked).toBe(false);
        expect(name.value).toBe(draft);
        expect([name, add].every(control => !control.matches(':disabled'))).toBe(true);
        expect(workspace.getAttribute('aria-busy')).toBe('false');
        expect(api.update).not.toHaveBeenCalled();
    });
});

describe('Shopping list deletion beyond the first page', () => {
    it('keeps page one and resumes page two after deleting an older list and an index refresh failure', async () => {
        const { harness, api, toastError } = await renderShoppingAsync(pagedOverview(), true);
        const older = { ...TRAVEL, createdAt: utcInstant('2026-11-18T08:00:00Z') };
        const remaining = {
            ...makeList('camping', 'Camping pantry', ['Granola']),
            createdAt: utcInstant('2026-11-17T08:00:00Z'),
        };
        api.getPage
            .mockReturnValueOnce(of([summary(older), summary(remaining)]))
            .mockReturnValueOnce(throwError(() => new Error('Overview refresh unavailable')))
            .mockReturnValueOnce(of([summary(remaining)]));
        api.getById.mockReturnValueOnce(of(older)).mockReturnValueOnce(of(remaining));
        api.deleteById.mockReturnValueOnce(of(void 0));

        const firstBrowser = openMobilePicker(harness);
        nativeButton(firstBrowser, 'SHOPPING_LIST.LOAD_MORE').click();
        harness.detectChanges();
        nativeButton(firstBrowser, older.name).click();
        await vi.waitFor(() => {
            harness.detectChanges();
            expect(requiredElement(page(harness), '.shopping-list__summary-title').textContent).toBe(older.name);
        });

        chooseSummaryAction(harness, 'SHOPPING_LIST.DELETE_LIST_BUTTON');
        nativeButton(requiredElement(document, 'fd-confirm-delete-dialog'), 'CONFIRM_DELETE.CONFIRM').click();
        harness.detectChanges();
        expect(api.deleteById).toHaveBeenCalledExactlyOnceWith(older.id);
        expect(toastError).toHaveBeenCalledExactlyOnceWith('SHOPPING_LIST.LOAD_ERROR');

        const recoveredBrowser = openMobilePicker(harness);
        expect(recoveredBrowser.querySelectorAll('.list-browser__row')).toHaveLength(FIRST_PAGE_SIZE);
        expect(recoveredBrowser.textContent).toContain(GROCERIES.name);
        expect(recoveredBrowser.textContent).not.toContain(older.name);
        nativeButton(recoveredBrowser, 'SHOPPING_LIST.LOAD_MORE').click();
        harness.detectChanges();
        expect(api.getPage.mock.calls).toEqual([[2, ''], [], [2, '']]);
        expect(recoveredBrowser.textContent).not.toContain(older.name);
        nativeButton(recoveredBrowser, remaining.name).click();
        await vi.waitFor(() => {
            harness.detectChanges();
            expect(requiredElement(page(harness), '.shopping-list__summary-title').textContent).toBe(remaining.name);
        });
        expect(page(harness).textContent).toContain('Granola');
        expect(api.getById.mock.calls).toEqual([[older.id], [remaining.id]]);
        expect(api.update).not.toHaveBeenCalled();
    });
});

describe('Shopping list deletion confirmed during autosave', () => {
    it('waits for the current save and keeps its revision and item identities when deletion fails', async () => {
        const { harness, api, toastError } = await renderShoppingAsync();
        const draftName = 'Add dried apricots';
        const nextName = 'Add oats';
        const saved = shoppingListWithAddedItem(GROCERIES, 'saved-apricots', draftName, SAVED_REVISION);
        const nextSave = shoppingListWithAddedItem(saved, 'saved-oats', nextName, 'c'.repeat(REVISION_LENGTH));
        const saving = new Subject<ShoppingList>();
        const deleting = new Subject<void>();
        api.update.mockReturnValueOnce(saving).mockReturnValueOnce(of(nextSave));
        api.deleteById.mockReturnValueOnce(deleting);

        addNativeItem(harness, draftName);
        chooseSummaryAction(harness, 'SHOPPING_LIST.DELETE_LIST_BUTTON');
        const confirmation = requiredElement(document, 'fd-confirm-delete-dialog');
        await waitForShoppingAutosaveAsync(harness, api.update, 1);
        nativeButton(confirmation, 'CONFIRM_DELETE.CONFIRM').click();
        harness.detectChanges();
        expect(api.deleteById).not.toHaveBeenCalled();
        expect(page(harness).textContent).toContain(draftName);

        saving.next(saved);
        saving.complete();
        await vi.waitFor(() => {
            harness.detectChanges();
            expect(api.deleteById).toHaveBeenCalledExactlyOnceWith(GROCERIES.id);
        });
        deleting.error(new Error('Cannot delete list now'));
        harness.detectChanges();
        expect(toastError).toHaveBeenCalledExactlyOnceWith('SHOPPING_LIST.DELETE_ERROR');
        expect(requiredElement(page(harness), '.shopping-list__summary-title').textContent).toBe(GROCERIES.name);
        expect(page(harness).textContent).toContain(draftName);

        addNativeItem(harness, nextName);
        await waitForShoppingAutosaveAsync(harness, api.update, 2);
        const [id, update] = api.update.mock.calls[1];
        expect(id).toBe(GROCERIES.id);
        expect(update.expectedRevision).toBe(SAVED_REVISION);
        expect(update.items?.find(item => item.name === draftName)?.id).toBe(saved.items[1].id);
        expect(page(harness).textContent).toContain(nextName);
    });

    it('keeps the draft after a failed prerequisite and deletes only after a new confirmation', async () => {
        const { harness, api } = await renderShoppingAsync();
        const draftName = 'Oranges for breakfast';
        const saved = shoppingListWithAddedItem(GROCERIES, 'saved-oranges', draftName, SAVED_REVISION);
        const saving = new Subject<ShoppingList>();
        api.update.mockReturnValueOnce(saving).mockReturnValueOnce(of(saved));
        api.deleteById.mockReturnValueOnce(of(void 0));
        api.getPage.mockReturnValueOnce(of([summary(TRAVEL)]));

        addNativeItem(harness, draftName);
        chooseSummaryAction(harness, 'SHOPPING_LIST.DELETE_LIST_BUTTON');
        const confirmation = requiredElement(document, 'fd-confirm-delete-dialog');
        await waitForShoppingAutosaveAsync(harness, api.update, 1);
        nativeButton(confirmation, 'CONFIRM_DELETE.CONFIRM').click();
        harness.detectChanges();
        saving.error(new Error('Autosave unavailable'));

        const host = page(harness);
        const alert = await vi.waitFor(() => {
            harness.detectChanges();
            return requiredElement(host, 'fd-ui-inline-alert');
        });
        expect(alert.textContent).toContain('SHOPPING_LIST.SAVE_ERROR');
        expect(host.textContent).toContain(draftName);
        expect(api.deleteById).not.toHaveBeenCalled();

        nativeButton(alert, 'ERRORS.RETRY').click();
        harness.detectChanges();
        expect(api.update).toHaveBeenCalledTimes(2);
        expect(api.deleteById).not.toHaveBeenCalled();
        chooseSummaryAction(harness, 'SHOPPING_LIST.DELETE_LIST_BUTTON');
        nativeButton(requiredElement(document, 'fd-confirm-delete-dialog'), 'CONFIRM_DELETE.CONFIRM').click();
        harness.detectChanges();
        expect(api.deleteById).toHaveBeenCalledExactlyOnceWith(GROCERIES.id);
        expect(requiredElement(host, '.shopping-list__summary-title').textContent).toBe(TRAVEL.name);
        expect(host.textContent).not.toContain(draftName);
    });
});

function addNativeItem(harness: RouterTestingHarness, name: string): void {
    const host = page(harness);
    inputText(requiredInput(host, '.shopping-list__quick-add-input input'), name);
    harness.detectChanges();
    nativeButton(host, 'SHOPPING_LIST.ADD_ITEM_BUTTON').click();
    harness.detectChanges();
}

function chooseSummaryAction(harness: RouterTestingHarness, action: string): void {
    nativeButton(requiredElement(page(harness), '.shopping-list__summary'), 'COMMON.MORE_ACTIONS').click();
    harness.detectChanges();
    nativeButton(requiredElement(document, '[role="menu"]'), action).click();
    harness.detectChanges();
}

function openConflictConfirmation(harness: RouterTestingHarness): HTMLElement {
    nativeButton(requiredElement(page(harness), 'fd-ui-inline-alert'), 'SHOPPING_LIST.CONFLICT_RELOAD').click();
    harness.detectChanges();
    return requiredElement(document, 'fd-confirm-delete-dialog');
}

function openMergePreview(harness: RouterTestingHarness): HTMLElement {
    nativeButton(requiredElement(page(harness), '.shopping-list__summary'), 'COMMON.MORE_ACTIONS').click();
    harness.detectChanges();
    nativeButton(requiredElement(document, '[role="menu"]'), 'SHOPPING_LIST.MERGE_TITLE').click();
    harness.detectChanges();
    return requiredElement(document, 'fd-shopping-merge-preview');
}

function mergeableList(): ShoppingList {
    const list = makeList('produce', 'Produce', ['Pear', 'Pear', 'Oat milk']);
    return { ...list, items: [{ ...list.items[0], amount: 2 }, { ...list.items[1], amount: 3 }, list.items[2]] };
}

describe('Shopping operation dialogs after route departure', () => {
    it.each([
        {
            name: 'item edit',
            open: (harness: RouterTestingHarness): HTMLElement => openShoppingRowEditor(harness, 'Oat milk'),
            action: 'SHOPPING_LIST.SAVE_ITEM',
        },
        { name: 'consolidation', open: openMergePreview, action: 'SHOPPING_LIST.MERGE_CONFIRM' },
        {
            name: 'list clearance',
            open: (harness: RouterTestingHarness): HTMLElement => openShoppingListConfirmation(harness, 'SHOPPING_LIST.CLEAR_LIST_BUTTON'),
            action: 'SHOPPING_LIST.CLEAR_LIST_BUTTON',
        },
        {
            name: 'list deletion',
            open: (harness: RouterTestingHarness): HTMLElement => openShoppingListConfirmation(harness, 'SHOPPING_LIST.DELETE_LIST_BUTTON'),
            action: 'CONFIRM_DELETE.CONFIRM',
        },
    ])('closes an unconfirmed $name and preserves the saved list', async ({ open, action }) => {
        const list = mergeableList();
        const { harness, api } = await renderShoppingAsync({
            selectedList: list,
            lists: { items: [summary(list)], hasMore: false, nextPage: null },
        });
        const dialog = open(harness);
        expect(nativeButton(dialog, action).disabled).toBe(false);

        await harness.navigateByUrl(HOME_URL, ShoppingHomeComponent);
        expect(page(harness).querySelector('h1')?.textContent).toBe('Home');
        expect(dialog.isConnected).toBe(false);
        expect(api.update).not.toHaveBeenCalled();
        expect(api.deleteById).not.toHaveBeenCalled();

        await harness.navigateByUrl(SHOPPING_URL);
        expect(page(harness).querySelectorAll('.shopping-list__item')).toHaveLength(list.items.length);
        expect(page(harness).textContent).toContain('Oat milk');
    });
});

describe('Shopping workspace after a departure is approved', () => {
    it.each([true, false])('dismisses the purchased confirmation while the destination loads (resolves=%s)', async resolves => {
        const destination = new Subject<boolean>();
        const checked = purchasedDraftResult();
        const overview: ShoppingListOverview = {
            selectedList: checked,
            lists: { items: [summary(checked)], hasMore: false, nextPage: null },
        };
        const { harness, api } = await renderShoppingAsync(overview, false, () => destination);
        openPurchasedConfirmation(harness);
        const dialog = requiredElement(document, 'fd-confirm-delete-dialog');
        const confirm = nativeButton(dialog, 'SHOPPING_LIST.CLEAR_PURCHASED');
        expect(confirm.disabled).toBe(false);

        const router = TestBed.inject(Router);
        const resolving = firstValueFrom(router.events.pipe(filter(event => event instanceof ResolveStart)));
        const departure = router.navigateByUrl(HOME_URL);
        await resolving;
        harness.detectChanges();
        const stillOpen = dialog.isConnected;
        if (stillOpen) {
            confirm.click();
            harness.detectChanges();
        }
        const purchasedStillChecked =
            page(harness).querySelector<HTMLInputElement>('.shopping-list__purchased input[type="checkbox"]')?.checked ?? false;
        if (resolves) {
            destination.next(true);
        }
        destination.complete();
        await expect(departure).resolves.toBe(resolves);

        expect(stillOpen).toBe(false);
        expect(purchasedStillChecked).toBe(true);
        expect(api.update).not.toHaveBeenCalled();
        if (!resolves) {
            harness.detectChanges();
            openPurchasedConfirmation(harness);
            const reopened = requiredElement(document, 'fd-confirm-delete-dialog');
            nativeButton(reopened, 'CONFIRM_DELETE.CANCEL').click();
            harness.detectChanges();
            expect(draftItemCheckbox(harness).checked).toBe(true);
        }
    });

    it.each([true, false])('protects the list while the destination is loading (resolves=%s)', async resolves => {
        const destination = new Subject<boolean>();
        const { harness, api } = await renderShoppingAsync(firstOverview(), false, () => destination);
        const router = TestBed.inject(Router);
        const resolving = firstValueFrom(router.events.pipe(filter(event => event instanceof ResolveStart)));
        const departure = router.navigateByUrl(HOME_URL);
        await resolving;

        harness.detectChanges();
        const host = page(harness);
        const item = requiredInput(host, 'fd-shopping-list-items-panel input[type="checkbox"]');
        const inertWhileLeaving = requiredElement(host, '.shopping-list').hasAttribute('inert');
        item.click();
        harness.detectChanges();
        const checkedWhileLeaving = item.checked;
        if (resolves) {
            destination.next(true);
        }
        destination.complete();

        await expect(departure).resolves.toBe(resolves);
        expect(checkedWhileLeaving).toBe(false);
        expect(inertWhileLeaving).toBe(true);
        expect(api.update).not.toHaveBeenCalled();
        if (resolves) {
            expect(page(harness).textContent).toContain('Home');
            return;
        }

        harness.detectChanges();
        expect(requiredElement(host, '.shopping-list').hasAttribute('inert')).toBe(false);
        const saved = { ...GROCERIES, revision: SAVED_REVISION, items: GROCERIES.items.map(entry => ({ ...entry, isChecked: true })) };
        api.update.mockReturnValueOnce(of(saved));
        item.click();
        await waitForShoppingAutosaveAsync(harness, api.update, 1);
        expect(api.update.mock.calls[0][0]).toBe(GROCERIES.id);
        expect(api.update.mock.calls[0][1].items?.[0].isChecked).toBe(true);
    });
});

function openShoppingListConfirmation(harness: RouterTestingHarness, label: string): HTMLElement {
    chooseSummaryAction(harness, label);
    return requiredElement(document, 'fd-confirm-delete-dialog');
}

function shoppingRowEditAction(harness: RouterTestingHarness, name: string): HTMLButtonElement {
    nativeButton(page(harness), `COMMON.MORE_ACTIONS: ${name}`).click();
    harness.detectChanges();
    return nativeButton(requiredElement(document, '[role="menu"]'), 'SHOPPING_LIST.EDIT_ITEM');
}

function openShoppingRowEditor(harness: RouterTestingHarness, name: string): HTMLElement {
    shoppingRowEditAction(harness, name).click();
    harness.detectChanges();
    return requiredElement(document, SHOPPING_ITEM_EDITOR);
}

async function waitForShoppingAutosaveAsync(harness: RouterTestingHarness, update: ShoppingApi['update'], count: number): Promise<void> {
    await vi.waitFor(
        () => {
            harness.detectChanges();
            expect(update).toHaveBeenCalledTimes(count);
        },
        { timeout: SHOPPING_AUTOSAVE_WAIT_MS },
    );
}

function shoppingListWithAddedItem(list: ShoppingList, id: string, name: string, revision: string): ShoppingList {
    return {
        ...list,
        revision,
        items: [
            ...list.items,
            {
                id: entityId<'shopping-list-item'>(id),
                shoppingListId: list.id,
                name,
                isChecked: false,
                sortOrder: list.items.length + 1,
            },
        ],
    };
}

describe('Shopping new item editor identity', () => {
    it('waits for the new item to receive a permanent ID before opening and saving its editor', async () => {
        const { harness, api } = await renderShoppingAsync();
        const initialWrite = new Subject<ShoppingList>();
        const saved = shoppingListWithAddedItem(GROCERIES, 'server-beans', CREATED_SHOPPING_ITEM_NAME, SAVED_REVISION);
        const permanentItem = saved.items.at(-1);
        if (permanentItem === undefined) {
            throw new Error('Expected the response to assign a permanent item');
        }
        const note = 'Buy dry';
        const edited = { ...saved, items: [...GROCERIES.items, { ...permanentItem, note }] };
        api.update.mockReturnValueOnce(initialWrite).mockReturnValueOnce(of(edited));
        addNativeItem(harness, CREATED_SHOPPING_ITEM_NAME);
        expect(requiredInput(page(harness), '.shopping-list__quick-add-input input').value).toBe('');
        const editWhilePending = shoppingRowEditAction(harness, CREATED_SHOPPING_ITEM_NAME);
        expect(editWhilePending.disabled).toBe(true);
        editWhilePending.click();
        expect(document.querySelector(SHOPPING_ITEM_EDITOR)).toBeNull();

        await waitForShoppingAutosaveAsync(harness, api.update, 1);
        expect(api.update).toHaveBeenCalledExactlyOnceWith(
            GROCERIES.id,
            expect.objectContaining({
                expectedRevision: FIRST_REVISION,
                items: [
                    expect.objectContaining({ id: GROCERIES.items[0].id, name: GROCERIES.items[0].name }),
                    expect.objectContaining({ id: null, name: CREATED_SHOPPING_ITEM_NAME, note: null }),
                ],
            }),
        );
        initialWrite.next(saved);
        initialWrite.complete();
        harness.detectChanges();
        const editor = openShoppingRowEditor(harness, CREATED_SHOPPING_ITEM_NAME);
        inputText(requiredInput(editor, 'input[aria-label="SHOPPING_LIST.ADD_ITEM_NOTE"]'), `  ${note}  `);
        nativeButton(editor, 'SHOPPING_LIST.SAVE_ITEM').click();
        harness.detectChanges();
        expect(document.querySelector(SHOPPING_ITEM_EDITOR)).toBeNull();
        expect(page(harness).textContent).toContain(note);
        await expect(TestBed.inject(Router).navigateByUrl(HOME_URL)).resolves.toBe(true);

        expect(api.update).toHaveBeenCalledTimes(2);
        expect(api.update).toHaveBeenLastCalledWith(
            GROCERIES.id,
            expect.objectContaining({
                expectedRevision: SAVED_REVISION,
                items: [
                    expect.objectContaining({ id: GROCERIES.items[0].id, name: GROCERIES.items[0].name }),
                    expect.objectContaining({ id: permanentItem.id, name: CREATED_SHOPPING_ITEM_NAME, note }),
                ],
            }),
        );
    });

    it('closes the rendered editor without applying changes when the user cancels', async () => {
        const { harness, api } = await renderShoppingAsync();
        const originalName = GROCERIES.items[0].name;
        const editor = openShoppingRowEditor(harness, originalName);
        inputText(requiredInput(editor, 'input[aria-label="SHOPPING_LIST.ADD_ITEM_NAME"]'), 'An unsubmitted name');
        nativeButton(editor, 'PUBLIC_RECIPES.SHOPPING_CANCEL').click();
        harness.detectChanges();

        expect(document.querySelector(SHOPPING_ITEM_EDITOR)).toBeNull();
        expect(page(harness).textContent).toContain(originalName);
        expect(page(harness).textContent).not.toContain('An unsubmitted name');
        await expect(TestBed.inject(Router).navigateByUrl(HOME_URL)).resolves.toBe(true);
        expect(api.update).not.toHaveBeenCalled();
    });
});

describe('Shopping removal of a new item during autosave', () => {
    it('keeps the removed row out of the list after the server acknowledges its creation', async () => {
        const { harness, api } = await renderShoppingAsync();
        const initialWrite = new Subject<ShoppingList>();
        const saved = shoppingListWithAddedItem(GROCERIES, 'server-beans', CREATED_SHOPPING_ITEM_NAME, SAVED_REVISION);
        api.update.mockReturnValueOnce(initialWrite).mockReturnValueOnce(of({ ...GROCERIES, revision: 'c'.repeat(REVISION_LENGTH) }));
        addNativeItem(harness, CREATED_SHOPPING_ITEM_NAME);
        await waitForShoppingAutosaveAsync(harness, api.update, 1);

        const host = page(harness);
        nativeButton(host, `COMMON.MORE_ACTIONS: ${CREATED_SHOPPING_ITEM_NAME}`).click();
        harness.detectChanges();
        nativeButton(requiredElement(document, '[role="menu"]'), 'SHOPPING_LIST.REMOVE_ITEM').click();
        harness.detectChanges();
        expect(host.textContent).not.toContain(CREATED_SHOPPING_ITEM_NAME);
        expect(api.update).toHaveBeenCalledOnce();

        initialWrite.next(saved);
        initialWrite.complete();
        harness.detectChanges();
        expect(host.textContent).not.toContain(CREATED_SHOPPING_ITEM_NAME);
        expect(host.textContent).toContain(GROCERIES.items[0].name);

        await expect(TestBed.inject(Router).navigateByUrl(HOME_URL)).resolves.toBe(true);
        expect(api.update).toHaveBeenCalledTimes(2);
        expect(api.update).toHaveBeenLastCalledWith(
            GROCERIES.id,
            expect.objectContaining({
                expectedRevision: SAVED_REVISION,
                items: [expect.objectContaining({ id: GROCERIES.items[0].id, name: GROCERIES.items[0].name })],
            }),
        );
    });
});

describe('Shopping item editor during a queued list change', () => {
    it('closes the previous item editor when the queued list loads successfully', async () => {
        const { harness, api, editor, nextList } = await prepareItemEditorDuringListSwitchAsync();
        nextList.next(TRAVEL);
        nextList.complete();
        await harness.fixture.whenStable();
        harness.detectChanges();

        expect(requiredElement(page(harness), '.shopping-list__summary-title').textContent).toBe(TRAVEL.name);
        expect(editor.isConnected).toBe(false);
        expect(document.querySelector(SHOPPING_ITEM_EDITOR)).toBeNull();
        const currentEditor = openShoppingRowEditor(harness, TRAVEL.items[0].name);
        expect(requiredInput(currentEditor, 'input[aria-label="SHOPPING_LIST.ADD_ITEM_NAME"]').value).toBe(TRAVEL.items[0].name);
        expect(api.update).toHaveBeenCalledOnce();
    });

    it('keeps the previous item editor and saves its draft if the queued list fails to load', async () => {
        const { harness, api, editor, nextList, draft, saved } = await prepareItemEditorDuringListSwitchAsync();
        const save = nativeButton(editor, 'SHOPPING_LIST.SAVE_ITEM');
        expect(save.disabled).toBe(true);
        nextList.error(new Error('List details unavailable'));
        await harness.fixture.whenStable();
        harness.detectChanges();

        expect(requiredElement(page(harness), '.shopping-list__summary-title').textContent).toBe(GROCERIES.name);
        expect(editor.isConnected).toBe(true);
        expect(requiredInput(editor, 'input[aria-label="SHOPPING_LIST.ADD_ITEM_NOTE"]').value).toBe(draft);
        api.update.mockReturnValueOnce(of({ ...saved, items: [{ ...saved.items[0], note: draft }, ...saved.items.slice(1)] }));
        expect(save.disabled).toBe(false);
        save.click();
        await expect(TestBed.inject(Router).navigateByUrl(HOME_URL)).resolves.toBe(true);

        expect(api.update).toHaveBeenCalledTimes(2);
        expect(api.update).toHaveBeenLastCalledWith(
            GROCERIES.id,
            expect.objectContaining({
                expectedRevision: SAVED_REVISION,
                items: [
                    expect.objectContaining({ id: GROCERIES.items[0].id, note: draft }),
                    expect.objectContaining({ id: 'server-beans' }),
                ],
            }),
        );
    });
});

it('does not open the outgoing item editor from a previously open row menu while the next list loads', async () => {
    const { harness, api } = await renderShoppingAsync();
    const outgoingSave = new Subject<ShoppingList>();
    const incomingList = new Subject<ShoppingList | null>();
    const saved = shoppingListWithAddedItem(GROCERIES, 'server-beans', CREATED_SHOPPING_ITEM_NAME, SAVED_REVISION);
    api.update.mockReturnValueOnce(outgoingSave);
    api.getById.mockReturnValueOnce(incomingList);

    addNativeItem(harness, CREATED_SHOPPING_ITEM_NAME);
    const host = page(harness);
    nativeButton(host, TRAVEL.name).click();
    await waitForShoppingAutosaveAsync(harness, api.update, 1);
    expect(api.getById).not.toHaveBeenCalled();
    const oldName = GROCERIES.items[0].name;
    const outgoingEdit = shoppingRowEditAction(harness, oldName);
    expect(outgoingEdit.disabled).toBe(false);

    outgoingSave.next(saved);
    outgoingSave.complete();
    const workspace = requiredElement(host, '.shopping-list__workspace');
    await vi.waitFor(() => {
        harness.detectChanges();
        expect(incomingList.observed).toBe(true);
        expect(workspace.getAttribute('aria-busy')).toBe('true');
    });
    expect(api.getById).toHaveBeenCalledExactlyOnceWith(TRAVEL.id);
    expect(requiredElement(host, '.shopping-list__content').matches(':disabled')).toBe(true);
    expect(workspace.textContent).toContain(oldName);

    if (outgoingEdit.isConnected) {
        outgoingEdit.click();
    }
    harness.detectChanges();
    const openedDuringLoad = document.querySelector(SHOPPING_ITEM_EDITOR) !== null;

    incomingList.next(TRAVEL);
    incomingList.complete();
    await vi.waitFor(() => {
        harness.detectChanges();
        expect(workspace.textContent).toContain(TRAVEL.items[0].name);
        expect(workspace.textContent).not.toContain(oldName);
        expect(document.querySelector(SHOPPING_ITEM_EDITOR)).toBeNull();
    });
    expect(api.update).toHaveBeenCalledExactlyOnceWith(GROCERIES.id, expect.objectContaining({ expectedRevision: FIRST_REVISION }));
    expect(openedDuringLoad).toBe(false);
});

it('ignores a queued row Edit after the selected list changes before the menu redraws', async () => {
    const { harness, api } = await renderShoppingAsync();
    const saving = new Subject<ShoppingList>();
    const loading = new Subject<ShoppingList | null>();
    api.update.mockReturnValueOnce(saving);
    api.getById.mockReturnValueOnce(loading);
    addNativeItem(harness, CREATED_SHOPPING_ITEM_NAME);
    const host = page(harness);
    nativeButton(host, TRAVEL.name).click();
    await waitForShoppingAutosaveAsync(harness, api.update, 1);
    const outgoingEdit = shoppingRowEditAction(harness, GROCERIES.items[0].name);

    saving.next(shoppingListWithAddedItem(GROCERIES, 'saved-beans', CREATED_SHOPPING_ITEM_NAME, SAVED_REVISION));
    saving.complete();
    expect(api.getById).toHaveBeenCalledExactlyOnceWith(TRAVEL.id);
    loading.next(TRAVEL);
    loading.complete();
    expect(outgoingEdit.isConnected).toBe(true);
    expect(outgoingEdit.disabled).toBe(false);
    outgoingEdit.click();
    harness.detectChanges();

    expect(requiredElement(host, '.shopping-list__summary-title').textContent).toBe(TRAVEL.name);
    expect(host.textContent).toContain(TRAVEL.items[0].name);
    expect(document.querySelector(SHOPPING_ITEM_EDITOR)).toBeNull();
    expect(api.update).toHaveBeenCalledTimes(1);
});

describe('Shopping item save while another list is loading', () => {
    it('keeps Save unavailable until the selected list finishes loading and avoids a write to the destination', async () => {
        const { harness, api, editor, nextList, draft } = await prepareItemEditorDuringListSwitchAsync();
        expect(nextList.observed).toBe(true);
        expect(api.update.mock.lastCall?.[0]).toBe(GROCERIES.id);
        api.update.mockReturnValueOnce(throwError(() => new Error('Unexpected write after selecting another list')));

        const save = nativeButton(editor, 'SHOPPING_LIST.SAVE_ITEM');
        const saveDisabledWhilePending = save.disabled;
        save.click();
        harness.detectChanges();
        const editorOpenWhilePending = editor.isConnected;
        const editAppliedWhilePending = page(harness).textContent.includes(draft);

        nextList.next(TRAVEL);
        nextList.complete();
        await harness.fixture.whenStable();
        harness.detectChanges();
        expect(requiredElement(page(harness), '.shopping-list__summary-title').textContent).toBe(TRAVEL.name);
        expect(editor.isConnected).toBe(false);

        const departed = await TestBed.inject(Router).navigateByUrl(HOME_URL);
        const laterWrites = api.update.mock.calls.slice(1).map(([listId, payload]) => ({
            listId,
            expectedRevision: payload.expectedRevision,
            items: payload.items?.map(({ id, name, note }) => ({ id, name, note })),
        }));

        expect({ saveDisabledWhilePending, editorOpenWhilePending, editAppliedWhilePending, departed, laterWrites }).toEqual({
            saveDisabledWhilePending: true,
            editorOpenWhilePending: true,
            editAppliedWhilePending: false,
            departed: true,
            laterWrites: [],
        });
    });
});

async function prepareItemEditorDuringListSwitchAsync(): Promise<{
    harness: RouterTestingHarness;
    api: ShoppingApi;
    editor: HTMLElement;
    nextList: Subject<ShoppingList | null>;
    draft: string;
    saved: ShoppingList;
}> {
    const { harness, api } = await renderShoppingAsync();
    const pendingSave = new Subject<ShoppingList>();
    const nextList = new Subject<ShoppingList | null>();
    const saved = shoppingListWithAddedItem(GROCERIES, 'server-beans', CREATED_SHOPPING_ITEM_NAME, SAVED_REVISION);
    api.update.mockReturnValueOnce(pendingSave);
    api.getById.mockReturnValueOnce(nextList);
    addNativeItem(harness, CREATED_SHOPPING_ITEM_NAME);
    const tab = nativeButton(page(harness), TRAVEL.name);
    expect(tab.disabled).toBe(false);
    tab.click();
    await waitForShoppingAutosaveAsync(harness, api.update, 1);
    expect(api.getById).not.toHaveBeenCalled();

    const editor = openShoppingRowEditor(harness, GROCERIES.items[0].name);
    const draft = 'Unsaved item note during the list switch';
    inputText(requiredInput(editor, 'input[aria-label="SHOPPING_LIST.ADD_ITEM_NOTE"]'), draft);
    pendingSave.next(saved);
    pendingSave.complete();
    await vi.waitFor(() => {
        harness.detectChanges();
        expect(api.getById).toHaveBeenCalledExactlyOnceWith(TRAVEL.id);
        expect(nextList.observed).toBe(true);
    });
    expect(editor.isConnected).toBe(true);
    return { harness, api, editor, nextList, draft, saved };
}

describe('Shopping saved edits before selecting another list', () => {
    it('preserves a row added during autosave and opens the other list only after persisting both changes', async () => {
        const { harness, api } = await renderShoppingAsync();
        const initialWrite = new Subject<ShoppingList>();
        const followupWrite = new Subject<ShoppingList>();
        const firstSaved = shoppingListWithAddedItem(GROCERIES, 'server-beans', CREATED_SHOPPING_ITEM_NAME, SAVED_REVISION);
        const secondName = 'Dark cherries';
        const secondSaved = shoppingListWithAddedItem(firstSaved, 'server-cherries', secondName, SAVED_REVISION);
        api.update.mockReturnValueOnce(initialWrite).mockReturnValueOnce(followupWrite);
        addNativeItem(harness, CREATED_SHOPPING_ITEM_NAME);
        await waitForShoppingAutosaveAsync(harness, api.update, 1);
        addNativeItem(harness, secondName);

        initialWrite.next(firstSaved);
        initialWrite.complete();
        harness.detectChanges();
        const host = page(harness);
        expect(host.textContent).toContain(CREATED_SHOPPING_ITEM_NAME);
        expect(host.textContent).toContain(secondName);
        nativeButton(host, TRAVEL.name).click();
        await waitForShoppingAutosaveAsync(harness, api.update, 2);

        expect(api.getById).not.toHaveBeenCalled();
        expect(requiredElement(host, '.shopping-list__summary-title').textContent).toBe(GROCERIES.name);
        expect(api.update).toHaveBeenLastCalledWith(
            GROCERIES.id,
            expect.objectContaining({
                expectedRevision: SAVED_REVISION,
                items: [
                    expect.objectContaining({ id: GROCERIES.items[0].id }),
                    expect.objectContaining({ id: entityId<'shopping-list-item'>('server-beans'), name: CREATED_SHOPPING_ITEM_NAME }),
                    expect.objectContaining({ id: null, name: secondName }),
                ],
            }),
        );
        followupWrite.next(secondSaved);
        followupWrite.complete();
        await vi.waitFor(() => {
            harness.detectChanges();
            expect(requiredElement(host, '.shopping-list__summary-title').textContent).toBe(TRAVEL.name);
        });
        expect(api.getById).toHaveBeenCalledExactlyOnceWith(TRAVEL.id);
        expect(host.textContent).not.toContain(secondName);
        expect(host.textContent).toContain(TRAVEL.items[0].name);
    });
});

describe('Shopping list selection after deleting the previous list', () => {
    it('lets the user reopen the remaining list after its first detail request fails', async () => {
        const { harness, api, toastError } = await renderShoppingAsync();
        const firstDetail = new Subject<ShoppingList | null>();
        const travel = { ...TRAVEL, revision: SAVED_REVISION };
        api.deleteById.mockReturnValueOnce(of(void 0));
        api.getPage.mockReturnValueOnce(of([summary(travel)]));
        api.getById.mockReturnValueOnce(firstDetail).mockReturnValueOnce(of(travel));
        const host = page(harness);
        const confirm = openShoppingListConfirmation(harness, 'SHOPPING_LIST.DELETE_LIST_BUTTON');
        nativeButton(confirm, 'CONFIRM_DELETE.CONFIRM').click();
        harness.detectChanges();

        expect(api.deleteById).toHaveBeenCalledExactlyOnceWith(GROCERIES.id);
        expect(api.getById).toHaveBeenCalledExactlyOnceWith(travel.id);
        const tabs = requiredElement(host, '.shopping-list__list-tabs');
        const travelTab = nativeButton(tabs, travel.name);
        expect(travelTab.disabled).toBe(true);

        firstDetail.next(null);
        firstDetail.complete();
        harness.detectChanges();
        expect(toastError).toHaveBeenCalledExactlyOnceWith('SHOPPING_LIST.LOAD_ERROR');
        expect(host.textContent).not.toContain(travel.items[0].name);
        expect(travelTab.disabled).toBe(false);

        addNativeItem(harness, 'Unassigned ingredient');
        expect(api.update).not.toHaveBeenCalled();
        travelTab.click();
        await vi.waitFor(() => {
            harness.detectChanges();
            expect(api.getById.mock.calls).toEqual([[travel.id], [travel.id]]);
            expect(travelTab.getAttribute('aria-current')).toBe('true');
            expect(requiredElement(host, '.shopping-list__summary-title').textContent).toBe(travel.name);
            expect(host.textContent).toContain(travel.items[0].name);
        });

        const ingredient = 'Extra trail snacks';
        const saved = shoppingListWithAddedItem(travel, 'travel-extra', ingredient, FIRST_REVISION);
        api.update.mockReturnValueOnce(of(saved));
        addNativeItem(harness, ingredient);
        await expect(TestBed.inject(Router).navigateByUrl(HOME_URL)).resolves.toBe(true);
        expect(api.update).toHaveBeenCalledExactlyOnceWith(
            travel.id,
            expect.objectContaining({
                expectedRevision: SAVED_REVISION,
                items: [expect.objectContaining({ name: travel.items[0].name }), expect.objectContaining({ name: ingredient })],
            }),
        );
    });
});
