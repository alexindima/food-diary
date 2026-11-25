import { DOCUMENT } from '@angular/common';
import { TestBed } from '@angular/core/testing';
import { By } from '@angular/platform-browser';
import { provideRouter, Router, withComponentInputBinding } from '@angular/router';
import { RouterTestingHarness } from '@angular/router/testing';
import { TranslateService } from '@ngx-translate/core';
import { FdUiDialogService } from 'fd-ui-kit/dialog/fd-ui-dialog.service';
import { FdUiSelectComponent } from 'fd-ui-kit/select/fd-ui-select';
import { firstValueFrom, of, Subject, throwError } from 'rxjs';
import { beforeEach, describe, expect, it, vi } from 'vitest';

import ruCore from '../../../../../../../assets/i18n/ru/core.json';
import { stubScrollIntoView } from '../../../../../../../src/testing/dom-testing';
import { provideTranslateTesting } from '../../../../../../../src/testing/translate-testing.module';
import { adminId, adminUtcInstant } from '../../../shared/models/semantics/admin-meaning';
import { AdminUsersFacade } from '../lib/admin-users.facade';
import type { AdminUser, PagedResponse } from '../models/admin-user.models';
import { AdminUsersComponent } from './admin-users';

const EXPECTED_ACTION_COUNT = 3;

const FIRST_PAGE = 1;
const SECOND_PAGE = 2;
const PAGE_SIZE = 20;
const USER_TOTAL_ITEMS = 21;
const OUT_OF_RANGE_PAGE = 99;

let component: AdminUsersComponent;
let fixture: RouterTestingHarness['fixture'];
let usersService: {
    getUsers: ReturnType<typeof vi.fn>;
};
let dialogService: { open: ReturnType<typeof vi.fn> };

const pagedUsers: PagedResponse<AdminUser> = {
    items: [
        {
            id: adminId<'user'>('u1'),
            email: 'user@example.com',
            username: 'alex',
            isActive: true,
            isEmailConfirmed: true,
            createdOnUtc: adminUtcInstant('2026-01-01T00:00:00Z'),
            lastActiveAtUtc: adminUtcInstant('2026-05-23T02:06:10Z'),
            roles: ['Admin'],
        },
    ],
    page: FIRST_PAGE,
    limit: PAGE_SIZE,
    totalPages: SECOND_PAGE,
    totalItems: USER_TOTAL_ITEMS,
};

describe('AdminUsersComponent', () => {
    beforeEach(async () => {
        await setupComponentAsync();
    });

    it('should create', () => {
        expect(component).toBeTruthy();
    });

    it('should load users on init', () => {
        expect(usersService.getUsers).toHaveBeenCalledWith(FIRST_PAGE, PAGE_SIZE, null, {
            status: 'active',
            role: '',
            emailConfirmed: '',
            lastActiveFrom: '',
            lastActiveTo: '',
        });
        expect(component['users']()).toEqual(pagedUsers.items);
        expect(component['totalPages']()).toBe(SECOND_PAGE);
        expect(component['totalItems']()).toBe(USER_TOTAL_ITEMS);
        expect(component['isLoading']()).toBe(false);
    });

    it('exposes persistent names for the user filters', () => {
        const element = fixture.nativeElement as HTMLElement;
        const searchInput = element.querySelector<HTMLInputElement>('fd-ui-input input');
        const statusButton = element.querySelector<HTMLButtonElement>('.admin-filter-bar > fd-ui-select button');
        if (searchInput === null || statusButton === null) {
            throw new Error('Expected user filter controls to render.');
        }
        const searchLabels = searchInput.labels;
        if (searchLabels === null) {
            throw new Error('Expected user filter labels to render.');
        }

        expect(searchLabels.item(0).textContent).toContain('Search users');
        expect(statusButton.labels.item(0).textContent).toContain('User status');
    });

    it('should update search and reload from page 1', async () => {
        component['onSearchChange']('john');
        await fixture.whenStable();

        expect(component['draftSearch']()).toBe('john');
        expect(component['draftPage']()).toBe(FIRST_PAGE);
        expect(usersService.getUsers).toHaveBeenLastCalledWith(FIRST_PAGE, PAGE_SIZE, 'john', {
            status: 'active',
            role: '',
            emailConfirmed: '',
            lastActiveFrom: '',
            lastActiveTo: '',
        });
    });

    it('should update status and reload', async () => {
        component['onStatusChange']('inactive');
        await fixture.whenStable();

        expect(component['draftStatus']()).toBe('inactive');
        expect(component['draftPage']()).toBe(FIRST_PAGE);
        expect(usersService.getUsers).toHaveBeenLastCalledWith(FIRST_PAGE, PAGE_SIZE, null, {
            status: 'inactive',
            role: '',
            emailConfirmed: '',
            lastActiveFrom: '',
            lastActiveTo: '',
        });
    });

    it('should change page only within valid bounds', async () => {
        component['goToPage'](SECOND_PAGE);
        await fixture.whenStable();
        expect(component['draftPage']()).toBe(SECOND_PAGE);
        expect(usersService.getUsers).toHaveBeenLastCalledWith(SECOND_PAGE, PAGE_SIZE, null, {
            status: 'active',
            role: '',
            emailConfirmed: '',
            lastActiveFrom: '',
            lastActiveTo: '',
        });

        const callCount = usersService.getUsers.mock.calls.length;
        component['goToPage'](0);
        await fixture.whenStable();
        component['goToPage'](OUT_OF_RANGE_PAGE);
        await fixture.whenStable();
        expect(usersService.getUsers.mock.calls.length).toBe(callCount);
    });
});

