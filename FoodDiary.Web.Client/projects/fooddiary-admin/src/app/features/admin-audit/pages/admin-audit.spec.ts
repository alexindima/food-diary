import type { ComponentFixture } from '@angular/core/testing';
import { TestBed } from '@angular/core/testing';
import { provideRouter, Router } from '@angular/router';
import { of, Subject } from 'rxjs';
import { beforeEach, describe, expect, it, vi } from 'vitest';

import { waitForAsyncTasksAsync } from '../../../../../../../src/testing/async-testing';
import { provideTranslateTesting } from '../../../../../../../src/testing/translate-testing.module';
import { adminId, adminUtcInstant } from '../../../shared/models/semantics/admin-meaning';
import { AdminAuditService } from '../api/admin-audit.service';
import type { AdminAuditEntry, AdminAuditPageResult } from '../models/admin-audit';
import { AdminAuditPageComponent } from './admin-audit';

const PAGE_SIZE = 25;
const SECOND_PAGE = 2;
const ACTION_MAX_LENGTH = 200;
const TARGET_TYPE_MAX_LENGTH = 100;
const TARGET_ID_MAX_LENGTH = 200;
const ENTRY: AdminAuditEntry = {
    id: adminId<'audit-event'>('audit-1'),
    actorUserId: adminId<'user'>('admin-1'),
    subjectClientUserId: adminId<'user'>('client-1'),
    action: 'Moderation.ReportReviewed',
    targetType: 'ContentReport',
    targetId: 'report-123',
    metadata: '{"adminNote":"Verified content"}',
    createdAtUtc: adminUtcInstant('2026-08-01T12:30:00Z'),
};
type AuditFixture = ComponentFixture<AdminAuditPageComponent>;
const getPage = vi.fn<AdminAuditService['getPage']>();

beforeEach(async () => {
    getPage.mockReset().mockReturnValue(of({ items: [ENTRY], totalItems: PAGE_SIZE + 1 }));
    await TestBed.configureTestingModule({
        imports: [AdminAuditPageComponent],
        providers: [provideRouter([]), ...provideTranslateTesting(), { provide: AdminAuditService, useValue: { getPage } }],
    }).compileComponents();
});

describe('AdminAuditPageComponent filters', () => {
    it('restores identity and target filters from the URL and submits edited values once with page reset', async () => {
        const fixture = await renderAsync(
            '?page=2&actorUserId=actor-old&subjectClientUserId=client-old&action=old-action&targetType=Recipe&targetId=old-target&period=custom&from=2026-08-01&to=2026-08-03',
        );
        expect(getPage).toHaveBeenCalledWith({
            page: SECOND_PAGE,
            limit: PAGE_SIZE,
            actorUserId: 'actor-old',
            subjectClientUserId: 'client-old',
            action: 'old-action',
            targetType: 'Recipe',
            targetId: 'old-target',
            fromUtc: '2026-08-01T00:00:00Z',
            toUtc: '2026-08-04T00:00:00.000Z',
        });
        expect(input(fixture, 'ADMIN_AUDIT.ACTOR').value).toBe('actor-old');
        expect(input(fixture, 'ADMIN_AUDIT.SUBJECT').value).toBe('client-old');
        edit(fixture, 'ADMIN_AUDIT.ACTOR', 'admin-new');
        edit(fixture, 'ADMIN_AUDIT.SUBJECT', 'client-new');
        edit(fixture, 'ADMIN_AUDIT.ACTION', 'User.Impersonated');
        edit(fixture, 'ADMIN_AUDIT.TYPE', 'User');
        edit(fixture, 'ADMIN_AUDIT.TARGET', 'target-new');
        await settleAsync(fixture);
        expect(getPage).toHaveBeenCalledTimes(1);
        button(fixture, 'ADMIN_CATALOG.APPLY').click();
        await settleAsync(fixture);
        expect(getPage).toHaveBeenCalledTimes(2);
        expect(getPage).toHaveBeenLastCalledWith({
            page: 1,
            limit: PAGE_SIZE,
            actorUserId: 'admin-new',
            subjectClientUserId: 'client-new',
            action: 'User.Impersonated',
            targetType: 'User',
            targetId: 'target-new',
            fromUtc: '2026-08-01T00:00:00Z',
            toUtc: '2026-08-04T00:00:00.000Z',
        });
        expect(TestBed.inject(Router).parseUrl(TestBed.inject(Router).url).queryParams['period']).toBe('custom');
    });

    it.each([
        ['ADMIN_AUDIT.ACTION', ACTION_MAX_LENGTH],
        ['ADMIN_AUDIT.TYPE', TARGET_TYPE_MAX_LENGTH],
        ['ADMIN_AUDIT.TARGET', TARGET_ID_MAX_LENGTH],
    ] as const)('enforces the %s search boundary without issuing an invalid request', async (label, maximum) => {
        const fixture = await renderAsync();
        expect(input(fixture, label).maxLength).toBe(maximum);
        edit(fixture, label, 'x'.repeat(maximum + 1));
        await settleAsync(fixture);
        button(fixture, 'ADMIN_CATALOG.APPLY').click();
        await settleAsync(fixture);
        expect(getPage).toHaveBeenCalledTimes(1);
        edit(fixture, label, 'valid');
        button(fixture, 'ADMIN_CATALOG.APPLY').click();
        await settleAsync(fixture);
        expect(getPage).toHaveBeenCalledTimes(2);
    });

    it('does not request data for an invalid custom period', async () => {
        const fixture = await renderAsync('?period=custom&from=2026-08-03&to=2026-08-01');
        expect(getPage).not.toHaveBeenCalled();
        expect(element(fixture).querySelector('[role="alert"]')?.textContent).toContain('ADMIN_COMMON.INVALID_PERIOD');
        expect(element(fixture).querySelector('article')).toBeNull();
        expect(element(fixture).querySelector('[role="status"]')).toBeNull();
    });
});

