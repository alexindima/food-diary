import { HttpStatusCode, provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting, type TestRequest } from '@angular/common/http/testing';
import { TestBed } from '@angular/core/testing';
import { provideRouter, Router, withComponentInputBinding } from '@angular/router';
import { RouterTestingHarness } from '@angular/router/testing';
import { FdUiDialogService } from 'fd-ui-kit';
import { describe, expect, it, vi } from 'vitest';

import { provideTranslateTesting } from '../../../../../../src/testing/translate-testing.module';
import { environment } from '../../../environments/environment';
import type { AdminMailInboxMessageDetailsHttpResponse } from '../../shared/api/sdk/generated/model/admin-mail-inbox-message-details-http-response';
import type { AdminMailInboxMessageSummaryHttpResponse } from '../../shared/api/sdk/generated/model/admin-mail-inbox-message-summary-http-response';
import { AdminAuthService } from '../admin-auth/contracts/admin-session';
import { adminDmarcRoutes } from './admin-mail-inbox.routes';
import { AdminMailMessageDialogComponent } from './dialogs/admin-mail-message-dialog';

const PATH = '/mail-dmarc';
const API = `${environment.apiUrls.auth.replace(/\/auth$/, '')}/admin/mail-inbox/messages`;
const DAY_MS = 86_400_000;
const STABLE_REPORT = message('stable-report', 'stable@example.test');
const FAILED_REPORT = message('failed-report', 'failed@example.test');

describe('DMARC attachment failure and retry', () => {
    it('retries only failed attachments for the same period and keeps the verified rows visible', async () => {
        const { harness, http, element, router } = await openDmarcAsync();
        const dateRangeStartUtc = flushList(http, [STABLE_REPORT, FAILED_REPORT]);
        requestReport(http, STABLE_REPORT).flush(verifiedDetail(STABLE_REPORT, dateRangeStartUtc, '192.0.2.1'));
        failReport(requestReport(http, FAILED_REPORT));
        await renderDmarcAsync(harness);
        expect(element.textContent).toContain('ADMIN_DMARC.PARTIAL');
        expect(element.textContent).toContain('stable@example.test');
        expect(element.textContent).toContain('ADMIN_DMARC.LOAD_ERROR');
        expect(element.textContent).toContain('192.0.2.1');

        const retry = retryButton(element);
        retry.click();
        retry.click();
        harness.detectChanges();
        expect(retryButton(element).disabled).toBe(true);
        expect(element.textContent).toContain('stable@example.test');
        expect(element.textContent).toContain('failed@example.test');
        expect(element.textContent).toContain('192.0.2.1');
        http.expectNone(request => request.url === `${API}/page` && request.method === 'GET');
        http.expectNone({ url: `${API}/${STABLE_REPORT.id}`, method: 'GET' });

        requestReport(http, FAILED_REPORT).flush(verifiedDetail(FAILED_REPORT, dateRangeStartUtc, '192.0.2.2'));
        await renderDmarcAsync(harness);

        expect(element.textContent).not.toContain('ADMIN_DMARC.PARTIAL');
        expect(element.textContent).toContain('192.0.2.1');
        expect(element.textContent).toContain('192.0.2.2');
        expect(element.textContent).not.toContain('ADMIN_COMMON.RETRY');
        expect(router.url).toBe(`${PATH}?period=30`);
        http.verify();
    });

    it('distinguishes an unread attachment from a successful empty period and cancels old retries on period change', async () => {
        const { harness, http, element, router } = await openDmarcAsync();
        flushList(http, [FAILED_REPORT]);
        failReport(requestReport(http, FAILED_REPORT));
        await renderDmarcAsync(harness);
        expect(element.textContent).not.toContain('ADMIN_DMARC.NO_VERIFIED_REPORTS');

        retryButton(element).click();
        harness.detectChanges();
        const pending = requestReport(http, FAILED_REPORT);
        await router.navigateByUrl(`${PATH}?period=7`);
        harness.detectChanges();
        expect(pending.cancelled).toBe(true);
        flushList(http, []);
        await renderDmarcAsync(harness);

        expect(element.textContent).toContain('ADMIN_DMARC.NO_VERIFIED_REPORTS');
        expect(element.textContent).not.toContain('ADMIN_COMMON.RETRY');
        expect(element.textContent).not.toContain('failed@example.test');
        http.verify();
    });
});

it('shows incomplete coverage without a retry when the message returned without a report', async () => {
    const { harness, http, element } = await openDmarcAsync();
    flushList(http, [STABLE_REPORT]);
    requestReport(http, STABLE_REPORT).flush(STABLE_REPORT);
    await renderDmarcAsync(harness);

    expect(element.textContent).toContain('ADMIN_DMARC.PARTIAL');
    expect(element.textContent).toContain('ADMIN_DMARC.UNAVAILABLE');
    expect(element.textContent).not.toContain('ADMIN_COMMON.RETRY');
    http.verify();
});