describe('AdminUsersComponent activity', () => {
    beforeEach(async () => {
        await setupComponentAsync();
    });

    it('renders Russian activity labels and an explicit unknown value without a login fallback', async () => {
        const translate = TestBed.inject(TranslateService);
        translate.setTranslation('ru', ruCore);
        await firstValueFrom(translate.use('ru'));
        usersService.getUsers.mockReturnValueOnce(of({ ...pagedUsers, items: [{ ...pagedUsers.items[0], lastActiveAtUtc: null }] }));
        component['loadUsers']();
        await fixture.whenStable();
        fixture.detectChanges();
        const element = fixture.nativeElement as HTMLElement;
        expect(element.querySelectorAll('th')[3].textContent).toBe('Был активен');
        expect(element.querySelectorAll('tbody td')[3].textContent.trim()).toBe('Нет данных');
        expect(element.textContent).toContain('Был активен с');
        expect(element.textContent).toContain('Был активен по');
        expect(element.textContent).not.toContain('\uFFFD');
    });

    it('preserves the first activity filter from the URL on apply and resets it when removed', async () => {
        const router = TestBed.inject(Router);
        await router.navigateByUrl('/?lastActiveFrom=2030-01-02&lastActiveFrom=2030-02-01&lastActiveTo=2030-01-03');
        await fixture.whenStable();
        expect(usersService.getUsers).toHaveBeenLastCalledWith(
            FIRST_PAGE,
            PAGE_SIZE,
            null,
            expect.objectContaining({
                lastActiveFrom: '2030-01-02',
                lastActiveTo: '2030-01-03',
            }),
        );
        component['applyFilters']();
        await fixture.whenStable();
        expect(router.url).toContain('lastActiveFrom=2030-01-02');
        expect(router.url).not.toContain('lastLogin');

        await router.navigateByUrl('/');
        await fixture.whenStable();
        expect(usersService.getUsers).toHaveBeenLastCalledWith(
            FIRST_PAGE,
            PAGE_SIZE,
            null,
            expect.objectContaining({ lastActiveFrom: '', lastActiveTo: '' }),
        );
    });
});

