import { ChangeDetectionStrategy, Component, input } from '@angular/core';
import { type FieldTree, FormField } from '@angular/forms/signals';
import { TranslatePipe } from '@ngx-translate/core';
import { FdUiCheckboxComponent } from 'fd-ui-kit/checkbox/fd-ui-checkbox';
import { FdUiInputComponent } from 'fd-ui-kit/input/fd-ui-input';

import type { RegisterFieldErrors, RegisterFormValues } from '../auth-lib/auth.types';

@Component({
    selector: 'fd-auth-register-fields',
    imports: [FormField, TranslatePipe, FdUiCheckboxComponent, FdUiInputComponent],
    templateUrl: './auth-register-fields.html',
    styleUrl: '../auth.scss',
    changeDetection: ChangeDetectionStrategy.OnPush,
})
export class AuthRegisterFieldsComponent {
    public readonly form = input.required<FieldTree<RegisterFormValues>>();
    public readonly errors = input.required<RegisterFieldErrors>();
}
