import { DOCUMENT } from '@angular/common';
import { type ComponentFixture, TestBed } from '@angular/core/testing';
import { ActivatedRoute, provideRouter, UrlSegment } from '@angular/router';
import { FdUiDialogService } from 'fd-ui-kit';
import { BehaviorSubject, of, Subject, throwError } from 'rxjs';
import { describe, expect, it, vi } from 'vitest';

import { provideTranslateTesting } from '../../../../../../../src/testing/translate-testing.module';
import { adminCalendarDate, adminId, adminUtcInstant } from '../../../shared/models/semantics/admin-meaning';
import { AdminUsersService } from '../api/admin-users.service';
import { AdminUserEditDialogComponent } from '../dialogs/admin-user-edit-dialog';
import { AdminUserImpersonationDialogComponent } from '../dialogs/admin-user-impersonation-dialog';
import { AdminUserNotificationDialogComponent } from '../dialogs/admin-user-notification-dialog';
import { AdminUserSetPasswordDialogComponent } from '../dialogs/admin-user-set-password-dialog';
import type { AdminUser, AdminUserLoginEvent, AdminUserRoleAuditEvent, PagedResponse } from '../models/admin-user.models';
import { AdminUserPageComponent } from './admin-user-page';

const EXPECTED_ACTION_COUNT = 3;

const ACTIVITY_PREVIEW_LIMIT = 3;

type UsersServiceMock = {
    getUser: ReturnType<typeof vi.fn>;
    getPremiumGrant: ReturnType<typeof vi.fn>;
    getLoginEvents: ReturnType<typeof vi.fn>;
    getUserRoleAudit: ReturnType<typeof vi.fn>;
};

type DialogServiceMock = {
    open: ReturnType<typeof vi.fn>;
};

type TestContext = {
    component: AdminUserPageComponent;
    fixture: ComponentFixture<AdminUserPageComponent>;
    usersService: UsersServiceMock;
    dialogs: DialogServiceMock;
    routeSegments: BehaviorSubject<UrlSegment[]>;
};

const baseUser: AdminUser = {
    id: adminId<'user'>('user-1'),
    email: 'jane@example.com',
    username: 'jane',
    firstName: 'Jane',
    lastName: 'Doe',
    birthDate: adminCalendarDate('1991-01-02T00:00:00Z'),
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
    profileImageAssetId: adminId<'image-asset'>('asset-1'),
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
    createdOnUtc: adminUtcInstant('2026-01-01T00:00:00Z'),
    deletedAt: null,
    lastActiveAtUtc: adminUtcInstant('2026-02-01T00:00:00Z'),
    roles: ['User'],
    aiInputTokenLimit: 1000,
    aiOutputTokenLimit: 2000,
    aiConsentAcceptedAt: '2026-01-02T00:00:00Z',
};

const loginEvent: AdminUserLoginEvent = {
    id: adminId<'login-event'>('login-1'),
    userId: baseUser.id,
    userEmail: baseUser.email,
    authProvider: 'Password',
    maskedIpAddress: '127.0.0.*',
    userAgent: 'browser',
    browserName: 'Chrome',
    browserVersion: '120',
    operatingSystem: 'Windows',
    deviceType: 'Desktop',
    loggedInAtUtc: adminUtcInstant('2026-02-02T00:00:00Z'),
};

