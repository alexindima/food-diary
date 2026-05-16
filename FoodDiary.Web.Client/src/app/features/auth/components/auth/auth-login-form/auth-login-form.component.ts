import { ChangeDetectionStrategy, Component, type ElementRef, input, output, viewChild } from '@angular/core';
import { ReactiveFormsModule } from '@angular/forms';
import { TranslatePipe } from '@ngx-translate/core';
import { FdUiButtonComponent } from 'fd-ui-kit/button/fd-ui-button.component';
import { FdUiCheckboxComponent } from 'fd-ui-kit/checkbox/fd-ui-checkbox.component';
import { FdUiFormErrorComponent } from 'fd-ui-kit/form-error/fd-ui-form-error.component';
import { FdUiInputComponent } from 'fd-ui-kit/input/fd-ui-input.component';

import { AuthGoogleSectionComponent } from '../auth-google-section/auth-google-section.component';
import type { LoginFieldErrors, LoginForm } from '../auth-lib/auth.types';

@Component({
    selector: 'fd-auth-login-form',
    imports: [
        ReactiveFormsModule,
        TranslatePipe,
        FdUiInputComponent,
        FdUiButtonComponent,
        FdUiCheckboxComponent,
        FdUiFormErrorComponent,
        AuthGoogleSectionComponent,
    ],
    templateUrl: './auth-login-form.component.html',
    styleUrl: '../auth.component.scss',
    changeDetection: ChangeDetectionStrategy.OnPush,
})
export class AuthLoginFormComponent {
    public readonly form = input.required<LoginForm>();
    public readonly errors = input.required<LoginFieldErrors>();
    public readonly globalError = input.required<string | null>();
    public readonly isSubmitting = input.required<boolean>();
    public readonly isRestoring = input.required<boolean>();
    public readonly showRestoreAction = input.required<boolean>();
    public readonly googleReady = input.required<boolean>();
    public readonly loginSubmitLabelKey = input.required<string>();
    public readonly isSubmitDisabled = input.required<boolean>();

    public readonly loginSubmit = output();
    public readonly loginNativeInput = output();
    public readonly passwordResetOpen = output();
    public readonly restoreSubmit = output();

    public readonly formElement = viewChild<ElementRef<HTMLFormElement>>('loginFormElement');
    public readonly googleButton = viewChild<ElementRef<HTMLElement>>('googleLoginButton');
}
