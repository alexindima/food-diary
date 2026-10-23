import type { ComponentFixture } from '@angular/core/testing';
import { TestBed } from '@angular/core/testing';
import { provideRouter, Router } from '@angular/router';
import { FdUiDialogService } from 'fd-ui-kit/dialog/fd-ui-dialog.service';
import { type Observable, of, Subject } from 'rxjs';
import { afterEach, beforeEach, describe, expect, it, vi } from 'vitest';

import { waitForAsyncTasksAsync } from '../../../../../../../src/testing/async-testing';
import { provideTranslateTesting } from '../../../../../../../src/testing/translate-testing.module';
import { adminId, adminUtcInstant } from '../../../shared/models/semantics/admin-meaning';
import { AdminModerationService } from '../api/admin-moderation.service';
import {
    AdminModerationActionDialogComponent,
    type AdminModerationActionDialogData,
    type AdminModerationActionDialogResult,
} from '../dialogs/admin-moderation-action-dialog';
import type { AdminContentReport } from '../models/admin-moderation.data';
import type { PagedResponse } from '../models/admin-moderation-page.models';
import { AdminModerationComponent } from './admin-moderation';

const PAGE_SIZE = 20;
const SECOND_PAGE = 2;
const REPORT: AdminContentReport = {
    id: adminId<'content-report'>('report-1'),
    reporterId: adminId<'user'>('reporter-1'),
    targetType: 'Recipe',
    targetId: 'recipe-12345678',
    reason: 'Incorrect ingredient information',
    status: 'Pending',
    createdAtUtc: adminUtcInstant('2026-08-01T12:30:00Z'),
    targetTitle: 'Oat pancakes',
    targetExcerpt: 'Recipe ingredients and description',
};
type ReportPage = PagedResponse<AdminContentReport>;
type ModerationFixture = ComponentFixture<AdminModerationComponent>;

const getReports = vi.fn<AdminModerationService['getReports']>();
let closed: Subject<AdminModerationActionDialogResult | undefined>;
const open = vi.fn(
    (
        _component: unknown,
        _options: { size: string; data: AdminModerationActionDialogData },
    ): { afterClosed: () => Observable<AdminModerationActionDialogResult | undefined> } => ({ afterClosed: () => closed.asObservable() }),
);
const scrollIntoViewDescriptor = Object.getOwnPropertyDescriptor(Element.prototype, 'scrollIntoView');

beforeEach(async () => {
    getReports.mockReset().mockReturnValue(of(reportPage([REPORT])));
    closed = new Subject<AdminModerationActionDialogResult | undefined>();
    open.mockClear();
    // The real select scrolls its active option when its overlay attaches.
    if (scrollIntoViewDescriptor === undefined) {
        Object.defineProperty(Element.prototype, 'scrollIntoView', { configurable: true, value: vi.fn() });
    }
    await TestBed.configureTestingModule({
        imports: [AdminModerationComponent],
        providers: [
            provideRouter([]),
            ...provideTranslateTesting(),
            { provide: AdminModerationService, useValue: { getReports } },
            { provide: FdUiDialogService, useValue: { open } },
        ],
    }).compileComponents();
});

afterEach(() => {
    if (scrollIntoViewDescriptor === undefined) {
        Reflect.deleteProperty(Element.prototype, 'scrollIntoView');
    } else {
        Object.defineProperty(Element.prototype, 'scrollIntoView', scrollIntoViewDescriptor);
    }
});

