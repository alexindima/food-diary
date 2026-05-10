import { NgClass } from '@angular/common';
import { ChangeDetectionStrategy, ChangeDetectorRef, Component, inject, input, ViewEncapsulation } from '@angular/core';
import { type ControlValueAccessor, NG_VALUE_ACCESSOR } from '@angular/forms';

import { FdUiIconComponent } from '../icon/fd-ui-icon.component';

@Component({
    selector: 'fd-ui-nutrient-input',
    standalone: true,
    imports: [NgClass, FdUiIconComponent],
    templateUrl: './fd-ui-nutrient-input.component.html',
    styleUrls: ['./fd-ui-nutrient-input.component.scss'],
    changeDetection: ChangeDetectionStrategy.OnPush,
    encapsulation: ViewEncapsulation.None,
    providers: [
        {
            provide: NG_VALUE_ACCESSOR,
            useExisting: FdUiNutrientInputComponent,
            multi: true,
        },
    ],
})
export class FdUiNutrientInputComponent implements ControlValueAccessor {
    private readonly cdr = inject(ChangeDetectorRef);

    public readonly label = input('');
    public readonly icon = input<string>();
    public readonly placeholder = input('0');
    public readonly name = input<string>();
    public readonly type = input<'text' | 'number'>('number');
    public readonly step = input<string | number>();
    public readonly min = input<string | number>();
    public readonly max = input<string | number>();
    public readonly required = input(false);
    public readonly readonly = input(false);
    public readonly error = input<string | null>();
    public readonly tintColor = input<string>();
    public readonly textColor = input<string>();
    public readonly size = input<'sm' | 'md' | 'lg'>('md');
    public readonly variant = input<'tinted' | 'outline'>('tinted');
    public readonly labelUppercase = input(true);
    public readonly valueAlign = input<'center' | 'left'>('center');
    public readonly unitLabel = input<string>();

    public disabled = false;
    public value = '';
    public inputSize = 1;
    public inputWidth = '1ch';
    public readonly maxInputChars = 8;

    private onChange: (value: string) => void = () => undefined;
    private onTouched: () => void = () => undefined;

    public writeValue(value: string | number | null): void {
        this.value = value === null ? '' : String(value);
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

        if (this.type() === 'number') {
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
