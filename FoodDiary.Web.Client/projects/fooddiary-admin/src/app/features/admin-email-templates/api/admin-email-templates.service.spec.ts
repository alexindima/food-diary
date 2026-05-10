import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { TestBed } from '@angular/core/testing';
import { afterEach, beforeEach, describe, expect, it } from 'vitest';

import { environment } from '../../../../environments/environment';
import type { AdminEmailTemplate } from '../models/admin-email-template.data';
import { AdminEmailTemplatesService } from './admin-email-templates.service';

describe('AdminEmailTemplatesService', () => {
    let service: AdminEmailTemplatesService;
    let httpMock: HttpTestingController;

    const baseUrl = `${environment.apiUrls.auth.replace(/\/auth$/, '')}/admin/email-templates`;

    beforeEach(() => {
        TestBed.configureTestingModule({
            providers: [AdminEmailTemplatesService, provideHttpClient(), provideHttpClientTesting()],
        });

        service = TestBed.inject(AdminEmailTemplatesService);
        httpMock = TestBed.inject(HttpTestingController);
    });

    afterEach(() => {
        httpMock.verify();
    });

    it('should request all templates', () => {
        const templates: AdminEmailTemplate[] = [
            {
                id: 't1',
                key: 'email_verification',
                locale: 'en',
                subject: 'Verify email',
                htmlBody: '<p>Hello</p>',
                textBody: 'Hello',
                isActive: true,
                createdOnUtc: '2026-01-01T00:00:00Z',
                updatedOnUtc: null,
            },
        ];

        service.getAll().subscribe(result => {
            expect(result).toEqual(templates);
        });

        const req = httpMock.expectOne(baseUrl);
        expect(req.request.method).toBe('GET');
        req.flush(templates);
    });

    it('should upsert template by key and locale', () => {
        const payload = {
            subject: 'Verify email',
            htmlBody: '<p>Hello</p>',
            textBody: 'Hello',
            isActive: true,
        } as const;

        service.upsert('email_verification', 'ru', payload).subscribe(result => {
            expect(result.locale).toBe('ru');
            expect(result.subject).toBe('Verify email');
        });

        const req = httpMock.expectOne(`${baseUrl}/email_verification/ru`);
        expect(req.request.method).toBe('PUT');
        expect(req.request.body).toEqual(payload);
        req.flush({
            id: 't1',
            key: 'email_verification',
            locale: 'ru',
            subject: 'Verify email',
            htmlBody: '<p>Hello</p>',
            textBody: 'Hello',
            isActive: true,
            createdOnUtc: '2026-01-01T00:00:00Z',
            updatedOnUtc: null,
        });
    });

    it('should send template test email', () => {
        const payload = {
            toEmail: 'admin@example.com',
            key: 'email_verification',
            subject: 'Verify email',
            htmlBody: '<p>Hello</p>',
            textBody: 'Hello',
        } as const;

        service.sendTest(payload).subscribe();

        const req = httpMock.expectOne(`${baseUrl}/test`);
        expect(req.request.method).toBe('POST');
        expect(req.request.body).toEqual(payload);
        req.flush(null);
    });
});
