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

describe('AdminUsersComponent', () => {
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
        page: 1,
        limit: 20,
        totalPages: 2,
        totalItems: 21,
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
        page: 1,
        limit: 20,
        totalPages: 1,
        totalItems: 1,
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
        page: 1,
        limit: 20,
        totalPages: 2,
        totalItems: 22,
    };

    const loginSummary: AdminUserLoginDeviceSummary[] = [
        {
            key: 'device:Desktop',
            count: 7,
            lastSeenAtUtc: '2026-01-01T00:00:00Z',
        },
    ];

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

    it('should create', () => {
        expect(component).toBeTruthy();
    });

    it('should load users on init', () => {
        expect(usersService.getUsers).toHaveBeenCalledWith(1, 20, null, false);
        expect(component.users()).toEqual(pagedUsers.items);
        expect(component.totalPages()).toBe(2);
        expect(component.totalItems()).toBe(21);
        expect(component.isLoading()).toBe(false);
    });

    it('should load impersonation sessions on init', () => {
        expect(usersService.getImpersonationSessions).toHaveBeenCalledWith(1, 20, null);
        expect(component.sessions()).toEqual(pagedSessions.items);
        expect(component.sessionsTotalPages()).toBe(1);
        expect(component.sessionsTotalItems()).toBe(1);
        expect(component.isSessionsLoading()).toBe(false);
    });

    it('should load login activity on init', () => {
        expect(usersService.getLoginEvents).toHaveBeenCalledWith(1, 20, null);
        expect(usersService.getLoginSummary).toHaveBeenCalled();
        expect(component.loginEvents()).toEqual(pagedLoginEvents.items);
        expect(component.loginEventsTotalPages()).toBe(2);
        expect(component.loginEventsTotalItems()).toBe(22);
        expect(component.loginSummary()).toEqual(loginSummary);
        expect(component.isLoginEventsLoading()).toBe(false);
    });

    it('should update search and reload from page 1', () => {
        component.onSearchChange('john');

        expect(component.search()).toBe('john');
        expect(component.page()).toBe(1);
        expect(usersService.getUsers).toHaveBeenLastCalledWith(1, 20, 'john', false);
    });

    it('should toggle includeDeleted and reload', () => {
        component.toggleIncludeDeleted();

        expect(component.includeDeleted()).toBe(true);
        expect(component.page()).toBe(1);
        expect(usersService.getUsers).toHaveBeenLastCalledWith(1, 20, null, true);
    });

    it('should change page only within valid bounds', () => {
        component.goToPage(2);
        expect(component.page()).toBe(2);
        expect(usersService.getUsers).toHaveBeenLastCalledWith(2, 20, null, false);

        const callCount = usersService.getUsers.mock.calls.length;
        component.goToPage(0);
        component.goToPage(99);
        expect(usersService.getUsers.mock.calls.length).toBe(callCount);
    });

    it('should update sessions search and reload from page 1', () => {
        component.onSessionsSearchChange('admin@example.com');

        expect(component.sessionsSearch()).toBe('admin@example.com');
        expect(component.sessionsPage()).toBe(1);
        expect(usersService.getImpersonationSessions).toHaveBeenLastCalledWith(1, 20, 'admin@example.com');
    });

    it('should change sessions page only within valid bounds', () => {
        usersService.getImpersonationSessions.mockReturnValue(
            of({
                ...pagedSessions,
                totalPages: 2,
            }),
        );
        component.sessionsTotalPages.set(2);

        component.goToSessionsPage(2);
        expect(component.sessionsPage()).toBe(2);
        expect(usersService.getImpersonationSessions).toHaveBeenLastCalledWith(2, 20, null);

        const callCount = usersService.getImpersonationSessions.mock.calls.length;
        component.goToSessionsPage(0);
        component.goToSessionsPage(99);
        expect(usersService.getImpersonationSessions.mock.calls.length).toBe(callCount);
    });

    it('should update login event search and reload from page 1', () => {
        component.onLoginEventsSearchChange('chrome');

        expect(component.loginEventsSearch()).toBe('chrome');
        expect(component.loginEventsPage()).toBe(1);
        expect(usersService.getLoginEvents).toHaveBeenLastCalledWith(1, 20, 'chrome');
    });

    it('should change login events page only within valid bounds', () => {
        usersService.getLoginEvents.mockReturnValue(
            of({
                ...pagedLoginEvents,
                totalPages: 3,
            }),
        );
        component.loginEventsTotalPages.set(3);

        component.goToLoginEventsPage(2);
        expect(component.loginEventsPage()).toBe(2);
        expect(usersService.getLoginEvents).toHaveBeenLastCalledWith(2, 20, null);

        const callCount = usersService.getLoginEvents.mock.calls.length;
        component.goToLoginEventsPage(0);
        component.goToLoginEventsPage(99);
        expect(usersService.getLoginEvents.mock.calls.length).toBe(callCount);
    });

    it('should reload users after successful dialog close', () => {
        const close$ = new Subject<boolean>();
        dialogService.open.mockReturnValue({
            afterClosed: () => close$.asObservable(),
        });

        component.openEdit(pagedUsers.items[0]);
        close$.next(true);
        close$.complete();

        expect(dialogService.open).toHaveBeenCalled();
        expect(usersService.getUsers).toHaveBeenCalledTimes(2);
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
        expect(usersService.getImpersonationSessions).toHaveBeenCalledTimes(2);
        expect(openSpy).toHaveBeenCalledWith('http://localhost:4200/dashboard?impersonationToken=token', '_blank', 'noopener,noreferrer');

        openSpy.mockRestore();
    });
});
