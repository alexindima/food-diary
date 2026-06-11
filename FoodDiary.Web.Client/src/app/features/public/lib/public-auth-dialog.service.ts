import { type DestroyRef, inject, Service } from '@angular/core';
import { FdUiDialogService } from 'fd-ui-kit/dialog/fd-ui-dialog.service';
import type { Observable } from 'rxjs';

export type PublicAuthMode = 'login' | 'register';

export type PublicAuthDialogOptions = {
    mode: PublicAuthMode;
    returnUrl?: string | null;
    adminReturnUrl?: string | null;
    destroyRef?: DestroyRef;
};

export type PublicAuthDialogRef = {
    afterClosed: () => Observable<unknown>;
};

@Service()
export class PublicAuthDialogService {
    private readonly fdDialogService = inject(FdUiDialogService);

    public async openAsync({
        mode,
        returnUrl = null,
        adminReturnUrl = null,
        destroyRef,
    }: PublicAuthDialogOptions): Promise<PublicAuthDialogRef | null> {
        const { AuthDialogComponent } = await import('../../auth/dialogs/auth-dialog/auth-dialog');
        if (destroyRef?.destroyed === true) {
            return null;
        }

        return this.fdDialogService.open(AuthDialogComponent, {
            preset: 'form',
            autoFocus: mode === 'login' ? '#auth-login-email' : '#auth-register-email',
            data: { mode, returnUrl, adminReturnUrl },
        });
    }
}
