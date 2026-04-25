import { afterEach, beforeEach, describe, expect, it } from 'vitest';
import { TestBed } from '@angular/core/testing';
import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { environment } from '../../../../environments/environment';
import { AdminMailInboxService } from './admin-mail-inbox.service';

describe('AdminMailInboxService', () => {
    let service: AdminMailInboxService;
    let httpMock: HttpTestingController;

    const baseUrl = `${environment.apiUrls.auth.replace(/\/auth$/, '')}/admin/mail-inbox/messages`;

    beforeEach(() => {
        TestBed.configureTestingModule({
            providers: [AdminMailInboxService, provideHttpClient(), provideHttpClientTesting()],
        });

        service = TestBed.inject(AdminMailInboxService);
        httpMock = TestBed.inject(HttpTestingController);
    });

    afterEach(() => {
        httpMock.verify();
    });

    it('should request inbound messages with limit', () => {
        const response = [
            {
                id: 'message-1',
                fromAddress: 'user@example.com',
                toRecipients: ['admin@fooddiary.club'],
                subject: 'Feedback',
                status: 'Received',
                receivedAtUtc: '2026-04-25T21:37:55Z',
            },
        ];

        service.getMessages(25).subscribe(result => {
            expect(result).toEqual(response);
        });

        const req = httpMock.expectOne(r => r.url === baseUrl && r.params.get('limit') === '25');
        expect(req.request.method).toBe('GET');
        req.flush(response);
    });

    it('should request one inbound message by id', () => {
        service.getMessage('message-1').subscribe(result => {
            expect(result.id).toBe('message-1');
            expect(result.rawMime).toBe('raw');
        });

        const req = httpMock.expectOne(`${baseUrl}/message-1`);
        expect(req.request.method).toBe('GET');
        req.flush({
            id: 'message-1',
            fromAddress: 'user@example.com',
            toRecipients: ['admin@fooddiary.club'],
            subject: 'Feedback',
            status: 'Received',
            receivedAtUtc: '2026-04-25T21:37:55Z',
            rawMime: 'raw',
        });
    });
});
