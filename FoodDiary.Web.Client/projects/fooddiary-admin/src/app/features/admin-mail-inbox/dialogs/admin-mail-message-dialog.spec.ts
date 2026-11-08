import { type ComponentFixture, TestBed } from '@angular/core/testing';
import { provideRouter } from '@angular/router';
import { FD_UI_DIALOG_DATA } from 'fd-ui-kit';
import { type Observable, of, Subject, throwError } from 'rxjs';
import { describe, expect, it, vi } from 'vitest';

import { provideTranslateTesting } from '../../../../../../../src/testing/translate-testing.module';
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
    it('keeps content and unread state after failure, shows recovery and retries without duplicate writes', () => {
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
        const host = fixture.nativeElement as HTMLElement;
        expect(host.querySelector('[role="alert"]')?.textContent).toContain('ADMIN_MAIL_INBOX.READ_ERROR');
        expect(host.querySelector('.mail-body')?.textContent).toContain('QA message');
        expect(onRead).not.toHaveBeenCalled();
        const retry = [...host.querySelectorAll<HTMLButtonElement>('button')].find(button =>
            button.textContent.includes('ADMIN_COMMON.RETRY'),
        );
        fixture.componentInstance['retryRead']();
        fixture.componentInstance['retryRead']();
        fixture.detectChanges();
        expect(markMessageRead).toHaveBeenCalledTimes(2);
        expect(host.querySelector('[role="alert"]')?.textContent).toContain('ADMIN_MAIL_INBOX.READ_ERROR');
        expect(retry?.isConnected).toBe(true);
        expect(retry?.disabled).toBe(true);
        expect(retry?.getAttribute('aria-busy')).toBe('true');
        expect(
            [...host.querySelectorAll<HTMLButtonElement>('button')].find(button => button.textContent.includes('ADMIN_COMMON.RETRY')),
        ).toBe(retry);
        expect(fixture.componentInstance['markingRead']()).toBe(true);
        pending.next();
        fixture.detectChanges();
        expect(host.querySelector('[role="alert"]')).toBeNull();
        expect(fixture.componentInstance['selectedMessage']()?.readAtUtc).toBeTruthy();
        expect(onRead).toHaveBeenCalledOnce();
    });
});

describe('mail message reader', () => {
    function renderMessage(content: Partial<AdminMailInboxMessageDetails>): ComponentFixture<AdminMailMessageDialogComponent> {
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
        return fixture;
    }

    it('opens formatted HTML with paragraphs while keeping technical details collapsed', () => {
        const fixture = renderMessage({ htmlBody: '<p>Hello,&nbsp;Alex!</p><p>A <strong>new message</strong>.</p>' });
        const host = fixture.nativeElement as HTMLElement;
        expect(host.querySelector('.mail-body p')?.textContent).toBe('Hello,\u00A0Alex!');
        expect(host.querySelector('.mail-body strong')?.textContent).toBe('new message');
        expect(host.querySelector('details')?.open).toBe(false);
    });

    it('decodes entities in plain text without interpreting decoded markup', () => {
        const fixture = renderMessage({ textBody: 'Hello,&nbsp;I&rsquo;m here.\n&lt;img src=x onerror=alert(1)&gt;' });
        const body = (fixture.nativeElement as HTMLElement).querySelector('.mail-body');
        expect(body?.textContent).toContain('Hello,\u00A0I’m here.\n<img src=x onerror=alert(1)>');
        expect(body?.querySelector('img')).toBeNull();
    });

    it('removes active content, remote resources and unsafe links from the reading view', () => {
        const fixture = renderMessage({
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

    it('keeps HTML and MIME source unchanged when switching out of the reading view', () => {
        const htmlBody = '<p>Hello&nbsp;<strong>reader</strong></p>';
        const rawMime = `Content-Type: text/html\r\n\r\n${htmlBody}`;
        const fixture = renderMessage({ htmlBody, rawMime });
        fixture.componentInstance['setBodyMode']('html');
        fixture.detectChanges();
        expect((fixture.nativeElement as HTMLElement).querySelector('pre')?.textContent).toBe(htmlBody);
        fixture.componentInstance['setBodyMode']('raw');
        fixture.detectChanges();
        expect((fixture.nativeElement as HTMLElement).querySelector('pre')?.textContent).toBe(rawMime);
    });

    it('derives readable text with line breaks when an email only contains HTML', () => {
        const fixture = renderMessage({ textBody: null, htmlBody: '<p>First paragraph</p><p>Second<br>Third</p>' });
        fixture.componentInstance['setBodyMode']('text');
        fixture.detectChanges();
        expect((fixture.nativeElement as HTMLElement).querySelector('.mail-body')?.textContent).toBe('First paragraph\nSecond\nThird');
    });

    it('falls back to text when HTML contains only blocked resources', () => {
        const fixture = renderMessage({ htmlBody: '<img src="https://tracker.example/pixel">', textBody: 'Actual message' });
        const body = (fixture.nativeElement as HTMLElement).querySelector('.mail-body');
        expect(body?.textContent).toBe('Actual message');
        expect(body?.querySelector('img')).toBeNull();
    });

    it('shows an empty state when both body formats are absent', () => {
        const fixture = renderMessage({ textBody: null, htmlBody: null });
        expect((fixture.nativeElement as HTMLElement).querySelector('.mail-body')?.textContent).toBe('ADMIN_MAIL_INBOX.NO_BODY');
    });
});
