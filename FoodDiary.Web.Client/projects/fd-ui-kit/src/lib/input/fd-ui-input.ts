import { AutofillMonitor } from '@angular/cdk/text-field';
import { CommonModule } from '@angular/common';
import { ChangeDetectionStrategy, Component, computed, effect, inject, input, model, output, signal } from '@angular/core';
import { afterNextRender, DestroyRef, type ElementRef, viewChild } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import type { FormValueControl } from '@angular/forms/signals';

import { FdUiIconComponent } from '../icon/fd-ui-icon';
import type { FdUiFieldSize } from '../types/field-size.type';
import { FD_UI_INPUT_AUTOFILL_SYNC_DELAYS_MS } from './fd-ui-input.tokens';

let uniqueId = 0;
export type FdUiInputAppearance = 'default' | 'auth' | 'search' | 'inline-edit';
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

@Component({
    selector: 'fd-ui-input',
    imports: [CommonModule, FdUiIconComponent],
    templateUrl: './fd-ui-input.html',
    styleUrls: ['./fd-ui-input.scss'],
    changeDetection: ChangeDetectionStrategy.OnPush,
})
export class FdUiInputComponent implements FormValueControl<string | number | null> {
    private readonly destroyRef = inject(DestroyRef);
    private readonly autofillMonitor = inject(AutofillMonitor);
    private readonly autofillSyncDelaysMs = inject(FD_UI_INPUT_AUTOFILL_SYNC_DELAYS_MS);
    private readonly control = viewChild<ElementRef<HTMLInputElement>>('control');

    public readonly id = input(`fd-ui-input-${uniqueId++}`);
    public readonly label = input<string>();
    public readonly placeholder = input<string>();
    public readonly type = input<'text' | 'number' | 'password' | 'email' | 'tel' | 'date' | 'datetime-local' | 'time'>('text');
    public readonly autocomplete = input<FdUiInputAutocomplete>();
    public readonly error = input<string | null>();
    public readonly required = input(false);
    public readonly readonly = input(false);
    public readonly maxLength = input<number>();
    public readonly suffixButtonIcon = input<string>();
    public readonly suffixButtonAriaLabel = input<string>();
    public readonly prefixIcon = input<string>();
    public readonly step = input<string | number>();
    public readonly size = input<FdUiFieldSize>('md');
    public readonly fillColor = input<string | null>(null);
    public readonly appearance = input<FdUiInputAppearance>('default');
    public readonly value = model<string | number | null>(null);
    public readonly touched = model(false);
    public readonly disabled = input(false);

    public readonly suffixButtonClicked = output();

    protected readonly internalValue = signal<string | number>('');
    protected readonly isFocused = signal(false);

    private autofillSyncTimers: Array<ReturnType<typeof setTimeout>> = [];

    public constructor() {
        effect(() => {
            this.internalValue.set(this.value() ?? '');
        });

        afterNextRender(() => {
            this.monitorAutofill();
            this.syncNativeValue();
            this.autofillSyncTimers = this.autofillSyncDelaysMs.map(delay =>
                setTimeout(() => {
                    this.syncNativeValue();
                }, delay),
            );
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
        const error = this.error();

        return error !== null && error !== undefined && error.trim().length > 0;
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
        const text = String(this.internalValue()).trim();
        return this.isFocused() || text.length > 0;
    });
    protected readonly shouldShowPlaceholder = computed(() => {
        const text = String(this.internalValue()).trim();
        return this.isFocused() && text.length === 0;
    });
    protected readonly placeholderAttribute = computed(() => (this.shouldShowPlaceholder() ? (this.placeholder() ?? null) : null));

    protected onInput(value: string): void {
        if (this.disabled()) {
            return;
        }

        this.internalValue.set(value);
        this.value.set(value);
    }

    protected onBlur(): void {
        this.isFocused.set(false);
        this.touched.set(true);
    }

    protected onFocus(): void {
        this.syncNativeValue();
        this.isFocused.set(true);
    }

    protected onAnimationStart(event: AnimationEvent, value: string): void {
        if (event.animationName !== 'fd-ui-input-autofill-start') {
            return;
        }

        this.syncValue(value);
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

        const target = event.target instanceof HTMLElement ? event.target : null;
        if (target?.closest('.fd-ui-input__suffix') !== null && target?.closest('.fd-ui-input__suffix') !== undefined) {
            return;
        }

        control.focus();
    }

    private syncNativeValue(): void {
        const nativeValue = this.control()?.nativeElement.value;

        if (nativeValue === undefined) {
            return;
        }

        this.syncValue(nativeValue);
    }

    private syncValue(value: string): void {
        if (value === String(this.internalValue())) {
            return;
        }

        this.internalValue.set(value);

        if (!this.disabled()) {
            this.value.set(value);
        }
    }

    private monitorAutofill(): void {
        const control = this.control();

        if (control === undefined) {
            return;
        }

        this.autofillMonitor
            .monitor(control)
            .pipe(takeUntilDestroyed(this.destroyRef))
            .subscribe(() => {
                this.syncNativeValue();
                setTimeout(() => {
                    this.syncNativeValue();
                });
            });
    }
}
