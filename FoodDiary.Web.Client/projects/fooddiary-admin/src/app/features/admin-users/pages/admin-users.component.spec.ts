import { type ComponentFixture, TestBed } from '@angular/core/testing';
import { FdUiDialogService } from 'fd-ui-kit/dialog/fd-ui-dialog.service';
import { of, Subject } from 'rxjs';
import { beforeEach, describe, expect, it, vi } from 'vitest';

import { type AdminUser, AdminUsersService, type PagedResponse } from '../api/admin-users.service';
import { AdminUserImpersonationDialogComponent } from '../dialogs/admin-user-impersonation-dialog.component';
import { AdminUsersComponent } from './admin-users.component';

const FIRST_PAGE = 1;
const SECOND_PAGE = 2;
const PAGE_SIZE = 20;
const USER_TOTAL_ITEMS = 21;
const OUT_OF_RANGE_PAGE = 99;

let component: AdminUsersComponent;
let fixture: ComponentFixture<AdminUsersComponent>;
let usersService: {
    getUsers: ReturnType<typeof vi.fn>;
};
let dialogService: { open: ReturnType<typeof vi.fn> };

const pagedUsers: PagedResponse<AdminUser> = {
    items: [
        {
            id: 'u1',
            email: 'user@example.com',
            username: 'alex',
            isActive: true,
            isEmailConfirmed: true,
            createdOnUtc: '2026-01-01T00:00:00Z',
            lastLoginAtUtc: '2026-05-23T02:06:10Z',
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
        expect(usersService.getUsers).toHaveBeenCalledWith(FIRST_PAGE, PAGE_SIZE, null, 'active');
        expect(component['users']()).toEqual(pagedUsers.items);
        expect(component['totalPages']()).toBe(SECOND_PAGE);
        expect(component['totalItems']()).toBe(USER_TOTAL_ITEMS);
        expect(component['isLoading']()).toBe(false);
    });

    it('should update search and reload from page 1', () => {
        component['onSearchChange']('john');

        expect(component['search']()).toBe('john');
        expect(component['page']()).toBe(FIRST_PAGE);
        expect(usersService.getUsers).toHaveBeenLastCalledWith(FIRST_PAGE, PAGE_SIZE, 'john', 'active');
    });

    it('should update status and reload', () => {
        component['onStatusChange']('inactive');

        expect(component['status']()).toBe('inactive');
        expect(component['page']()).toBe(FIRST_PAGE);
        expect(usersService.getUsers).toHaveBeenLastCalledWith(FIRST_PAGE, PAGE_SIZE, null, 'inactive');
    });

    it('should change page only within valid bounds', () => {
        component['goToPage'](SECOND_PAGE);
        expect(component['page']()).toBe(SECOND_PAGE);
        expect(usersService.getUsers).toHaveBeenLastCalledWith(SECOND_PAGE, PAGE_SIZE, null, 'active');

        const callCount = usersService.getUsers.mock.calls.length;
        component['goToPage'](0);
        component['goToPage'](OUT_OF_RANGE_PAGE);
        expect(usersService.getUsers.mock.calls.length).toBe(callCount);
    });

    it('should reload users after successful dialog close', () => {
        const close$ = new Subject<boolean>();
        dialogService.open.mockReturnValue({
            afterClosed: () => close$.asObservable(),
        });

        component['openEdit'](pagedUsers.items[0]);
        close$.next(true);
        close$.complete();

        expect(dialogService.open).toHaveBeenCalled();
        expect(usersService.getUsers).toHaveBeenCalledTimes(SECOND_PAGE);
    });

    it('should open impersonation dialog and start session from dialog result', () => {
        const close$ = new Subject<{ accessToken: string; expiresAtUtc: string; reason: string } | null>();
        const openSpy = vi.spyOn(window, 'open').mockImplementation(() => null);
        dialogService.open.mockReturnValue({
            afterClosed: () => close$.asObservable(),
        });

        component['startImpersonation'](pagedUsers.items[0]);
        close$.next({
            accessToken: 'token',
            expiresAtUtc: '2026-01-01T00:10:00Z',
            reason: 'Support case investigation',
        });
        close$.complete();

        expect(dialogService.open).toHaveBeenCalledWith(AdminUserImpersonationDialogComponent, {
            size: 'sm',
            data: pagedUsers.items[0],
        });
        expect(openSpy).toHaveBeenCalledWith('http://localhost:4200/dashboard?impersonationToken=token', '_blank', 'noopener,noreferrer');

        openSpy.mockRestore();
    });
});

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
        imports: [AdminUsersComponent],
        providers: [
            { provide: AdminUsersService, useValue: usersService },
            { provide: FdUiDialogService, useValue: dialogService },
        ],
    }).compileComponents();

    fixture = TestBed.createComponent(AdminUsersComponent);
    component = fixture.componentInstance;
    fixture.detectChanges();
}
