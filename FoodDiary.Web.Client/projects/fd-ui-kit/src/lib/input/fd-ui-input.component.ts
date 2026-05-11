import { AutofillMonitor } from '@angular/cdk/text-field';
import { CommonModule } from '@angular/common';
import { ChangeDetectionStrategy, Component, computed, inject, input, output, signal } from '@angular/core';
import { afterNextRender, DestroyRef, type ElementRef, viewChild } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { type ControlValueAccessor, NG_VALUE_ACCESSOR } from '@angular/forms';

import { FdUiIconComponent } from '../icon/fd-ui-icon.component';
import type { FdUiFieldSize } from '../types/field-size.type';

const AUTOFILL_SYNC_FAST_DELAY_MS = 100;
const AUTOFILL_SYNC_SHORT_DELAY_MS = 500;
const AUTOFILL_SYNC_MEDIUM_DELAY_MS = 1000;
const AUTOFILL_SYNC_LONG_DELAY_MS = 2500;
const AUTOFILL_SYNC_FINAL_DELAY_MS = 5000;
const AUTOFILL_SYNC_DELAYS_MS = [
    AUTOFILL_SYNC_FAST_DELAY_MS,
    AUTOFILL_SYNC_SHORT_DELAY_MS,
    AUTOFILL_SYNC_MEDIUM_DELAY_MS,
    AUTOFILL_SYNC_LONG_DELAY_MS,
    AUTOFILL_SYNC_FINAL_DELAY_MS,
] as const;

let uniqueId = 0;
export type FdUiInputAppearance = 'default' | 'auth' | 'search' | 'inline-edit';

@Component({
    selector: 'fd-ui-input',
    standalone: true,
    imports: [CommonModule, FdUiIconComponent],
    templateUrl: './fd-ui-input.component.html',
    styleUrls: ['./fd-ui-input.component.scss'],
    changeDetection: ChangeDetectionStrategy.OnPush,
    providers: [
        {
            provide: NG_VALUE_ACCESSOR,
            useExisting: FdUiInputComponent,
            multi: true,
        },
    ],
})
export class FdUiInputComponent implements ControlValueAccessor {
    private readonly destroyRef = inject(DestroyRef);
    private readonly autofillMonitor = inject(AutofillMonitor);
    private readonly control = viewChild<ElementRef<HTMLInputElement>>('control');

    public readonly id = input(`fd-ui-input-${uniqueId++}`);
    public readonly label = input<string>();
    public readonly placeholder = input<string>();
    public readonly type = input<'text' | 'number' | 'password' | 'email' | 'tel' | 'date' | 'datetime-local' | 'time'>('text');
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

    public readonly suffixButtonClicked = output<void>();

    protected readonly internalValue = signal<string | number>('');
    protected readonly disabled = signal(false);
    protected readonly isFocused = signal(false);

    private onChange: (value: string) => void = () => undefined;
    private onTouched: () => void = () => undefined;
    private autofillSyncTimers: Array<ReturnType<typeof setTimeout>> = [];

    public constructor() {
        afterNextRender(() => {
            this.monitorAutofill();
            this.syncNativeValue();
            this.autofillSyncTimers = AUTOFILL_SYNC_DELAYS_MS.map(delay =>
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
    protected readonly hostClass = computed(
        () =>
            `fd-ui-input ${this.sizeClass()} ${this.appearanceClass()}${this.error() !== null ? ' fd-ui-input--has-error' : ''}${this.shouldFloatLabel() ? ' fd-ui-input--floating' : ''}`,
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

    public writeValue(value: string | number | null): void {
        this.internalValue.set(value ?? '');
    }

    public registerOnChange(fn: (value: string) => void): void {
        this.onChange = fn;
    }

    public registerOnTouched(fn: () => void): void {
        this.onTouched = fn;
    }

    public setDisabledState(isDisabled: boolean): void {
        this.disabled.set(isDisabled);
    }

    protected onInput(value: string): void {
        if (this.disabled()) {
            return;
        }

        this.internalValue.set(value);
        this.onChange(value);
    }

    protected onBlur(): void {
        this.isFocused.set(false);
        this.onTouched();
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

        const target = event.target as HTMLElement | null;
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
            this.onChange(value);
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
