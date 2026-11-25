import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { type ComponentFixture, TestBed } from '@angular/core/testing';
import { provideRouter } from '@angular/router';
import { FD_UI_DIALOG_DATA } from 'fd-ui-kit';
import { type Observable, of, Subject, throwError } from 'rxjs';
import { describe, expect, it, vi } from 'vitest';

import { provideTranslateTesting } from '../../../../../../../src/testing/translate-testing.module';
import { environment } from '../../../../environments/environment';
import type { AdminMailInboxMessageDetailsHttpResponse } from '../../../shared/api/sdk/generated/model/admin-mail-inbox-message-details-http-response';
import { adminId, adminUtcInstant } from '../../../shared/models/semantics/admin-meaning';
import { AdminMailInboxFacade } from '../lib/admin-mail-inbox.facade';
import type { AdminMailInboxMessageDetails } from '../models/admin-mail-inbox.data';
import { AdminMailMessageDialogComponent } from './admin-mail-message-dialog';

const message: AdminMailInboxMessageDetails = {
    id: adminId<'mail-inbox-message'>('message-id'),
    isTrustedRelay: true,
    toRecipients: ['fd.qa.test@example.com'],
    category: 'general',
    status: 'processed',
    receivedAtUtc: adminUtcInstant('2026-10-06T00:00:00Z'),
    readAtUtc: null,
    textBody: 'QA message',
};

describe('mail message read state', () => {
    it('keeps content and unread state after failure, shows recovery and retries without duplicate writes', async () => {
        const pending = new Subject<void>();
        const markMessageRead = vi
            .fn()
            .mockReturnValueOnce(throwError(() => new Error('offline')))
            .mockReturnValue(pending);
        const onRead = vi.fn();
        TestBed.configureTestingModule({
            imports: [AdminMailMessageDialogComponent],
            providers: [
                provideRouter([]),
                provideTranslateTesting(),
                { provide: FD_UI_DIALOG_DATA, useValue: { id: message.id, onRead } },
                {
                    provide: AdminMailInboxFacade,
                    useValue: { getMessage: (): Observable<AdminMailInboxMessageDetails> => of(message), markMessageRead },
                },
            ],
        });
        const fixture = TestBed.createComponent(AdminMailMessageDialogComponent);
        fixture.detectChanges();
        await settleDetailsAsync(fixture);
        const host = fixture.nativeElement as HTMLElement;
        expect(host.querySelector('[role="alert"]')?.textContent).toContain('ADMIN_MAIL_INBOX.READ_ERROR');
        expect(host.querySelector('.mail-body')?.textContent).toContain('QA message');
        expect(onRead).not.toHaveBeenCalled();
        const retry = [...host.querySelectorAll<HTMLButtonElement>('button')].find(button =>
            button.textContent.includes('ADMIN_COMMON.RETRY'),
        );
        if (retry === undefined) {
            throw new Error('Expected the retry control.');
        }
        retry.click();
        retry.click();
        fixture.detectChanges();
        expect(markMessageRead).toHaveBeenCalledTimes(2);
        expect(host.querySelector('[role="alert"]')?.textContent).toContain('ADMIN_MAIL_INBOX.READ_ERROR');
        expect(retry.isConnected).toBe(true);
        expect(retry.disabled).toBe(true);
        expect(retry.getAttribute('aria-busy')).toBe('true');
        expect(
            [...host.querySelectorAll<HTMLButtonElement>('button')].find(button => button.textContent.includes('ADMIN_COMMON.RETRY')),
        ).toBe(retry);
        expect(fixture.componentInstance['markingRead']()).toBe(true);
        pending.next();
        fixture.detectChanges();
        expect(host.querySelector('[role="alert"]')).toBeNull();
        expect(host.querySelector('.mail-read-state')?.textContent.trim()).toBe('ADMIN_MAIL_INBOX.MESSAGE_READ');
        expect(onRead).toHaveBeenCalledOnce();
    });
});

