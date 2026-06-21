import type { ElementRef } from '@angular/core';
import {
    ChangeDetectionStrategy,
    ChangeDetectorRef,
    Component,
    computed,
    effect,
    inject,
    input,
    model,
    signal,
    viewChild,
} from '@angular/core';
import type { FormValueControl } from '@angular/forms/signals';

import { FdUiIconComponent } from '../icon/fd-ui-icon';

const DEFAULT_MAX_INPUT_CHARS = 8;
const COMPACT_VALUE_LENGTH = 6;
const DENSE_VALUE_LENGTH = 7;
const EXTRA_DENSE_VALUE_LENGTH = 8;

@Component({
    selector: 'fd-ui-nutrient-input',
    imports: [FdUiIconComponent],
    templateUrl: './fd-ui-nutrient-input.html',
    styleUrls: ['./fd-ui-nutrient-input.scss'],
    changeDetection: ChangeDetectionStrategy.OnPush,
})
export class FdUiNutrientInputComponent implements FormValueControl<string | number | null> {
    private readonly cdr = inject(ChangeDetectorRef);
    private readonly control = viewChild<ElementRef<HTMLInputElement>>('control');

    public readonly label = input('');
    public readonly icon = input<string>();
    public readonly placeholder = input('0');
    public readonly name = input('');
    public readonly type = input<'text' | 'number'>('number');
    public readonly step = input<string | number>();
    public readonly min = input<string | number>();
    public readonly max = input<string | number>();
    public readonly maximum = input<string | number>();
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
    protected valueDensityClass = 'fd-ui-nutrient-input--value-normal';
    protected readonly maxInputChars = DEFAULT_MAX_INPUT_CHARS;
    protected readonly isFocused = signal(false);
    protected readonly visiblePlaceholder = computed(() => (this.isFocused() ? null : this.placeholder()));

    public constructor() {
        effect(() => {
            this.setValue(this.value());
        });
    }

    protected get hostClass(): string {
        const error = this.error();
        const hasError = error !== null && error !== undefined && error.length > 0;
        return `fd-ui-nutrient-input fd-ui-nutrient-input--${this.size()} fd-ui-nutrient-input--${this.variant()} fd-ui-nutrient-input--value-${this.valueAlign()} ${this.valueDensityClass}${this.disabled() ? ' fd-ui-nutrient-input--disabled' : ''}${this.isReadonly() ? ' fd-ui-nutrient-input--readonly' : ''}${hasError ? ' fd-ui-nutrient-input--error' : ''}`;
    }

    protected isReadonly(): boolean {
        return this.readonly() || this.controlReadonly();
    }

    protected focusControl(event: Event): void {
        if (this.disabled() || this.isReadonly() || event.target instanceof HTMLInputElement) {
            return;
        }

        event.preventDefault();
        this.control()?.nativeElement.focus();
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
        this.isFocused.set(false);
        this.touched.set(true);
    }

    protected onFocus(): void {
        this.isFocused.set(true);
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
        this.valueDensityClass = this.resolveValueDensityClass(length);
    }

    private resolveValueDensityClass(length: number): string {
        if (length >= EXTRA_DENSE_VALUE_LENGTH) {
            return 'fd-ui-nutrient-input--value-x-dense';
        }

        if (length >= DENSE_VALUE_LENGTH) {
            return 'fd-ui-nutrient-input--value-dense';
        }

        if (length >= COMPACT_VALUE_LENGTH) {
            return 'fd-ui-nutrient-input--value-compact';
        }

        return 'fd-ui-nutrient-input--value-normal';
    }

    private setValue(value: string | number | null): void {
        this.displayValue = value === null ? '' : String(value);
        this.updateInputMeasure(this.displayValue);
        this.cdr.markForCheck();
    }
}
