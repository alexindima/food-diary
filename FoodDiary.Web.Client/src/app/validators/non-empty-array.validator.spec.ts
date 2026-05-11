import { FormControl } from '@angular/forms';
import { describe, expect, it } from 'vitest';

import { nonEmptyArrayValidator } from './non-empty-array.validator';

const VALID_ARRAY_VALUE = ['first', 'second', 'third'];

describe('nonEmptyArrayValidator', () => {
    const validator = nonEmptyArrayValidator();

    it('should return null for non-empty array', () => {
        const control = new FormControl(VALID_ARRAY_VALUE);
        const result = validator(control);
        expect(result).toBeNull();
    });

    it('should return error for empty array', () => {
        const control = new FormControl([]);
        const result = validator(control);
        expect(result).toEqual({ nonEmptyArray: true });
    });

    it('should return error for non-array value', () => {
        const control = new FormControl('not an array');
        const result = validator(control);
        expect(result).toEqual({ nonEmptyArray: true });
    });

    it('should return error for null', () => {
        const control = new FormControl(null);
        const result = validator(control);
        expect(result).toEqual({ nonEmptyArray: true });
    });
});
