import { HttpStatusCode, provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { TestBed } from '@angular/core/testing';
import { FD_UI_DIALOG_DATA } from 'fd-ui-kit/dialog/fd-ui-dialog-data';
import { FdUiDialogRef } from 'fd-ui-kit/dialog/fd-ui-dialog-ref';
import { Subject, throwError } from 'rxjs';
import { describe, expect, it, vi } from 'vitest';

import { provideTranslateTesting } from '../../../../../../../src/testing/translate-testing.module';
import { AdminModerationFacade } from '../lib/admin-moderation.facade';
import { AdminModerationActionDialogComponent } from './admin-moderation-action-dialog';

describe('AdminModerationActionDialogComponent failure recovery', () => {
    it.each(['review', 'dismiss'] as const)('announces a failed %s and retains the note before retry', async action => {
        const operation = vi.fn().mockReturnValueOnce(throwError(() => new Error('unavailable')));
        const dialogRef = { close: vi.fn() };
        await TestBed.configureTestingModule({
            imports: [AdminModerationActionDialogComponent],
            providers: [
                provideTranslateTesting(),
                {
                    provide: FD_UI_DIALOG_DATA,
                    useValue: {
                        action,
                        report: { id: 'owned-report', targetType: 'Recipe', targetId: 'owned-recipe', reason: 'QA report' },
                    },
                },
                { provide: FdUiDialogRef, useValue: dialogRef },
                { provide: AdminModerationFacade, useValue: { reviewReport: operation, dismissReport: operation } },
            ],
        }).compileComponents();
        const fixture = TestBed.createComponent(AdminModerationActionDialogComponent);
        fixture.componentInstance['adminNote'].set('  Retained QA note  ');
        fixture.componentInstance['onConfirm']();
        fixture.detectChanges();
        const host = fixture.nativeElement as HTMLElement;
        expect(host.querySelector('[role="alert"]')?.textContent).toContain('ADMIN_MODERATION.ACTION_ERROR');
        expect(host.querySelector('textarea')?.value).toBe('  Retained QA note  ');
        expect(dialogRef.close).not.toHaveBeenCalled();

        const pending = new Subject<void>();
        operation.mockReturnValueOnce(pending.asObservable());
        fixture.componentInstance['onConfirm']();
        fixture.detectChanges();
        expect(host.querySelector('[role="alert"]')).toBeNull();
        expect(host.querySelector<HTMLTextAreaElement>('textarea')?.disabled).toBe(true);
        expect(host.querySelector<HTMLButtonElement>('.fd-ui-dialog__close-button')?.disabled).toBe(true);
        expect([...host.querySelectorAll<HTMLButtonElement>('.dialog-actions button')].at(0)?.disabled).toBe(true);
        expect([...host.querySelectorAll<HTMLButtonElement>('.dialog-actions button')].at(-1)?.disabled).toBe(true);
        fixture.componentInstance['updateAdminNote']('Input during request');
        fixture.componentInstance['onCancel']();
        fixture.componentInstance['onConfirm']();
        expect(fixture.componentInstance['adminNote']()).toBe('  Retained QA note  ');
        expect(dialogRef.close).not.toHaveBeenCalled();
        expect(operation).toHaveBeenCalledTimes(2);
        expect(operation).toHaveBeenLastCalledWith('owned-report', { adminNote: 'Retained QA note' });
        pending.error(new Error('offline'));
        fixture.detectChanges();
        expect(host.querySelector<HTMLTextAreaElement>('textarea')?.disabled).toBe(false);
        expect(host.querySelector<HTMLButtonElement>('.fd-ui-dialog__close-button')?.disabled).toBe(false);
        expect([...host.querySelectorAll<HTMLButtonElement>('.dialog-actions button')].at(0)?.disabled).toBe(false);
        expect(fixture.componentInstance['adminNote']()).toBe('  Retained QA note  ');
        const retry = new Subject<void>();
        operation.mockReturnValueOnce(retry);
        fixture.componentInstance['onConfirm']();
        retry.next();
        retry.complete();
        expect(dialogRef.close).toHaveBeenCalledExactlyOnceWith({ confirmed: true });
    });
});

function renderModerationAction(action: 'review' | 'dismiss'): { host: HTMLElement; close: ReturnType<typeof vi.fn> } {
    const close = vi.fn();
    TestBed.configureTestingModule({
        imports: [AdminModerationActionDialogComponent],
        providers: [
            provideTranslateTesting(),
            provideHttpClient(),
            provideHttpClientTesting(),
            {
                provide: FD_UI_DIALOG_DATA,
                useValue: { action, report: { id: 'owned-report', targetType: 'Recipe', targetId: 'owned-recipe', reason: 'Test report' } },
            },
            { provide: FdUiDialogRef, useValue: { close } },
        ],
    });
    const fixture = TestBed.createComponent(AdminModerationActionDialogComponent);
    fixture.detectChanges();
    return { host: fixture.nativeElement as HTMLElement, close };
}

describe('Admin moderation confirmation controls', () => {
    it('submits a trimmed note entered in the rendered textarea', () => {
        const { host, close } = renderModerationAction('review');
        const note = host.querySelector('textarea');
        if (note === null) {
            throw new Error('Expected the moderator note field.');
        }
        note.value = '  Checked against the recipe  ';
        note.dispatchEvent(new Event('input', { bubbles: true }));
        const confirm = [...host.querySelectorAll<HTMLButtonElement>('.dialog-actions button')].at(-1);
        if (confirm === undefined) {
            throw new Error('Expected the review confirmation.');
        }
        confirm.click();
        const http = TestBed.inject(HttpTestingController);
        const request = http.expectOne(current => current.url.endsWith('/admin/moderation/owned-report/review'));
        expect(request.request.body).toEqual({ adminNote: 'Checked against the recipe' });
        request.flush(null, { status: HttpStatusCode.NoContent, statusText: 'No Content' });
        expect(close).toHaveBeenCalledExactlyOnceWith({ confirmed: true });
        http.verify();
    });

    it.each(['review', 'dismiss'] as const)('performs %s through the real endpoint with no optional note', action => {
        const { host, close } = renderModerationAction(action);
        const confirm = [...host.querySelectorAll<HTMLButtonElement>('.dialog-actions button')].at(-1);
        expect(confirm).toBeTruthy();
        confirm?.click();

        const http = TestBed.inject(HttpTestingController);
        const request = http.expectOne(current => current.url.endsWith(`/admin/moderation/owned-report/${action}`));
        expect(request.request.method).toBe('POST');
        expect(request.request.body).toEqual({ adminNote: null });
        expect(close).not.toHaveBeenCalled();
        request.flush(null, { status: HttpStatusCode.NoContent, statusText: 'No Content' });

        expect(close).toHaveBeenCalledExactlyOnceWith({ confirmed: true });
        http.verify();
    });

    it('cancels through the native button without sending an action', () => {
        const { host, close } = renderModerationAction('review');
        const cancel = host.querySelector<HTMLButtonElement>('.dialog-actions button');
        expect(cancel?.textContent).toContain('Cancel');
        cancel?.click();

        expect(close).toHaveBeenCalledExactlyOnceWith({ confirmed: false });
        TestBed.inject(HttpTestingController).verify();
    });
});
