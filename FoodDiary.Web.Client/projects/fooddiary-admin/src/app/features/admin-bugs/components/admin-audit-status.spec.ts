import { TestBed } from '@angular/core/testing';
import { beforeEach, describe, expect, it } from 'vitest';

import { provideTranslateTesting } from '../../../../../../../src/testing/translate-testing.module';
import { adminId, adminUtcInstant } from '../../../shared/models/semantics/admin-meaning';
import type { AdminAuditRunSummary } from '../models/admin-bug-report';
import { AdminAuditStatusComponent } from './admin-audit-status';

const audit: AdminAuditRunSummary = {
    id: adminId<'audit-run'>('audit-id'),
    fromUtc: adminUtcInstant('2026-10-11T05:00:00Z'),
    toUtc: adminUtcInstant('2026-10-12T05:00:00Z'),
    startedAtUtc: adminUtcInstant('2026-10-12T05:00:00Z'),
    completedAtUtc: adminUtcInstant('2026-10-12T05:10:00Z'),
    status: 'clean',
    summary: 'No significant findings',
    findingCount: 0,
    contentExpired: false,
    isStale: false,
    lastSuccessfulAtUtc: adminUtcInstant('2026-10-12T05:10:00Z'),
};

describe('Production audit status', () => {
    beforeEach(() => {
        TestBed.configureTestingModule({ imports: [AdminAuditStatusComponent], providers: [provideTranslateTesting()] });
    });

    it('shows a clean complete audit, its interval and the latest successful completion', () => {
        const fixture = TestBed.createComponent(AdminAuditStatusComponent);
        fixture.componentRef.setInput('audit', audit);
        fixture.detectChanges();
        expect((fixture.nativeElement as HTMLElement).textContent).toContain('ADMIN_BUGS.AUDIT_CLEAN');
        expect((fixture.nativeElement as HTMLElement).textContent).toContain('ADMIN_BUGS.AUDIT_LAST_SUCCESS');
        expect(fixture.componentInstance['accent']()).toBe('success');
    });

    it.each(['incomplete', 'failed', 'running'])('does not label a %s audit as clean', status => {
        const fixture = TestBed.createComponent(AdminAuditStatusComponent);
        fixture.componentRef.setInput('audit', { ...audit, status });
        fixture.detectChanges();
        expect((fixture.nativeElement as HTMLElement).textContent).not.toContain('ADMIN_BUGS.AUDIT_CLEAN');
        expect(fixture.componentInstance['accent']()).toBe('default');
    });

    it('makes an overdue clean result visibly stale and hides expired summaries', () => {
        const fixture = TestBed.createComponent(AdminAuditStatusComponent);
        fixture.componentRef.setInput('audit', { ...audit, isStale: true, contentExpired: true, summary: 'Expired content' });
        fixture.detectChanges();
        expect((fixture.nativeElement as HTMLElement).textContent).toContain('ADMIN_BUGS.AUDIT_STALE');
        expect((fixture.nativeElement as HTMLElement).textContent).not.toContain('Expired content');
        expect(fixture.componentInstance['accent']()).toBe('default');
    });

    it('distinguishes no audit history from a clean audit', () => {
        const fixture = TestBed.createComponent(AdminAuditStatusComponent);
        fixture.componentRef.setInput('audit', null);
        fixture.detectChanges();
        expect((fixture.nativeElement as HTMLElement).textContent).toContain('ADMIN_BUGS.AUDIT_NONE');
        expect((fixture.nativeElement as HTMLElement).textContent).not.toContain('ADMIN_BUGS.AUDIT_CLEAN');
    });
});
