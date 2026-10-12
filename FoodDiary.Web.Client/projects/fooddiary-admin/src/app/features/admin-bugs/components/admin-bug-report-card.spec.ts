import { TestBed } from '@angular/core/testing';
import { provideRouter, Router } from '@angular/router';
import { expect, it } from 'vitest';

import { provideTranslateTesting } from '../../../../../../../src/testing/translate-testing.module';
import { AdminBugReportCardComponent } from './admin-bug-report-card';

it('preserves the complete list query when opening a bug report', async () => {
    TestBed.configureTestingModule({ imports: [AdminBugReportCardComponent], providers: [provideRouter([]), provideTranslateTesting()] });
    const router = TestBed.inject(Router);
    await router.navigateByUrl('/?search=QA&status=failed&period=7d&page=2&extra=kept');
    const fixture = TestBed.createComponent(AdminBugReportCardComponent);
    fixture.componentRef.setInput('report', {
        id: 'qa-bug',
        sourceMessageId: 'qa-mail',
        receivedAtUtc: '2026-10-06T00:00:00Z',
        subject: 'QA report',
        status: 'failed',
        attempt: 1,
        summary: 'QA',
        mergeRequestUrl: null,
        contentExpired: false,
    });
    fixture.componentRef.setInput('pullRequestUrl', null);
    fixture.detectChanges();
    const href = (fixture.nativeElement as HTMLElement).querySelector('h2 a')?.getAttribute('href');
    expect(href).toBeTruthy();
    const url = new URL(href ?? '', 'http://localhost');
    expect(url.pathname).toBe('/bugs/qa-bug');
    expect(Object.fromEntries(url.searchParams)).toEqual({ search: 'QA', status: 'failed', period: '7d', page: '2', extra: 'kept' });
});

it('renders a log finding with its draft link and period without a link to a fictitious email', () => {
    TestBed.configureTestingModule({ imports: [AdminBugReportCardComponent], providers: [provideRouter([]), provideTranslateTesting()] });
    const fixture = TestBed.createComponent(AdminBugReportCardComponent);
    fixture.componentRef.setInput('report', {
        id: 'audit-finding',
        source: 'logs',
        sourceMessageId: null,
        auditRunId: 'audit-id',
        auditFromUtc: '2026-10-11T05:00:00Z',
        auditToUtc: '2026-10-12T05:00:00Z',
        receivedAtUtc: '2026-10-12T05:00:00Z',
        subject: 'Navigation failure',
        status: 'draft_ready',
        attempt: 1,
        summary: 'Regression verified',
        mergeRequestUrl: 'https://github.com/example/repo/pull/1',
        contentExpired: false,
    });
    fixture.componentRef.setInput('pullRequestUrl', 'https://github.com/example/repo/pull/1');
    fixture.detectChanges();
    const element = fixture.nativeElement as HTMLElement;
    expect(element.textContent).toContain('ADMIN_BUGS.SOURCE_LOGS');
    expect(element.textContent).toContain('ADMIN_BUGS.AUDIT_WINDOW');
    expect(element.querySelector('a[href*="mail-inbox"]')).toBeNull();
    expect(element.querySelector('a[target="_blank"]')?.getAttribute('href')).toBe('https://github.com/example/repo/pull/1');
});
