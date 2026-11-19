import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { TestBed } from '@angular/core/testing';
import { NavigationEnd, provideRouter, Router } from '@angular/router';
import { RouterTestingHarness } from '@angular/router/testing';
import { filter, of, Subject, take } from 'rxjs';
import { describe, expect, it, type Mock, vi } from 'vitest';

import { provideTranslateTesting } from '../../../../../../../src/testing/translate-testing.module';
import { environment } from '../../../../environments/environment';
import { ADMIN_DEVELOPMENT_SCREENS, type AdminDevelopmentScreen } from '../../../shared/development/admin-development-screen';
import { adminId, adminUtcInstant } from '../../../shared/models/semantics/admin-meaning';
import { AdminOutgoingEmailsFacade } from '../lib/admin-outgoing-emails.facade';
import type { OutgoingEmail, OutgoingEmailPage } from '../models/outgoing-email';
import { AdminOutgoingEmailsComponent } from './admin-outgoing-emails';

describe('AdminOutgoingEmailsComponent', () => {
    it('shows a failed request and recovers to the empty state after refresh', async () => {
        const first = new Subject<OutgoingEmailPage>();
        const retry = new Subject<OutgoingEmailPage>();
        const getPage = vi.fn().mockReturnValueOnce(first).mockReturnValueOnce(retry);
        await TestBed.configureTestingModule({
            imports: [AdminOutgoingEmailsComponent],
            providers: [provideRouter([]), ...provideTranslateTesting(), { provide: AdminOutgoingEmailsFacade, useValue: { getPage } }],
        }).compileComponents();
        const fixture = TestBed.createComponent(AdminOutgoingEmailsComponent);
        const element = fixture.nativeElement as HTMLElement;
        fixture.detectChanges();
        expect(element.querySelector('[role="status"]')).not.toBeNull();
        first.error(new Error('Unavailable'));
        fixture.detectChanges();
        expect(element.querySelector('[role="alert"]')).not.toBeNull();
        const refresh = element.querySelector<HTMLButtonElement>('fd-ui-button button');
        expect(refresh).not.toBeNull();
        refresh?.click();
        await fixture.whenStable();
        retry.next({ items: [], totalItems: 0 });
        fixture.detectChanges();
        expect(element.querySelector('[role="alert"]')).toBeNull();
        expect(element.textContent).toContain('ADMIN_OUTGOING.EMPTY');
        expect(getPage).toHaveBeenLastCalledWith(1, '', '', { recipient: '', id: '', correlationId: '' });
        fixture.destroy();
    });
});

const OUTBOX = '/outgoing-emails';
const OUTBOX_TOTAL = 51;
const REQUESTED_PAGE = 4;
const LAST_PAGE = 2;
const RECIPIENT = 'customer@example.test';
const MAIL: OutgoingEmail = {
    id: adminId<'outgoing-email'>('mail-1'),
    status: 'sent',
    purpose: 'legacy-purpose',
    fromAddress: 'feedback@example.test',
    to: [RECIPIENT],
    subject: 'Your request was received',
    createdAtUtc: adminUtcInstant('2026-10-06T00:00:00Z'),
    sentAtUtc: null,
    attemptCount: 1,
    maxAttempts: 1,
    correlationId: 'request-1',
    textBody: 'Thanks for writing.',
    contentHidden: false,
    replyTo: null,
    inReplyTo: null,
};

async function setupOutboxAsync(
    url: string,
    getPage: Mock<AdminOutgoingEmailsFacade['getPage']>,
    attach?: (screen: AdminDevelopmentScreen) => () => void,
): Promise<{ harness: RouterTestingHarness; element: HTMLElement; router: Router }> {
    await TestBed.configureTestingModule({
        providers: [
            provideRouter([{ path: 'outgoing-emails', component: AdminOutgoingEmailsComponent }]),
            ...provideTranslateTesting(),
            { provide: AdminOutgoingEmailsFacade, useValue: { getPage } },
            { provide: ADMIN_DEVELOPMENT_SCREENS, useValue: attach === undefined ? null : { attach } },
        ],
    }).compileComponents();
    const harness = await RouterTestingHarness.create(url);
    await renderOutboxAsync(harness);
    const element = harness.routeNativeElement;
    if (element === null) {
        throw new Error('Expected the outgoing mail route');
    }
    return { harness, element, router: TestBed.inject(Router) };
}

async function renderOutboxAsync(harness: RouterTestingHarness): Promise<void> {
    await harness.fixture.whenStable();
    harness.detectChanges();
}

function requireOutboxButton(element: HTMLElement, name: string): HTMLButtonElement {
    const button = [...element.querySelectorAll('button')].find(candidate => candidate.textContent.trim() === name);
    if (button === undefined) {
        throw new Error(`Expected the ${name} button`);
    }
    return button;
}

