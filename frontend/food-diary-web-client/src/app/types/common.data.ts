import { FormArray, FormControl, FormGroup } from '@angular/forms';
type IsTuple<T> = T extends [infer _A, ...infer _B] | [] ? true : false;

export type FormGroupControls<T> = {
    [K in keyof T]: T[K] extends Array<infer U>
        ? IsTuple<T[K]> extends true
            ? FormControl<T[K]>
            : FormArray<FormGroup<FormGroupControls<U>>>
        : T[K] extends object
            ? FormGroup<FormGroupControls<T[K]>>
            : FormControl<T[K]>;
};

export type RecursivePartial<T> = {
    [P in keyof T]?: T[P] extends object
        ? RecursivePartial<T[P]>
        : T[P];
};
