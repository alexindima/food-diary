import { OverlayContainer } from '@angular/cdk/overlay';
import { signal } from '@angular/core';
import { TestBed } from '@angular/core/testing';
import { provideRouter, Router, withComponentInputBinding } from '@angular/router';
import { RouterTestingHarness } from '@angular/router/testing';
import { FdUiDialogService } from 'fd-ui-kit/dialog/fd-ui-dialog.service';
import { type Observable, of, Subject, throwError } from 'rxjs';
import { describe, expect, it, type Mock, vi } from 'vitest';

import { stubScrollIntoView } from '../../../../../../../src/testing/dom-testing';
import { provideTranslateTesting } from '../../../../../../../src/testing/translate-testing.module';
import { ADMIN_DEVELOPMENT_SCREENS, type AdminDevelopmentScreen } from '../../../shared/development/admin-development-screen';
import { type AdminId, adminId, adminUtcInstant, optionalAdminUtcInstant } from '../../../shared/models/semantics/admin-meaning';
import { AdminAuthService } from '../../admin-auth/contracts/admin-session';
import { AdminMailMessageDialogComponent, type AdminMailMessageDialogData } from '../dialogs/admin-mail-message-dialog';
import { AdminMailInboxFacade } from '../lib/admin-mail-inbox.facade';
import type { AdminMailInboxMessagePage, AdminMailInboxMessageSummary } from '../models/admin-mail-inbox.data';
import { AdminMailInboxComponent } from './admin-mail-inbox';

describe('AdminMailInboxComponent row activation', () => {
    it.each([null, '2026-09-08T00:00:00Z'])('opens mail from every cell and the keyboard when readAtUtc is %s', async readAtUtc => {
        const open = vi.fn();
        await TestBed.configureTestingModule({
            imports: [AdminMailInboxComponent],
            providers: [
                ...provideTranslateTesting(),
                provideRouter([]),
                { provide: AdminAuthService, useValue: { getUserId: (): null => null } },
                { provide: FdUiDialogService, useValue: { open } },
                {
                    provide: AdminMailInboxFacade,
                    useValue: {
                        getMessagePage: (): Observable<AdminMailInboxMessagePage> =>
                            of({
                                totalItems: 1,
                                items: [
                                    {
                                        id: adminId<'mail-inbox-message'>('mail-1'),
                                        subject: 'Test message',
                                        fromAddress: 'sender@example.com',
                                        toRecipients: ['bugs@example.com'],
                                        category: 'general',
                                        status: 'received',
                                        isTrustedRelay: true,
                                        receivedAtUtc: adminUtcInstant('2026-09-07T00:00:00Z'),
                                        readAtUtc: optionalAdminUtcInstant(readAtUtc),
                                    },
                                ],
                            }),
                    },
                },
            ],
        }).compileComponents();
        const fixture = TestBed.createComponent(AdminMailInboxComponent);
        fixture.detectChanges();
        await fixture.whenStable();
        fixture.detectChanges();
        const element = fixture.nativeElement as HTMLElement;
        const row = element.querySelector<HTMLTableRowElement>('tbody tr');
        if (row === null) {
            throw new Error('Expected a mail row');
        }
        for (const cell of row.cells) {
            open.mockClear();
            cell.click();
            expect(open).toHaveBeenCalledTimes(1);
            expect(open.mock.calls[0]?.[0]).toBe(AdminMailMessageDialogComponent);
            expect(open.mock.calls[0]?.[1]).toHaveProperty('data.id', 'mail-1');
        }
        for (const key of ['Enter', ' ']) {
            open.mockClear();
            row.dispatchEvent(new KeyboardEvent('keydown', { key, bubbles: true, cancelable: true }));
            expect(open).toHaveBeenCalledTimes(1);
        }
        fixture.destroy();
    });
});