it('keeps the requested page when a final detail click follows a return to the list', async () => {
    TestBed.configureTestingModule({
        providers: [
            provideRouter([{ path: 'outgoing-emails', component: AdminOutgoingEmailsComponent }]),
            provideTranslateTesting(),
            provideHttpClient(),
            provideHttpClientTesting(),
        ],
    });
    const harness = await RouterTestingHarness.create(`${OUTBOX}?status=sent&id=${MAIL.id}`);
    const router = TestBed.inject(Router);
    const requests = TestBed.inject(HttpTestingController);
    const api = `${environment.apiUrls.auth.replace(/\/auth$/, '')}/admin/outgoing-emails`;
    const request = requests.expectOne(candidate => candidate.url === api && candidate.params.get('id') === MAIL.id);
    request.flush({ items: [MAIL], totalItems: 1 });
    harness.detectChanges();
    const element = harness.routeNativeElement;
    if (element === null) {
        throw new Error('Expected the outgoing email reader.');
    }
    const close = requireOutboxButton(element, 'ADMIN_OUTGOING.CLOSE');
    const list = `${OUTBOX}?status=sent&page=${LAST_PAGE}`;
    const clickBeforeRender = vi.fn<(connected: boolean) => void>();
    router.events
        .pipe(
            filter(event => event instanceof NavigationEnd && event.urlAfterRedirects === list),
            take(1),
        )
        .subscribe(() => {
            clickBeforeRender(close.isConnected);
            close.click();
        });
    await router.navigateByUrl(list);
    expect(clickBeforeRender).toHaveBeenCalledExactlyOnceWith(true);
    const listRequest = requests.expectOne(candidate => candidate.url === api && !candidate.params.has('id'));
    expect(listRequest.request.params.get('page')).toBe(String(LAST_PAGE));
    listRequest.flush({ items: [MAIL], totalItems: OUTBOX_TOTAL });
    await renderOutboxAsync(harness);

    expect(router.url).toBe(list);
    expect(element.querySelector('fd-outgoing-email-details')).toBeNull();
    expect(element.querySelector('tbody')?.textContent).toContain(RECIPIENT);
    requests.verify();
});

describe('outgoing mail route state', () => {
    it('skips an invalid period and preserves the corrected UTC bounds when recovering an out-of-range page', async () => {
        const getPage = vi
            .fn<AdminOutgoingEmailsFacade['getPage']>()
            .mockImplementation(page => of({ items: page === LAST_PAGE ? [MAIL] : [], totalItems: OUTBOX_TOTAL }));
        const { element, harness, router } = await setupOutboxAsync(`${OUTBOX}?period=custom&from=2026-10-07&to=2026-10-06`, getPage);
        expect(getPage).not.toHaveBeenCalled();
        expect(element.textContent).toContain('ADMIN_OUTGOING.EMPTY');

        await harness.navigateByUrl(
            `${OUTBOX}?page=${REQUESTED_PAGE}&period=custom&from=2026-10-06&to=2026-10-06&recipient=${RECIPIENT}&status=sent`,
        );
        await renderOutboxAsync(harness);

        const filters = {
            recipient: RECIPIENT,
            fromUtc: '2026-10-06T00:00:00Z',
            toUtc: '2026-10-07T00:00:00.000Z',
            id: '',
            correlationId: '',
        };
        expect(getPage).toHaveBeenNthCalledWith(1, REQUESTED_PAGE, '', 'sent', filters);
        expect(getPage).toHaveBeenNthCalledWith(2, LAST_PAGE, '', 'sent', filters);
        expect(router.parseUrl(router.url).queryParamMap.get('page')).toBe(String(LAST_PAGE));
        expect(element.querySelector('tbody')?.textContent).toContain('ADMIN_OUTGOING.PURPOSES.other');
    });

    it('paginates, opens a message by its permalink and closes it without dropping list filters', async () => {
        const getPage = vi.fn<AdminOutgoingEmailsFacade['getPage']>().mockImplementation((_page, _purpose, _status, filters) =>
            of({
                items: [{ ...MAIL, purpose: 'password_reset' }],
                totalItems: filters?.['id'] === MAIL.id ? 1 : OUTBOX_TOTAL,
                statusCounts: { sent: OUTBOX_TOTAL },
            }),
        );
        const { element, harness, router } = await setupOutboxAsync(`${OUTBOX}?status=sent&recipient=${RECIPIENT}`, getPage);
        requireOutboxButton(element, '2').click();
        await renderOutboxAsync(harness);
        expect(getPage).toHaveBeenLastCalledWith(LAST_PAGE, '', 'sent', expect.objectContaining({ recipient: RECIPIENT }));
        expect(element.querySelector('tbody')?.textContent).toContain('ADMIN_OUTGOING.PURPOSES.password_reset');

        const link = element.querySelector<HTMLAnchorElement>('tbody a');
        expect(link).not.toBeNull();
        link?.click();
        await renderOutboxAsync(harness);
        expect(getPage).toHaveBeenLastCalledWith(1, '', 'sent', expect.objectContaining({ id: MAIL.id, recipient: RECIPIENT }));
        expect(element.querySelector('fd-outgoing-email-details .mail-body')?.textContent).toBe(MAIL.textBody);

        requireOutboxButton(element, 'ADMIN_OUTGOING.CLOSE').click();
        await renderOutboxAsync(harness);
        const params = router.parseUrl(router.url).queryParamMap;
        expect(params.has('id')).toBe(false);
        expect(params.get('recipient')).toBe(RECIPIENT);
        expect(params.get('status')).toBe('sent');
        expect(element.querySelector('fd-outgoing-email-details')).toBeNull();
    });

    it('keeps the empty state when a message permalink has no matching record', async () => {
        const getPage = vi.fn<AdminOutgoingEmailsFacade['getPage']>().mockReturnValue(of({ items: [], totalItems: 0 }));
        const { element } = await setupOutboxAsync(`${OUTBOX}?id=missing`, getPage);
        expect(element.querySelector('fd-outgoing-email-details')).toBeNull();
        expect(element.textContent).toContain('ADMIN_OUTGOING.EMPTY');
        expect(getPage).toHaveBeenCalledWith(1, '', '', expect.objectContaining({ id: 'missing' }));
    });
});