describe('AdminModerationComponent filters', () => {
    it('restores URL filters and applies draft edits once, resetting the page and retaining the period', async () => {
        const fixture = await renderAsync(
            '?page=2&status=Reviewed&targetType=Recipe&reporterId=old&targetId=original&period=custom&from=2026-08-01&to=2026-08-03',
        );
        expect(getReports).toHaveBeenCalledWith(SECOND_PAGE, PAGE_SIZE, 'Reviewed', {
            targetType: 'Recipe',
            reporterId: 'old',
            targetId: 'original',
            fromUtc: '2026-08-01T00:00:00Z',
            toUtc: '2026-08-04T00:00:00.000Z',
        });
        expect(input(fixture, 'ADMIN_MODERATION.REPORTER_ID').value).toBe('old');
        await selectAsync(fixture, 'ADMIN_MODERATION.STATUS', 'ADMIN_MODERATION.DISMISSED');
        await selectAsync(fixture, 'ADMIN_MODERATION.TYPE', 'ADMIN_MODERATION.COMMENT');
        edit(fixture, 'ADMIN_MODERATION.REPORTER_ID', 'new-reporter');
        edit(fixture, 'ADMIN_MODERATION.TARGET_ID', 'new-comment');
        await settleAsync(fixture);
        expect(getReports).toHaveBeenCalledTimes(1);
        button(fixture, 'ADMIN_COMMON.APPLY').click();
        await settleAsync(fixture);
        expect(getReports).toHaveBeenCalledTimes(2);
        expect(getReports).toHaveBeenLastCalledWith(
            1,
            PAGE_SIZE,
            'Dismissed',
            expect.objectContaining({
                targetType: 'Comment',
                reporterId: 'new-reporter',
                targetId: 'new-comment',
                fromUtc: '2026-08-01T00:00:00Z',
                toUtc: '2026-08-04T00:00:00.000Z',
            }),
        );
        expect(TestBed.inject(Router).parseUrl(TestBed.inject(Router).url).queryParams['page']).toBe('1');
    });

    it('maps all statuses to null and clears empty identity filters', async () => {
        const fixture = await renderAsync('?reporterId=old&targetId=old');
        await selectAsync(fixture, 'ADMIN_MODERATION.STATUS', 'ADMIN_COMMON.ALL');
        edit(fixture, 'ADMIN_MODERATION.REPORTER_ID', '');
        edit(fixture, 'ADMIN_MODERATION.TARGET_ID', '');
        button(fixture, 'ADMIN_COMMON.APPLY').click();
        await settleAsync(fixture);
        expect(getReports).toHaveBeenLastCalledWith(1, PAGE_SIZE, null, { targetType: '', reporterId: '', targetId: '' });
        expect(TestBed.inject(Router).parseUrl(TestBed.inject(Router).url).queryParams).not.toHaveProperty('reporterId');
    });

    it('rejects an invalid URL period before requesting reports', async () => {
        const fixture = await renderAsync('?period=custom&from=2026-08-04&to=2026-08-01');
        expect(getReports).not.toHaveBeenCalled();
        expect(element(fixture).querySelector('[role="alert"]')?.textContent).toContain('ADMIN_COMMON.INVALID_PERIOD');
        expect(element(fixture).textContent).toContain('ADMIN_COMMON.EMPTY');
    });
});

