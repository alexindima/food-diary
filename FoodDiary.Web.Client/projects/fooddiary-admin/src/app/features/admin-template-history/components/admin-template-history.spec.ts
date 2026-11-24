import { type ComponentFixture, TestBed } from '@angular/core/testing';
import { of, Subject, throwError } from 'rxjs';
import { beforeEach, describe, expect, it, vi } from 'vitest';

import { provideTranslateTesting } from '../../../../../../../src/testing/translate-testing.module';
import { adminId, adminUtcInstant } from '../../../shared/models/semantics/admin-meaning';
import { AdminTemplateHistoryFacade } from '../lib/admin-template-history.facade';
import type { AdminTemplateRevision } from '../models/admin-template-revision';
import { AdminTemplateHistoryComponent } from './admin-template-history';

const revision: AdminTemplateRevision = {
    id: adminId<'template-revision'>('old'),
    subject: 'Previous',
    htmlBody: '<script>unsafe()</script>',
    textBody: 'Previous text',
    isActive: true,
    version: null,
    savedOnUtc: adminUtcInstant('2026-01-01T00:00:00Z'),
    archivedOnUtc: adminUtcInstant('2026-01-02T00:00:00Z'),
};

describe('AdminTemplateHistoryComponent', () => {
    const getRevisions = vi.fn();

    beforeEach(async () => {
        getRevisions.mockReset().mockReturnValue(of([revision]));
        await TestBed.configureTestingModule({
            imports: [AdminTemplateHistoryComponent],
            providers: [...provideTranslateTesting(), { provide: AdminTemplateHistoryFacade, useValue: { getRevisions } }],
        }).compileComponents();
    });

    function create(): ComponentFixture<AdminTemplateHistoryComponent> {
        const fixture = TestBed.createComponent(AdminTemplateHistoryComponent);
        fixture.componentRef.setInput('kind', 'email-templates');
        fixture.componentRef.setInput('templateKey', 'account_created');
        fixture.componentRef.setInput('locale', 'en');
        fixture.detectChanges();
        return fixture;
    }

    it('discards a previous locale request and renders HTML history as text', async () => {
        const pending = new Subject<AdminTemplateRevision[]>();
        getRevisions.mockReturnValueOnce(pending);
        const fixture = create();
        expect(pending.observed).toBe(true);
        fixture.componentRef.setInput('locale', 'ru');
        fixture.detectChanges();
        expect(pending.observed).toBe(false);
        pending.next([{ ...revision, id: adminId<'template-revision'>('stale') }]);
        await fixture.whenStable();
        expect(fixture.componentInstance['revisions']()).toEqual([revision]);
        expect((fixture.nativeElement as HTMLElement).querySelector('script')).toBeNull();
        expect((fixture.nativeElement as HTMLElement).textContent).toContain('<script>unsafe()</script>');
    });

    it('retries a failed history request and emits a restore without saving', async () => {
        getRevisions.mockReturnValueOnce(throwError(() => new Error('offline')));
        const fixture = create();
        await fixture.whenStable();
        expect(fixture.componentInstance['failed']()).toBe(true);
        fixture.componentInstance['retry']();
        fixture.detectChanges();
        await fixture.whenStable();
        expect(fixture.componentInstance['failed']()).toBe(false);
        const restored = vi.fn();
        fixture.componentInstance.restored.subscribe(restored);
        (fixture.nativeElement as HTMLElement).querySelector<HTMLButtonElement>('fd-ui-button button')?.click();
        expect(restored).toHaveBeenCalledWith(revision);
        expect(getRevisions).toHaveBeenCalledTimes(2);
    });

    it('reloads the same template when the refresh token changes and hides older revisions while loading', async () => {
        const fixture = create();
        await fixture.whenStable();
        const host = fixture.nativeElement as HTMLElement;
        expect(host.querySelector('details')).not.toBeNull();

        const pending = new Subject<AdminTemplateRevision[]>();
        getRevisions.mockReturnValueOnce(pending);
        fixture.componentRef.setInput('refreshToken', 1);
        fixture.detectChanges();
        expect(getRevisions).toHaveBeenCalledTimes(2);
        expect(getRevisions).toHaveBeenLastCalledWith('email-templates', 'account_created', 'en');
        expect(host.querySelector('[role="status"]')?.textContent).toContain('ADMIN_COMMON.LOADING');
        expect(host.querySelector('details')).toBeNull();

        pending.next([{ ...revision, subject: 'Newly archived' }]);
        pending.complete();
        await fixture.whenStable();
        expect(host.textContent).toContain('Newly archived');
    });

    it.each(['templateKey', 'locale'])('clears history without a request when %s is empty', async inputName => {
        const fixture = create();
        await fixture.whenStable();
        const host = fixture.nativeElement as HTMLElement;
        expect(host.querySelector('details')).not.toBeNull();

        fixture.componentRef.setInput(inputName, '');
        fixture.detectChanges();
        await fixture.whenStable();
        expect(getRevisions).toHaveBeenCalledOnce();
        expect(host.querySelector('details')).toBeNull();
        expect(host.textContent).toContain('ADMIN_TEMPLATE_HISTORY.EMPTY');
    });
});
