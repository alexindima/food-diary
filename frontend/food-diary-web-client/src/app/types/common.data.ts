import { FormControl } from '@angular/forms';

export type FormGroupControls<T> = {
    [K in keyof T]: FormControl<T[K]>;
};