describe('AdminAuditPageComponent request states', () => {
    it('renders loading and cancels stale requests after filters are applied', async () => {
        const first = new Subject<AdminAuditPageResult>();
        const second = new Subject<AdminAuditPageResult>();
        getPage.mockReturnValueOnce(first).mockReturnValueOnce(second);
        const fixture = await renderAsync();
        expect(element(fixture).querySelector('[role="status"]')?.textContent).toContain('ADMIN_COMMON.LOADING');
        expect(element(fixture).querySelector('article')).toBeNull();
        expect([...element(fixture).querySelectorAll('button')].some(control => control.textContent.trim() === 'ADMIN_COMMON.NEXT')).toBe(
            false,
        );
        edit(fixture, 'ADMIN_AUDIT.ACTOR', 'replacement-actor');
        await settleAsync(fixture);
        expect(getPage).toHaveBeenCalledTimes(1);
        button(fixture, 'ADMIN_CATALOG.APPLY').click();
        await settleAsync(fixture);
        expect(getPage).toHaveBeenCalledTimes(2);
        expect(first.observed).toBe(false);
        first.next({ items: [ENTRY], totalItems: 1 });
        expect(element(fixture).querySelector('[role="status"]')).not.toBeNull();
        second.next({ items: [], totalItems: 0 });
        await settleAsync(fixture);
        expect(element(fixture).textContent).toContain('ADMIN_AUDIT.EMPTY');
        expect(element(fixture).querySelector('article')).toBeNull();
        expect(button(fixture, 'ADMIN_COMMON.PREVIOUS').disabled).toBe(true);
        expect(button(fixture, 'ADMIN_COMMON.NEXT').disabled).toBe(true);
    });

    it('shows an error and retries the retained request without changing the URL', async () => {
        const pending = new Subject<AdminAuditPageResult>();
        getPage.mockReturnValueOnce(pending);
        const fixture = await renderAsync('?actorUserId=admin-2&subjectClientUserId=user-2&action=User.Banned');
        const originalUrl = TestBed.inject(Router).url;
        pending.error(new Error('unavailable'));
        await settleAsync(fixture);
        expect(element(fixture).querySelector('[role="alert"]')?.textContent).toContain('ADMIN_COMMON.LOAD_ERROR');
        expect(element(fixture).querySelector('article')).toBeNull();
        expect(input(fixture, 'ADMIN_AUDIT.ACTOR').value).toBe('admin-2');
        button(fixture, 'ADMIN_COMMON.RETRY').click();
        await settleAsync(fixture);
        expect(getPage).toHaveBeenCalledTimes(2);
        expect(getPage.mock.calls[1]).toEqual(getPage.mock.calls[0]);
        expect(TestBed.inject(Router).url).toBe(originalUrl);
        expect(element(fixture).querySelector('[role="alert"]')).toBeNull();
        expect(element(fixture).querySelector('h2')?.textContent).toBe(ENTRY.action);
    });
});

