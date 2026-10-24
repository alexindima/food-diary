import { afterNextRender, Directive, inject, Injector } from '@angular/core';
import { type FieldTree, FormRoot } from '@angular/forms/signals';

export function focusFirstInvalidField<T>(field: FieldTree<T>): void {
    const error = field()
        .errorSummary()
        .find(item => !item.fieldTree().disabled() && !item.fieldTree().hidden());
    error?.fieldTree().focusBoundControl();
}

/** Keeps native submit semantics and adds visible validation plus a keyboard destination. */
@Directive({
    selector: 'form[formRoot]',
    hostDirectives: [{ directive: FormRoot, inputs: ['formRoot'] }],
    host: { '(submit)': 'onSubmit()' },
})
export class FdUiFormDirective {
    private readonly root = inject(FormRoot);
    private readonly injector = inject(Injector);

    protected onSubmit(): void {
        const field = this.root.fieldTree();
        field().markAsTouched();
        afterNextRender(
            () => {
                focusFirstInvalidField(field);
            },
            { injector: this.injector },
        );
    }
}
