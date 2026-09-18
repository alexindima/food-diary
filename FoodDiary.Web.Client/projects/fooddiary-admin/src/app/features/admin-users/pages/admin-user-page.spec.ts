import { type ComponentFixture, TestBed } from '@angular/core/testing';
import { ActivatedRoute, convertToParamMap, provideRouter } from '@angular/router';
import { FdUiDialogService } from 'fd-ui-kit';
import { BehaviorSubject, of, Subject, throwError } from 'rxjs';
import { describe, expect, it, vi } from 'vitest';

import { provideTranslateTesting } from '../../../../../../../src/testing/translate-testing.module';
import { AdminUserEditDialogComponent } from '../dialogs/admin-user-edit-dialog';
import { AdminUserImpersonationDialogComponent } from '../dialogs/admin-user-impersonation-dialog';
import { AdminUserSetPasswordDialogComponent } from '../dialogs/admin-user-set-password-dialog';
import { AdminUsersFacade } from '../lib/admin-users.facade';
import type { AdminUser, AdminUserLoginEvent, AdminUserRoleAuditEvent, PagedResponse } from '../models/admin-user.models';
import { AdminUserPageComponent } from './admin-user-page';

const EXPECTED_ACTION_COUNT = 3;

const ACTIVITY_PREVIEW_LIMIT = 3;

type UsersFacadeMock = {
    getUser: ReturnType<typeof vi.fn>;
    getLoginEvents: ReturnType<typeof vi.fn>;
    getUserRoleAudit: ReturnType<typeof vi.fn>;
};

type DialogServiceMock = {
    open: ReturnType<typeof vi.fn>;
};

type TestContext = {
    component: AdminUserPageComponent;
    fixture: ComponentFixture<AdminUserPageComponent>;
    usersFacade: UsersFacadeMock;
    dialogs: DialogServiceMock;
    routeParams: BehaviorSubject<ReturnType<typeof convertToParamMap>>;
};

const baseUser: AdminUser = {
    id: 'user-1',
    email: 'jane@example.com',
    username: 'jane',
    firstName: 'Jane',
    lastName: 'Doe',
    birthDate: '1991-01-02T00:00:00Z',
    gender: 'Female',
    weightKg: 71,
    desiredWeightKg: 65,
    desiredWaistCm: null,
    heightCm: 174,
    activityLevel: 'Moderate',
    dailyCalorieTarget: 2100,
    proteinTarget: 120,
    fatTarget: 70,
    carbTarget: 220,
    fiberTarget: 30,
    stepGoal: 8000,
    waterGoal: 2000,
    hydrationGoal: null,
    calorieCyclingEnabled: true,
    mondayCalories: 2000,
    tuesdayCalories: 2100,
    wednesdayCalories: 2200,
    thursdayCalories: 2100,
    fridayCalories: 2000,
    saturdayCalories: 2300,
    sundayCalories: 1900,
    profileImage: null,
    profileImageAssetId: 'asset-1',
    dashboardLayoutJson: '',
    language: 'en',
    theme: 'dark',
    uiStyle: 'compact',
    pushNotificationsEnabled: true,
    fastingPushNotificationsEnabled: false,
    socialPushNotificationsEnabled: true,
    fastingCheckInReminderHours: 12,
    fastingCheckInFollowUpReminderHours: 24,
    telegramUserId: 12345,
    isActive: true,
    isEmailConfirmed: true,
    hasPassword: true,
    createdOnUtc: '2026-01-01T00:00:00Z',
    deletedAt: null,
    lastLoginAtUtc: '2026-02-01T00:00:00Z',
    roles: ['User'],
    aiInputTokenLimit: 1000,
    aiOutputTokenLimit: 2000,
    aiConsentAcceptedAt: '2026-01-02T00:00:00Z',
};

