import { type ComponentFixture, TestBed } from '@angular/core/testing';
import { FdUiDialogService } from 'fd-ui-kit/dialog/fd-ui-dialog.service';
import { of, Subject } from 'rxjs';
import { beforeEach, describe, expect, it, vi } from 'vitest';

import {
    type AdminImpersonationSession,
    type AdminUser,
    type AdminUserLoginDeviceSummary,
    type AdminUserLoginEvent,
    AdminUsersService,
    type PagedResponse,
} from '../api/admin-users.service';
import { AdminUserImpersonationDialogComponent } from '../dialogs/admin-user-impersonation-dialog.component';
import { AdminUsersComponent } from './admin-users.component';

const FIRST_PAGE = 1;
const SECOND_PAGE = 2;
const THIRD_PAGE = 3;
const PAGE_SIZE = 20;
const USER_TOTAL_ITEMS = 21;
const LOGIN_TOTAL_ITEMS = 22;
const LOGIN_SUMMARY_COUNT = 7;
const OUT_OF_RANGE_PAGE = 99;

let component: AdminUsersComponent;
let fixture: ComponentFixture<AdminUsersComponent>;
let usersService: {
    getUsers: ReturnType<typeof vi.fn>;
    getImpersonationSessions: ReturnType<typeof vi.fn>;
    getLoginEvents: ReturnType<typeof vi.fn>;
    getLoginSummary: ReturnType<typeof vi.fn>;
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
            roles: ['Admin'],
        },
    ],
    page: FIRST_PAGE,
    limit: PAGE_SIZE,
    totalPages: SECOND_PAGE,
    totalItems: USER_TOTAL_ITEMS,
};

const pagedSessions: PagedResponse<AdminImpersonationSession> = {
    items: [
        {
            id: 's1',
            actorUserId: 'admin-1',
            actorEmail: 'admin@example.com',
            targetUserId: 'u1',
            targetEmail: 'user@example.com',
            reason: 'Support case',
            actorIpAddress: '127.0.0.1',
            actorUserAgent: 'Vitest',
            startedAtUtc: '2026-01-01T00:00:00Z',
        },
    ],
    page: FIRST_PAGE,
    limit: PAGE_SIZE,
    totalPages: FIRST_PAGE,
    totalItems: FIRST_PAGE,
};

const pagedLoginEvents: PagedResponse<AdminUserLoginEvent> = {
    items: [
        {
            id: 'login-1',
            userId: 'u1',
            userEmail: 'user@example.com',
            authProvider: 'password',
            maskedIpAddress: '203.0.113.0',
            userAgent: 'Vitest',
            browserName: 'Chrome',
            browserVersion: '125.0',
            operatingSystem: 'Windows',
            deviceType: 'Desktop',
            loggedInAtUtc: '2026-01-01T00:00:00Z',
        },
    ],
    page: FIRST_PAGE,
    limit: PAGE_SIZE,
    totalPages: SECOND_PAGE,
    totalItems: LOGIN_TOTAL_ITEMS,
};

const loginSummary: AdminUserLoginDeviceSummary[] = [
    {
        key: 'device:Desktop',
        count: LOGIN_SUMMARY_COUNT,
        lastSeenAtUtc: '2026-01-01T00:00:00Z',
    },
];

describe('AdminUsersComponent', () => {
    beforeEach(async () => {
        usersService = {
            getUsers: vi.fn(),
            getImpersonationSessions: vi.fn(),
            getLoginEvents: vi.fn(),
            getLoginSummary: vi.fn(),
        };
        dialogService = { open: vi.fn() };

        usersService.getUsers.mockReturnValue(of(pagedUsers));
        usersService.getImpersonationSessions.mockReturnValue(of(pagedSessions));
        usersService.getLoginEvents.mockReturnValue(of(pagedLoginEvents));
        usersService.getLoginSummary.mockReturnValue(of(loginSummary));
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
    });

    registerLoadTests();
    registerUsersPagingTests();
    registerSessionsPagingTests();
    registerLoginEventsTests();
    registerDialogTests();
});

function registerLoadTests(): void {
    describe('loading', () => {
        it('should create', () => {
            expect(component).toBeTruthy();
        });

        it('should load users on init', () => {
            expect(usersService.getUsers).toHaveBeenCalledWith(FIRST_PAGE, PAGE_SIZE, null, false);
            expect(component.users()).toEqual(pagedUsers.items);
            expect(component.totalPages()).toBe(SECOND_PAGE);
            expect(component.totalItems()).toBe(USER_TOTAL_ITEMS);
            expect(component.isLoading()).toBe(false);
        });

        it('should load impersonation sessions on init', () => {
            expect(usersService.getImpersonationSessions).toHaveBeenCalledWith(FIRST_PAGE, PAGE_SIZE, null);
            expect(component.sessions()).toEqual(pagedSessions.items);
            expect(component.sessionsTotalPages()).toBe(FIRST_PAGE);
            expect(component.sessionsTotalItems()).toBe(FIRST_PAGE);
            expect(component.isSessionsLoading()).toBe(false);
        });

        it('should load login activity on init', () => {
            expect(usersService.getLoginEvents).toHaveBeenCalledWith(FIRST_PAGE, PAGE_SIZE, null);
            expect(usersService.getLoginSummary).toHaveBeenCalled();
            expect(component.loginEvents()).toEqual(pagedLoginEvents.items);
            expect(component.loginEventsTotalPages()).toBe(SECOND_PAGE);
            expect(component.loginEventsTotalItems()).toBe(LOGIN_TOTAL_ITEMS);
            expect(component.loginSummary()).toEqual(loginSummary);
            expect(component.isLoginEventsLoading()).toBe(false);
        });
    });
}

