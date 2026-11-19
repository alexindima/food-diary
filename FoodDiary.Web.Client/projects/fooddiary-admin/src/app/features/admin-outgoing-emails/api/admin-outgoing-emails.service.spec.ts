import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { TestBed } from '@angular/core/testing';
import { firstValueFrom } from 'rxjs';
import { afterEach, beforeEach, describe, expect, it } from 'vitest';

import { environment } from '../../../../environments/environment';
import type { AdminOutgoingEmailHttpResponse } from '../../../shared/api/sdk/generated/model/admin-outgoing-email-http-response';
import { AdminOutgoingEmailsFacade } from '../lib/admin-outgoing-emails.facade';

const OUTBOX_URL = `${environment.apiUrls.auth.replace(/\/auth$/, '')}/admin/outgoing-emails`;
const OUTBOX_PAGE = 2;
const RECIPIENT = 'customer@example.test';
const MAIL = {
    id: 'outgoing-message',
    status: 'sent',
    purpose: 'email_verification',
    fromAddress: 'support@example.test',
    to: [RECIPIENT],
    subject: 'Account email',
    createdAtUtc: '2026-10-06T00:00:00Z',
    attemptCount: 1,
    maxAttempts: 1,
    contentHidden: true,
} satisfies AdminOutgoingEmailHttpResponse;

describe('admin outgoing mail SDK requests', () => {
    let outbox: AdminOutgoingEmailsFacade;
    let http: HttpTestingController;

    beforeEach(() => {
        TestBed.configureTestingModule({ providers: [provideHttpClient(), provideHttpClientTesting()] });
        outbox = TestBed.inject(AdminOutgoingEmailsFacade);
        http = TestBed.inject(HttpTestingController);
    });

    afterEach(() => {
        http.verify();
    });

    it('normalizes the recipient from the URL and sends the date and correlation filters', async () => {
        const fromUtc = '2026-10-06T00:00:00Z';
        const result = firstValueFrom(
            outbox.getPage(OUTBOX_PAGE, 'email_verification', 'sent', {
                recipient: `  ${RECIPIENT}  `,
                fromUtc,
                correlationId: 'request-1',
                id: '',
            }),
        );
        const request = http.expectOne(candidate => candidate.url === OUTBOX_URL);
        request.flush({ items: [MAIL], totalItems: 1, statusCounts: { sent: 1 } });
        const params = request.request.params;
        expect(request.request.method).toBe('GET');
        expect(params.get('page')).toBe(String(OUTBOX_PAGE));
        expect(params.get('limit')).toBe('50');
        expect(params.get('recipient')).toBe(RECIPIENT);
        expect(params.get('fromUtc')).toBe(fromUtc);
        expect(params.get('correlationId')).toBe('request-1');
        expect(params.has('id')).toBe(false);
        expect(await result).toEqual({
            items: [{ ...MAIL, sentAtUtc: null, correlationId: null, textBody: null, replyTo: null, inReplyTo: null }],
            totalItems: 1,
            statusCounts: { sent: 1 },
        });
    });

    it('preserves supplied delivery data and leaves the default recipient filter blank', async () => {
        const delivered = {
            ...MAIL,
            sentAtUtc: '2026-10-06T01:02:03Z',
            correlationId: 'request-1',
            textBody: 'Message',
            replyTo: 'reply@example.test',
            inReplyTo: 'previous-message',
            contentHidden: false,
        };
        const result = firstValueFrom(outbox.getPage(1, '', ''));
        const request = http.expectOne(candidate => candidate.url === OUTBOX_URL);
        expect(request.request.params.get('recipient')).toBe('');
        request.flush({ items: [delivered], totalItems: 1 });
        expect(await result).toEqual({ totalItems: 1, items: [delivered] });
    });

    it('rejects a response that omits its total instead of displaying a fabricated page', async () => {
        const result = firstValueFrom(outbox.getPage(1, '', ''));
        const check = expect(result).rejects.toThrow('API response is missing required field: totalItems.');
        http.expectOne(candidate => candidate.url === OUTBOX_URL).flush({ items: [] });
        await check;
    });
});