const roleAuditEvent: AdminUserRoleAuditEvent = {
    id: adminId<'role-audit-event'>('role-1'),
    userId: baseUser.id,
    roleName: 'Support',
    action: 'Added',
    actorUserId: adminId<'user'>('actor-1'),
    actorEmail: 'admin@example.com',
    source: 'AdminPanel',
    occurredAtUtc: adminUtcInstant('2026-02-03T00:00:00Z'),
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

function detailField(fixture: ComponentFixture<AdminUserPageComponent>, label: string): string | null | undefined {
    const term = [...host(fixture).querySelectorAll('dt')].find(field => field.textContent === label);
    return term?.nextElementSibling?.textContent;
}

function createUsersServiceMock(): UsersServiceMock {
    return {
        getUser: vi.fn().mockReturnValue(of(baseUser)),
        getPremiumGrant: vi.fn().mockReturnValue(of(null)),
        getLoginEvents: vi.fn().mockReturnValue(of(pagedLogins([loginEvent]))),
        getUserRoleAudit: vi.fn().mockReturnValue(of([roleAuditEvent])),
    };
}

async function createContextAsync(configure?: (usersService: UsersServiceMock) => void, initialUser = baseUser): Promise<TestContext> {
    const routeSegments = new BehaviorSubject([new UrlSegment(initialUser.id, {})]);
    const usersService = createUsersServiceMock();
    const dialogs: DialogServiceMock = { open: vi.fn().mockReturnValue({ afterClosed: () => of(false) }) };
    configure?.(usersService);

    await TestBed.configureTestingModule({
        imports: [AdminUserPageComponent],
        providers: [
            provideTranslateTesting(),
            { provide: AdminUsersService, useValue: usersService },
            provideRouter([]),
            { provide: FdUiDialogService, useValue: dialogs },
            { provide: ActivatedRoute, useValue: { url: routeSegments } },
        ],
    }).compileComponents();

    const fixture = TestBed.createComponent(AdminUserPageComponent);
    const component = fixture.componentInstance;
    fixture.detectChanges();

    return { component, fixture, usersService, dialogs, routeSegments };
}

describe('AdminUserPageComponent avatars', () => {
    it('renders an external HTTPS avatar and falls back to initials after a loading error', async () => {
        const account = { ...baseUser, profileImage: 'https://images.example.test/avatar.png' };
        const { fixture, component } = await createContextAsync(users => {
            users.getUser.mockReturnValue(of(account));
        });
        const image = host(fixture).querySelector<HTMLImageElement>('img.admin-user-details__avatar');
        expect(image?.src).toBe(account.profileImage);
        expect(image?.getAttribute('referrerpolicy')).toBe('no-referrer');

        image?.dispatchEvent(new Event('error'));
        fixture.detectChanges();

        expect(host(fixture).querySelector('img.admin-user-details__avatar')).toBeNull();
        expect(host(fixture).querySelector('.admin-user-details__avatar--initials')?.textContent).toBe('JD');

        component['reload']();
        fixture.detectChanges();
        expect(host(fixture).querySelector<HTMLImageElement>('img.admin-user-details__avatar')?.src).toBe(account.profileImage);
    });

    it('does not request an insecure remote avatar', async () => {
        const { fixture } = await createContextAsync(users => {
            users.getUser.mockReturnValue(of({ ...baseUser, profileImage: 'http://images.example.test/avatar.png' }));
        });

        expect(host(fixture).querySelector('img.admin-user-details__avatar')).toBeNull();
        expect(host(fixture).querySelector('.admin-user-details__avatar--initials')?.textContent).toBe('JD');
    });

    it('does not apply an old avatar error to a different account', async () => {
        const avatar = 'https://images.example.test/avatar.png';
        const first = { ...baseUser, profileImage: avatar };
        const { component, fixture, usersService, routeSegments } = await createContextAsync(users => {
            users.getUser.mockReturnValueOnce(of(first));
        });
        const next = { ...baseUser, id: adminId<'user'>('next-user'), profileImage: avatar };
        usersService.getUser.mockReturnValueOnce(of(next));
        routeSegments.next([new UrlSegment(next.id, {})]);
        fixture.detectChanges();

        component['onAvatarError'](first.id, avatar);
        fixture.detectChanges();

        expect(host(fixture).querySelector<HTMLImageElement>('img.admin-user-details__avatar')?.src).toBe(avatar);
    });
});

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
        const { component, fixture } = await createContextAsync(usersService => {
            usersService.getUser.mockReturnValueOnce(of(telegramUser));
        }, telegramUser);

        expect(host(fixture).querySelector('.admin-user-details__avatar--initials')?.textContent).toBe('?');
        expect(detailField(fixture, 'Email')).toBe('-');
        expect(component['failed']()).toBe(false);
        expect(host(fixture).textContent).not.toContain('Could not load user details.');
    });

    it('loads user details and activity preview', async () => {
        const { fixture, component, usersService } = await createContextAsync();

        expect(component).toBeTruthy();
        expect(usersService.getUser).toHaveBeenCalledWith(baseUser.id);
        expect(usersService.getLoginEvents).toHaveBeenCalledWith(1, ACTIVITY_PREVIEW_LIMIT, null, { userId: baseUser.id });
        expect(usersService.getUserRoleAudit).toHaveBeenCalledWith(baseUser.id);
        expect(component['activityLoading']()).toBe(false);
        expect(component['failed']()).toBe(false);
        expect(host(fixture).textContent).toContain(baseUser.email);
        expect(host(fixture).textContent).toContain('Role history');
    });

    it('keeps details visible when activity loading fails', async () => {
        const { component, fixture } = await createContextAsync(usersService => {
            usersService.getLoginEvents.mockReturnValueOnce(throwError(() => new Error('activity failed')));
        });

        expect(component['activityLoading']()).toBe(false);
        expect(component['failed']()).toBe(false);
        expect(component['activityFailed']()).toBe(true);
        expect(detailField(fixture, 'Email')).toBe(baseUser.email);
        expect(component['loginEvents']()).toEqual([]);
        expect(component['roleAuditEvents']()).toEqual([]);
    });

    it('shows an error when user details fail to load', async () => {
        const { component, fixture } = await createContextAsync(usersService => {
            usersService.getUser.mockReturnValueOnce(throwError(() => new Error('details failed')));
        });

        expect(component['activityLoading']()).toBe(false);
        expect(component['failed']()).toBe(true);
        expect(host(fixture).querySelector('fd-admin-load-error')).not.toBeNull();
    });
});

