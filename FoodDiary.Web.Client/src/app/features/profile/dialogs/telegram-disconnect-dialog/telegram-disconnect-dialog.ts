import { HttpErrorResponse, HttpStatusCode } from '@angular/common/http';
import { afterNextRender, ChangeDetectionStrategy, Component, DestroyRef, type ElementRef, inject, signal, viewChild } from '@angular/core';
import { form, FormField, maxLength, required } from '@angular/forms/signals';
import { TranslatePipe } from '@ngx-translate/core';
import { FdUiButtonComponent } from 'fd-ui-kit/button/fd-ui-button';
import { FdUiDialogComponent } from 'fd-ui-kit/dialog/fd-ui-dialog';
import { FD_UI_DIALOG_DATA } from 'fd-ui-kit/dialog/fd-ui-dialog-data';
import { FdUiDialogFooterDirective } from 'fd-ui-kit/dialog/fd-ui-dialog-footer.directive';
import { FdUiDialogRef } from 'fd-ui-kit/dialog/fd-ui-dialog-ref';
import { FdUiFormDirective } from 'fd-ui-kit/form/fd-ui-form.directive';
import { FdUiInputComponent } from 'fd-ui-kit/input/fd-ui-input';
import { firstValueFrom } from 'rxjs';

import { environment } from '../../../../../environments/environment';
import { AuthService } from '../../../../services/auth.service';
import { AUTH_PASSWORD_MAX_LENGTH } from '../../../../shared/auth/auth.constants';
import type { TelegramDisconnectConfirmation } from '../../../../shared/auth/auth.data';
import { GoogleIdentityService } from '../../../../shared/auth/google-identity.service';
import { injectCurrentLanguage } from '../../../../shared/i18n/inject-current-language';
import type { User } from '../../../../shared/models/user.data';

export type TelegramDisconnectDialogData = {
    owner: User['id'];
    canUsePassword: boolean;
    canUseGoogle: boolean;
};

@Component({
    selector: 'fd-telegram-disconnect-dialog',
    templateUrl: './telegram-disconnect-dialog.html',
    changeDetection: ChangeDetectionStrategy.OnPush,
    imports: [
        FdUiDialogComponent,
        FdUiDialogFooterDirective,
        FdUiButtonComponent,
        FdUiInputComponent,
        FdUiFormDirective,
        FormField,
        TranslatePipe,
    ],
})
export class TelegramDisconnectDialogComponent {
    private readonly auth = inject(AuthService);
    private readonly dialogRef = inject(FdUiDialogRef<TelegramDisconnectDialogComponent, User>);
    private readonly destroyRef = inject(DestroyRef);
    private readonly google = inject(GoogleIdentityService);
    private readonly locale = injectCurrentLanguage();
    private readonly googleButton = viewChild<ElementRef<HTMLElement>>('googleButton');

    protected readonly data = inject<TelegramDisconnectDialogData>(FD_UI_DIALOG_DATA);
    protected readonly isSubmitting = signal(false);
    protected readonly errorKey = signal<string | null>(null);
    protected readonly googleUnavailable = signal(false);
    protected readonly passwordMaximumLength = AUTH_PASSWORD_MAX_LENGTH;
    private readonly values = signal({ password: '' });
    private readonly confirmPasswordAsync = async (): Promise<void> => this.confirmAsync({ password: this.values().password });
    protected readonly form = form(
        this.values,
        path => {
            required(path.password);
            maxLength(path.password, AUTH_PASSWORD_MAX_LENGTH);
        },
        { submission: { action: this.confirmPasswordAsync } },
    );

    public constructor() {
        afterNextRender(() => {
            if (this.data.canUseGoogle) {
                void this.initializeGoogleAsync();
            }
        });
    }

    protected cancel(): void {
        if (!this.isSubmitting()) {
            this.dialogRef.close();
        }
    }

    private async initializeGoogleAsync(): Promise<void> {
        const clientId = environment.googleClientId;
        if (clientId === undefined || clientId.length === 0) {
            this.googleUnavailable.set(true);
            return;
        }
        try {
            await this.google.initializeAsync({
                clientId,
                callback: credential => {
                    if (!this.destroyRef.destroyed) {
                        void this.confirmAsync({ googleCredential: credential });
                    }
                },
            });
            const target = this.googleButton()?.nativeElement;
            if (target !== undefined && !this.destroyRef.destroyed) {
                this.google.renderButton(target, 'filled_blue', this.locale());
            }
        } catch {
            this.googleUnavailable.set(true);
        }
    }

    private async confirmAsync(confirmation: TelegramDisconnectConfirmation): Promise<void> {
        if (this.isSubmitting()) {
            return;
        }
        this.errorKey.set(null);
        this.isSubmitting.set(true);
        try {
            const user = await firstValueFrom(this.auth.disconnectTelegram(this.data.owner, confirmation));
            this.values.set({ password: '' });
            this.dialogRef.close(user);
        } catch (error: unknown) {
            if (error instanceof HttpErrorResponse && error.status === Number(HttpStatusCode.TooManyRequests)) {
                this.errorKey.set('USER_MANAGE.TELEGRAM_DISCONNECT_RATE_LIMITED');
            } else {
                this.errorKey.set('USER_MANAGE.TELEGRAM_DISCONNECT_CONFIRM_ERROR');
            }
        } finally {
            this.isSubmitting.set(false);
        }
    }
}
