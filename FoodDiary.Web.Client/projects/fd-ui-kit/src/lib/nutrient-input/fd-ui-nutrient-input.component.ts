import { ChangeDetectionStrategy, Component, forwardRef, Input, ViewEncapsulation } from '@angular/core';
import { NgClass } from '@angular/common';
import { ControlValueAccessor, NG_VALUE_ACCESSOR } from '@angular/forms';
import { MatIconModule } from '@angular/material/icon';

@Component({
    selector: 'fd-ui-nutrient-input',
    standalone: true,
    imports: [NgClass, MatIconModule],
    templateUrl: './fd-ui-nutrient-input.component.html',
    styleUrls: ['./fd-ui-nutrient-input.component.scss'],
    changeDetection: ChangeDetectionStrategy.OnPush,
    encapsulation: ViewEncapsulation.None,
    providers: [
        {
            provide: NG_VALUE_ACCESSOR,
            useExisting: forwardRef((): typeof FdUiNutrientInputComponent => FdUiNutrientInputComponent),
            multi: true,
        },
    ],
})
export class FdUiNutrientInputComponent implements ControlValueAccessor {
    @Input() public label: string = '';
    @Input() public icon?: string;
    @Input() public placeholder: string = '0';
    @Input() public name?: string;
    @Input() public type: 'text' | 'number' = 'number';
    @Input() public step?: string | number;
    @Input() public min?: string | number;
    @Input() public max?: string | number;
    @Input() public required = false;
    @Input() public readonly = false;
    @Input() public error?: string | null;
    @Input() public tintColor?: string;
    @Input() public textColor?: string;
    @Input() public size: 'sm' | 'md' | 'lg' = 'md';
    @Input() public variant: 'tinted' | 'outline' = 'tinted';
    @Input() public labelUppercase = true;
    @Input() public valueAlign: 'center' | 'left' = 'center';

    public disabled = false;
    public value = '';

    private onChange: (value: string) => void = () => undefined;
    private onTouched: () => void = () => undefined;

    public writeValue(value: string | number | null): void {
        this.value = value === null || value === undefined ? '' : String(value);
    }

    public registerOnChange(fn: (value: string) => void): void {
        this.onChange = fn;
    }

    public registerOnTouched(fn: () => void): void {
        this.onTouched = fn;
    }

    public setDisabledState(isDisabled: boolean): void {
        this.disabled = isDisabled;
    }

    public onInput(event: Event): void {
        const target = event.target as HTMLInputElement;
        const rawValue = target.value;

        if (this.type === 'number') {
            const sanitized = this.sanitizeDecimalInput(rawValue);
            this.value = sanitized;
            target.value = sanitized;
            this.onChange(sanitized);
            return;
        }

        this.value = rawValue;
        this.onChange(rawValue);
    }

    public onBlur(): void {
        this.onTouched();
    }

    private sanitizeDecimalInput(value: string): string {
        if (!value) {
            return '';
        }

        const normalized = value.replace(',', '.').replace(/[^0-9.]/g, '');
        const parts = normalized.split('.');

        if (parts.length <= 1) {
            return normalized;
        }

        return `${parts[0]}.${parts.slice(1).join('')}`;
    }
}
