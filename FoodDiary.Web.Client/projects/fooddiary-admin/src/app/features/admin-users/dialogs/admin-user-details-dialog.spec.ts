import { type ComponentFixture, TestBed } from '@angular/core/testing';
import { FD_UI_DIALOG_DATA } from 'fd-ui-kit/dialog/fd-ui-dialog-data';
import { FdUiDialogRef } from 'fd-ui-kit/dialog/fd-ui-dialog-ref';
import { of, throwError } from 'rxjs';
import { describe, expect, it, vi } from 'vitest';

import { AdminUsersFacade } from '../lib/admin-users.facade';
import type { AdminUser, AdminUserLoginEvent, AdminUserRoleAuditEvent, PagedResponse } from '../models/admin-user.models';
import { AdminUserDetailsDialogComponent } from './admin-user-details-dialog';

const ACTIVITY_PREVIEW_LIMIT = 3;

type UsersFacadeMock = {
    getUser: ReturnType<typeof vi.fn>;
    getLoginEvents: ReturnType<typeof vi.fn>;
    getUserRoleAudit: ReturnType<typeof vi.fn>;
};

type DialogRefMock = {
    close: ReturnType<typeof vi.fn>;
};

type TestContext = {
    component: AdminUserDetailsDialogComponent;
    fixture: ComponentFixture<AdminUserDetailsDialogComponent>;
    usersFacade: UsersFacadeMock;
    dialogRef: DialogRefMock;
};

const baseUser: AdminUser = {
    id: 'user-1',
    email: 'jane@example.com',
    username: 'jane',
    firstName: 'Jane',
    lastName: 'Doe',
    birthDate: '1991-01-02T00:00:00Z',
    gender: 'Female',
    weight: 71,
    desiredWeight: 65,
    desiredWaist: null,
    height: 174,
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

function host(fixture: ComponentFixture<AdminUserDetailsDialogComponent>): HTMLElement {
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
    const usersFacade = createUsersFacadeMock();
    const dialogRef: DialogRefMock = { close: vi.fn() };
    configure?.(usersFacade);

    await TestBed.configureTestingModule({
        imports: [AdminUserDetailsDialogComponent],
        providers: [
            { provide: AdminUsersFacade, useValue: usersFacade },
            { provide: FdUiDialogRef, useValue: dialogRef },
            { provide: FD_UI_DIALOG_DATA, useValue: initialUser },
        ],
    }).compileComponents();

    const fixture = TestBed.createComponent(AdminUserDetailsDialogComponent);
    const component = fixture.componentInstance;
    fixture.detectChanges();

    return { component, fixture, usersFacade, dialogRef };
}

describe('AdminUserDetailsDialogComponent', () => {
    it('loads user details and activity preview', async () => {
        const { fixture, component, usersFacade } = await createContextAsync();

        expect(component).toBeTruthy();
        expect(usersFacade.getUser).toHaveBeenCalledWith(baseUser.id);
        expect(usersFacade.getLoginEvents).toHaveBeenCalledWith(1, ACTIVITY_PREVIEW_LIMIT, null, baseUser.id);
        expect(usersFacade.getUserRoleAudit).toHaveBeenCalledWith(baseUser.id);
        expect(component['isLoading']()).toBe(false);
        expect(component['hasError']()).toBe(false);
        expect(host(fixture).textContent).toContain(baseUser.email);
        expect(host(fixture).textContent).toContain('Role history');
    });

    it('keeps details visible when activity loading fails', async () => {
        const { component } = await createContextAsync(usersFacade => {
            usersFacade.getLoginEvents.mockReturnValueOnce(throwError(() => new Error('activity failed')));
        });

        expect(component['isLoading']()).toBe(false);
        expect(component['hasError']()).toBe(false);
        expect(component['loginEvents']()).toEqual([]);
        expect(component['roleAuditEvents']()).toEqual([]);
    });

    it('shows an error when user details fail to load', async () => {
        const { component, fixture } = await createContextAsync(usersFacade => {
            usersFacade.getUser.mockReturnValueOnce(throwError(() => new Error('details failed')));
        });

        expect(component['isLoading']()).toBe(false);
        expect(component['hasError']()).toBe(true);
        expect(host(fixture).textContent).toContain('Could not load user details.');
    });

    it('closes with expected action results', async () => {
        const { component, dialogRef } = await createContextAsync();

        component['edit']();
        component['impersonate']();
        component['close']();

        expect(dialogRef.close).toHaveBeenCalledWith('edit');
        expect(dialogRef.close).toHaveBeenCalledWith('impersonate');
        expect(dialogRef.close).toHaveBeenCalledWith(null);
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
