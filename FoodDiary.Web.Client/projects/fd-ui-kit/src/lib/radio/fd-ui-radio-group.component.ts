
import { ChangeDetectionStrategy, ChangeDetectorRef, Component, forwardRef, inject, input } from '@angular/core';
import { ControlValueAccessor, FormControl, NG_VALUE_ACCESSOR, ReactiveFormsModule } from '@angular/forms';
import { MatRadioModule } from '@angular/material/radio';

export interface FdUiRadioOption<T = unknown> {
    label: string;
    value: T;
    description?: string;
}

@Component({
    selector: 'fd-ui-radio-group',
    standalone: true,
    imports: [ReactiveFormsModule, MatRadioModule],
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
    private readonly cdr = inject(ChangeDetectorRef);

    public readonly label = input<string>();
    public readonly hint = input<string>();
    public readonly error = input<string | null>();
    public readonly required = input(false);
    public readonly orientation = input<'vertical' | 'horizontal'>('vertical');
    public readonly options = input<FdUiRadioOption<T>[]>([]);

    protected disabled = false;
    protected internalValue: T | null = null;
    protected readonly control = new FormControl<T | null>(null);

    private onChange: (value: T | null) => void = () => undefined;
    private onTouched: () => void = () => undefined;

    public constructor() {
        this.control.valueChanges.subscribe(value => {
            this.internalValue = value;
            this.onChange(this.internalValue);
        });
    }

    public writeValue(value: T | null): void {
        this.internalValue = value;
        this.control.setValue(value, { emitEvent: false });
        this.cdr.markForCheck();
    }

    public registerOnChange(fn: (value: T | null) => void): void {
        this.onChange = fn;
    }

    public registerOnTouched(fn: () => void): void {
        this.onTouched = fn;
    }

    public setDisabledState(isDisabled: boolean): void {
        this.disabled = isDisabled;
        if (isDisabled) {
            this.control.disable({ emitEvent: false });
        } else {
            this.control.enable({ emitEvent: false });
        }
        this.cdr.markForCheck();
    }

    protected handleBlur(): void {
        this.onTouched();
    }

    protected trackByValue(_: number, option: FdUiRadioOption<T>): unknown {
        return option.value as unknown;
    }
}
