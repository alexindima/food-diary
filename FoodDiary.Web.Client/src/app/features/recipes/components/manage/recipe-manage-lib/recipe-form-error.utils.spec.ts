import type { TranslateService } from '@ngx-translate/core';
import { describe, expect, it, vi } from 'vitest';

import { type RecipeControlErrorState, resolveRecipeControlError } from './recipe-form-error.utils';

const MIN_AMOUNT = 0.01;

type TranslateServiceMock = Pick<TranslateService, 'instant'> & {
    instant: ReturnType<typeof vi.fn>;
};

describe('resolveRecipeControlError', () => {
    it('should not show errors before the control is touched or dirty', () => {
        const control = createControlState({
            errors: { required: true },
        });

        expect(resolveRecipeControlError(control, createTranslateServiceMock())).toBeNull();
    });

    it('should resolve required errors', () => {
        const control = createControlState({
            errors: { required: true },
            touched: true,
        });

        expect(resolveRecipeControlError(control, createTranslateServiceMock())).toBe('FORM_ERRORS.REQUIRED');
    });

    it('should resolve min errors with min value params', () => {
        const translateService = createTranslateServiceMock();
        const control = createControlState({
            errors: { min: { min: MIN_AMOUNT } },
            touched: true,
        });

        expect(resolveRecipeControlError(control, translateService)).toBe('FORM_ERRORS.INVALID_MIN_AMOUNT_MUST_BE_MORE_ZERO');
        expect(translateService.instant).toHaveBeenCalledWith('FORM_ERRORS.INVALID_MIN_AMOUNT_MUST_BE_MORE_ZERO', { min: MIN_AMOUNT });
    });

    it('should resolve non-empty array errors', () => {
        const control = createControlState({
            dirty: true,
            errors: { nonEmptyArray: true },
        });

        expect(resolveRecipeControlError(control, createTranslateServiceMock())).toBe('FORM_ERRORS.NON_EMPTY_ARRAY');
    });
});

function createControlState(overrides: Partial<RecipeControlErrorState> = {}): RecipeControlErrorState {
    return {
        dirty: false,
        errors: null,
        touched: false,
        ...overrides,
    };
}

function createTranslateServiceMock(): TranslateServiceMock {
    const translateService: TranslateServiceMock = {
        instant: vi.fn((key: string) => key),
    };
    return translateService;
}
