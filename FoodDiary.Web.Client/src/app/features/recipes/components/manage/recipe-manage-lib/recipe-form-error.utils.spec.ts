import { FormControl, Validators } from '@angular/forms';
import type { TranslateService } from '@ngx-translate/core';
import { describe, expect, it, vi } from 'vitest';

import { resolveRecipeControlError } from './recipe-form-error.utils';

const MIN_AMOUNT = 0.01;

type TranslateServiceMock = Pick<TranslateService, 'instant'> & {
    instant: ReturnType<typeof vi.fn>;
};

describe('resolveRecipeControlError', () => {
    it('should not show errors before the control is touched or dirty', () => {
        const control = new FormControl('', Validators.required);

        expect(resolveRecipeControlError(control, createTranslateServiceMock())).toBeNull();
    });

    it('should resolve required errors', () => {
        const control = new FormControl('', Validators.required);
        control.markAsTouched();

        expect(resolveRecipeControlError(control, createTranslateServiceMock())).toBe('FORM_ERRORS.REQUIRED');
    });

    it('should resolve min errors with min value params', () => {
        const translateService = createTranslateServiceMock();
        const control = new FormControl(0, Validators.min(MIN_AMOUNT));
        control.markAsTouched();

        expect(resolveRecipeControlError(control, translateService)).toBe('FORM_ERRORS.INVALID_MIN_AMOUNT_MUST_BE_MORE_ZERO');
        expect(translateService.instant).toHaveBeenCalledWith('FORM_ERRORS.INVALID_MIN_AMOUNT_MUST_BE_MORE_ZERO', { min: MIN_AMOUNT });
    });

    it('should resolve non-empty array errors', () => {
        const control = new FormControl('');
        control.setErrors({ nonEmptyArray: true });
        control.markAsDirty();

        expect(resolveRecipeControlError(control, createTranslateServiceMock())).toBe('FORM_ERRORS.NON_EMPTY_ARRAY');
    });
});

function createTranslateServiceMock(): TranslateServiceMock {
    const translateService: TranslateServiceMock = {
        instant: vi.fn((key: string) => key),
    };
    return translateService;
}
