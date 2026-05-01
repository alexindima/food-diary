import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { TestBed } from '@angular/core/testing';
import { afterEach, beforeEach, describe, expect, it } from 'vitest';

import { environment } from '../../../../environments/environment';
import { AdminUsersService } from './admin-users.service';

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
            page: 2,
            limit: 20,
            totalPages: 3,
            totalItems: 41,
        };

        service.getUsers(2, 20, 'alex', true).subscribe(result => {
            expect(result.items).toEqual(response.data as any);
            expect(result.page).toBe(2);
            expect(result.limit).toBe(20);
            expect(result.totalPages).toBe(3);
            expect(result.totalItems).toBe(41);
        });

        const req = httpMock.expectOne(
            r =>
                r.url === baseUrl &&
                r.params.get('page') === '2' &&
                r.params.get('limit') === '20' &&
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