describe('AdminModerationComponent loading and pagination', () => {
    it('hides pagination while pending and cancels stale requests when applied filters change', async () => {
        const first = new Subject<ReportPage>();
        const second = new Subject<ReportPage>();
        getReports.mockReturnValueOnce(first).mockReturnValueOnce(second);
        const fixture = await renderAsync();
        expect(element(fixture).textContent).toContain('ADMIN_COMMON.LOADING');
        expect(element(fixture).querySelector('tbody')).toBeNull();
        expect(element(fixture).querySelector('fd-ui-pagination')).toBeNull();
        edit(fixture, 'ADMIN_MODERATION.TARGET_ID', 'new-target');
        await settleAsync(fixture);
        expect(getReports).toHaveBeenCalledTimes(1);
        button(fixture, 'ADMIN_COMMON.APPLY').click();
        await settleAsync(fixture);
        expect(getReports).toHaveBeenCalledTimes(2);
        expect(first.observed).toBe(false);
        first.next(reportPage([REPORT]));
        expect(element(fixture).textContent).toContain('ADMIN_COMMON.LOADING');
        second.next(reportPage([]));
        await settleAsync(fixture);
        expect(element(fixture).textContent).toContain('ADMIN_COMMON.EMPTY');
        expect(element(fixture).textContent).not.toContain(REPORT.reason);
    });

    it('moves through rendered pagination without losing filters and disables its boundaries', async () => {
        const fixture = await renderAsync('?status=Dismissed&targetId=target');
        expect(button(fixture, 'PAGINATION.PREVIOUS').disabled).toBe(true);
        button(fixture, 'PAGINATION.PREVIOUS').click();
        expect(getReports).toHaveBeenCalledTimes(1);
        button(fixture, 'PAGINATION.NEXT').click();
        await settleAsync(fixture);
        expect(getReports).toHaveBeenLastCalledWith(SECOND_PAGE, PAGE_SIZE, 'Dismissed', {
            targetType: '',
            reporterId: '',
            targetId: 'target',
        });
        expect(element(fixture).querySelector('[aria-current="page"]')?.textContent.trim()).toBe('2');
        expect(button(fixture, 'PAGINATION.NEXT').disabled).toBe(true);
        button(fixture, 'PAGINATION.NEXT').click();
        expect(getReports).toHaveBeenCalledTimes(2);
    });

    it('restores an obsolete URL page to the final available page', async () => {
        const fixture = await renderAsync('?page=99&status=Reviewed');
        expect(getReports).toHaveBeenCalledTimes(2);
        expect(getReports).toHaveBeenLastCalledWith(SECOND_PAGE, PAGE_SIZE, 'Reviewed', expect.anything());
        expect(element(fixture).querySelector('[aria-current="page"]')?.textContent.trim()).toBe('2');
    });

    it('shows a load error and retries the same filters before rendering an empty response', async () => {
        const pending = new Subject<ReportPage>();
        getReports.mockReturnValueOnce(pending).mockReturnValueOnce(of(reportPage([])));
        const fixture = await renderAsync('?status=Reviewed&reporterId=reporter-2');
        pending.error(new Error('unavailable'));
        await settleAsync(fixture);
        expect(element(fixture).querySelector('[role="alert"]')?.textContent).toContain('ADMIN_COMMON.LOAD_ERROR');
        expect(element(fixture).querySelector('tbody')).toBeNull();
        button(fixture, 'ADMIN_COMMON.RETRY').click();
        await settleAsync(fixture);
        expect(getReports).toHaveBeenCalledTimes(2);
        expect(getReports).toHaveBeenLastCalledWith(1, PAGE_SIZE, 'Reviewed', { targetType: '', reporterId: 'reporter-2', targetId: '' });
        expect(element(fixture).querySelector('[role="alert"]')).toBeNull();
        expect(element(fixture).textContent).toContain('ADMIN_COMMON.EMPTY');
    });
});

