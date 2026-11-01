import { TestBed } from '@angular/core/testing';
import { provideRouter } from '@angular/router';
import { RouterTestingHarness } from '@angular/router/testing';
import { TranslateService } from '@ngx-translate/core';
import { FdUiDialogService } from 'fd-ui-kit/dialog/fd-ui-dialog.service';
import { of } from 'rxjs';
import { describe, expect, it, type Mock,vi } from 'vitest';

import { provideTranslateTesting } from '../../../../../../../src/testing/translate-testing.module';
import { adminId, adminUtcInstant } from '../../../shared/models/semantics/admin-meaning';
import { AdminMailInboxFacade } from '../lib/admin-mail-inbox.facade';
import { AdminMailInboxComponent } from './admin-mail-inbox';

const MAILBOX = 'feedback@fooddiary.club';
const PAGE_SIZE = 50;
const TOTAL = 120;
const UNREAD = 7;

async function setupFeedbackAsync(url: string): Promise<{
    getMessagePage: Mock<AdminMailInboxFacade['getMessagePage']>;
    open: Mock;
    harness: RouterTestingHarness;
}> {
    const getMessagePage = vi.fn<AdminMailInboxFacade['getMessagePage']>().mockReturnValue(
        of({
            totalItems: TOTAL,
            unreadCount: UNREAD,
            readCount: TOTAL - UNREAD,
            items: [
                {
                    id: adminId<'mail-inbox-message'>('feedback-1'),
                    subject: '<script>plain feedback</script>',
                    fromAddress: 'reader@example.test',
                    toRecipients: [MAILBOX],
                    category: 'general',
                    status: 'received',
                    isTrustedRelay: false,
                    receivedAtUtc: adminUtcInstant('2026-11-01T10:00:00Z'),
                    readAtUtc: null,
                },
            ],
        }),
    );
    const open = vi.fn();
    await TestBed.configureTestingModule({
        providers: [
            ...provideTranslateTesting(),
            provideRouter([
                { path: 'feedback', component: AdminMailInboxComponent, data: { feedbackMailbox: true } },
                { path: 'mail-inbox', component: AdminMailInboxComponent },
            ]),
            { provide: AdminMailInboxFacade, useValue: { getMessagePage } },
            { provide: FdUiDialogService, useValue: { open } },
        ],
    }).compileComponents();
    const harness = await RouterTestingHarness.create(url);
    await harness.fixture.whenStable();
    harness.detectChanges();
    return { getMessagePage, open, harness };
}

describe('Admin feedback mailbox', () => {
    it('fixes the server recipient while preserving page, search and read filters', async () => {
        const { getMessagePage, harness } = await setupFeedbackAsync('/feedback?recipient=other@example.test');
        expect(getMessagePage).toHaveBeenLastCalledWith(1, PAGE_SIZE, expect.objectContaining({ recipient: MAILBOX }));
        await harness.navigateByUrl('/feedback?page=2&search=missing&unread=true&recipient=other@example.test');
        expect(getMessagePage).toHaveBeenLastCalledWith(
            2,
            PAGE_SIZE,
            expect.objectContaining({
                recipient: MAILBOX,
                search: 'missing',
                unread: true,
            }),
        );
        const recipientLabel = String(TestBed.inject(TranslateService).instant('ADMIN_MAIL_INBOX.RECIPIENT_FILTER'));
        const labels = Array.from(harness.routeNativeElement?.querySelectorAll('label') ?? []);
        expect(labels.some(label => label.textContent.trim() === recipientLabel)).toBe(false);
    });

    it('keeps feedback text safe and message links inside the feedback view', async () => {
        const { harness, open } = await setupFeedbackAsync('/feedback');
        const element = harness.routeNativeElement;
        expect(element?.textContent).toContain('<script>plain feedback</script>');
        expect(element?.querySelector('script')).toBeNull();
        expect(element?.querySelector('tbody a')?.getAttribute('href')).toContain('/feedback?');
        element?.querySelector<HTMLTableRowElement>('tbody tr')?.click();
        expect(open).toHaveBeenCalledOnce();
        expect(open.mock.calls[0]?.[1]).toHaveProperty('data.id', 'feedback-1');
    });

    it('preserves the normal inbox recipient filter when navigating away', async () => {
        const { getMessagePage, harness } = await setupFeedbackAsync('/feedback');
        await harness.navigateByUrl('/mail-inbox?recipient=other@example.test');
        expect(getMessagePage).toHaveBeenLastCalledWith(1, PAGE_SIZE, expect.objectContaining({ recipient: 'other@example.test' }));
    });
});
