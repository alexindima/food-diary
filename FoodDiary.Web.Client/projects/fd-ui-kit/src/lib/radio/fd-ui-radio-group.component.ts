import { ChangeDetectionStrategy, Component, forwardRef, input, model } from '@angular/core';
import { ControlValueAccessor, NG_VALUE_ACCESSOR } from '@angular/forms';

export interface FdUiRadioOption<T = unknown> {
    label: string;
    value: T;
    description?: string;
}

let nextId = 0;

@Component({
    selector: 'fd-ui-radio-group',
    standalone: true,
    templateUrl: './fd-ui-radio-group.component.html',
    styleUrls: ['./fd-ui-radio-group.component.scss'],
    changeDetection: ChangeDetectionStrategy.OnPush,
    providers: [
        {
            provide: NG_VALUE_ACCESSOR,
            useExisting: forwardRef(() => FdUiRadioGroupComponent),
            multi: true,
        },
    ],
})
export class FdUiRadioGroupComponent<T = unknown> implements ControlValueAccessor {
    protected readonly isEqual = Object.is;

    public readonly id = input(`fd-radio-${nextId++}`);
    public readonly label = input<string>();
    public readonly hint = input<string>();
    public readonly error = input<string | null>();
    public readonly required = input(false);
    public readonly orientation = input<'vertical' | 'horizontal'>('vertical');
    public readonly options = input<FdUiRadioOption<T>[]>([]);

    protected readonly disabled = model(false);
    protected internalValue: T | null = null;

    private onChange: (value: T | null) => void = () => undefined;
    private onTouched: () => void = () => undefined;

    public writeValue(value: T | null): void {
        this.internalValue = value;
    }

    public registerOnChange(fn: (value: T | null) => void): void {
        this.onChange = fn;
    }

    public registerOnTouched(fn: () => void): void {
        this.onTouched = fn;
    }

    public setDisabledState(isDisabled: boolean): void {
        this.disabled.set(isDisabled);
    }

    protected selectOption(option: FdUiRadioOption<T>): void {
        if (this.disabled()) {
            return;
        }

        this.internalValue = option.value;
        this.onChange(option.value);
    }

    protected handleBlur(): void {
        this.onTouched();
    }

    protected handleKeydown(index: number, event: KeyboardEvent): void {
        const options = this.options();
        if (!options.length) {
            return;
        }

        let nextIndex: number | null = null;

        switch (event.key) {
            case 'ArrowRight':
            case 'ArrowDown':
                nextIndex = (index + 1) % options.length;
                break;
            case 'ArrowLeft':
            case 'ArrowUp':
                nextIndex = (index - 1 + options.length) % options.length;
                break;
            case 'Home':
                nextIndex = 0;
                break;
            case 'End':
                nextIndex = options.length - 1;
                break;
            default:
                return;
        }

        event.preventDefault();
        this.selectOption(options[nextIndex]);
    }

    protected trackByValue(_: number, option: FdUiRadioOption<T>): unknown {
        return option.value as unknown;
    }
}