function registerUsersPagingTests(): void {
    describe('users paging', () => {
        it('should update search and reload from page 1', () => {
            component.onSearchChange('john');

            expect(component.search()).toBe('john');
            expect(component.page()).toBe(FIRST_PAGE);
            expect(usersService.getUsers).toHaveBeenLastCalledWith(FIRST_PAGE, PAGE_SIZE, 'john', false);
        });

        it('should toggle includeDeleted and reload', () => {
            component.toggleIncludeDeleted();

            expect(component.includeDeleted()).toBe(true);
            expect(component.page()).toBe(FIRST_PAGE);
            expect(usersService.getUsers).toHaveBeenLastCalledWith(FIRST_PAGE, PAGE_SIZE, null, true);
        });

        it('should change page only within valid bounds', () => {
            component.goToPage(SECOND_PAGE);
            expect(component.page()).toBe(SECOND_PAGE);
            expect(usersService.getUsers).toHaveBeenLastCalledWith(SECOND_PAGE, PAGE_SIZE, null, false);

            const callCount = usersService.getUsers.mock.calls.length;
            component.goToPage(0);
            component.goToPage(OUT_OF_RANGE_PAGE);
            expect(usersService.getUsers.mock.calls.length).toBe(callCount);
        });
    });
}

function registerSessionsPagingTests(): void {
    describe('sessions paging', () => {
        it('should update sessions search and reload from page 1', () => {
            component.onSessionsSearchChange('admin@example.com');

            expect(component.sessionsSearch()).toBe('admin@example.com');
            expect(component.sessionsPage()).toBe(FIRST_PAGE);
            expect(usersService.getImpersonationSessions).toHaveBeenLastCalledWith(FIRST_PAGE, PAGE_SIZE, 'admin@example.com');
        });

        it('should change sessions page only within valid bounds', () => {
            usersService.getImpersonationSessions.mockReturnValue(
                of({
                    ...pagedSessions,
                    totalPages: SECOND_PAGE,
                }),
            );
            component.sessionsTotalPages.set(SECOND_PAGE);

            component.goToSessionsPage(SECOND_PAGE);
            expect(component.sessionsPage()).toBe(SECOND_PAGE);
            expect(usersService.getImpersonationSessions).toHaveBeenLastCalledWith(SECOND_PAGE, PAGE_SIZE, null);

            const callCount = usersService.getImpersonationSessions.mock.calls.length;
            component.goToSessionsPage(0);
            component.goToSessionsPage(OUT_OF_RANGE_PAGE);
            expect(usersService.getImpersonationSessions.mock.calls.length).toBe(callCount);
        });
    });
}

function registerLoginEventsTests(): void {
    describe('login events', () => {
        it('should update login event search and reload from page 1', () => {
            component.onLoginEventsSearchChange('chrome');

            expect(component.loginEventsSearch()).toBe('chrome');
            expect(component.loginEventsPage()).toBe(FIRST_PAGE);
            expect(usersService.getLoginEvents).toHaveBeenLastCalledWith(FIRST_PAGE, PAGE_SIZE, 'chrome');
        });

        it('should change login events page only within valid bounds', () => {
            usersService.getLoginEvents.mockReturnValue(
                of({
                    ...pagedLoginEvents,
                    totalPages: THIRD_PAGE,
                }),
            );
            component.loginEventsTotalPages.set(THIRD_PAGE);

            component.goToLoginEventsPage(SECOND_PAGE);
            expect(component.loginEventsPage()).toBe(SECOND_PAGE);
            expect(usersService.getLoginEvents).toHaveBeenLastCalledWith(SECOND_PAGE, PAGE_SIZE, null);

            const callCount = usersService.getLoginEvents.mock.calls.length;
            component.goToLoginEventsPage(0);
            component.goToLoginEventsPage(OUT_OF_RANGE_PAGE);
            expect(usersService.getLoginEvents.mock.calls.length).toBe(callCount);
        });
    });
}

function registerDialogTests(): void {
    describe('dialogs', () => {
        it('should reload users after successful dialog close', () => {
            const close$ = new Subject<boolean>();
            dialogService.open.mockReturnValue({
                afterClosed: () => close$.asObservable(),
            });

            component.openEdit(pagedUsers.items[0]);
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

            component.startImpersonation(pagedUsers.items[0]);
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
            expect(usersService.getImpersonationSessions).toHaveBeenCalledTimes(SECOND_PAGE);
            expect(openSpy).toHaveBeenCalledWith(
                'http://localhost:4200/dashboard?impersonationToken=token',
                '_blank',
                'noopener,noreferrer',
            );

            openSpy.mockRestore();
        });
    });
}