const INBOX = '/mail-inbox';
const PAGE_SIZE = 50;
const PAGE_TWO = 2;
const INBOX_TOTAL = 51;
const REQUEST_COUNT_WITH_RELOAD = 3;
const currentMail: AdminMailInboxMessageSummary = {
    id: adminId<'mail-inbox-message'>('current-mail'),
    subject: 'Current message',
    fromAddress: 'sender@example.test',
    toRecipients: ['support@example.test'],
    category: 'dmarc-report',
    status: 'received',
    isTrustedRelay: false,
    receivedAtUtc: adminUtcInstant('2026-10-06T00:00:00Z'),
    readAtUtc: null,
};
type OpenMessage = (component: typeof AdminMailMessageDialogComponent, options: { data: AdminMailMessageDialogData }) => void;
type InboxTest = {
    harness: RouterTestingHarness;
    element: HTMLElement;
    router: Router;
    open: Mock<OpenMessage>;
};
type InboxTestOptions = {
    attach?: (screen: AdminDevelopmentScreen) => () => void;
    waitForPage?: boolean;
    currentUserId?: AdminAuthService['getUserId'];
};

async function setupInboxAsync(
    url: string,
    getMessagePage: Mock<AdminMailInboxFacade['getMessagePage']>,
    { attach, waitForPage = true, currentUserId = (): null => null }: InboxTestOptions = {},
): Promise<InboxTest> {
    const open = vi.fn<OpenMessage>();
    await TestBed.configureTestingModule({
        providers: [
            provideTranslateTesting(),
            provideRouter(
                [
                    { path: 'mail-inbox', component: AdminMailInboxComponent },
                    { path: 'feedback', component: AdminMailInboxComponent, data: { feedbackMailbox: true } },
                    { path: 'exit', children: [] },
                ],
                withComponentInputBinding({ unmatchedInputBehavior: 'undefinedIfStale' }),
            ),
            { provide: AdminAuthService, useValue: { getUserId: currentUserId } },
            { provide: ADMIN_DEVELOPMENT_SCREENS, useValue: attach === undefined ? null : { attach } },
            { provide: AdminMailInboxFacade, useValue: { getMessagePage } },
            { provide: FdUiDialogService, useValue: { open } },
        ],
    }).compileComponents();
    const harness = await RouterTestingHarness.create(url);
    if (waitForPage) {
        await renderInboxAsync(harness);
    }
    const element = harness.routeNativeElement;
    if (element === null) {
        throw new Error('Expected the inbox route');
    }
    return { harness, element, router: TestBed.inject(Router), open };
}

async function renderInboxAsync(harness: RouterTestingHarness): Promise<void> {
    await harness.fixture.whenStable();
    harness.detectChanges();
}

function requireNamedButton(element: HTMLElement, name: string): HTMLButtonElement {
    const button = [...element.querySelectorAll('button')].find(candidate => candidate.textContent.trim() === name);
    if (button === undefined) {
        throw new Error(`Expected the ${name} button`);
    }
    return button;
}