describe('AdminUserPageComponent actions and navigation', () => {
    it('opens test notifications for the selected active user', async () => {
        const { component, dialogs } = await createContextAsync();
        component['sendNotification'](baseUser);
        expect(dialogs.open).toHaveBeenCalledWith(AdminUserNotificationDialogComponent, { size: 'sm', data: baseUser });
    });

    it('disables notifications for inactive and deleted users', async () => {
        const { component, dialogs } = await createContextAsync();
        for (const user of [
            { ...baseUser, isActive: false },
            { ...baseUser, deletedAt: adminUtcInstant('2026-10-31T12:00:00Z') },
        ]) {
            component['user'].set(user);
            expect(component['canNotify']()).toBe(false);
            component['sendNotification'](user);
        }
        expect(dialogs.open).not.toHaveBeenCalled();
    });
    it('exposes edit, password and impersonation actions directly on the page', async () => {
        const { component, dialogs, usersService } = await createContextAsync();
        dialogs.open.mockReturnValue({ afterClosed: () => of(true) });
        component['edit'](baseUser);
        component['setPassword'](baseUser);
        expect(dialogs.open).toHaveBeenCalledWith(AdminUserEditDialogComponent, { size: 'sm', data: baseUser });
        expect(dialogs.open).toHaveBeenCalledWith(AdminUserSetPasswordDialogComponent, { size: 'sm', data: baseUser });
        expect(usersService.getUser).toHaveBeenCalledTimes(EXPECTED_ACTION_COUNT);
        dialogs.open.mockReturnValue({ afterClosed: () => of(null) });
        component['impersonate'](baseUser);
        expect(dialogs.open).toHaveBeenCalledWith(AdminUserImpersonationDialogComponent, { size: 'sm', data: baseUser });
    });

    it('passes the impersonation code in the URL fragment', async () => {
        const { component, dialogs } = await createContextAsync();
        const view = TestBed.inject(DOCUMENT).defaultView;
        if (view === null) {
            throw new Error('A browser window is required for the handoff test');
        }
        const open = vi.spyOn(view, 'open').mockReturnValue(null);
        dialogs.open.mockReturnValue({ afterClosed: () => of({ code: 'one-time-code' }) });

        component['impersonate'](baseUser);

        expect(open).toHaveBeenCalledOnce();
        const destination = new URL(String(open.mock.calls[0]?.[0]));
        expect(destination.searchParams.has('impersonationCode')).toBe(false);
        expect(new URLSearchParams(destination.hash.slice(1)).get('impersonationCode')).toBe('one-time-code');
        open.mockRestore();
    });

    it('drops stale activity when navigating to a different account', async () => {
        const pendingLogins = new Subject<PagedResponse<AdminUserLoginEvent>>();
        const { component, usersService, routeSegments } = await createContextAsync(facade => {
            facade.getLoginEvents.mockReturnValueOnce(pendingLogins);
        });
        expect(component['activityLoading']()).toBe(true);
        const nextUser = { ...baseUser, id: 'user-2', email: 'second@example.com' };
        usersService.getUser.mockReturnValueOnce(of(nextUser));
        usersService.getLoginEvents.mockReturnValueOnce(of(pagedLogins([])));
        usersService.getUserRoleAudit.mockReturnValueOnce(of([]));
        routeSegments.next([new UrlSegment(nextUser.id, {})]);
        pendingLogins.next(pagedLogins([loginEvent]));
        pendingLogins.complete();
        expect(component['user']()?.id).toBe(nextUser.id);
        expect(component['loginEvents']()).toEqual([]);
        expect(component['roleAuditEvents']()).toEqual([]);
    });

    it('disables impersonation for admin and deleted users', async () => {
        const adminUser: AdminUser = { ...baseUser, roles: ['Admin'] };
        const { component } = await createContextAsync(usersService => {
            usersService.getUser.mockReturnValueOnce(of(adminUser));
        }, adminUser);

        expect(component['canImpersonate']()).toBe(false);

        component['user'].set({ ...baseUser, deletedAt: adminUtcInstant('2026-03-01T00:00:00Z') });

        expect(component['canImpersonate']()).toBe(false);
    });
});

