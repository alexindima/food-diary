import { ChangeDetectionStrategy, Component, input, output } from '@angular/core';
import { ReactiveFormsModule } from '@angular/forms';
import { TranslatePipe } from '@ngx-translate/core';
import { FdUiButtonComponent } from 'fd-ui-kit/button/fd-ui-button';
import { FdUiFormErrorComponent } from 'fd-ui-kit/form-error/fd-ui-form-error';
import { FdUiInputComponent } from 'fd-ui-kit/input/fd-ui-input';

import type { PasswordResetFieldErrors, PasswordResetForm } from '../auth-lib/auth.types';

@Component({
    selector: 'fd-auth-password-reset-form',
    imports: [ReactiveFormsModule, TranslatePipe, FdUiButtonComponent, FdUiFormErrorComponent, FdUiInputComponent],
    templateUrl: './auth-password-reset-form.html',
    styleUrl: '../auth.scss',
    changeDetection: ChangeDetectionStrategy.OnPush,
})
export class AuthPasswordResetFormComponent {
    public readonly form = input.required<PasswordResetForm>();
    public readonly errors = input.required<PasswordResetFieldErrors>();
    public readonly globalError = input.required<string | null>();
    public readonly isPasswordResetting = input.required<boolean>();
    public readonly passwordResetSent = input.required<boolean>();
    public readonly passwordResetCooldownSeconds = input.required<number>();

    public readonly passwordResetSubmit = output();
    public readonly passwordResetClose = output();
}
