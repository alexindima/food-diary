import type { AbstractControl, ValidationErrors, ValidatorFn } from '@angular/forms';

export function matchFieldValidator(matchTo: string): ValidatorFn {
    return (control: AbstractControl): ValidationErrors | null => {
        if (control.parent === null) {
            return null;
        }

        const matchingControl = control.parent.get(matchTo);
        const isMatch = control.value === matchingControl?.value;

        return isMatch ? null : { matchField: true };
    };
}
