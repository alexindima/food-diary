import { TestBed } from '@angular/core/testing';
import { provideRouter } from '@angular/router';
import { FD_UI_DIALOG_DATA } from 'fd-ui-kit';
import { type Observable, of, Subject, throwError } from 'rxjs';
import { describe, expect, it, vi } from 'vitest';

import { provideTranslateTesting } from '../../../../../../../src/testing/translate-testing.module';
import { AdminMailInboxFacade } from '../lib/admin-mail-inbox.facade';
import type { AdminMailInboxMessageDetails } from '../models/admin-mail-inbox.data';
import { AdminMailMessageDialogComponent } from './admin-mail-message-dialog';

const message: AdminMailInboxMessageDetails = {
    id: 'message-id',
    isTrustedRelay: true,
    toRecipients: ['fd.qa.test@example.com'],
    category: 'general',
    status: 'processed',
    receivedAtUtc: '2026-10-06T00:00:00Z',
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
