import { type ComponentFixture, TestBed } from '@angular/core/testing';
import { FD_UI_DIALOG_DATA } from 'fd-ui-kit/dialog/fd-ui-dialog-data';
import { FdUiDialogRef } from 'fd-ui-kit/dialog/fd-ui-dialog-ref';
import { FdUiToastService } from 'fd-ui-kit/toast/fd-ui-toast.service';
import { type Observable, of, Subject, throwError } from 'rxjs';
import { describe, expect, it, vi } from 'vitest';

import { provideTranslateTesting } from '../../../../testing/translate-testing.module';
import { AuthService } from '../../../services/auth.service';
import { PremiumRequiredDialogComponent, type PremiumRequiredDialogData } from './premium-required-dialog';
import { PREMIUM_TRIAL_ACTIONS } from './premium-trial-actions';

function createComponent(
    data: PremiumRequiredDialogData | null = {},
    eligibility: boolean | Observable<boolean> = false,
): {
    fixture: ComponentFixture<PremiumRequiredDialogComponent>;
    dialogRef: { close: ReturnType<typeof vi.fn>; disableClose: boolean };
    start: ReturnType<typeof vi.fn<() => Observable<unknown>>>;
    refreshSession: ReturnType<typeof vi.fn<AuthService['refreshSession']>>;
} {
    const dialogRef = { close: vi.fn(), disableClose: false };
    const start = vi.fn<() => Observable<unknown>>(() => of(undefined));
    const refreshSession = vi.fn<AuthService['refreshSession']>(() => of(true));

    TestBed.configureTestingModule({
        imports: [PremiumRequiredDialogComponent],
        providers: [
            provideTranslateTesting(),
            { provide: FdUiDialogRef, useValue: dialogRef },
            { provide: FD_UI_DIALOG_DATA, useValue: data },
            {
                provide: PREMIUM_TRIAL_ACTIONS,
                useValue: {
                    canStart: (): Observable<boolean> => (typeof eligibility === 'boolean' ? of(eligibility) : eligibility),
                    start,
                },
            },
            { provide: AuthService, useValue: { refreshSession } },
            { provide: FdUiToastService, useValue: { success: vi.fn() } },
        ],
    });

    const fixture = TestBed.createComponent(PremiumRequiredDialogComponent);
    fixture.detectChanges();
    return { fixture, dialogRef, start, refreshSession };
}

describe('PremiumRequiredDialogComponent', () => {
    it('should close with true on confirm', () => {
        const { fixture, dialogRef } = createComponent();
        fixture.componentInstance['onConfirm']();
        expect(dialogRef.close).toHaveBeenCalledWith(true);
    });

    it('should close with false on cancel', () => {
        const { fixture, dialogRef } = createComponent();
        fixture.componentInstance['onCancel']();
        expect(dialogRef.close).toHaveBeenCalledWith(false);
    });

    it('should work without data', () => {
        const { fixture } = createComponent(null);
        expect(fixture.componentInstance['data']).toEqual({});
    });

    it('should accept custom labels', () => {
        const { fixture } = createComponent({
            title: 'Upgrade Required',
            message: 'This feature requires premium.',
            actionLabel: 'Upgrade Now',
            cancelLabel: 'Maybe Later',
        });
        expect(fixture.componentInstance['data'].title).toBe('Upgrade Required');
        expect(fixture.componentInstance['data'].actionLabel).toBe('Upgrade Now');
    });

    it('offers Premium without the trial action when the account is ineligible', () => {
        const { fixture } = createComponent();
        const host = fixture.nativeElement as HTMLElement;
        expect(host.textContent).toContain('AI.PREMIUM_DIALOG.ACTION');
        expect(host.textContent).not.toContain('PREMIUM_PAGE.OVERVIEW.START_TRIAL_ACTION');
    });

    it('starts an eligible trial once and refreshes access before closing', async () => {
        const { fixture, dialogRef, start, refreshSession } = createComponent({}, true);
        const component = fixture.componentInstance;
        const host = fixture.nativeElement as HTMLElement;
        expect(host.textContent).toContain('PREMIUM_PAGE.OVERVIEW.START_TRIAL_ACTION');

        await Promise.all([component['startTrialAsync'](), component['startTrialAsync']()]);

        expect(start).toHaveBeenCalledOnce();
        expect(refreshSession).toHaveBeenCalledOnce();
        expect(dialogRef.close).toHaveBeenCalledWith(false);
    });

    it('shows an activation error and keeps the dialog open when starting the trial fails', async () => {
        const { fixture, dialogRef, start, refreshSession } = createComponent({}, true);
        start.mockReturnValue(throwError(() => new Error('Trial unavailable')));

        await fixture.componentInstance['startTrialAsync']();
        fixture.detectChanges();

        const host = fixture.nativeElement as HTMLElement;
        expect(host.querySelector('[role="alert"]')?.textContent).toContain('PREMIUM_PAGE.ERROR_TRIAL');
        expect(refreshSession).not.toHaveBeenCalled();
        expect(dialogRef.close).not.toHaveBeenCalled();
    });
});

describe('Premium trial access recovery', () => {
    it('keeps the Premium action available when trial eligibility cannot load', () => {
        const { fixture, dialogRef, start } = createComponent(
            {},
            throwError(() => new Error('Billing overview unavailable')),
        );
        const host = fixture.nativeElement as HTMLElement;
        expect(host.textContent).not.toContain('PREMIUM_PAGE.OVERVIEW.START_TRIAL_ACTION');
        findAction(host, 'AI.PREMIUM_DIALOG.ACTION').click();

        expect(start).not.toHaveBeenCalled();
        expect(dialogRef.close).toHaveBeenCalledExactlyOnceWith(true);
    });

    it.each(['session unavailable', 'session lock rejected'] as const)(
        'keeps the dialog open after activation when access cannot refresh: %s',
        async outcome => {
            const { fixture, dialogRef, start, refreshSession } = createComponent({}, true);
            const refresh = new Subject<boolean>();
            refreshSession.mockReturnValue(refresh);
            const host = fixture.nativeElement as HTMLElement;
            const trialAction = findAction(host, 'PREMIUM_PAGE.OVERVIEW.START_TRIAL_ACTION');
            trialAction.click();
            fixture.detectChanges();

            expect(dialogRef.disableClose).toBe(true);
            expect(trialAction.disabled).toBe(true);
            await vi.waitFor(() => {
                expect(refreshSession).toHaveBeenCalledOnce();
            });
            expect(start).toHaveBeenCalledOnce();

            if (outcome === 'session unavailable') {
                refresh.next(false);
                refresh.complete();
            } else {
                refresh.error(new Error('Auth cookie session lock unavailable'));
            }
            await vi.waitFor(() => {
                fixture.detectChanges();
                expect(host.querySelector('[role="alert"]')?.textContent).toContain('PREMIUM_PAGE.ERROR_ACCESS_REFRESH');
            });

            expect(dialogRef.disableClose).toBe(false);
            expect(dialogRef.close).not.toHaveBeenCalled();
            expect(host.textContent).not.toContain('PREMIUM_PAGE.OVERVIEW.START_TRIAL_ACTION');
            findAction(host, 'AI.PREMIUM_DIALOG.CANCEL').click();
            expect(dialogRef.close).toHaveBeenCalledExactlyOnceWith(false);
        },
    );
});

function findAction(host: HTMLElement, label: string): HTMLButtonElement {
    const control = Array.from(host.querySelectorAll('button')).find(button => button.textContent.trim() === label);
    if (control === undefined) {
        throw new Error(`Missing dialog action ${label}`);
    }
    return control;
}
