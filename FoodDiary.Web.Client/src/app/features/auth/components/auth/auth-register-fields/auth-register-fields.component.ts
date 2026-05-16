import { ChangeDetectionStrategy, Component, input } from '@angular/core';
import { ReactiveFormsModule } from '@angular/forms';
import { TranslatePipe } from '@ngx-translate/core';
import { FdUiCheckboxComponent } from 'fd-ui-kit/checkbox/fd-ui-checkbox.component';
import { FdUiInputComponent } from 'fd-ui-kit/input/fd-ui-input.component';

import type { RegisterFieldErrors, RegisterForm } from '../auth-lib/auth.types';

@Component({
    selector: 'fd-auth-register-fields',
    imports: [ReactiveFormsModule, TranslatePipe, FdUiCheckboxComponent, FdUiInputComponent],
    templateUrl: './auth-register-fields.component.html',
    styleUrl: '../auth.component.scss',
    changeDetection: ChangeDetectionStrategy.OnPush,
})
export class AuthRegisterFieldsComponent {
    public readonly form = input.required<RegisterForm>();
    public readonly errors = input.required<RegisterFieldErrors>();
}
