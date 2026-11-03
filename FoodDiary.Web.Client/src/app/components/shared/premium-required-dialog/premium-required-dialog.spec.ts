import { type ComponentFixture, TestBed } from '@angular/core/testing';
import { FD_UI_DIALOG_DATA } from 'fd-ui-kit/dialog/fd-ui-dialog-data';
import { FdUiDialogRef } from 'fd-ui-kit/dialog/fd-ui-dialog-ref';
import { FdUiToastService } from 'fd-ui-kit/toast/fd-ui-toast.service';
import { type Observable, of, throwError } from 'rxjs';
import { describe, expect, it, vi } from 'vitest';

import { provideTranslateTesting } from '../../../../testing/translate-testing.module';
import { AuthService } from '../../../services/auth.service';
import { PremiumRequiredDialogComponent, type PremiumRequiredDialogData } from './premium-required-dialog';
import { PREMIUM_TRIAL_ACTIONS } from './premium-trial-actions';

describe('PremiumRequiredDialogComponent', () => {
    let component: PremiumRequiredDialogComponent;
    let fixture: ComponentFixture<PremiumRequiredDialogComponent>;
    let dialogRefSpy: { close: ReturnType<typeof vi.fn> };

    function createComponent(
        data: PremiumRequiredDialogData | null = {},
        eligible = false,
    ): {
        start: ReturnType<typeof vi.fn>;
        refreshSession: ReturnType<typeof vi.fn>;
    } {
        dialogRefSpy = { close: vi.fn() };
        const start = vi.fn(() => of(undefined));
        const refreshSession = vi.fn(() => of(true));

        TestBed.configureTestingModule({
            imports: [PremiumRequiredDialogComponent],
            providers: [
                provideTranslateTesting(),
                { provide: FdUiDialogRef, useValue: dialogRefSpy },
                { provide: FD_UI_DIALOG_DATA, useValue: data },
                { provide: PREMIUM_TRIAL_ACTIONS, useValue: { canStart: (): Observable<boolean> => of(eligible), start } },
                { provide: AuthService, useValue: { refreshSession } },
                { provide: FdUiToastService, useValue: { success: vi.fn() } },
            ],
        });

        fixture = TestBed.createComponent(PremiumRequiredDialogComponent);
        component = fixture.componentInstance;
        fixture.detectChanges();
        return { start, refreshSession };
    }

    it('should close with true on confirm', () => {
        createComponent();
        component['onConfirm']();
        expect(dialogRefSpy.close).toHaveBeenCalledWith(true);
    });

    it('should close with false on cancel', () => {
        createComponent();
        component['onCancel']();
        expect(dialogRefSpy.close).toHaveBeenCalledWith(false);
    });

    it('should work without data', () => {
        createComponent(null);
        expect(component['data']).toEqual({});
    });

    it('should accept custom labels', () => {
        createComponent({
            title: 'Upgrade Required',
            message: 'This feature requires premium.',
            actionLabel: 'Upgrade Now',
            cancelLabel: 'Maybe Later',
        });
        expect(component['data'].title).toBe('Upgrade Required');
        expect(component['data'].actionLabel).toBe('Upgrade Now');
    });

    it('offers Premium without the trial action when the account is ineligible', () => {
        createComponent();
        const host = fixture.nativeElement as HTMLElement;
        expect(host.textContent).toContain('AI.PREMIUM_DIALOG.ACTION');
        expect(host.textContent).not.toContain('PREMIUM_PAGE.OVERVIEW.START_TRIAL_ACTION');
    });

    it('starts an eligible trial once and refreshes access before closing', async () => {
        const { start, refreshSession } = createComponent({}, true);
        const host = fixture.nativeElement as HTMLElement;
        expect(host.textContent).toContain('PREMIUM_PAGE.OVERVIEW.START_TRIAL_ACTION');

        await Promise.all([component['startTrialAsync'](), component['startTrialAsync']()]);

        expect(start).toHaveBeenCalledOnce();
        expect(refreshSession).toHaveBeenCalledOnce();
        expect(dialogRefSpy.close).toHaveBeenCalledWith(false);
    });

    it('shows an activation error and keeps the dialog open when starting the trial fails', async () => {
        const { start, refreshSession } = createComponent({}, true);
        start.mockReturnValue(throwError(() => new Error('Trial unavailable')));

        await component['startTrialAsync']();
        fixture.detectChanges();

        const host = fixture.nativeElement as HTMLElement;
        expect(host.querySelector('[role="alert"]')?.textContent).toContain('PREMIUM_PAGE.ERROR_TRIAL');
        expect(refreshSession).not.toHaveBeenCalled();
        expect(dialogRefSpy.close).not.toHaveBeenCalled();
    });
});