describe('AdminUsersComponent dialogs', () => {
    beforeEach(async () => {
        await setupComponentAsync();
    });

    it('keeps the current page and filters after cancelling account creation', async () => {
        const router = TestBed.inject(Router);
        await router.navigate([], { queryParams: { page: SECOND_PAGE, status: 'inactive' } });
        fixture.detectChanges();
        const root = fixture.nativeElement as HTMLElement;
        const loadsBeforeDialog = usersService.getUsers.mock.calls.length;
        findControl(root, 'header fd-ui-button button').click();
        await fixture.whenStable();

        expect(dialogService.open).toHaveBeenCalledOnce();
        expect(usersService.getUsers).toHaveBeenCalledTimes(loadsBeforeDialog);
        expect(router.parseUrl(router.url).queryParams).toEqual({ page: '2', status: 'inactive' });
    });

    it.each([FIRST_PAGE, SECOND_PAGE])('reloads page one with applied filters after account creation from page %s', async page => {
        const router = TestBed.inject(Router);
        await router.navigate([], { queryParams: { page, status: 'inactive', role: 'Support' } });
        await fixture.whenStable();
        component['draftRole'].set('Admin');
        const close$ = new Subject<boolean>();
        dialogService.open.mockReturnValue({
            afterClosed: () => close$.asObservable(),
        });

        usersService.getUsers.mockClear();
        (fixture.nativeElement as HTMLElement).querySelector<HTMLButtonElement>('header fd-ui-button button')?.click();
        close$.next(true);
        close$.complete();
        await fixture.whenStable();

        expect(dialogService.open).toHaveBeenCalled();
        expect(router.parseUrl(router.url).queryParams).toEqual({ page: '1', status: 'inactive', role: 'Support' });
        expect(usersService.getUsers).toHaveBeenCalledExactlyOnceWith(
            FIRST_PAGE,
            PAGE_SIZE,
            null,
            expect.objectContaining({ status: 'inactive', role: 'Support' }),
        );
    });

    it('closes an open create dialog when leaving the page and ignores a late result', () => {
        const closed = new Subject<boolean>();
        const close = vi.fn();
        dialogService.open.mockReturnValue({ afterClosed: () => closed.asObservable(), close });
        component['openCreate']();
        const loadsBeforeLeaving = usersService.getUsers.mock.calls.length;

        fixture.destroy();
        closed.next(true);
        closed.complete();

        expect(close).toHaveBeenCalledOnce();
        expect(usersService.getUsers).toHaveBeenCalledTimes(loadsBeforeLeaving);
    });

    it('releases the page cleanup after the create dialog has already closed', async () => {
        const closed = new Subject<boolean>();
        const close = vi.fn();
        dialogService.open.mockReturnValue({ afterClosed: () => closed.asObservable(), close });
        component['openCreate']();

        closed.next(true);
        closed.complete();
        await fixture.whenStable();
        fixture.destroy();

        expect(usersService.getUsers).toHaveBeenCalledTimes(SECOND_PAGE);
        expect(close).not.toHaveBeenCalled();
    });

    it('navigates row click and keyboard activation to the user page with list filters', () => {
        const navigate = vi.spyOn(TestBed.inject(Router), 'navigate').mockResolvedValue(true);
        const row = (fixture.nativeElement as HTMLElement).querySelector<HTMLTableRowElement>('tbody .users-table__row');
        if (row === null) {
            throw new Error('Expected an account row');
        }
        expect(row.querySelector('a')).toBeNull();
        row.click();
        row.dispatchEvent(new KeyboardEvent('keydown', { key: 'Enter', bubbles: true }));
        row.dispatchEvent(new KeyboardEvent('keydown', { key: ' ', bubbles: true }));
        expect(navigate).toHaveBeenCalledTimes(EXPECTED_ACTION_COUNT);
        expect(navigate).toHaveBeenLastCalledWith(['/users', 'u1'], { queryParamsHandling: 'preserve' });
        expect(dialogService.open).not.toHaveBeenCalled();
    });
});

describe('Admin users filters and pagination', () => {
    beforeEach(async () => {
        await setupComponentAsync();
    });

    it('applies role and confirmation changes while clearing both activity filters and returning to page one', async () => {
        stubScrollIntoView();
        const router = TestBed.inject(Router);
        await router.navigate([], {
            queryParams: { page: SECOND_PAGE, lastActiveFrom: '2026-01-02', lastActiveTo: '2026-01-03' },
        });
        fixture.detectChanges();
        const root = fixture.nativeElement as HTMLElement;
        const roleLabel = [...root.querySelectorAll<HTMLLabelElement>('fd-ui-input label')].find(
            label => label.textContent.trim() === 'ADMIN_USER.ROLES',
        );
        const role = roleLabel?.control;
        if (!(role instanceof HTMLInputElement)) {
            throw new Error('Expected the Roles input.');
        }
        role.value = 'Support';
        role.dispatchEvent(new Event('input', { bubbles: true }));
        findControl(root, 'fd-ui-select:has(button[id="admin-users-field-1"]) button').click();
        fixture.detectChanges();
        findControl(TestBed.inject(DOCUMENT), '[role="option"]:has(span[title="ADMIN_USER.NO"])').click();
        fixture.detectChanges();
        for (const clearButton of root.querySelectorAll<HTMLButtonElement>('fd-ui-date-input button[aria-label^="COMMON.CLEAR:"]')) {
            clearButton.click();
        }
        expect(usersService.getUsers).toHaveBeenLastCalledWith(
            SECOND_PAGE,
            PAGE_SIZE,
            null,
            expect.objectContaining({ role: '', lastActiveFrom: '2026-01-02', lastActiveTo: '2026-01-03' }),
        );
        findControl(root, '.admin-filter-bar > fd-ui-button button').click();
        await fixture.whenStable();

        expect(router.parseUrl(router.url).queryParams).toEqual({ page: '1', status: 'active', role: 'Support', emailConfirmed: 'false' });
        expect(usersService.getUsers).toHaveBeenLastCalledWith(FIRST_PAGE, PAGE_SIZE, null, {
            status: 'active',
            role: 'Support',
            emailConfirmed: 'false',
            lastActiveFrom: '',
            lastActiveTo: '',
        });
    });

    it('clears the table for an invalid period and reloads when the period is restored', async () => {
        const router = TestBed.inject(Router);
        const root = fixture.nativeElement as HTMLElement;
        expect(root.querySelector('.summary')?.textContent).toContain('Total users: 21');

        await router.navigate([], { queryParams: { period: 'custom', from: '2026-01-03', to: '2026-01-02' } });
        fixture.detectChanges();
        expect(usersService.getUsers).toHaveBeenCalledOnce();
        expect(root.querySelector('.summary')?.textContent).toContain('Total users: 0');
        expect(root.querySelector('tbody')?.textContent).toContain('No users found.');
        expect(root.querySelector('fd-ui-pagination nav')).toBeNull();
        expect(root.querySelector('fd-admin-load-error')).toBeNull();

        await router.navigate([], { queryParams: { period: 'all' } });
        fixture.detectChanges();
        expect(usersService.getUsers).toHaveBeenCalledTimes(SECOND_PAGE);
        expect(root.querySelector('tbody')?.textContent).toContain(pagedUsers.items[0].email);
    });
});

