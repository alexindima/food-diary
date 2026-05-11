import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { TestBed } from '@angular/core/testing';
import { afterEach, beforeEach, describe, expect, it } from 'vitest';

import { environment } from '../../../../environments/environment';
import { type AdminUser, AdminUsersService } from './admin-users.service';

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

    it('should request paged users and map response to items', () => {
        const response = {
            data: [
                {
                    id: 'u1',
                    email: 'user@example.com',
                    isActive: true,
                    isEmailConfirmed: true,
                    createdOnUtc: '2026-01-01T00:00:00Z',
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

        service.getUsers(USERS_PAGE, USERS_LIMIT, 'alex', true).subscribe(result => {
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
                r.params.get('includeDeleted') === 'true',
        );
        expect(req.request.method).toBe('GET');
        req.flush(response);
    });

    it('should patch user update payload', () => {
        const payload = {
            isActive: false,
            isEmailConfirmed: true,
            roles: ['Support'],
            language: 'ru',
        };

        service.updateUser('u1', payload).subscribe(result => {
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
});
