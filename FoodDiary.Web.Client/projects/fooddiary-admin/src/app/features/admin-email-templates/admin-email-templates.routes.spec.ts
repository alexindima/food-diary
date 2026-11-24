import { TestBed } from '@angular/core/testing';
import { provideRouter, Router, withComponentInputBinding } from '@angular/router';
import { RouterTestingHarness } from '@angular/router/testing';
import { FdUiDialogService } from 'fd-ui-kit/dialog/fd-ui-dialog.service';
import { type Observable, of } from 'rxjs';
import { expect, it, vi } from 'vitest';

import { provideTranslateTesting } from '../../../../../../src/testing/translate-testing.module';
import { adminId, adminUtcInstant } from '../../shared/models/semantics/admin-meaning';
import { AdminAuthService } from '../admin-auth/contracts/admin-session';
import { adminEmailTemplatesRoutes } from './admin-email-templates.routes';
import { AdminEmailTemplatesFacade } from './lib/admin-email-templates.facade';
import type { AdminEmailTemplate } from './models/admin-email-template.data';

const TEMPLATE_COUNT = 21;
const available = Array.from({ length: TEMPLATE_COUNT }, (_, index): AdminEmailTemplate => ({
    id: adminId<'email-template'>(`template-${index + 1}`),
    subject: `Notice ${index + 1}`,
    key: 'email_verification',
    locale: 'en',
    htmlBody: '<p>Notice</p>',
    textBody: 'Notice',
    isActive: true,
    createdOnUtc: adminUtcInstant('2026-01-01T00:00:00Z'),
    updatedOnUtc: null,
}));

function clickButton(root: HTMLElement, caption: string): void {
    const button = [...root.querySelectorAll('button')].find(candidate => candidate.textContent.trim() === caption);
    if (button === undefined) {
        throw new Error(`Expected email template button ${caption}.`);
    }
    button.click();
}

it('pages filtered templates through the lazy route and preserves the list after cancelling either dialog', async () => {
    const getAll = vi.fn<AdminEmailTemplatesFacade['getAll']>().mockReturnValue(of(available));
    const afterClosed = vi.fn<() => Observable<boolean>>().mockReturnValue(of(false));
    TestBed.configureTestingModule({
        providers: [
            provideRouter(
                [{ path: 'email-templates', children: adminEmailTemplatesRoutes }],
                withComponentInputBinding({ unmatchedInputBehavior: 'undefinedIfStale' }),
            ),
            provideTranslateTesting(),
            { provide: AdminEmailTemplatesFacade, useValue: { getAll } },
            { provide: FdUiDialogService, useValue: { open: (): { afterClosed: typeof afterClosed } => ({ afterClosed }) } },
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
    const harness = await RouterTestingHarness.create('/email-templates?search=Notice&page=9');
    const router = TestBed.inject(Router);
    await vi.waitFor(() => {
        expect(router.parseUrl(router.url).queryParams).toEqual({ search: 'Notice', page: '2' });
    });
    harness.detectChanges();
    const root = harness.routeNativeElement;
    if (root === null) {
        throw new Error('Expected the email templates route.');
    }
    expect(root.querySelector('tbody')?.textContent).toContain('Notice 21');
    expect(root.querySelectorAll('tbody tr')).toHaveLength(1);
    clickButton(root, 'Edit');
    clickButton(root, 'Create template');
    expect(afterClosed).toHaveBeenCalledTimes(2);
    expect(getAll).toHaveBeenCalledOnce();

    clickButton(root, 'PAGINATION.PREVIOUS');
    await vi.waitFor(() => {
        expect(router.parseUrl(router.url).queryParams).toEqual({ search: 'Notice', page: '1' });
    });
    harness.detectChanges();
    expect(root.querySelectorAll('tbody tr')).toHaveLength(TEMPLATE_COUNT - 1);

    await router.navigateByUrl('/email-templates?search=no-results');
    harness.detectChanges();
    expect(root.querySelector('tbody')?.textContent).toContain('No email templates found.');
    expect(root.querySelector('fd-ui-pagination nav')).toBeNull();
});
