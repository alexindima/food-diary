import { CommonModule } from '@angular/common';
import { ChangeDetectionStrategy, Component, computed, input, signal } from '@angular/core';
import { type ControlValueAccessor, NG_VALUE_ACCESSOR } from '@angular/forms';

import type { FdUiFieldSize } from '../types/field-size.type';

let uniqueId = 0;

@Component({
    selector: 'fd-ui-textarea',
    standalone: true,
    imports: [CommonModule],
    templateUrl: './fd-ui-textarea.component.html',
    styleUrls: ['./fd-ui-textarea.component.scss'],
    changeDetection: ChangeDetectionStrategy.OnPush,
    providers: [
        {
            provide: NG_VALUE_ACCESSOR,
            useExisting: FdUiTextareaComponent,
            multi: true,
        },
    ],
})
export class FdUiTextareaComponent implements ControlValueAccessor {
    public readonly id = input(`fd-ui-textarea-${uniqueId++}`);
    public readonly label = input<string>();
    public readonly placeholder = input<string>();
    public readonly error = input<string | null>();
    public readonly required = input(false);
    public readonly readonly = input(false);
    public readonly rows = input(4);
    public readonly maxLength = input<number>();
    public readonly size = input<FdUiFieldSize>('md');
    public readonly fillColor = input<string | null>(null);

    protected readonly internalValue = signal('');
    protected readonly disabled = signal(false);
    protected readonly isFocused = signal(false);

    private onChange: (value: string) => void = () => undefined;
    private onTouched: () => void = () => undefined;

    protected readonly sizeClass = computed(() => `fd-ui-textarea--size-${this.size()}`);
    protected readonly shouldFloatLabel = computed(() => this.isFocused() || this.internalValue().trim().length > 0);
    protected readonly hostClass = computed(
        () =>
            `fd-ui-textarea ${this.sizeClass()}${this.error() ? ' fd-ui-textarea--has-error' : ''}${this.shouldFloatLabel() ? ' fd-ui-textarea--floating' : ''}`,
    );
    protected readonly shouldShowPlaceholder = computed(() => this.isFocused() && this.internalValue().trim().length === 0);
    protected readonly placeholderAttribute = computed(() => (this.shouldShowPlaceholder() ? (this.placeholder() ?? null) : null));

    public writeValue(value: string | number | null): void {
        this.internalValue.set(value === null ? '' : String(value));
    }

    public registerOnChange(fn: (value: string) => void): void {
        this.onChange = fn;
    }

    public registerOnTouched(fn: () => void): void {
        this.onTouched = fn;
    }

    public setDisabledState(isDisabled: boolean): void {
        this.disabled.set(isDisabled);
    }

    protected onInput(value: string): void {
        if (this.disabled()) {
            return;
        }

        this.internalValue.set(value);
        this.onChange(value);
    }

    protected onBlur(): void {
        this.isFocused.set(false);
        this.onTouched();
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
