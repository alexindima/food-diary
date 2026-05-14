import type { AbstractControl } from '@angular/forms';

import { getNumberProperty } from '../../../../../shared/lib/unknown-value.utils';

type InstantTranslator = {
    instant: (key: string, interpolateParams?: object) => string;
};

export function resolveRecipeControlError(control: AbstractControl | null, translateService: InstantTranslator): string | null {
    if (control === null) {
        return null;
    }

    if (!control.touched && !control.dirty) {
        return null;
    }

    const errors = control.errors;
    if (errors === null) {
        return null;
    }

    if (errors['required'] !== undefined) {
        return translateService.instant('FORM_ERRORS.REQUIRED');
    }

    const minError: unknown = errors['min'];
    if (minError !== undefined) {
        const min = getNumberProperty(minError, 'min') ?? 0;
        return translateService.instant('FORM_ERRORS.INVALID_MIN_AMOUNT_MUST_BE_MORE_ZERO', { min });
    }

    if (errors['nonEmptyArray'] !== undefined) {
        return translateService.instant('FORM_ERRORS.NON_EMPTY_ARRAY');
    }

    return translateService.instant('FORM_ERRORS.UNKNOWN');
}