describe('AdminAuditPageComponent results and pagination', () => {
    it('renders actor and subject identity links, target, timestamp and expandable metadata', async () => {
        const fixture = await renderAsync();
        const article = required(element(fixture).querySelector('article'));
        expect(article.querySelector('h2')?.textContent).toBe(ENTRY.action);
        expect(article.textContent).toContain('ContentReport');
        expect(article.textContent).toContain('report-123');
        expect(article.textContent).toContain('2026');
        expect(article.querySelector('a[href="/users/admin-1"]')?.textContent).toBe(ENTRY.actorUserId);
        expect(article.querySelector('a[href="/users/client-1"]')?.textContent).toBe(ENTRY.subjectClientUserId);
        required(article.querySelector('summary')).click();
        expect(article.querySelector('details')?.open).toBe(true);
        expect(article.querySelector('pre')?.textContent).toBe(ENTRY.metadata);
        expect(element(fixture).querySelector('a[href="/users/impersonation-sessions"]')).not.toBeNull();
    });

    it('omits absent subject and metadata from audit entries', async () => {
        getPage.mockReturnValueOnce(
            of({ items: [{ ...ENTRY, subjectClientUserId: null, metadata: null, targetId: null }], totalItems: 1 }),
        );
        const fixture = await renderAsync();
        expect(element(fixture).querySelectorAll('article a')).toHaveLength(1);
        expect(element(fixture).querySelector('article details')).toBeNull();
    });

    it('preserves identity filters through pagination and prevents requests outside the rendered boundaries', async () => {
        const fixture = await renderAsync('?actorUserId=admin-1&subjectClientUserId=client-1');
        expect(button(fixture, 'ADMIN_COMMON.PREVIOUS').disabled).toBe(true);
        button(fixture, 'ADMIN_COMMON.PREVIOUS').click();
        expect(getPage).toHaveBeenCalledTimes(1);
        button(fixture, 'ADMIN_COMMON.NEXT').click();
        await settleAsync(fixture);
        expect(getPage).toHaveBeenCalledTimes(2);
        expect(getPage).toHaveBeenLastCalledWith(
            expect.objectContaining({ page: SECOND_PAGE, limit: PAGE_SIZE, actorUserId: 'admin-1', subjectClientUserId: 'client-1' }),
        );
        expect(button(fixture, 'ADMIN_COMMON.NEXT').disabled).toBe(true);
        button(fixture, 'ADMIN_COMMON.NEXT').click();
        expect(getPage).toHaveBeenCalledTimes(2);
        button(fixture, 'ADMIN_COMMON.PREVIOUS').click();
        await settleAsync(fixture);
        expect(getPage).toHaveBeenLastCalledWith(expect.objectContaining({ page: 1 }));
    });

    it('normalizes an obsolete URL page after the response total shrinks', async () => {
        const fixture = await renderAsync('?page=99&actorUserId=admin-1');
        expect(getPage).toHaveBeenCalledTimes(2);
        expect(getPage).toHaveBeenLastCalledWith(expect.objectContaining({ page: SECOND_PAGE, actorUserId: 'admin-1' }));
        expect(button(fixture, 'ADMIN_COMMON.NEXT').disabled).toBe(true);
    });
});

async function renderAsync(query = ''): Promise<AuditFixture> {
    await TestBed.inject(Router).navigateByUrl(`/${query}`);
    const fixture = TestBed.createComponent(AdminAuditPageComponent);
    await settleAsync(fixture);
    return fixture;
}

async function settleAsync(fixture: AuditFixture): Promise<void> {
    fixture.detectChanges();
    await waitForAsyncTasksAsync();
    await fixture.whenStable();
    fixture.detectChanges();
}

function element(fixture: AuditFixture): HTMLElement {
    return fixture.nativeElement as HTMLElement;
}

function required<T>(value: T | null | undefined): T {
    if (value === null || value === undefined) {
        throw new Error('Expected a rendered audit control');
    }
    return value;
}

function button(fixture: AuditFixture, text: string): HTMLButtonElement {
    return required([...element(fixture).querySelectorAll('button')].find(control => control.textContent.trim() === text));
}

function input(fixture: AuditFixture, label: string): HTMLInputElement {
    return required([...element(fixture).querySelectorAll('input')].find(control => control.getAttribute('aria-label') === label));
}

function edit(fixture: AuditFixture, label: string, value: string): void {
    const control = input(fixture, label);
    control.value = value;
    control.dispatchEvent(new Event('input', { bubbles: true }));
}