describe('admin inbox recipient preferences', () => {
    it('applies saved exclusions only in the main inbox and scopes them to the administrator', async () => {
        const userId = signal<AdminId<'user'> | null>(adminId<'user'>('mail-preferences-admin'));
        const supportMessage = { ...currentMail, subject: 'Question about DMARC', toRecipients: ['support@fooddiary.club'] };
        const getMessagePage = vi
            .fn<AdminMailInboxFacade['getMessagePage']>()
            .mockReturnValue(of({ items: [supportMessage], totalItems: 1 }));
        const { element, harness, router } = await setupInboxAsync(INBOX, getMessagePage, { currentUserId: userId });
        expect(getMessagePage).toHaveBeenLastCalledWith(
            1,
            PAGE_SIZE,
            expect.objectContaining({ recipient: '', category: '', excludeRecipients: ['dmarc@fooddiary.club'] }),
        );
        expect(element.querySelector('tbody')?.textContent).toContain(supportMessage.subject);

        requireNamedButton(element, 'ADMIN_MAIL_INBOX.MAILBOX_PREFERENCES').click();
        await renderInboxAsync(harness);
        const support = [...element.querySelectorAll('fd-ui-checkbox label')].find(
            label => label.textContent.trim() === 'support@fooddiary.club',
        );
        const checkbox = element.querySelector<HTMLInputElement>(`input[id="${support?.getAttribute('for')}"]`);
        if (checkbox === null) {
            throw new Error('Expected a checkbox for the support inbox');
        }
        checkbox.click();
        await renderInboxAsync(harness);
        expect(getMessagePage).toHaveBeenLastCalledWith(
            1,
            PAGE_SIZE,
            expect.objectContaining({ recipient: '', category: '', excludeRecipients: ['dmarc@fooddiary.club', 'support@fooddiary.club'] }),
        );

        const supportFilter = requireNamedButton(element, 'support@fooddiary.club');
        supportFilter.click();
        await renderInboxAsync(harness);
        expect(supportFilter.getAttribute('aria-pressed')).toBe('true');
        expect(router.parseUrl(router.url).queryParamMap.get('recipient')).toBe('support@fooddiary.club');
        expect(getMessagePage).toHaveBeenLastCalledWith(
            1,
            PAGE_SIZE,
            expect.objectContaining({ recipient: 'support@fooddiary.club', excludeRecipients: undefined }),
        );

        await harness.navigateByUrl(`${INBOX}?id=report-1`);
        await renderInboxAsync(harness);
        expect(getMessagePage).toHaveBeenLastCalledWith(
            1,
            PAGE_SIZE,
            expect.objectContaining({ id: 'report-1', category: '', excludeRecipients: undefined }),
        );

        await harness.navigateByUrl('/exit');
        await harness.navigateByUrl(INBOX);
        await renderInboxAsync(harness);
        expect(getMessagePage).toHaveBeenLastCalledWith(
            1,
            PAGE_SIZE,
            expect.objectContaining({ excludeRecipients: ['dmarc@fooddiary.club', 'support@fooddiary.club'] }),
        );

        userId.set(adminId<'user'>('other-mail-preferences-admin'));
        await renderInboxAsync(harness);
        expect(getMessagePage).toHaveBeenLastCalledWith(
            1,
            PAGE_SIZE,
            expect.objectContaining({ excludeRecipients: ['dmarc@fooddiary.club'] }),
        );
    });
});

describe('admin inbox requests', () => {
    it('ignores results from outdated filters and reloads the active request after reading a message', async () => {
        const previous = new Subject<AdminMailInboxMessagePage>();
        const filtered = new Subject<AdminMailInboxMessagePage>();
        const reload = new Subject<AdminMailInboxMessagePage>();
        const attach = vi.fn<(screen: AdminDevelopmentScreen) => () => void>().mockReturnValue(vi.fn());
        const getMessagePage = vi
            .fn<AdminMailInboxFacade['getMessagePage']>()
            .mockReturnValueOnce(previous)
            .mockReturnValueOnce(filtered)
            .mockReturnValueOnce(reload);
        const { element, harness, open, router } = await setupInboxAsync(`${INBOX}?unread=false`, getMessagePage, {
            attach,
            waitForPage: false,
        });
        expect(previous.observed).toBe(true);
        expect(getMessagePage).toHaveBeenLastCalledWith(1, PAGE_SIZE, expect.objectContaining({ unread: false }));
        await harness.navigateByUrl(`${INBOX}?category=dmarc-report&search=Current&fromAddress=sender@example.test`);
        expect(previous.observed).toBe(false);
        filtered.next({
            items: [currentMail, { ...currentMail, id: adminId<'mail-inbox-message'>('another'), subject: 'Another message' }],
            totalItems: 2,
        });
        previous.next({ items: [{ ...currentMail, subject: 'Stale result' }], totalItems: 1 });
        await renderInboxAsync(harness);
        expect(element.querySelector('tbody')?.textContent).toContain('Current message');
        expect(element.querySelector('tbody')?.textContent).not.toContain('Stale result');
        expect(element.querySelector('.mail-badge')?.textContent.trim()).toBe('DMARC');

        const subject = element.querySelector<HTMLInputElement>('input[aria-label="ADMIN_MAIL_INBOX.SUBJECT_FILTER"]');
        if (subject === null) {
            throw new Error('Expected the inbox subject filter');
        }
        subject.value = 'Draft subject';
        subject.dispatchEvent(new Event('input', { bubbles: true }));
        expect(getMessagePage).toHaveBeenCalledTimes(2);
        element.querySelector<HTMLTableRowElement>('tbody tr')?.click();
        expect(open).toHaveBeenCalledOnce();
        const data = open.mock.calls[0][1].data;
        expect(data.id).toBe(currentMail.id);
        expect(data.onRead).toBeTypeOf('function');
        data.onRead?.(currentMail.id, currentMail.receivedAtUtc);
        harness.detectChanges();
        expect(filtered.observed).toBe(false);
        expect(reload.observed).toBe(true);
        expect(getMessagePage).toHaveBeenCalledTimes(REQUEST_COUNT_WITH_RELOAD);
        expect(getMessagePage).toHaveBeenLastCalledWith(
            1,
            PAGE_SIZE,
            expect.objectContaining({
                category: 'dmarc-report',
                search: 'Draft subject',
                fromAddress: 'sender@example.test',
                unread: undefined,
            }),
        );
        expect(router.parseUrl(router.url).queryParamMap.get('search')).toBe('Current');
        expect(attach.mock.lastCall?.[0].read()).toMatchObject({
            loading: true,
            items: [{ id: currentMail.id, readAtUtc: currentMail.receivedAtUtc }, { id: 'another' }],
        });
        reload.next({ items: [{ ...currentMail, readAtUtc: currentMail.receivedAtUtc }], totalItems: 1 });
        await renderInboxAsync(harness);
        expect(element.querySelector('.mail-read-state')?.textContent.trim()).toBe('Read');
    });
});