it('keeps unverified and failed attachments with missing sender data available as original email', async () => {
    const { harness, http, element, open } = await openDmarcAsync();
    const unverified = { ...STABLE_REPORT, fromAddress: null, subject: 'Unverified original' };
    const failed = { ...FAILED_REPORT, fromAddress: null };
    const from = flushList(http, [unverified, failed]);
    const detail = verifiedDetail(unverified, from, '192.0.2.1');
    requestReport(http, unverified).flush({ ...detail, dmarcReport: { ...detail.dmarcReport, isSourceVerified: false } });
    failReport(requestReport(http, failed));
    await renderDmarcAsync(harness);

    const rows = [...element.querySelectorAll('fd-admin-dmarc-reports tbody tr')];
    expect(rows[0]?.textContent).toContain('ADMIN_DMARC.UNVERIFIED');
    expect(rows[1]?.textContent).toContain('ADMIN_DMARC.LOAD_ERROR');
    for (const row of rows) {
        expect(row.querySelector('th')?.textContent.trim()).toBe('—');
    }
    rows[0]?.querySelector('button')?.click();
    expect(open).toHaveBeenCalledExactlyOnceWith(
        AdminMailMessageDialogComponent,
        expect.objectContaining({ data: { id: unverified.id, subject: unverified.subject } }),
    );
    http.verify();
});

it('retries an initial list failure and switches the report window from the rendered control', async () => {
    const { harness, http, element, router } = await openDmarcAsync();
    failReport(http.expectOne(request => request.url === `${API}/page` && request.method === 'GET'));
    await renderDmarcAsync(harness);
    expect(element.querySelector('[role="alert"]')).not.toBeNull();
    element.querySelector<HTMLButtonElement>('fd-admin-load-error button')?.click();
    harness.detectChanges();
    const previousStart = flushList(http, []);
    await renderDmarcAsync(harness);

    expect(element.querySelector('[role="alert"]')).toBeNull();
    const week = [...element.querySelectorAll<HTMLButtonElement>('fd-ui-segmented-toggle button')].find(
        button => button.textContent.trim() === 'ADMIN_DMARC.PERIOD_7',
    );
    expect(week).toBeDefined();
    week?.click();
    await vi.waitFor(() => {
        expect(router.url).toBe(`${PATH}?period=7`);
    });
    harness.detectChanges();
    expect(Date.parse(flushList(http, []))).toBeGreaterThan(Date.parse(previousStart));
    await renderDmarcAsync(harness);
    http.verify();
});

async function openDmarcAsync(): Promise<{
    harness: RouterTestingHarness;
    http: HttpTestingController;
    element: HTMLElement;
    router: Router;
    open: ReturnType<typeof vi.fn>;
}> {
    const open = vi.fn();
    TestBed.configureTestingModule({
        providers: [
            provideRouter(
                [{ path: 'mail-dmarc', children: adminDmarcRoutes }],
                withComponentInputBinding({ unmatchedInputBehavior: 'undefinedIfStale' }),
            ),
            provideTranslateTesting(),
            provideHttpClient(),
            provideHttpClientTesting(),
            { provide: FdUiDialogService, useValue: { open } },
            {
                provide: AdminAuthService,
                useValue: {
                    applySsoFromQueryAsync: vi.fn<AdminAuthService['applySsoFromQueryAsync']>().mockResolvedValue(undefined),
                    sessionUnavailable: (): boolean => false,
                    isAuthenticated: (): boolean => true,
                    isAdmin: (): boolean => true,
                },
            },
        ],
    });
    const harness = await RouterTestingHarness.create(`${PATH}?period=30`);
    if (harness.routeNativeElement === null) {
        throw new Error('Expected the DMARC route');
    }
    return {
        harness,
        http: TestBed.inject(HttpTestingController),
        element: harness.routeNativeElement,
        router: TestBed.inject(Router),
        open,
    };
}

async function renderDmarcAsync(harness: RouterTestingHarness): Promise<void> {
    await harness.fixture.whenStable();
    harness.detectChanges();
}

function retryButton(element: HTMLElement): HTMLButtonElement {
    const button = [...element.querySelectorAll<HTMLButtonElement>('fd-admin-dmarc-reports button')].find(
        candidate => candidate.textContent.trim() === 'ADMIN_COMMON.RETRY',
    );
    if (button === undefined) {
        throw new Error('Expected the attachment retry action');
    }
    return button;
}

function flushList(http: HttpTestingController, items: AdminMailInboxMessageSummaryHttpResponse[]): string {
    const request = http.expectOne(candidate => candidate.url === `${API}/page` && candidate.method === 'GET');
    const from = request.request.params.get('fromUtc');
    if (from === null) {
        throw new Error('Expected the requested DMARC window');
    }
    request.flush({ items, totalItems: items.length });
    return from;
}

function requestReport(http: HttpTestingController, report: AdminMailInboxMessageSummaryHttpResponse): TestRequest {
    return http.expectOne({ url: `${API}/${report.id}`, method: 'GET' });
}

function failReport(request: TestRequest): void {
    request.flush(null, { status: HttpStatusCode.ServiceUnavailable, statusText: 'Service Unavailable' });
}

function message(id: string, fromAddress: string): AdminMailInboxMessageSummaryHttpResponse {
    return {
        id,
        fromAddress,
        category: 'dmarc-report',
        status: 'processed',
        isTrustedRelay: true,
        toRecipients: ['dmarc@fooddiary.club'],
        receivedAtUtc: '2026-11-01T00:00:00Z',
    };
}

function verifiedDetail(
    report: AdminMailInboxMessageSummaryHttpResponse,
    dateRangeStartUtc: string,
    sourceIp: string,
): AdminMailInboxMessageDetailsHttpResponse {
    return {
        ...report,
        dmarcReport: {
            domain: 'fooddiary.club',
            organizationName: report.fromAddress,
            reportId: report.id,
            isSourceVerified: true,
            dateRangeStartUtc,
            dateRangeEndUtc: new Date(Date.parse(dateRangeStartUtc) + DAY_MS).toISOString(),
            records: [{ sourceIp, count: 1, dkim: 'pass' }],
        },
    };
}