function renderMailOverHttp(): {
    fixture: ComponentFixture<AdminMailMessageDialogComponent>;
    requests: HttpTestingController;
    onRead: ReturnType<typeof vi.fn>;
} {
    const onRead = vi.fn();
    TestBed.configureTestingModule({
        imports: [AdminMailMessageDialogComponent],
        providers: [
            provideRouter([]),
            provideTranslateTesting(),
            provideHttpClient(),
            provideHttpClientTesting(),
            { provide: FD_UI_DIALOG_DATA, useValue: { id: message.id, onRead } },
        ],
    });
    const fixture = TestBed.createComponent(AdminMailMessageDialogComponent);
    fixture.detectChanges();
    return { fixture, requests: TestBed.inject(HttpTestingController), onRead };
}

describe('mail message HTTP details', () => {
    const url = `${environment.apiUrls.auth.replace(/\/auth$/, '')}/admin/mail-inbox/messages/${message.id}`;

    it('renders a read DMARC report with no listed recipients and preserves its raw source', async () => {
        const { fixture, requests, onRead } = renderMailOverHttp();
        const rawMime = 'Content-Type: text/plain\r\n\r\nAggregated report';
        const report = {
            ...message,
            category: 'dmarc-report',
            toRecipients: [],
            readAtUtc: message.receivedAtUtc,
            rawMime,
            dmarcReport: { domain: 'example.test', records: [{ sourceIp: '192.0.2.1', count: 1, disposition: 'none' }] },
        } satisfies AdminMailInboxMessageDetailsHttpResponse;
        requests.expectOne({ method: 'GET', url }).flush(report);
        await settleDetailsAsync(fixture);
        const host = fixture.nativeElement as HTMLElement;
        expect(host.querySelector('.mail-badge--dmarc')?.textContent.trim()).toBe('DMARC');
        expect(host.querySelector('.mail-details__meta')?.textContent.trim()).toBe('ADMIN_MAIL_INBOX.TO: -');
        expect(host.querySelector('.dmarc-report tbody tr')?.textContent).toContain('192.0.2.1');
        expect(host.querySelector('.dmarc-report__warning')?.textContent).toContain('ADMIN_MAIL_INBOX.DMARC_UNVERIFIED_WARNING');
        selectBodyMode(fixture, 'ADMIN_MAIL_INBOX.VIEW_RAW');
        fixture.detectChanges();
        expect(host.querySelector('pre.mail-body--source')?.textContent).toBe(rawMime);
        expect(onRead).not.toHaveBeenCalled();
        requests.expectNone({ method: 'POST', url: `${url}/read` });
        requests.verify();
    });

    it('disposes a pending read-mark request when the message dialog is closed', async () => {
        const { fixture, requests, onRead } = renderMailOverHttp();
        requests.expectOne({ method: 'GET', url }).flush(message);
        await settleDetailsAsync(fixture);
        const pending = requests.expectOne({ method: 'POST', url: `${url}/read` });
        fixture.detectChanges();
        const host = fixture.nativeElement as HTMLElement;
        expect(host.querySelector('.mail-read-state--unread')).not.toBeNull();
        fixture.destroy();
        expect(pending.cancelled).toBe(true);
        expect(onRead).not.toHaveBeenCalled();
        requests.verify();
    });

    it('shows the read acknowledgement before a parent closes the dialog and reloads the message on reopening', async () => {
        const { fixture, requests, onRead } = renderMailOverHttp();
        requests.expectOne({ method: 'GET', url }).flush(message);
        await settleDetailsAsync(fixture);
        const host = fixture.nativeElement as HTMLElement;
        const pending = requests.expectOne({ method: 'POST', url: `${url}/read` });
        let badgeAtClose: string | undefined;
        onRead.mockImplementationOnce(() => {
            fixture.detectChanges();
            badgeAtClose = host.querySelector('.mail-read-state')?.textContent.trim();
            fixture.destroy();
        });

        pending.flush(null);

        expect(badgeAtClose).toBe('ADMIN_MAIL_INBOX.MESSAGE_READ');
        expect(onRead).toHaveBeenCalledExactlyOnceWith(message.id, expect.any(String));

        const reopened = TestBed.createComponent(AdminMailMessageDialogComponent);
        reopened.detectChanges();
        requests.expectOne({ method: 'GET', url }).flush({ ...message, readAtUtc: message.receivedAtUtc });
        await settleDetailsAsync(reopened);
        const reopenedHost = reopened.nativeElement as HTMLElement;
        expect(reopenedHost.querySelector('.mail-read-state')?.textContent.trim()).toBe('ADMIN_MAIL_INBOX.MESSAGE_READ');
        requests.expectNone({ method: 'POST', url: `${url}/read` });
        requests.verify();
    });
});

