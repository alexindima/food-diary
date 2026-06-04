import type { TranslateService } from '@ngx-translate/core';
import type { FdValidationErrors } from 'fd-ui-kit/form-error/fd-ui-form-error';
import { beforeEach, describe, expect, it, vi } from 'vitest';

import { resolveTranslatedControlError, type TranslatedControlErrorState } from './validation-error.utils';

const MIN_LENGTH = 5;
const OVERRIDDEN_MIN_LENGTH = 8;

describe('resolveTranslatedControlError', () => {
    const instant = vi.fn((key: string | string[], params?: Record<string, unknown>) => {
        const normalizedKey = Array.isArray(key) ? key[0] : key;
        return `${normalizedKey}:${JSON.stringify(params ?? {})}`;
    });
    const translateService = {
        instant,
    } as unknown as TranslateService;

    beforeEach(() => {
        vi.clearAllMocks();
    });

    it('should return null for valid control', () => {
        const control = createControlState({ invalid: false });

        expect(resolveTranslatedControlError(control, {}, translateService)).toBeNull();
    });

    it('should not show error before control is touched or dirty', () => {
        const control = createControlState({
            errors: { required: true },
            invalid: true,
        });

        expect(resolveTranslatedControlError(control, { required: () => 'FORM_ERRORS.REQUIRED' }, translateService)).toBeNull();
    });

    it('should translate string validation result with control params', () => {
        const control = createControlState({
            errors: {
                minlength: { requiredLength: MIN_LENGTH },
            },
            invalid: true,
            touched: true,
        });
        const validationErrors: FdValidationErrors = {
            minlength: () => 'FORM_ERRORS.PASSWORD.MIN_LENGTH',
        };

        const result = resolveTranslatedControlError(control, validationErrors, translateService);

        expect(result).toContain('FORM_ERRORS.PASSWORD.MIN_LENGTH');
        expect(instant).toHaveBeenCalledWith('FORM_ERRORS.PASSWORD.MIN_LENGTH', expect.objectContaining({ requiredLength: MIN_LENGTH }));
    });

    it('should merge params from object validation result', () => {
        const control = createControlState({
            dirty: true,
            errors: {
                minlength: { requiredLength: MIN_LENGTH },
            },
            invalid: true,
        });
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

    it('should allow callers to suppress dirty-only errors', () => {
        const control = createControlState({
            dirty: true,
            errors: {
                minlength: { requiredLength: MIN_LENGTH },
            },
            invalid: true,
        });

        const result = resolveTranslatedControlError(control, { minlength: () => 'FORM_ERRORS.PASSWORD.MIN_LENGTH' }, translateService, {
            showOnDirty: false,
        });

        expect(result).toBeNull();
    });

    it('should return unknown error when no resolver matches', () => {
        const control = createControlState({
            errors: { required: true },
            invalid: true,
            touched: true,
        });

        expect(resolveTranslatedControlError(control, {}, translateService)).toBe('FORM_ERRORS.UNKNOWN:{}');
    });
});

function createControlState(overrides: Partial<TranslatedControlErrorState> = {}): TranslatedControlErrorState {
    return {
        dirty: false,
        errors: null,
        invalid: false,
        touched: false,
        ...overrides,
    };
}
