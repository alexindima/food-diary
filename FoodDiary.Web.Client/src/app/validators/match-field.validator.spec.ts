import { FormControl, FormGroup } from '@angular/forms';
import { matchFieldValidator } from './match-field.validator';

describe('matchFieldValidator', () => {
    it('should return null when fields match', () => {
        const group = new FormGroup({
            password: new FormControl('secret'),
            confirmPassword: new FormControl('secret', matchFieldValidator('password')),
        });

        const result = group.controls.confirmPassword.errors;
        expect(result).toBeNull();
    });

    it('should return error when fields don\'t match', () => {
        const group = new FormGroup({
            password: new FormControl('secret'),
            confirmPassword: new FormControl('different', matchFieldValidator('password')),
        });

        group.controls.confirmPassword.updateValueAndValidity();
        const result = group.controls.confirmPassword.errors;
        expect(result).toEqual({ matchField: true });
    });

    it('should handle null parent', () => {
        const control = new FormControl('value');
        const validator = matchFieldValidator('other');

        // Control without a parent group
        const result = validator(control);
        expect(result).toBeNull();
    });

    it('should handle missing control', () => {
        const group = new FormGroup({
            confirmPassword: new FormControl('value', matchFieldValidator('nonExistent')),
        });

        group.controls.confirmPassword.updateValueAndValidity();
        const result = group.controls.confirmPassword.errors;
        expect(result).toEqual({ matchField: true });
    });
});
