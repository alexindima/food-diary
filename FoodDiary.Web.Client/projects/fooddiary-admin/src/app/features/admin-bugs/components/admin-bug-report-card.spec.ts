import { TestBed } from '@angular/core/testing';
import { provideRouter } from '@angular/router';
import { expect, it } from 'vitest';

import { provideTranslateTesting } from '../../../../../../../src/testing/translate-testing.module';
import { AdminBugReportCardComponent } from './admin-bug-report-card';

it('shows a descriptive heading as plain text with collapsed investigation details', () => {
    TestBed.configureTestingModule({ imports: [AdminBugReportCardComponent], providers: [provideRouter([]), provideTranslateTesting()] });
    const fixture = TestBed.createComponent(AdminBugReportCardComponent);
    fixture.componentRef.setInput('report', {
        id: 'qa-bug',
        sourceMessageId: 'qa-mail',
        receivedAtUtc: '2026-10-06T00:00:00Z',
        subject: 'QA report',
        title: 'The Dynamics card cannot be hidden',
        shortSummary: 'The fix has been prepared.',
        status: 'failed',
        attempt: 1,
        summary: 'QA',
        mergeRequestUrl: null,
        contentExpired: false,
    });
    fixture.componentRef.setInput('pullRequestUrl', null);
    fixture.detectChanges();
    const element = fixture.nativeElement as HTMLElement;
    expect(element.querySelector('h2')?.textContent).toBe('The Dynamics card cannot be hidden');
    expect(element.querySelector('h2 a')).toBeNull();
    expect(element.querySelector('.report-card__preview')?.textContent).toBe('The fix has been prepared.');
    expect(element.querySelector('details')?.open).toBe(false);
    expect(element.querySelector('a[href*="mail-inbox"]')?.textContent).toContain('ADMIN_BUGS.OPEN_EMAIL');
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
    expect(element.textContent).toContain('ADMIN_BUGS.STATUS_DRAFT_READY');
    expect(element.querySelector('a[href*="mail-inbox"]')).toBeNull();
    expect(element.querySelector('a[target="_blank"]')?.getAttribute('href')).toBe('https://github.com/example/repo/pull/1');
});

it('does not promote an opaque ID to a title or expose expired display content', () => {
    TestBed.configureTestingModule({ imports: [AdminBugReportCardComponent], providers: [provideRouter([]), provideTranslateTesting()] });
    const fixture = TestBed.createComponent(AdminBugReportCardComponent);
    fixture.componentRef.setInput('report', {
        id: 'old-id',
        source: 'email',
        subject: '3b59c26a-9202-427a-84be-c0c867e16851',
        title: '3b59c26a-9202-427a-84be-c0c867e16851',
        shortSummary: null,
        summary: 'Legacy investigation',
        status: 'expired',
        attempt: 1,
        sourceMessageId: null,
        receivedAtUtc: '2026-10-12T05:00:00Z',
        contentExpired: true,
    });
    fixture.detectChanges();
    const element = fixture.nativeElement as HTMLElement;
    expect(element.querySelector('h2')?.textContent).toContain('ADMIN_BUGS.UNTITLED');
    expect(element.textContent).not.toContain('Legacy investigation');
});