describe('AdminModerationComponent report actions', () => {
    it('keeps the current reports when the dialog closes without a result', async () => {
        const fixture = await renderAsync();
        button(fixture, 'ADMIN_MODERATION.REVIEW').click();
        closed.next(undefined);
        closed.complete();
        await settleAsync(fixture);
        expect(getReports).toHaveBeenCalledTimes(1);
        expect(element(fixture).querySelector('.reason-cell')?.textContent).toBe(REPORT.reason);
    });

    it('opens report context with its identity links and target history', async () => {
        const fixture = await renderAsync();
        const summary = required(element(fixture).querySelector<HTMLElement>('tbody summary'));
        expect(summary.textContent).toBe(REPORT.targetTitle);
        summary.click();
        const context = required(element(fixture).querySelector<HTMLDetailsElement>('tbody details'));
        expect(context.open).toBe(true);
        expect(context.textContent).toContain(REPORT.targetExcerpt);
        expect(context.querySelector('a')?.getAttribute('href')).toBe('/users/reporter-1');
        expect(context.querySelector('a[href^="/moderation"]')?.getAttribute('href')).toBe(
            '/moderation?targetId=recipe-12345678&targetType=Recipe&status=',
        );
        expect(element(fixture).querySelector('.reason-cell')?.textContent).toBe(REPORT.reason);
    });

    it.each(['review', 'dismiss'] as const)(
        'passes %s and the selected report to the dialog, refreshing only after confirmation',
        async action => {
            const fixture = await renderAsync();
            const label = action === 'review' ? 'ADMIN_MODERATION.REVIEW' : 'ADMIN_MODERATION.DISMISS';
            button(fixture, label).click();
            const [component, options] = required(open.mock.calls[0]);
            expect(component).toBe(AdminModerationActionDialogComponent);
            expect(options.size).toBe('sm');
            expect(options.data.action).toBe(action);
            expect(options.data.report).toMatchObject(REPORT);
            expect(getReports).toHaveBeenCalledTimes(1);
            closed.next({ confirmed: false });
            closed.complete();
            await settleAsync(fixture);
            expect(getReports).toHaveBeenCalledTimes(1);
            closed = new Subject<AdminModerationActionDialogResult | undefined>();
            button(fixture, label).click();
            expect(open).toHaveBeenCalledTimes(2);
            getReports.mockReturnValueOnce(of(reportPage([{ ...REPORT, status: action === 'review' ? 'Reviewed' : 'Dismissed' }])));
            closed.next({ confirmed: true });
            await settleAsync(fixture);
            expect(getReports).toHaveBeenCalledTimes(2);
            expect(element(fixture).querySelector('.action-buttons')).toBeNull();
            expect(element(fixture).querySelector('.status-badge')?.textContent.trim()).toBe(
                action === 'review' ? 'Reviewed' : 'Dismissed',
            );
        },
    );

    it('shows reviewer details and unavailable context without exposing actions for completed reports', async () => {
        getReports.mockReturnValueOnce(
            of(
                reportPage([
                    {
                        ...REPORT,
                        status: 'Reviewed',
                        targetTitle: null,
                        targetExcerpt: null,
                        reviewedByUserId: adminId<'user'>('reviewer-1'),
                        reviewedAtUtc: adminUtcInstant('2026-08-02T10:00:00Z'),
                        adminNote: 'Verified manually',
                    },
                ]),
            ),
        );
        const fixture = await renderAsync();
        expect(element(fixture).textContent).toContain('ADMIN_MODERATION.CONTEXT_UNAVAILABLE');
        expect(element(fixture).textContent).toContain('Verified manually');
        expect(element(fixture).querySelector('a[href="/users/reviewer-1"]')).not.toBeNull();
        expect(element(fixture).querySelector('.action-buttons')).toBeNull();
    });
});

function reportPage(items: AdminContentReport[]): ReportPage {
    return { items, totalPages: items.length > 0 ? SECOND_PAGE : 1, totalItems: items.length > 0 ? PAGE_SIZE + 1 : 0 };
}

async function renderAsync(query = ''): Promise<ModerationFixture> {
    await TestBed.inject(Router).navigateByUrl(`/${query}`);
    const fixture = TestBed.createComponent(AdminModerationComponent);
    await settleAsync(fixture);
    return fixture;
}

async function settleAsync(fixture: ModerationFixture): Promise<void> {
    fixture.detectChanges();
    await waitForAsyncTasksAsync();
    await fixture.whenStable();
    fixture.detectChanges();
}

function element(fixture: ModerationFixture): HTMLElement {
    return fixture.nativeElement as HTMLElement;
}

function required<T>(value: T | null | undefined): T {
    if (value === null || value === undefined) {
        throw new Error('Expected a rendered moderation control');
    }
    return value;
}

function button(fixture: ModerationFixture, text: string): HTMLButtonElement {
    return required([...element(fixture).querySelectorAll('button')].find(control => control.textContent.trim() === text));
}

function input(fixture: ModerationFixture, label: string): HTMLInputElement {
    return required([...element(fixture).querySelectorAll('input')].find(control => control.getAttribute('aria-label') === label));
}

function edit(fixture: ModerationFixture, label: string, value: string): void {
    const control = input(fixture, label);
    control.value = value;
    control.dispatchEvent(new Event('input', { bubbles: true }));
}

async function selectAsync(fixture: ModerationFixture, label: string, option: string): Promise<void> {
    const selectLabel = required([...element(fixture).querySelectorAll('label')].find(control => control.textContent.trim() === label));
    required(element(fixture).querySelector<HTMLButtonElement>(`button[id="${selectLabel.htmlFor}"]`)).click();
    await settleAsync(fixture);
    required(
        [...document.querySelectorAll<HTMLButtonElement>('[role="option"]')].find(control => control.textContent.trim() === option),
    ).click();
    await settleAsync(fixture);
}
