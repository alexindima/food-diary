import { describe, expect, it } from 'vitest';

import { nonEmptyArrayValidator } from './non-empty-array.validator';

const VALID_ARRAY_VALUE = ['first', 'second', 'third'];

type NonEmptyArrayControl = Parameters<ReturnType<typeof nonEmptyArrayValidator>>[0];

function createControlState(value: unknown): NonEmptyArrayControl {
    const control = Object.create(null) as NonEmptyArrayControl;
    Object.defineProperty(control, 'value', { value });

    return control;
}

describe('nonEmptyArrayValidator', () => {
    const validator = nonEmptyArrayValidator();

    it('should return null for non-empty array', () => {
        const control = createControlState(VALID_ARRAY_VALUE);
        const result = validator(control);
        expect(result).toBeNull();
    });

    it('should return error for empty array', () => {
        const control = createControlState([]);
        const result = validator(control);
        expect(result).toEqual({ nonEmptyArray: true });
    });

    it('should return error for non-array value', () => {
        const control = createControlState('not an array');
        const result = validator(control);
        expect(result).toEqual({ nonEmptyArray: true });
    });

    it('should return error for null', () => {
        const control = createControlState(null);
        const result = validator(control);
        expect(result).toEqual({ nonEmptyArray: true });
    });
});
