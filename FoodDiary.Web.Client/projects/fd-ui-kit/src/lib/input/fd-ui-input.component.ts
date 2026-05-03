import { AutofillMonitor } from '@angular/cdk/text-field';
import { CommonModule } from '@angular/common';
import { ChangeDetectionStrategy, ChangeDetectorRef, Component, forwardRef, inject, input, output } from '@angular/core';
import { afterNextRender, DestroyRef, type ElementRef, viewChild } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { type ControlValueAccessor, NG_VALUE_ACCESSOR } from '@angular/forms';

import { FdUiIconComponent } from '../icon/fd-ui-icon.component';
import { type FdUiFieldSize } from '../types/field-size.type';

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
            useExisting: forwardRef((): typeof FdUiInputComponent => FdUiInputComponent),
            multi: true,
        },
    ],
})
export class FdUiInputComponent implements ControlValueAccessor {
    private readonly cdr = inject(ChangeDetectorRef);
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

    protected internalValue: string | number = '';
    protected disabled = false;
    protected isFocused = false;

    private onChange: (value: string) => void = () => undefined;
    private onTouched: () => void = () => undefined;
    private autofillSyncTimers: Array<ReturnType<typeof setTimeout>> = [];

    public constructor() {
        afterNextRender(() => {
            this.monitorAutofill();
            this.syncNativeValue();
            this.autofillSyncTimers = [100, 500, 1000, 2500, 5000].map(delay =>
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

    protected get sizeClass(): string {
        return `fd-ui-input--size-${this.size()}`;
    }

    protected get appearanceClass(): string {
        return `fd-ui-input--appearance-${this.appearance()}`;
    }

    protected get isDateInput(): boolean {
        const type = this.type();
        return type === 'date' || type === 'datetime-local' || type === 'time';
    }

    public writeValue(value: string | number | null): void {
        this.internalValue = value ?? '';
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

    protected onInput(value: string): void {
        if (this.disabled) {
            return;
        }

        this.internalValue = value;
        this.onChange(value);
    }

    protected onBlur(): void {
        this.isFocused = false;
        this.onTouched();
    }

    protected onFocus(): void {
        this.syncNativeValue();
        this.isFocused = true;
    }

    protected onAnimationStart(event: AnimationEvent, value: string): void {
        if (event.animationName !== 'fd-ui-input-autofill-start') {
            return;
        }

        this.syncValue(value);
    }

    protected get shouldFloatLabel(): boolean {
        const text = String(this.internalValue ?? '').trim();
        return this.isFocused || text.length > 0;
    }

    protected get shouldShowPlaceholder(): boolean {
        const text = String(this.internalValue ?? '').trim();
        return this.isFocused && text.length === 0;
    }

    protected triggerSuffixButton(): void {
        if (this.disabled || !this.suffixButtonIcon()) {
            return;
        }

        this.suffixButtonClicked.emit();
    }

    protected focusControl(event: MouseEvent, control: HTMLInputElement): void {
        if (this.disabled) {
            return;
        }

        const target = event.target as HTMLElement | null;
        if (target?.closest('.fd-ui-input__suffix')) {
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
        if (value === String(this.internalValue ?? '')) {
            return;
        }

        this.internalValue = value;

        if (!this.disabled) {
            this.onChange(value);
        }

        this.cdr.markForCheck();
    }

    private monitorAutofill(): void {
        const control = this.control();

        if (!control) {
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
