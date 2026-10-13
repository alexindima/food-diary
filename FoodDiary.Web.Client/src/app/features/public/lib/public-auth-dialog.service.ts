import { isPlatformBrowser } from '@angular/common';
import { type DestroyRef, inject, PLATFORM_ID, Service } from '@angular/core';
import { TranslateService } from '@ngx-translate/core';
import { FdUiDialogService } from 'fd-ui-kit/dialog/fd-ui-dialog.service';
import type { Observable } from 'rxjs';

export type PublicAuthMode = 'login' | 'register';

export type PublicAuthDialogOptions = {
    mode: PublicAuthMode;
    returnUrl?: string | null;
    adminReturnUrl?: string | null;
    destroyRef?: DestroyRef;
    messageKey?: string;
};

export type PublicAuthDialogRef = {
    afterClosed: () => Observable<unknown>;
};

@Service()
export class PublicAuthDialogService {
    private readonly fdDialogService = inject(FdUiDialogService);
    private readonly translate = inject(TranslateService);
    private readonly isBrowser = isPlatformBrowser(inject(PLATFORM_ID));

    public async openAsync({
        mode,
        returnUrl = null,
        adminReturnUrl = null,
        destroyRef,
        messageKey,
    }: PublicAuthDialogOptions): Promise<PublicAuthDialogRef | null> {
        if (!this.isBrowser) {
            return null;
        }
        const { AuthDialogComponent } = await import('../../auth/contracts/auth-dialog');
        if (destroyRef?.destroyed === true) {
            return null;
        }

        return this.fdDialogService.open(AuthDialogComponent, {
            preset: 'form',
            ariaLabel: this.translate.instant('AUTH.DIALOG_TITLE'),
            autoFocus: mode === 'login' ? '#auth-login-email' : '#auth-register-email',
            data: { mode, returnUrl, adminReturnUrl, messageKey },
        });
    }
}
