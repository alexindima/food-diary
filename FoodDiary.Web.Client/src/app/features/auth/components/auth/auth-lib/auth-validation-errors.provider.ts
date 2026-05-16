import type { FactoryProvider } from '@angular/core';
import { FD_VALIDATION_ERRORS, type FdValidationErrors, getNumberProperty } from 'fd-ui-kit/form-error/fd-ui-form-error.component';

export const AUTH_VALIDATION_ERRORS_PROVIDER: FactoryProvider = {
    provide: FD_VALIDATION_ERRORS,
    useFactory: (): FdValidationErrors => ({
        required: () => 'FORM_ERRORS.REQUIRED',
        requiredTrue: () => 'FORM_ERRORS.REQUIRED',
        email: () => 'FORM_ERRORS.EMAIL',
        matchField: () => 'FORM_ERRORS.PASSWORD.MATCH',
        minlength: (error?: unknown) => ({
            key: 'FORM_ERRORS.PASSWORD.MIN_LENGTH',
            params: { requiredLength: getNumberProperty(error, 'requiredLength') },
        }),
        userExists: () => 'FORM_ERRORS.USER_EXISTS',
    }),
};
