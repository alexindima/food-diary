import { ChangeDetectionStrategy, ChangeDetectorRef, Component, forwardRef, Input, ViewEncapsulation, inject } from '@angular/core';
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
    private readonly cdr = inject(ChangeDetectorRef);
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
    @Input() public unitLabel?: string;

    public disabled = false;
    public value = '';
    public inputSize = 1;
    public inputWidth = '1ch';
    public readonly maxInputChars = 8;

    private onChange: (value: string) => void = () => undefined;
    private onTouched: () => void = () => undefined;

    public writeValue(value: string | number | null): void {
        this.value = value === null || value === undefined ? '' : String(value);
        this.updateInputMeasure(this.value);
        this.cdr.markForCheck();
    }

    public registerOnChange(fn: (value: string) => void): void {
        this.onChange = fn;
    }

    public registerOnTouched(fn: () => void): void {
        this.onTouched = fn;
    }

    public setDisabledState(isDisabled: boolean): void {
        this.disabled = isDisabled;
        this.cdr.markForCheck();
    }

    public onInput(event: Event): void {
        const target = event.target as HTMLInputElement;
        const rawValue = target.value;

        if (this.type === 'number') {
            const sanitized = this.sanitizeDecimalInput(rawValue);
            this.value = sanitized;
            target.value = sanitized;
            this.updateInputMeasure(sanitized);
            this.onChange(sanitized);
            return;
        }

        this.value = rawValue;
        this.updateInputMeasure(rawValue);
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

    private updateInputMeasure(value: string): void {
        const length = Math.max(1, value.length);
        const clamped = Math.min(this.maxInputChars, length);
        this.inputSize = clamped;
        this.inputWidth = `${clamped}ch`;
    }
}
