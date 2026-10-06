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
