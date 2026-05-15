import { FormControl, Validators } from '@angular/forms';
import type { TranslateService } from '@ngx-translate/core';
import type { FdValidationErrors } from 'fd-ui-kit/form-error/fd-ui-form-error.component';
import { beforeEach, describe, expect, it, vi } from 'vitest';

import { resolveTranslatedControlError } from './user-manage-validation.mapper';

const MIN_LENGTH = 5;
const OVERRIDDEN_MIN_LENGTH = 8;

describe('resolveTranslatedControlError', () => {
    const instant = vi.fn((key: string, params?: Record<string, unknown>) => `${key}:${JSON.stringify(params ?? {})}`);
    const translateService = {
        instant,
    } as unknown as TranslateService;

    beforeEach(() => {
        vi.clearAllMocks();
    });

    it('should return null for valid control', () => {
        const control = new FormControl('value', Validators.required);

        expect(resolveTranslatedControlError(control, {}, translateService)).toBeNull();
    });

    it('should not show error before control is touched or dirty', () => {
        const control = new FormControl('', Validators.required);

        expect(resolveTranslatedControlError(control, { required: () => 'FORM_ERRORS.REQUIRED' }, translateService)).toBeNull();
    });

    it('should translate string validation result with control params', () => {
        const control = new FormControl('abc', Validators.minLength(MIN_LENGTH));
        control.markAsTouched();
        const validationErrors: FdValidationErrors = {
            minlength: () => 'FORM_ERRORS.PASSWORD.MIN_LENGTH',
        };

        const result = resolveTranslatedControlError(control, validationErrors, translateService);

        expect(result).toContain('FORM_ERRORS.PASSWORD.MIN_LENGTH');
        expect(instant).toHaveBeenCalledWith('FORM_ERRORS.PASSWORD.MIN_LENGTH', expect.objectContaining({ requiredLength: MIN_LENGTH }));
    });

    it('should merge params from object validation result', () => {
        const control = new FormControl('abc', Validators.minLength(MIN_LENGTH));
        control.markAsDirty();
        const validationErrors: FdValidationErrors = {
            minlength: () => ({
                key: 'FORM_ERRORS.PASSWORD.MIN_LENGTH',
                params: { requiredLength: OVERRIDDEN_MIN_LENGTH },
            }),
        };

        resolveTranslatedControlError(control, validationErrors, translateService);

        expect(instant).toHaveBeenCalledWith(
            'FORM_ERRORS.PASSWORD.MIN_LENGTH',
            expect.objectContaining({ requiredLength: OVERRIDDEN_MIN_LENGTH }),
        );
    });

    it('should return unknown error when no resolver matches', () => {
        const control = new FormControl('', Validators.required);
        control.markAsTouched();

        expect(resolveTranslatedControlError(control, {}, translateService)).toBe('FORM_ERRORS.UNKNOWN:{}');
    });
});
