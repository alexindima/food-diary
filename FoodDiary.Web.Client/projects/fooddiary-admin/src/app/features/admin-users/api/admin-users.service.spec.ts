/* eslint-disable max-lines-per-function -- Service contract spec intentionally groups endpoint cases. */
import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { TestBed } from '@angular/core/testing';
import { firstValueFrom } from 'rxjs';
import { afterEach, beforeEach, describe, expect, it } from 'vitest';

import { environment } from '../../../../environments/environment';
import { adminId, adminUtcInstant } from '../../../shared/models/semantics/admin-meaning';
import type { AdminUser, AdminUserCreate } from '../models/admin-user.models';
import { AdminUsersService } from './admin-users.service';

const LOGIN_EVENTS_PAGE = 1;
const LOGIN_EVENTS_LIMIT = 3;
const ROLE_AUDIT_LIMIT = 10;
const USERS_PAGE = 2;
const USERS_LIMIT = 20;
const USERS_TOTAL_PAGES = 3;
const USERS_TOTAL_ITEMS = 41;

describe('AdminUsersService', () => {
    let service: AdminUsersService;
    let httpMock: HttpTestingController;

    const baseUrl = `${environment.apiUrls.auth.replace(/\/auth$/, '')}/admin/users`;

    beforeEach(() => {
        TestBed.configureTestingModule({
            providers: [AdminUsersService, provideHttpClient(), provideHttpClientTesting()],
        });

        service = TestBed.inject(AdminUsersService);
        httpMock = TestBed.inject(HttpTestingController);
    });

    afterEach(() => {
        httpMock.verify();
    });

    it('reads and writes manual Premium independently of the user and billing endpoints', () => {
        const user = adminId<'user'>('grant-user');
        const path = `${baseUrl}/${user}/premium-grant`;
        const grant = { endsAtUtc: adminUtcInstant('2026-12-12T12:30:00.000Z'), reason: 'Test access' };
        const empty = { endsAtUtc: null, reason: null };

        service.getPremiumGrant(user).subscribe(result => {
            expect(result).toBeNull();
        });
        httpMock.expectOne({ method: 'GET', url: path }).flush(empty);

        service.setPremiumGrant(user, grant).subscribe(result => {
            expect(result).toEqual(grant);
        });
        const update = httpMock.expectOne({ method: 'PUT', url: path });
        expect(update.request.body).toEqual(grant);
        update.flush(grant);

        service.revokePremiumGrant(user).subscribe(result => {
            expect(result).toBeNull();
        });
        httpMock.expectOne({ method: 'DELETE', url: path }).flush(empty);
    });

    it('should request paged users and map response to items', () => {
        const response = {
            data: [
                {
                    id: adminId<'user'>('u1'),
                    email: 'user@example.com',
                    isActive: true,
                    isEmailConfirmed: true,
                    createdOnUtc: adminUtcInstant('2026-01-01T00:00:00Z'),
                    roles: ['Admin'],
                },
            ],
            page: USERS_PAGE,
            limit: USERS_LIMIT,
            totalPages: USERS_TOTAL_PAGES,
            totalItems: USERS_TOTAL_ITEMS,
        } satisfies {
            data: AdminUser[];
            page: number;
            limit: number;
            totalPages: number;
            totalItems: number;
        };

        service.getUsers(USERS_PAGE, USERS_LIMIT, 'alex', { status: 'inactive' }).subscribe(result => {
            expect(result.items).toEqual(response.data);
            expect(result.page).toBe(USERS_PAGE);
            expect(result.limit).toBe(USERS_LIMIT);
            expect(result.totalPages).toBe(USERS_TOTAL_PAGES);
            expect(result.totalItems).toBe(USERS_TOTAL_ITEMS);
        });

        const req = httpMock.expectOne(
            r =>
                r.url === baseUrl &&
                r.params.get('page') === String(USERS_PAGE) &&
                r.params.get('limit') === String(USERS_LIMIT) &&
                r.params.get('search') === 'alex' &&
                r.params.get('status') === 'inactive',
        );
        expect(req.request.method).toBe('GET');
        req.flush(response);
    });

    it('schedules for the selected user with a stable idempotency key per invocation', () => {
        const request = service.scheduleTestNotification(adminId<'user'>('selected-user'), {
            type: 'EatingWindowStarted',
            delaySeconds: 30,
        });
        request.subscribe();
        const first = httpMock.expectOne(`${baseUrl}/selected-user/notifications/test`);
        const key = first.request.headers.get('Idempotency-Key');
        expect(key).toBeTruthy();
        expect(first.request.method).toBe('POST');
        expect(first.request.body).toEqual({ type: 'EatingWindowStarted', delaySeconds: 30 });
        first.flush({ type: 'EatingWindowStarted', delaySeconds: 30, scheduledAtUtc: '2026-10-31T12:00:30Z' });
        request.subscribe();
        const second = httpMock.expectOne(`${baseUrl}/selected-user/notifications/test`);
        expect(second.request.headers.get('Idempotency-Key')).toBe(key);
        second.flush({});
    });

    it('should patch user update payload', () => {
        const payload = {
            isActive: false,
            isEmailConfirmed: true,
            roles: ['Support'],
            language: 'ru',
        };

        service.updateUser(adminId<'user'>('u1'), payload).subscribe(result => {
            expect(result.id).toBe('u1');
            expect(result.language).toBe('ru');
        });

        const req = httpMock.expectOne(`${baseUrl}/u1`);
        expect(req.request.method).toBe('PATCH');
        expect(req.request.body).toEqual(payload);
        req.flush({
            id: 'u1',
            email: 'user@example.com',
            language: 'ru',
            isActive: false,
            isEmailConfirmed: true,
            createdOnUtc: '2026-01-01T00:00:00Z',
            roles: ['Support'],
        });
    });

    it('should patch user password payload', () => {
        service.setPassword(adminId<'user'>('u1'), { newPassword: 'NewPassword123!' }).subscribe(result => {
            expect(result).toBeNull();
        });

        const req = httpMock.expectOne(`${baseUrl}/u1/password`);
        expect(req.request.method).toBe('PATCH');
        expect(req.request.body).toEqual({ newPassword: 'NewPassword123!' });
        req.flush(null);
    });

    it('should request user details by id', () => {
        service.getUser(adminId<'user'>('u1')).subscribe(result => {
            expect(result.id).toBe('u1');
        });

        const req = httpMock.expectOne(`${baseUrl}/u1`);
        expect(req.request.method).toBe('GET');
        req.flush({
            id: 'u1',
            email: 'user@example.com',
            isActive: true,
            isEmailConfirmed: true,
            createdOnUtc: '2026-01-01T00:00:00Z',
            roles: [],
        });
    });

    it('should filter login events by user id', () => {
        service.getLoginEvents(LOGIN_EVENTS_PAGE, LOGIN_EVENTS_LIMIT, null, { userId: 'u1' }).subscribe(result => {
            expect(result.items).toEqual([]);
        });

        const req = httpMock.expectOne(
            r =>
                r.url === `${baseUrl}/login-events` &&
                r.params.get('page') === String(LOGIN_EVENTS_PAGE) &&
                r.params.get('limit') === String(LOGIN_EVENTS_LIMIT) &&
                r.params.get('userId') === 'u1',
        );
        expect(req.request.method).toBe('GET');
        req.flush({
            data: [],
            page: LOGIN_EVENTS_PAGE,
            limit: LOGIN_EVENTS_LIMIT,
            totalPages: 0,
            totalItems: 0,
        });
    });

    it('should request user role audit by user id', () => {
        service.getUserRoleAudit(adminId<'user'>('u1'), ROLE_AUDIT_LIMIT).subscribe(result => {
            expect(result).toEqual([]);
        });

        const req = httpMock.expectOne(r => r.url === `${baseUrl}/u1/role-audit` && r.params.get('limit') === String(ROLE_AUDIT_LIMIT));
        expect(req.request.method).toBe('GET');
        req.flush([]);
    });

    it('sends account creation preferences and maps a returned account without an email address', async () => {
        const payload: AdminUserCreate = {
            email: 'created@example.test',
            language: 'en',
            roles: ['User'],
            generatePassword: true,
            isEmailConfirmed: false,
            sendCredentialsEmail: true,
            requirePasswordChange: true,
        };
        const result = firstValueFrom(service.createUser(payload));
        const request = httpMock.expectOne({ method: 'POST', url: baseUrl });
        expect(request.request.body).toEqual(payload);
        request.flush({
            user: {
                id: 'created-user',
                isActive: true,
                isEmailConfirmed: false,
                roles: ['User'],
                createdOnUtc: '2026-01-01T00:00:00Z',
            },
            temporaryPassword: 'generated-fixture',
            credentialsEmailQueued: true,
        });

        await expect(result).resolves.toMatchObject({
            user: { id: 'created-user', email: null, roles: ['User'] },
            temporaryPassword: 'generated-fixture',
            credentialsEmailQueued: true,
        });
    });

    it('maps one-use impersonation results and paged session audit data while retaining the selected identities', async () => {
        const user = adminId<'user'>('selected-user');
        const actor = 'current-admin';
        const reason = 'Support case review';
        const started = firstValueFrom(service.startImpersonation(user, reason));
        const create = httpMock.expectOne({ method: 'POST', url: `${baseUrl}/${user}/impersonation` });
        expect(create.request.body).toEqual({ reason });
        create.flush({ code: 'one-use-fixture', targetUserId: user, actorUserId: actor, reason });
        await expect(started).resolves.toEqual({
            code: 'one-use-fixture',
            targetUserId: user,
            actorUserId: actor,
            reason,
            targetEmail: null,
        });

        const startedAtUtc = '2026-01-02T03:04:05Z';
        const loaded = firstValueFrom(service.getImpersonationSessions(USERS_PAGE, USERS_LIMIT, reason, { actorId: actor }));
        const list = httpMock.expectOne(request => request.url === `${baseUrl}/impersonation-sessions`);
        expect(list.request.params.get('actorId')).toBe(actor);
        expect(list.request.params.get('search')).toBe(reason);
        const entry = {
            id: 'session-fixture',
            actorUserId: actor,
            targetUserId: user,
            reason,
            startedAtUtc,
            actorEmail: 'admin@example.test',
        };
        list.flush({ data: [entry], page: USERS_PAGE, limit: USERS_LIMIT, totalPages: USERS_TOTAL_PAGES, totalItems: USERS_TOTAL_ITEMS });
        await expect(loaded).resolves.toEqual({
            items: [{ ...entry, targetEmail: null }],
            page: USERS_PAGE,
            limit: USERS_LIMIT,
            totalPages: USERS_TOTAL_PAGES,
            totalItems: USERS_TOTAL_ITEMS,
        });
    });

    it('keeps masked login audit metadata and maps role audit and device summaries', async () => {
        const userId = adminId<'user'>('selected-user');
        const at = '2026-01-02T03:04:05Z';
        const login = {
            id: 'login-fixture',
            userId,
            authProvider: 'Google',
            loggedInAtUtc: at,
            deviceType: 'Mobile',
            maskedIpAddress: '192.0.2.*',
        };
        const history = firstValueFrom(service.getLoginEvents(LOGIN_EVENTS_PAGE, LOGIN_EVENTS_LIMIT, 'Mobile', { userId }));
        const loginRequest = httpMock.expectOne(request => request.url === `${baseUrl}/login-events`);
        expect(loginRequest.request.params.get('search')).toBe('Mobile');
        expect(loginRequest.request.params.get('userId')).toBe(userId);
        loginRequest.flush({ data: [login], page: LOGIN_EVENTS_PAGE, limit: LOGIN_EVENTS_LIMIT, totalPages: 1, totalItems: 1 });
        await expect(history).resolves.toMatchObject({ items: [{ ...login, userEmail: null }] });

        const auditEntry = { id: 'audit-fixture', userId, roleName: 'User', action: 'Grant', source: 'Administration', occurredAtUtc: at };
        const audit = firstValueFrom(service.getUserRoleAudit(userId));
        httpMock.expectOne(request => request.url === `${baseUrl}/${userId}/role-audit`).flush([auditEntry]);
        await expect(audit).resolves.toEqual([auditEntry]);

        const device = { key: 'Mobile', count: 1, lastSeenAtUtc: at };
        const summary = firstValueFrom(service.getLoginSummary());
        httpMock.expectOne({ method: 'GET', url: `${baseUrl}/login-summary` }).flush([device]);
        await expect(summary).resolves.toEqual([device]);
    });

    it('rejects an incomplete manual grant instead of inventing a reason', async () => {
        const selected = adminId<'user'>('selected-user');
        const result = firstValueFrom(service.getPremiumGrant(selected));
        httpMock.expectOne({ method: 'GET', url: `${baseUrl}/${selected}/premium-grant` }).flush({ endsAtUtc: '2026-07-01T12:00:00Z' });
        await expect(result).rejects.toThrow('reason');
    });
});
