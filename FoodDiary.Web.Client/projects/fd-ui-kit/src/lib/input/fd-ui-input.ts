import { AutofillMonitor } from '@angular/cdk/text-field';
import { CommonModule } from '@angular/common';
import { ChangeDetectionStrategy, Component, computed, inject, input, linkedSignal, model, output, signal } from '@angular/core';
import { afterNextRender, afterRenderEffect, DestroyRef, ElementRef, viewChild } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import type { FormValueControl, ValidationError } from '@angular/forms/signals';

import { injectFdUiFieldError } from '../form-error/fd-ui-signal-field-error';
import { FdUiIconComponent } from '../icon/fd-ui-icon';
import type { FdUiEnterKeyHint } from '../types/enter-key-hint.type';
import type { FdUiFieldSize } from '../types/field-size.type';
import { FD_UI_INPUT_AUTOFILL_SYNC_DELAYS_MS } from './fd-ui-input.tokens';

let uniqueId = 0;
export type FdUiInputAppearance = 'default' | 'auth' | 'search' | 'inline-edit';
export type FdUiInputValue = string | number | null | undefined;
export type FdUiInputAutocomplete =
    | 'off'
    | 'on'
    | 'name'
    | 'email'
    | 'username'
    | 'current-password'
    | 'new-password'
    | 'one-time-code'
    | 'tel'
    | 'url'
    | 'street-address'
    | 'postal-code'
    | 'country'
    | 'birthday';

export function fdUiCoerceInputTextValue(value: FdUiInputValue): string {
    return value === null || value === undefined ? '' : String(value);
}

export function fdUiCoerceInputNumberValue(value: FdUiInputValue): string | number {
    return value ?? '';
}

@Component({
    selector: 'fd-ui-input',
    imports: [CommonModule, FdUiIconComponent],
    templateUrl: './fd-ui-input.html',
    styleUrls: ['./fd-ui-input.scss'],
    changeDetection: ChangeDetectionStrategy.OnPush,
    host: { '[attr.id]': 'null' },
})
export class FdUiInputComponent implements FormValueControl<string | number | null> {
    private readonly destroyRef = inject(DestroyRef);
    private readonly autofillMonitor = inject(AutofillMonitor);
    private readonly autofillSyncDelaysMs = inject(FD_UI_INPUT_AUTOFILL_SYNC_DELAYS_MS);
    private readonly control = viewChild.required<ElementRef<HTMLInputElement>>('control');
    private readonly host = inject<ElementRef<HTMLElement>>(ElementRef);

    public readonly id = input(`fd-ui-input-${uniqueId++}`);
    public readonly name = input('');
    public readonly label = input<string>();
    public readonly floatLabel = input<'auto' | 'always'>('auto');
    public readonly ariaLabel = input<string>();
    public readonly placeholder = input<string>();
    public readonly type = input<'text' | 'number' | 'password' | 'email' | 'tel' | 'url' | 'date' | 'datetime-local' | 'time'>('text');
    public readonly localizedNumber = input(false);
    public readonly preserveInvalidNumberDraft = input(false);
    public readonly numberNormalizer = input<(value: number | null) => number | null>();
    public readonly autocomplete = input<FdUiInputAutocomplete>();
    public readonly enterKeyHint = input<FdUiEnterKeyHint>();
    public readonly error = input<string | null>();
    public readonly errors = input<readonly ValidationError.WithOptionalFieldTree[]>([]);
    public readonly required = input(false);
    public readonly showRequiredIndicator = input(true);
    public readonly suffixText = input<string>();
    public readonly inputMode = input<'text' | 'decimal' | 'numeric' | 'tel' | 'email' | 'url' | 'search' | 'none'>();
    public readonly selectOnFocus = input(false);
    public readonly readonly = input(false);
    public readonly controlReadonly = input(false);
    public readonly maxLength = input<number>();
    public readonly maximumLength = input<number>();
    public readonly max = input<string | number>();
    public readonly maximum = input<string | number>();
    public readonly minimum = input<string | number>();
    public readonly suffixButtonIcon = input<string>();
    public readonly suffixButtonAriaLabel = input<string>();
    public readonly prefixIcon = input<string>();
    public readonly step = input<string | number>();
    public readonly size = input<FdUiFieldSize>('md');
    public readonly fillColor = input<string | null>(null);
    public readonly appearance = input<FdUiInputAppearance>('default');
    public readonly value = model<string | number | null>(null);
    public readonly touched = model(false);
    protected readonly resolvedError = injectFdUiFieldError(this.error, this.errors, this.touched);
    public readonly touch = output();
    public readonly disabled = input(false);

