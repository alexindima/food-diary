import { AbstractControl, ValidationErrors, ValidatorFn } from '@angular/forms';

export function nonEmptyArrayValidator(): ValidatorFn {
    return (control: AbstractControl): ValidationErrors | null => {
        const value = control.value;
        return Array.isArray(value) && value.length > 0 ? null : { nonEmptyArray: true };
    };
}