describe('admin inbox request recovery', () => {
    it('rejects invalid periods on refresh and recovers a failed request without dropping filters', async () => {
        const getMessagePage = vi
            .fn<AdminMailInboxFacade['getMessagePage']>()
            .mockReturnValueOnce(throwError(() => new Error('Inbox unavailable')))
            .mockReturnValue(of({ items: [], totalItems: 0 }));
        const { element, harness } = await setupInboxAsync(`${INBOX}?period=custom&from=2026-10-07&to=2026-10-06`, getMessagePage);
        requireNamedButton(element, 'Refresh').click();
        expect(getMessagePage).not.toHaveBeenCalled();

        await harness.navigateByUrl(`${INBOX}?recipient=support@example.test&category=general`);
        await renderInboxAsync(harness);
        expect(element.querySelector('[role="alert"]')?.textContent).toContain('ADMIN_MAIL_INBOX.LIST_LOAD_ERROR');
        requireNamedButton(element, 'Refresh').click();
        await renderInboxAsync(harness);
        expect(element.querySelector('[role="alert"]')).toBeNull();
        expect(element.querySelector('tbody')?.textContent).toContain('No inbound messages found.');
        expect(getMessagePage).toHaveBeenCalledTimes(2);
        expect(getMessagePage).toHaveBeenLastCalledWith(
            1,
            PAGE_SIZE,
            expect.objectContaining({ recipient: 'support@example.test', category: 'general' }),
        );
    });
});