async function settleDetailsAsync(fixture: ComponentFixture<AdminMailMessageDialogComponent>): Promise<void> {
    await fixture.whenStable();
    fixture.detectChanges();
}

async function renderMessageAsync(
    content: Partial<AdminMailInboxMessageDetails>,
): Promise<ComponentFixture<AdminMailMessageDialogComponent>> {
    TestBed.configureTestingModule({
        imports: [AdminMailMessageDialogComponent],
        providers: [
            provideRouter([]),
            provideTranslateTesting(),
            { provide: FD_UI_DIALOG_DATA, useValue: { id: message.id, onRead: vi.fn() } },
            {
                provide: AdminMailInboxFacade,
                useValue: {
                    getMessage: (): Observable<AdminMailInboxMessageDetails> =>
                        of({ ...message, readAtUtc: message.receivedAtUtc, ...content }),
                    markMessageRead: vi.fn(),
                },
            },
        ],
    });
    const fixture = TestBed.createComponent(AdminMailMessageDialogComponent);
    fixture.detectChanges();
    await settleDetailsAsync(fixture);
    return fixture;
}

describe('mail message reader', () => {
    it('opens formatted HTML with paragraphs while keeping technical details collapsed', async () => {
        const fixture = await renderMessageAsync({ htmlBody: '<p>Hello,&nbsp;Alex!</p><p>A <strong>new message</strong>.</p>' });
        const host = fixture.nativeElement as HTMLElement;
        expect(host.querySelector('.mail-body p')?.textContent).toBe('Hello,\u00A0Alex!');
        expect(host.querySelector('.mail-body strong')?.textContent).toBe('new message');
        expect(host.querySelector('details')?.open).toBe(false);
    });

    it('decodes entities in plain text without interpreting decoded markup', async () => {
        const fixture = await renderMessageAsync({ textBody: 'Hello,&nbsp;I&rsquo;m here.\n&lt;img src=x onerror=alert(1)&gt;' });
        const body = (fixture.nativeElement as HTMLElement).querySelector('.mail-body');
        expect(body?.textContent).toContain('Hello,\u00A0I’m here.\n<img src=x onerror=alert(1)>');
        expect(body?.querySelector('img')).toBeNull();
    });

    it('removes active content, remote resources and unsafe links from the reading view', async () => {
        const fixture = await renderMessageAsync({
            htmlBody:
                '<style>body{display:none}</style><script>alert(1)</script><img src="https://tracker.example/pixel">' +
                '<iframe src="https://tracker.example"></iframe><svg onload="alert(1)"></svg>' +
                '<p onclick="alert(1)">Safe <a href="javascript:alert(1)">bad link</a> ' +
                '<a href="https://example.com" ping="https://tracker.example">good link</a></p>',
        });
        const body = (fixture.nativeElement as HTMLElement).querySelector('.mail-body');
        expect(body?.textContent).toContain('Safe');
        expect(body?.querySelector('script, style, img, iframe, svg, [onclick], [ping]')).toBeNull();
        expect(body?.querySelector('a[href^="javascript:"]')).toBeNull();
        expect(body?.querySelector('a[href="https://example.com"]')?.getAttribute('rel')).toBe('noopener noreferrer');
    });

    it('keeps HTML and MIME source unchanged when switching out of the reading view', async () => {
        const htmlBody = '<p>Hello&nbsp;<strong>reader</strong></p>';
        const rawMime = `Content-Type: text/html\r\n\r\n${htmlBody}`;
        const fixture = await renderMessageAsync({ htmlBody, rawMime });
        selectBodyMode(fixture, 'ADMIN_MAIL_INBOX.VIEW_HTML');
        fixture.detectChanges();
        expect((fixture.nativeElement as HTMLElement).querySelector('pre')?.textContent).toBe(htmlBody);
        selectBodyMode(fixture, 'ADMIN_MAIL_INBOX.VIEW_RAW');
        fixture.detectChanges();
        expect((fixture.nativeElement as HTMLElement).querySelector('pre')?.textContent).toBe(rawMime);
    });

    it('derives readable text with line breaks when an email only contains HTML', async () => {
        const fixture = await renderMessageAsync({ textBody: null, htmlBody: '<p>First paragraph</p><p>Second<br>Third</p>' });
        fixture.componentInstance['setBodyMode']('text');
        fixture.detectChanges();
        expect((fixture.nativeElement as HTMLElement).querySelector('.mail-body')?.textContent).toBe('First paragraph\nSecond\nThird');
    });

    it('falls back to text when HTML contains only blocked resources', async () => {
        const fixture = await renderMessageAsync({ htmlBody: '<img src="https://tracker.example/pixel">', textBody: 'Actual message' });
        const body = (fixture.nativeElement as HTMLElement).querySelector('.mail-body');
        expect(body?.textContent).toBe('Actual message');
        expect(body?.querySelector('img')).toBeNull();
    });

    it('shows an empty state when both body formats are absent', async () => {
        const fixture = await renderMessageAsync({ textBody: null, htmlBody: null });
        expect((fixture.nativeElement as HTMLElement).querySelector('.mail-body')?.textContent).toBe('ADMIN_MAIL_INBOX.NO_BODY');

        selectBodyMode(fixture, 'ADMIN_MAIL_INBOX.VIEW_HTML');
        fixture.detectChanges();
        expect((fixture.nativeElement as HTMLElement).querySelector('pre')?.textContent).toBe('ADMIN_MAIL_INBOX.NO_BODY');
        selectBodyMode(fixture, 'ADMIN_MAIL_INBOX.VIEW_RAW');
        fixture.detectChanges();
        expect((fixture.nativeElement as HTMLElement).querySelector('pre')?.textContent).toBe('ADMIN_MAIL_INBOX.NO_BODY');
    });
});