    public readonly suffixButtonClicked = output();
    public readonly nativeNumberDraftInvalidChange = output<boolean>();

    protected readonly internalValue = linkedSignal({
        source: this.value,
        computation: (value, previous): string | number => {
            const draft = previous?.value ?? '';
            if (this.nativeNumberDraftInvalid) {
                return draft;
            }
            if (this.localizedNumber() && this.type() === 'number' && Object.is(this.toModelValue(String(draft)), value)) {
                return draft;
            }
            return value ?? '';
        },
    });
    protected readonly isFocused = signal(false);
    protected readonly isIdentifier = computed(
        () =>
            this.appearance() === 'search' ||
            ['email', 'url', 'password', 'number', 'tel', 'date', 'datetime-local', 'time'].includes(this.type()) ||
            ['email', 'username', 'current-password', 'new-password', 'one-time-code', 'url'].includes(this.autocomplete() ?? ''),
    );

    private readonly autofillSyncTimers = new Set<ReturnType<typeof setTimeout>>();
    private nativeNumberDraftInvalid = false;
    private synchronizedNativeValue = '';

    public constructor() {
        afterRenderEffect({
            earlyRead: () => {
                this.synchronizedNativeValue = String(this.internalValue());
            },
        });

        afterNextRender(() => {
            const control = this.control().nativeElement;
            this.monitorAutofill(control);
            this.syncValue(control);
            this.autofillSyncDelaysMs.forEach(delay => {
                this.scheduleAutofillSync(control, delay);
            });
        });

        this.destroyRef.onDestroy(() => {
            this.autofillSyncTimers.forEach(timer => {
                clearTimeout(timer);
            });
        });
    }

    protected readonly sizeClass = computed(() => `fd-ui-input--size-${this.size()}`);
    protected readonly appearanceClass = computed(() => `fd-ui-input--appearance-${this.appearance()}`);
    protected readonly hasError = computed(() => {
        const error = this.resolvedError();

        return typeof error === 'string' && error.trim().length > 0;
    });
    protected readonly hostClass = computed(
        () =>
            `fd-ui-input ${this.sizeClass()} ${this.appearanceClass()}${this.hasError() ? ' fd-ui-input--has-error' : ''}${this.shouldFloatLabel() ? ' fd-ui-input--floating' : ''}`,
    );
    protected readonly isDateInput = computed(() => {
        const type = this.type();
        return type === 'date' || type === 'datetime-local' || type === 'time';
    });
    protected readonly shouldFloatLabel = computed(() => {
        const label = this.label();
        const text = String(this.internalValue()).trim();

        return label !== undefined && label.trim().length > 0 && (this.floatLabel() === 'always' || this.isFocused() || text.length > 0);
    });
    protected readonly shouldShowPlaceholder = computed(() => {
        const text = String(this.internalValue()).trim();
        return (this.floatLabel() === 'always' || this.isFocused()) && text.length === 0;
    });
    protected readonly placeholderAttribute = computed(() => (this.shouldShowPlaceholder() ? (this.placeholder() ?? null) : null));

    protected readonly describedBy = computed(() => {
        const ids = [(this.suffixText() ?? '') !== '' ? `${this.id()}-unit` : null, this.hasError() ? `${this.id()}-error` : null]
            .filter(Boolean)
            .join(' ');
        return ids.length > 0 ? ids : null;
    });

    protected onInput(control: HTMLInputElement): void {
        if (this.disabled()) {
            return;
        }

        this.updateInputValue(control);
    }

    // eslint-disable-next-line local/prefer-protected-template-members -- Public focus contract used by interaction directives.
    public focus(): void {
        if (!this.disabled()) {
            this.host.nativeElement.querySelector<HTMLInputElement>('.fd-ui-input__control')?.focus();
        }
    }

    // eslint-disable-next-line local/prefer-protected-template-members -- Public read contract for consumers of rendered native input.
    public readNativeValue(): string {
        return this.control().nativeElement.value;
    }