it('retries the applied role while keeping the edited role available', async () => {
    await setupComponentAsync();
    const router = TestBed.inject(Router);
    usersService.getUsers.mockReturnValueOnce(throwError(() => new Error('Unavailable')));
    await router.navigateByUrl('/?role=Support');
    await fixture.whenStable();
    fixture.detectChanges();

    component['draftRole'].set('Admin');
    findControl(fixture.nativeElement as HTMLElement, 'fd-admin-load-error button').click();
    await fixture.whenStable();

    expect(usersService.getUsers).toHaveBeenLastCalledWith(FIRST_PAGE, PAGE_SIZE, null, expect.objectContaining({ role: 'Support' }));
    expect(router.url).toBe('/?role=Support');
    expect(component['draftRole']()).toBe('Admin');
});

it('restores the first active page when the status select model is cleared', async () => {
    await setupComponentAsync();
    const router = TestBed.inject(Router);
    await router.navigate([], { queryParams: { page: SECOND_PAGE, status: 'inactive', role: 'Support' } });
    fixture.detectChanges();
    const select = fixture.debugElement.query(By.css('.admin-filter-bar > fd-ui-select')).injector.get(FdUiSelectComponent);
    expect(select.value()).toBe('inactive');

    select.value.set(null);
    await fixture.whenStable();
    fixture.detectChanges();

    expect(select.value()).toBe('active');
    expect(router.parseUrl(router.url).queryParams).toEqual({ page: '1', status: 'active', role: 'Support' });
    expect(usersService.getUsers).toHaveBeenLastCalledWith(
        FIRST_PAGE,
        PAGE_SIZE,
        null,
        expect.objectContaining({ status: 'active', role: 'Support' }),
    );
});

function findControl(root: ParentNode, selector: string): HTMLButtonElement {
    const control = root.querySelector<HTMLButtonElement>(selector);
    if (control === null) {
        throw new Error(`Expected control matching ${selector}.`);
    }
    return control;
}

async function setupComponentAsync(): Promise<void> {
    usersService = {
        getUsers: vi.fn(),
    };
    dialogService = { open: vi.fn() };

    usersService.getUsers.mockReturnValue(of(pagedUsers));
    dialogService.open.mockReturnValue({
        afterClosed: () => of(false),
    });

    await TestBed.configureTestingModule({
        providers: [
            provideRouter(
                [{ path: '', component: AdminUsersComponent }],
                withComponentInputBinding({ unmatchedInputBehavior: 'undefinedIfStale' }),
            ),
            provideTranslateTesting(),
            { provide: AdminUsersFacade, useValue: usersService },
            { provide: FdUiDialogService, useValue: dialogService },
        ],
    }).compileComponents();

    const harness = await RouterTestingHarness.create();
    component = await harness.navigateByUrl('/', AdminUsersComponent);
    fixture = harness.fixture;
    await fixture.whenStable();
    fixture.detectChanges();
}