function selectBodyMode(fixture: ComponentFixture<AdminMailMessageDialogComponent>, label: string): void {
    const element = fixture.nativeElement as HTMLElement;
    const option = [...element.querySelectorAll<HTMLButtonElement>('button[role="radio"]')].find(
        button => button.textContent.trim() === label,
    );
    if (option === undefined) {
        throw new Error(`Expected the ${label} mail view`);
    }
    option.click();
}

it('preserves text inside legacy wrappers, named anchors and relative links', async () => {
    const fixture = await renderMessageAsync({
        htmlBody: '<center><a name="overview"><strong>Overview</strong></a><p><a href="/help">Support</a></p></center>',
        textBody: null,
    });
    const host = fixture.nativeElement as HTMLElement;
    const body = host.querySelector('.mail-body--formatted');
    expect(body?.innerHTML).not.toContain('<center');
    expect(body?.querySelector('strong')?.textContent).toBe('Overview');
    expect(body?.querySelector('p')?.textContent).toBe('Support');
    expect(body?.querySelector('a[href], a[name]')).toBeNull();
});

describe('mail message loading', () => {
    it('shows a loading status and reports a failed load without marking the message read', async () => {
        const details = new Subject<AdminMailInboxMessageDetails>();
        const markMessageRead = vi.fn();
        const onRead = vi.fn();
        TestBed.configureTestingModule({
            imports: [AdminMailMessageDialogComponent],
            providers: [
                provideRouter([]),
                provideTranslateTesting(),
                { provide: FD_UI_DIALOG_DATA, useValue: { id: message.id, onRead } },
                { provide: AdminMailInboxFacade, useValue: { getMessage: vi.fn().mockReturnValue(details), markMessageRead } },
            ],
        });
        const fixture = TestBed.createComponent(AdminMailMessageDialogComponent);
        const element = fixture.nativeElement as HTMLElement;
        fixture.detectChanges();
        expect(element.querySelector('[role="status"]')?.textContent).toContain('ADMIN_MAIL_INBOX.LOADING_MESSAGE');

        details.error(new Error('Details unavailable'));
        await settleDetailsAsync(fixture);
        expect(element.querySelector('[role="status"]')).toBeNull();
        expect(element.querySelector('[role="alert"]')?.textContent).toContain('ADMIN_MAIL_INBOX.LOAD_ERROR');
        expect(markMessageRead).not.toHaveBeenCalled();
        expect(onRead).not.toHaveBeenCalled();
    });
});
