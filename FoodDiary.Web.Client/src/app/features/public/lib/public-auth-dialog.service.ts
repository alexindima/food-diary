import { inject, Injectable } from '@angular/core';
import { FdUiDialogService } from 'fd-ui-kit/dialog/fd-ui-dialog.service';

export type PublicAuthMode = 'login' | 'register';

export type PublicAuthDialogOptions = {
    mode: PublicAuthMode;
    returnUrl?: string | null;
    adminReturnUrl?: string | null;
};

@Injectable({ providedIn: 'root' })
export class PublicAuthDialogService {
    private readonly fdDialogService = inject(FdUiDialogService);

    public async openAsync({ mode, returnUrl = null, adminReturnUrl = null }: PublicAuthDialogOptions): Promise<void> {
        const { AuthDialogComponent } = await import('../../auth/dialogs/auth-dialog/auth-dialog.component');

        this.fdDialogService.open(AuthDialogComponent, {
            preset: 'form',
            autoFocus: mode === 'login' ? '#auth-login-email' : '#auth-register-email',
            data: { mode, returnUrl, adminReturnUrl },
        });
    }
}
