import { ChangeDetectionStrategy, ChangeDetectorRef, Component, effect, inject, input, model } from '@angular/core';
import type { FormValueControl } from '@angular/forms/signals';

import { FdUiIconComponent } from '../icon/fd-ui-icon';

const DEFAULT_MAX_INPUT_CHARS = 8;

@Component({
    selector: 'fd-ui-nutrient-input',
    imports: [FdUiIconComponent],
    templateUrl: './fd-ui-nutrient-input.html',
    styleUrls: ['./fd-ui-nutrient-input.scss'],
    changeDetection: ChangeDetectionStrategy.OnPush,
})
export class FdUiNutrientInputComponent implements FormValueControl<string | number | null> {
    private readonly cdr = inject(ChangeDetectorRef);

    public readonly label = input('');
    public readonly icon = input<string>();
    public readonly placeholder = input('0');
    public readonly name = input('');
    public readonly type = input<'text' | 'number'>('number');
    public readonly step = input<string | number>();
    public readonly min = input<string | number>();
    public readonly max = input<string | number>();
    public readonly required = input(false);
    public readonly readonly = input(false);
    public readonly controlReadonly = input(false);
    public readonly error = input<string | null>();
    public readonly tintColor = input<string>();
    public readonly textColor = input<string>();
    public readonly size = input<'sm' | 'md' | 'lg'>('md');
    public readonly variant = input<'tinted' | 'outline'>('tinted');
    public readonly labelUppercase = input(true);
    public readonly valueAlign = input<'center' | 'left'>('center');
    public readonly unitLabel = input<string>();
    public readonly value = model<string | number | null>(null);
    public readonly touched = model(false);
    public readonly disabled = input(false);

    protected displayValue = '';
    protected inputSize = 1;
    protected inputWidth = '1ch';
    protected readonly maxInputChars = DEFAULT_MAX_INPUT_CHARS;

    public constructor() {
        effect(() => {
            this.setValue(this.value());
        });
    }

    protected get hostClass(): string {
        const error = this.error();
        const hasError = error !== null && error !== undefined && error.length > 0;
        return `fd-ui-nutrient-input fd-ui-nutrient-input--${this.size()} fd-ui-nutrient-input--${this.variant()} fd-ui-nutrient-input--value-${this.valueAlign()}${this.disabled() ? ' fd-ui-nutrient-input--disabled' : ''}${this.isReadonly() ? ' fd-ui-nutrient-input--readonly' : ''}${hasError ? ' fd-ui-nutrient-input--error' : ''}`;
    }

    protected isReadonly(): boolean {
        return this.readonly() || this.controlReadonly();
    }

    protected onInput(event: Event): void {
        if (!(event.target instanceof HTMLInputElement)) {
            return;
        }

        const target = event.target;
        const rawValue = target.value;

        if (this.type() === 'number') {
            const sanitized = this.sanitizeDecimalInput(rawValue);
            this.displayValue = sanitized;
            target.value = sanitized;
            this.updateInputMeasure(sanitized);
            this.value.set(sanitized);
            return;
        }

        this.displayValue = rawValue;
        this.updateInputMeasure(rawValue);
        this.value.set(rawValue);
    }

    protected onBlur(): void {
        this.touched.set(true);
    }

    private sanitizeDecimalInput(value: string): string {
        if (value.length === 0) {
            return '';
        }

        const normalized = value.replace(',', '.').replaceAll(/[^\d.]/g, '');
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

    private setValue(value: string | number | null): void {
        this.displayValue = value === null ? '' : String(value);
        this.updateInputMeasure(this.displayValue);
        this.cdr.markForCheck();
    }
}
