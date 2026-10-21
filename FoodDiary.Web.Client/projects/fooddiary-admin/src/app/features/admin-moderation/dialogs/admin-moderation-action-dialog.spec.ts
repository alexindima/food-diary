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
        expect([...host.querySelectorAll<HTMLButtonElement>('.dialog-actions button')].at(-1)?.disabled).toBe(true);
        expect(operation).toHaveBeenLastCalledWith('owned-report', { adminNote: 'Retained QA note' });
        pending.next();
        pending.complete();
        expect(dialogRef.close).toHaveBeenCalledExactlyOnceWith({ confirmed: true });
    });
});
