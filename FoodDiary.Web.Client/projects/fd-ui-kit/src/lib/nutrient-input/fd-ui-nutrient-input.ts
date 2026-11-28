import { ChangeDetectionStrategy, Component, computed, input, linkedSignal, model, output, signal } from '@angular/core';
import type { FormValueControl, ValidationError } from '@angular/forms/signals';

import { injectFdUiControlFocus } from '../form/fd-ui-control-focus';
import { injectFdUiFieldError } from '../form-error/fd-ui-signal-field-error';
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
    // eslint-disable-next-line local/prefer-protected-template-members -- Public Signal Forms focus contract.
    public readonly focus = injectFdUiControlFocus();

    public readonly label = input('');
    public readonly ariaLabel = input<string>();
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
    public readonly errors = input<readonly ValidationError.WithOptionalFieldTree[]>([]);
    public readonly tintColor = input<string>();
    public readonly textColor = input<string>();
    public readonly size = input<'sm' | 'md' | 'lg'>('md');
    public readonly variant = input<'tinted' | 'outline'>('tinted');
    public readonly labelUppercase = input(true);
    public readonly valueAlign = input<'center' | 'left'>('center');
    public readonly unitLabel = input<string>();
    public readonly value = model<string | number | null>(null);
    public readonly touched = model(false);
    protected readonly resolvedError = injectFdUiFieldError(this.error, this.errors, this.touched);
    public readonly touch = output();
    public readonly disabled = input(false);

    protected readonly displayValue = linkedSignal({
        source: () => ({ type: this.type(), value: this.value() }),
        computation: ({ type, value }, previous): string => {
            const draft = previous?.value ?? '';
            if (type === 'number' && Object.is(this.toNumericValue(draft), value)) {
                return draft;
            }
            return value === null ? '' : String(value);
        },
    });
    private readonly displayLength = computed(() => Math.max(1, this.displayValue().length));
    protected readonly inputSize = computed(() => Math.min(DEFAULT_MAX_INPUT_CHARS, this.displayLength()));
    protected readonly inputWidth = computed(() => `${this.inputSize()}ch`);
    private readonly valueDensityClass = computed(() => this.resolveValueDensityClass(this.displayLength()));
    protected readonly isFocused = signal(false);
    protected readonly visiblePlaceholder = computed(() => (this.isFocused() ? null : this.placeholder()));
    protected readonly effectiveAriaLabel = computed(() => {
        const explicitLabel = this.ariaLabel()?.trim();
        if (explicitLabel !== undefined && explicitLabel.length > 0) {
            return explicitLabel;
        }

        const label = this.label().trim();
        const unit = this.unitLabel()?.trim();
        return unit !== undefined && unit.length > 0 ? `${label}, ${unit}` : label.length > 0 ? label : null;
    });

    protected get hostClass(): string {
        const error = this.resolvedError();
        const hasError = error !== null && error.length > 0;
        return `fd-ui-nutrient-input fd-ui-nutrient-input--${this.size()} fd-ui-nutrient-input--${this.variant()} fd-ui-nutrient-input--value-${this.valueAlign()} ${this.valueDensityClass()}${this.disabled() ? ' fd-ui-nutrient-input--disabled' : ''}${this.isReadonly() ? ' fd-ui-nutrient-input--readonly' : ''}${hasError ? ' fd-ui-nutrient-input--error' : ''}`;
    }

    protected isReadonly(): boolean {
        return this.readonly() || this.controlReadonly();
    }

    protected focusControl(event: Event, control: HTMLInputElement): void {
        if (this.disabled() || this.isReadonly() || event.target instanceof HTMLInputElement) {
            return;
        }

        event.preventDefault();
        control.focus();
    }

    protected onInput(control: HTMLInputElement): void {
        const rawValue = control.value;

        if (this.type() === 'number') {
            const sanitized = this.sanitizeDecimalInput(rawValue);
            this.displayValue.set(sanitized);
            control.value = sanitized;
            this.value.set(this.toNumericValue(sanitized));
            return;
        }

        this.displayValue.set(rawValue);
        this.value.set(rawValue);
    }

    protected onBlur(): void {
        this.isFocused.set(false);
        this.touched.set(true);
        this.touch.emit();
    }

    protected onFocus(): void {
        this.isFocused.set(true);
    }

    private sanitizeDecimalInput(value: string): string {
        if (value.length === 0) {
            return '';
        }

        const sign = value.startsWith('-') ? '-' : '';
        const normalized = value.replace(',', '.').replaceAll(/[^\d.]/g, '');
        const parts = normalized.split('.');

        if (parts.length <= 1) {
            return `${sign}${normalized}`;
        }

        return `${sign}${parts[0]}.${parts.slice(1).join('')}`;
    }

    private toNumericValue(value: string): number | null {
        return value.trim() === '' ? null : Number(value);
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
}