describe('Admin account detail fallback fields', () => {
    it('formats initials and fallback fields', async () => {
        const { fixture, usersService, routeSegments } = await createContextAsync();

        expect(host(fixture).querySelector('.admin-user-details__avatar--initials')?.textContent).toBe('JD');
        expect(detailField(fixture, 'Roles')).toBe('User');

        const alternate = {
            ...baseUser,
            id: adminId<'user'>('fallback-user'),
            firstName: '',
            lastName: '',
            email: 'fallback@example.com',
            roles: [],
        };
        usersService.getUser.mockReturnValueOnce(of(alternate));
        routeSegments.next([new UrlSegment(alternate.id, {})]);
        fixture.detectChanges();

        expect(host(fixture).querySelector('.admin-user-details__avatar--initials')?.textContent).toBe('F');
        expect(detailField(fixture, 'Roles')).toBe('-');
    });
});

describe('Admin account detail action results', () => {
    it('ignores a click on a stale impersonation button while changing to an admin account', async () => {
        const { fixture, dialogs, usersService, routeSegments } = await createContextAsync();
        const action = [...host(fixture).querySelectorAll('button')].find(button => button.textContent.trim() === 'ADMIN_USER.IMPERSONATE');
        if (action === undefined) {
            throw new Error('Expected the impersonation action.');
        }
        const admin = { ...baseUser, id: adminId<'user'>('other-admin'), email: 'other-admin@example.test', roles: ['Admin'] };
        usersService.getUser.mockReturnValueOnce(of(admin));
        routeSegments.next([new UrlSegment(admin.id, {})]);

        expect(action.disabled).toBe(false);
        action.click();
        expect(dialogs.open).not.toHaveBeenCalled();

        fixture.detectChanges();
        expect(action.disabled).toBe(true);
        expect(host(fixture).querySelector('h1')?.textContent).toBe(admin.email);
    });

    it.each([
        { label: 'ADMIN_USER.EDIT', dialog: AdminUserEditDialogComponent },
        { label: 'ADMIN_USER.SET_PASSWORD', dialog: AdminUserSetPasswordDialogComponent },
    ])('reloads the visible account after a successful $label but keeps it on cancellation', async ({ label, dialog }) => {
        const { fixture, dialogs, usersService } = await createContextAsync();
        const action = [...host(fixture).querySelectorAll('button')].find(button => button.textContent.trim() === label);
        if (action === undefined) {
            throw new Error(`Expected the ${label} action.`);
        }
        action.click();
        expect(dialogs.open).toHaveBeenCalledWith(dialog, { size: 'sm', data: baseUser });
        expect(usersService.getUser).toHaveBeenCalledOnce();

        const pending = new Subject<AdminUser>();
        usersService.getUser.mockReturnValueOnce(pending);
        dialogs.open.mockReturnValueOnce({ afterClosed: () => of(true) });
        action.click();
        fixture.detectChanges();
        expect(host(fixture).querySelector('[role="status"]')?.textContent).toContain('ADMIN_COMMON.LOADING');

        pending.next({ ...baseUser, email: 'updated@example.test' });
        pending.complete();
        fixture.detectChanges();
        expect(host(fixture).querySelector('h1')?.textContent).toBe('updated@example.test');
        expect(usersService.getUser).toHaveBeenLastCalledWith(baseUser.id);
    });

    it('does not open dialogs or show Premium grant controls for a deleted account', async () => {
        const account = { ...baseUser, deletedAt: adminUtcInstant('2026-03-01T00:00:00Z') };
        const { fixture, dialogs } = await createContextAsync(users => {
            users.getUser.mockReturnValueOnce(of(account));
        });
        const root = host(fixture);
        for (const label of ['ADMIN_NOTIFICATION.TITLE', 'ADMIN_USER.IMPERSONATE']) {
            const action = [...root.querySelectorAll('button')].find(button => button.textContent.trim() === label);
            if (action === undefined) {
                throw new Error(`Expected the ${label} action.`);
            }
            expect(action.disabled).toBe(true);
            action.click();
        }
        expect(dialogs.open).not.toHaveBeenCalled();
        expect(root.querySelector('fd-admin-user-premium-grant')).toBeNull();
    });
});