const loginEvent: AdminUserLoginEvent = {
    id: 'login-1',
    userId: baseUser.id,
    userEmail: baseUser.email,
    authProvider: 'Password',
    maskedIpAddress: '127.0.0.*',
    userAgent: 'browser',
    browserName: 'Chrome',
    browserVersion: '120',
    operatingSystem: 'Windows',
    deviceType: 'Desktop',
    loggedInAtUtc: '2026-02-02T00:00:00Z',
};

const roleAuditEvent: AdminUserRoleAuditEvent = {
    id: 'role-1',
    userId: baseUser.id,
    roleName: 'Support',
    action: 'Added',
    actorUserId: 'actor-1',
    actorEmail: 'admin@example.com',
    source: 'AdminPanel',
    occurredAtUtc: '2026-02-03T00:00:00Z',
};

function pagedLogins(items: AdminUserLoginEvent[]): PagedResponse<AdminUserLoginEvent> {
    return {
        items,
        page: 1,
        limit: ACTIVITY_PREVIEW_LIMIT,
        totalPages: 1,
        totalItems: items.length,
    };
}

function host(fixture: ComponentFixture<AdminUserPageComponent>): HTMLElement {
    return fixture.nativeElement as HTMLElement;
}

function createUsersFacadeMock(): UsersFacadeMock {
    return {
        getUser: vi.fn().mockReturnValue(of(baseUser)),
        getLoginEvents: vi.fn().mockReturnValue(of(pagedLogins([loginEvent]))),
        getUserRoleAudit: vi.fn().mockReturnValue(of([roleAuditEvent])),
    };
}

async function createContextAsync(configure?: (usersFacade: UsersFacadeMock) => void, initialUser = baseUser): Promise<TestContext> {
    const routeParams = new BehaviorSubject(convertToParamMap({ id: initialUser.id }));
    const usersFacade = createUsersFacadeMock();
    const dialogs: DialogServiceMock = { open: vi.fn().mockReturnValue({ afterClosed: () => of(false) }) };
    configure?.(usersFacade);

    await TestBed.configureTestingModule({
        imports: [AdminUserPageComponent],
        providers: [
            provideTranslateTesting(),
            { provide: AdminUsersFacade, useValue: usersFacade },
            provideRouter([]),
            { provide: FdUiDialogService, useValue: dialogs },
            { provide: ActivatedRoute, useValue: { paramMap: routeParams } },
        ],
    }).compileComponents();

    const fixture = TestBed.createComponent(AdminUserPageComponent);
    const component = fixture.componentInstance;
    fixture.detectChanges();

    return { component, fixture, usersFacade, dialogs, routeParams };
}

describe('AdminUserPageComponent', () => {
    it('renders a Telegram-only account without email or names', async () => {
        const telegramUser: AdminUser = {
            ...baseUser,
            email: null,
            firstName: '',
            lastName: '',
            hasPassword: false,
            isEmailConfirmed: false,
        };
        const { component, fixture } = await createContextAsync(usersFacade => {
            usersFacade.getUser.mockReturnValueOnce(of(telegramUser));
        }, telegramUser);

        expect(component['initials']()).toBe('?');
        expect(component['sections']().flatMap(section => section.fields)).toContainEqual({ label: 'Email', value: '-' });
        expect(component['failed']()).toBe(false);
        expect(host(fixture).textContent).not.toContain('Could not load user details.');
    });

    it('loads user details and activity preview', async () => {
        const { fixture, component, usersFacade } = await createContextAsync();

        expect(component).toBeTruthy();
        expect(usersFacade.getUser).toHaveBeenCalledWith(baseUser.id);
        expect(usersFacade.getLoginEvents).toHaveBeenCalledWith(1, ACTIVITY_PREVIEW_LIMIT, null, { userId: baseUser.id });
        expect(usersFacade.getUserRoleAudit).toHaveBeenCalledWith(baseUser.id);
        expect(component['activityLoading']()).toBe(false);
        expect(component['failed']()).toBe(false);
        expect(host(fixture).textContent).toContain(baseUser.email);
        expect(host(fixture).textContent).toContain('Role history');
    });

    it('keeps details visible when activity loading fails', async () => {
        const { component } = await createContextAsync(usersFacade => {
            usersFacade.getLoginEvents.mockReturnValueOnce(throwError(() => new Error('activity failed')));
        });

        expect(component['activityLoading']()).toBe(false);
        expect(component['failed']()).toBe(false);
        expect(component['activityFailed']()).toBe(true);
        expect(component['sections']()).not.toHaveLength(0);
        expect(component['loginEvents']()).toEqual([]);
        expect(component['roleAuditEvents']()).toEqual([]);
    });

    it('shows an error when user details fail to load', async () => {
        const { component, fixture } = await createContextAsync(usersFacade => {
            usersFacade.getUser.mockReturnValueOnce(throwError(() => new Error('details failed')));
        });

        expect(component['activityLoading']()).toBe(false);
        expect(component['failed']()).toBe(true);
        expect(host(fixture).querySelector('fd-admin-load-error')).not.toBeNull();
    });
});