describe('admin inbox navigation', () => {
    it('opens the next page and resets to page one when choosing another message category', async () => {
        stubScrollIntoView();
        const getMessagePage = vi
            .fn<AdminMailInboxFacade['getMessagePage']>()
            .mockReturnValue(of({ items: [currentMail], totalItems: INBOX_TOTAL }));
        const { element, harness, router } = await setupInboxAsync(`${INBOX}?search=Current`, getMessagePage);
        requireNamedButton(element, '2').click();
        await renderInboxAsync(harness);
        expect(getMessagePage).toHaveBeenLastCalledWith(PAGE_TWO, PAGE_SIZE, expect.objectContaining({ search: 'Current' }));

        const category = [...element.querySelectorAll('fd-ui-select')].find(select =>
            select.textContent.includes('ADMIN_MAIL_INBOX.TYPE_FILTER'),
        );
        const trigger = category?.querySelector<HTMLButtonElement>('button[aria-haspopup="listbox"]');
        expect(trigger).toBeTruthy();
        trigger?.click();
        await renderInboxAsync(harness);
        const overlay = TestBed.inject(OverlayContainer).getContainerElement();
        const option = [...overlay.querySelectorAll<HTMLElement>('[role="option"]')].find(item => item.textContent.trim() === 'DMARC');
        expect(option).toBeDefined();
        option?.click();
        await renderInboxAsync(harness);
        const params = router.parseUrl(router.url).queryParamMap;
        expect(params.get('page')).toBe('1');
        expect(params.get('category')).toBe('dmarc-report');
        expect(getMessagePage).toHaveBeenLastCalledWith(
            1,
            PAGE_SIZE,
            expect.objectContaining({ category: 'dmarc-report', search: 'Current' }),
        );

        for (const [label, categoryFilter] of [
            ['ADMIN_MAIL_INBOX.MAIL', 'general'],
            ['ADMIN_MAIL_INBOX.ALL', ''],
        ]) {
            trigger?.click();
            await renderInboxAsync(harness);
            const choice = [...overlay.querySelectorAll<HTMLElement>('[role="option"]')].find(item => item.textContent.trim() === label);
            expect(choice).toBeTruthy();
            choice?.click();
            await renderInboxAsync(harness);
            expect(getMessagePage).toHaveBeenLastCalledWith(1, PAGE_SIZE, expect.objectContaining({ category: categoryFilter }));
        }
    });

    it('restores a bookmarked page that no longer exists, then resets it when choosing the read count', async () => {
        const getMessagePage = vi
            .fn<AdminMailInboxFacade['getMessagePage']>()
            .mockReturnValue(of({ items: [currentMail], totalItems: INBOX_TOTAL, readCount: 1 }));
        const { element, harness, router } = await setupInboxAsync(`${INBOX}?page=99&search=Current`, getMessagePage);

        expect(router.parseUrl(router.url).queryParamMap.get('page')).toBe('2');
        expect(getMessagePage).toHaveBeenLastCalledWith(PAGE_TWO, PAGE_SIZE, expect.objectContaining({ search: 'Current' }));
        requireNamedButton(element, 'ADMIN_MAIL_INBOX.READ: 1').click();
        await renderInboxAsync(harness);

        const params = router.parseUrl(router.url).queryParamMap;
        expect(params.get('page')).toBe('1');
        expect(params.get('unread')).toBe('false');
        expect(getMessagePage).toHaveBeenLastCalledWith(1, PAGE_SIZE, expect.objectContaining({ unread: false, search: 'Current' }));
    });
});

describe('admin inbox screen state', () => {
    it.each([
        { path: INBOX, recipient: 'alternate@example.test' },
        { path: '/feedback', recipient: 'feedback@fooddiary.club' },
    ])('reports and filters $path while respecting its mailbox scope', async ({ path, recipient }) => {
        const detach = vi.fn();
        const attach = vi.fn<(screen: AdminDevelopmentScreen) => () => void>().mockReturnValue(detach);
        const getMessagePage = vi.fn<AdminMailInboxFacade['getMessagePage']>().mockReturnValue(of({ items: [currentMail], totalItems: 1 }));
        const { harness } = await setupInboxAsync(path, getMessagePage, { attach });
        const screen = attach.mock.lastCall?.[0];
        expect(screen?.kind).toBe('admin-mail-inbox');
        await screen?.setFiltersAsync?.({ recipient: 'alternate@example.test', search: 'Current' });
        await renderInboxAsync(harness);

        expect(getMessagePage).toHaveBeenLastCalledWith(1, PAGE_SIZE, expect.objectContaining({ recipient, search: 'Current' }));
        expect(screen?.read()).toMatchObject({
            filters: { recipient, search: 'Current' },
            page: 1,
            loading: false,
            error: false,
            items: [{ id: currentMail.id, status: currentMail.status }],
        });
        await harness.navigateByUrl('/exit');
        expect(detach).toHaveBeenCalledOnce();
    });
});
