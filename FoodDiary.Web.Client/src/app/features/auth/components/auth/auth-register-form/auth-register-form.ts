import { ChangeDetectionStrategy, Component, type ElementRef, input, output, viewChild } from '@angular/core';
import { ReactiveFormsModule } from '@angular/forms';
import { TranslatePipe } from '@ngx-translate/core';
import { FdUiButtonComponent } from 'fd-ui-kit/button/fd-ui-button';
import { FdUiFormErrorComponent } from 'fd-ui-kit/form-error/fd-ui-form-error';

import { AuthGoogleSectionComponent } from '../auth-google-section/auth-google-section';
import type { RegisterFieldErrors, RegisterForm } from '../auth-lib/auth.types';
import { AuthRegisterFieldsComponent } from '../auth-register-fields/auth-register-fields';

@Component({
    selector: 'fd-auth-register-form',
    imports: [
        ReactiveFormsModule,
        TranslatePipe,
        FdUiButtonComponent,
        FdUiFormErrorComponent,
        AuthRegisterFieldsComponent,
        AuthGoogleSectionComponent,
    ],
    templateUrl: './auth-register-form.html',
    styleUrl: '../auth.scss',
    changeDetection: ChangeDetectionStrategy.OnPush,
})
export class AuthRegisterFormComponent {
    public readonly form = input.required<RegisterForm>();
    public readonly errors = input.required<RegisterFieldErrors>();
    public readonly globalError = input.required<string | null>();
    public readonly isSubmitting = input.required<boolean>();
    public readonly googleReady = input.required<boolean>();

    public readonly registerSubmit = output();

    public readonly googleButton = viewChild<ElementRef<HTMLElement>>('googleRegisterButton');
}