describe('AdminUserPageComponent actions and navigation', () => {
    it('exposes edit, password and impersonation actions directly on the page', async () => {
        const { component, dialogs, usersFacade } = await createContextAsync();
        dialogs.open.mockReturnValue({ afterClosed: () => of(true) });
        component['edit'](baseUser);
        component['setPassword'](baseUser);
        expect(dialogs.open).toHaveBeenCalledWith(AdminUserEditDialogComponent, { size: 'sm', data: baseUser });
        expect(dialogs.open).toHaveBeenCalledWith(AdminUserSetPasswordDialogComponent, { size: 'sm', data: baseUser });
        expect(usersFacade.getUser).toHaveBeenCalledTimes(EXPECTED_ACTION_COUNT);
        dialogs.open.mockReturnValue({ afterClosed: () => of(null) });
        component['impersonate'](baseUser);
        expect(dialogs.open).toHaveBeenCalledWith(AdminUserImpersonationDialogComponent, { size: 'sm', data: baseUser });
    });

    it('drops stale activity when navigating to a different account', async () => {
        const pendingLogins = new Subject<PagedResponse<AdminUserLoginEvent>>();
        const { component, usersFacade, routeParams } = await createContextAsync(facade => {
            facade.getLoginEvents.mockReturnValueOnce(pendingLogins);
        });
        expect(component['activityLoading']()).toBe(true);
        const nextUser = { ...baseUser, id: 'user-2', email: 'second@example.com' };
        usersFacade.getUser.mockReturnValueOnce(of(nextUser));
        usersFacade.getLoginEvents.mockReturnValueOnce(of(pagedLogins([])));
        usersFacade.getUserRoleAudit.mockReturnValueOnce(of([]));
        routeParams.next(convertToParamMap({ id: nextUser.id }));
        pendingLogins.next(pagedLogins([loginEvent]));
        pendingLogins.complete();
        expect(component['user']()?.id).toBe(nextUser.id);
        expect(component['loginEvents']()).toEqual([]);
        expect(component['roleAuditEvents']()).toEqual([]);
    });

    it('disables impersonation for admin and deleted users', async () => {
        const adminUser: AdminUser = { ...baseUser, roles: ['Admin'] };
        const { component } = await createContextAsync(usersFacade => {
            usersFacade.getUser.mockReturnValueOnce(of(adminUser));
        }, adminUser);

        expect(component['canImpersonate']()).toBe(false);

        component['user'].set({ ...baseUser, deletedAt: '2026-03-01T00:00:00Z' });

        expect(component['canImpersonate']()).toBe(false);
    });

    it('formats initials and fallback fields', async () => {
        const { component } = await createContextAsync();

        expect(component['initials']()).toBe('JD');
        expect(component['sections']()[0]?.fields.some(field => field.label === 'Roles' && field.value === 'User')).toBe(true);

        component['user'].set({ ...baseUser, firstName: '', lastName: '', email: 'fallback@example.com', roles: [] });

        expect(component['initials']()).toBe('F');
        expect(component['sections']()[0]?.fields.some(field => field.label === 'Roles' && field.value === '-')).toBe(true);
    });
});