    // eslint-disable-next-line local/prefer-protected-template-members -- Public reset contract for a consumer that discards an unfinished number.
    public resetNativeNumberDraft(): void {
        if (!this.nativeNumberDraftInvalid) {
            return;
        }

        const value = this.value() ?? '';
        this.control().nativeElement.value = String(value);
        this.writeDraft(value);
        this.setNativeNumberDraftInvalid(false);
    }

    protected onBlur(): void {
        this.isFocused.set(false);
        this.touched.set(true);
        this.touch.emit();
    }

    protected onFocus(control: HTMLInputElement): void {
        this.syncValue(control);
        this.isFocused.set(true);
        if (this.selectOnFocus() && this.type() === 'text') {
            control.select();
        }
    }

    protected onAnimationStart(event: AnimationEvent, control: HTMLInputElement): void {
        if (event.animationName !== 'fd-ui-input-autofill-start') {
            return;
        }

        this.syncValue(control);
    }

    protected triggerSuffixButton(): void {
        if (this.disabled() || this.suffixButtonIcon() === undefined) {
            return;
        }

        this.suffixButtonClicked.emit();
    }

    protected focusControl(event: MouseEvent, control: HTMLInputElement): void {
        if (this.disabled()) {
            return;
        }

        const hasSuffixAction = event.composedPath().some(target => {
            return target instanceof Element && target.matches('.fd-ui-input__suffix, .fd-ui-input__suffix-content');
        });
        if (hasSuffixAction) {
            return;
        }

        control.focus();
    }

    private syncValue(control: HTMLInputElement): void {
        const value = control.value;
        // The DOM can still show the previous value when focus arrives before rendering a model update.
        if (this.nativeNumberDraftInvalid || value === this.synchronizedNativeValue) {
            return;
        }

        if (this.disabled()) {
            this.writeDraft(value);
            return;
        }
        this.updateInputValue(control);
    }

    private updateInputValue(control: HTMLInputElement): void {
        this.setNativeNumberDraftInvalid(this.preserveInvalidNumberDraft() && control.type === 'number' && control.validity.badInput);
        if (this.nativeNumberDraftInvalid) {
            return;
        }

        const raw = control.value;
        const parsed = this.toModelValue(raw);
        const normalizer = this.numberNormalizer();
        if (normalizer !== undefined && typeof parsed !== 'string' && this.type() === 'number') {
            const normalized = normalizer(parsed);
            const display = normalized === null ? '' : String(normalized);
            this.writeDraft(display);
            control.value = display;
            this.value.set(normalized);
            return;
        }
        this.writeDraft(raw);
        this.value.set(parsed);
    }

    private writeDraft(value: string | number): void {
        this.internalValue.set(value);
        this.synchronizedNativeValue = String(value);
    }

    private setNativeNumberDraftInvalid(invalid: boolean): void {
        if (this.nativeNumberDraftInvalid !== invalid) {
            this.nativeNumberDraftInvalid = invalid;
            this.nativeNumberDraftInvalidChange.emit(invalid);
        }
    }

    private toModelValue(value: string): string | number | null {
        if (this.type() !== 'number') {
            return value;
        }
        if (this.localizedNumber()) {
            return this.parseLocalizedNumber(value);
        }
        const numberValue = Number(value);
        return value.trim() !== '' && Number.isFinite(numberValue) ? numberValue : null;
    }

    private parseLocalizedNumber(value: string): number | null {
        const text = value.trim();
        if (text === '') {
            return null;
        }
        const isDecimal = /^[+-]?\d*[.,]?\d*$/u.test(text) && /\d/u.test(text);
        const numberValue = isDecimal ? Number(text.replace(',', '.')) : Number.NaN;
        return Number.isFinite(numberValue) ? numberValue : Number.NaN;
    }

    private monitorAutofill(control: HTMLInputElement): void {
        this.autofillMonitor
            .monitor(control)
            .pipe(takeUntilDestroyed(this.destroyRef))
            .subscribe(() => {
                this.syncValue(control);
                this.scheduleAutofillSync(control);
            });
    }

    private scheduleAutofillSync(control: HTMLInputElement, delay = 0): void {
        const timer = setTimeout(() => {
            this.autofillSyncTimers.delete(timer);
            this.syncValue(control);
        }, delay);
        this.autofillSyncTimers.add(timer);
    }
}
