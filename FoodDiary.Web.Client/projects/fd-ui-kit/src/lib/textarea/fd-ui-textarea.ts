import { CommonModule } from '@angular/common';
import { ChangeDetectionStrategy, Component, computed, effect, input, model, signal } from '@angular/core';
import type { FormValueControl } from '@angular/forms/signals';

import type { FdUiFieldSize } from '../types/field-size.type';

let uniqueId = 0;
const DEFAULT_ROWS = 4;

@Component({
    selector: 'fd-ui-textarea',
    imports: [CommonModule],
    templateUrl: './fd-ui-textarea.html',
    styleUrls: ['./fd-ui-textarea.scss'],
    changeDetection: ChangeDetectionStrategy.OnPush,
})
export class FdUiTextareaComponent implements FormValueControl<string | number | null> {
    public readonly id = input(`fd-ui-textarea-${uniqueId++}`);
    public readonly label = input<string>();
    public readonly placeholder = input<string>();
    public readonly error = input<string | null>();
    public readonly required = input(false);
    public readonly readonly = input(false);
    public readonly rows = input(DEFAULT_ROWS);
    public readonly maxLength = input<number>();
    public readonly maximumLength = input<number>();
    public readonly size = input<FdUiFieldSize>('md');
    public readonly fillColor = input<string | null>(null);
    public readonly value = model<string | number | null>(null);
    public readonly touched = model(false);
    public readonly disabled = input(false);

    protected readonly internalValue = signal('');
    protected readonly isFocused = signal(false);

    public constructor() {
        effect(() => {
            const value = this.value();
            this.internalValue.set(value === null ? '' : String(value));
        });
    }

    protected readonly sizeClass = computed(() => `fd-ui-textarea--size-${this.size()}`);
    protected readonly shouldFloatLabel = computed(() => this.isFocused() || this.internalValue().trim().length > 0);
    protected readonly hasError = computed(() => {
        const error = this.error();

        return error !== null && error !== undefined && error.trim().length > 0;
    });
    protected readonly hostClass = computed(
        () =>
            `fd-ui-textarea ${this.sizeClass()}${this.hasError() ? ' fd-ui-textarea--has-error' : ''}${this.shouldFloatLabel() ? ' fd-ui-textarea--floating' : ''}`,
    );
    protected readonly shouldShowPlaceholder = computed(() => this.isFocused() && this.internalValue().trim().length === 0);
    protected readonly placeholderAttribute = computed(() => (this.shouldShowPlaceholder() ? (this.placeholder() ?? null) : null));

    protected onInput(value: string): void {
        if (this.disabled()) {
            return;
        }

        this.internalValue.set(value);
        this.value.set(value);
    }

    protected onBlur(): void {
        this.isFocused.set(false);
        this.touched.set(true);
    }

    protected onFocus(): void {
        this.isFocused.set(true);
    }

    protected focusControl(control: HTMLTextAreaElement): void {
        if (this.disabled()) {
            return;
        }

        control.focus();
    }
}