describe('outgoing mail applied filters', () => {
    it('reports visible rows without their bodies and refreshes the same canonical URL after filtering', async () => {
        const attach = vi.fn<(screen: AdminDevelopmentScreen) => () => void>().mockReturnValue(vi.fn());
        const getPage = vi
            .fn<AdminOutgoingEmailsFacade['getPage']>()
            .mockReturnValue(of({ items: [MAIL], totalItems: 1, statusCounts: { sent: 1 } }));
        const { element, harness, router } = await setupOutboxAsync(OUTBOX, getPage, attach);
        const screen = attach.mock.lastCall?.[0];
        expect(screen?.kind).toBe('admin-outgoing-emails');
        await screen?.setFiltersAsync?.({ page: '1', status: 'sent', recipient: RECIPIENT });
        await renderOutboxAsync(harness);

        const state = screen?.read();
        expect(state).toMatchObject({
            filters: { page: '1', status: 'sent', recipient: RECIPIENT },
            statusCounts: { sent: 1 },
            items: [{ id: MAIL.id, correlationId: MAIL.correlationId }],
        });
        expect(state).not.toMatchObject({ items: [{ textBody: MAIL.textBody }] });
        const appliedUrl = router.url;
        const callsBeforeRefresh = getPage.mock.calls.length;
        requireOutboxButton(element, 'ADMIN_OUTGOING.REFRESH').click();
        await renderOutboxAsync(harness);

        expect(router.url).toBe(appliedUrl);
        expect(getPage).toHaveBeenCalledTimes(callsBeforeRefresh + 1);
        expect(getPage).toHaveBeenLastCalledWith(1, '', 'sent', expect.objectContaining({ recipient: RECIPIENT }));
    });
});

describe('outgoing receipt source and delivery counts', () => {
    it('links a bug report receipt to its correlated inbox message', async () => {
        const receipt = { ...MAIL, purpose: 'bug_report_received', correlationId: 'source-message' };
        const getPage = vi.fn<AdminOutgoingEmailsFacade['getPage']>().mockReturnValue(of({ items: [receipt], totalItems: 1 }));
        const { element } = await setupOutboxAsync(`${OUTBOX}?id=${receipt.id}`, getPage);
        const source = element.querySelector<HTMLAnchorElement>('fd-outgoing-email-details a');
        expect(source?.hidden).toBe(false);
        expect(source?.textContent).toContain('ADMIN_BUGS.SOURCE');
        expect(source?.getAttribute('href')).toBe('/mail-inbox?id=source-message');
    });

    it('hides missing counts, shows help for empty counts and renders returned delivery statuses', async () => {
        const page = { items: [MAIL], totalItems: 1 };
        const getPage = vi
            .fn<AdminOutgoingEmailsFacade['getPage']>()
            .mockReturnValueOnce(of(page))
            .mockReturnValueOnce(of({ ...page, statusCounts: {} }))
            .mockReturnValueOnce(of({ ...page, statusCounts: { sent: 1 } }));
        const { element, harness } = await setupOutboxAsync(OUTBOX, getPage);
        const counts = element.querySelector('fd-outgoing-status-counts');
        expect(counts?.textContent).not.toContain('ADMIN_OUTGOING.STATUS_COUNTS_HELP');

        requireOutboxButton(element, 'ADMIN_OUTGOING.REFRESH').click();
        await renderOutboxAsync(harness);
        expect(counts?.textContent).toContain('ADMIN_OUTGOING.STATUS_COUNTS_HELP');
        expect(counts?.querySelector('button')).toBeNull();

        requireOutboxButton(element, 'ADMIN_OUTGOING.REFRESH').click();
        await renderOutboxAsync(harness);
        expect(counts?.querySelector('button')?.textContent.trim()).toBe('ADMIN_OUTGOING.STATUSES.sent: 1');
    });
});
