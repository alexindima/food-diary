import { CommonModule } from '@angular/common';
import { ChangeDetectionStrategy, ChangeDetectorRef, Component, effect, inject, input, signal } from '@angular/core';
import { type ControlValueAccessor, NG_VALUE_ACCESSOR } from '@angular/forms';
import { disabled as disabledRule, form, FormField } from '@angular/forms/signals';

import { fdUiFormatDateInputValue, fdUiParseLocalDate } from '../date/fd-ui-date.utils';
import { FdUiDateInputComponent } from '../date-input/fd-ui-date-input';
import type { FdUiFieldSize } from '../types/field-size.type';

export type FdUiDateRangeValue = {
    start: Date | null;
    end: Date | null;
};

@Component({
    selector: 'fd-ui-date-range-input',
    imports: [CommonModule, FormField, FdUiDateInputComponent],
    templateUrl: './fd-ui-date-range-input.html',
    styleUrls: ['./fd-ui-date-range-input.scss'],
    changeDetection: ChangeDetectionStrategy.OnPush,
    providers: [
        {
            provide: NG_VALUE_ACCESSOR,
            useExisting: FdUiDateRangeInputComponent,
            multi: true,
        },
    ],
})
export class FdUiDateRangeInputComponent implements ControlValueAccessor {
    private readonly cdr = inject(ChangeDetectorRef);

    public readonly startPlaceholder = input<string>();
    public readonly endPlaceholder = input<string>();
    public readonly startLabel = input<string>();
    public readonly endLabel = input<string>();
    public readonly size = input<FdUiFieldSize>('md');
    public readonly disabled = input(false);

    protected readonly disabledState = signal(false);
    protected readonly rangeModel = signal<{ start: string | null; end: string | null }>({
        start: null,
        end: null,
    });
    protected readonly rangeForm = form(this.rangeModel, path => {
        disabledRule(path.start, { when: () => this.disabledState() || this.disabled() });
        disabledRule(path.end, { when: () => this.disabledState() || this.disabled() });
    });
    private skippedModelUpdate: string | null = null;

    private onChange: (value: FdUiDateRangeValue) => void = () => {};
    private onTouched: () => void = () => {};

    public constructor() {
        effect(() => {
            const value = this.rangeModel();
            const serialized = JSON.stringify(value);
            if (serialized === this.skippedModelUpdate) {
                this.skippedModelUpdate = null;
                return;
            }

            this.onChange({
                start: this.toDateValue(value.start),
                end: this.toDateValue(value.end),
            });
        });
    }

    public writeValue(value: FdUiDateRangeValue | null | undefined): void {
        const coerced = this.toRangeValue(value ?? null);
        this.skippedModelUpdate = JSON.stringify(coerced);
        this.rangeModel.set(coerced);
        this.cdr.markForCheck();
    }

    public registerOnChange(fn: (value: FdUiDateRangeValue) => void): void {
        this.onChange = fn;
    }

    public registerOnTouched(fn: () => void): void {
        this.onTouched = fn;
    }

    public setDisabledState(isDisabled: boolean): void {
        this.disabledState.set(isDisabled);
        this.cdr.markForCheck();
    }

    protected touchRange(): void {
        this.onTouched();
    }

    private toRangeValue(value: FdUiDateRangeValue | null): { start: string | null; end: string | null } {
        if (value === null) {
            return { start: null, end: null };
        }

        return {
            start: this.formatDate(value.start),
            end: this.formatDate(value.end),
        };
    }

    private toDateValue(value: string | Date | null | undefined): Date | null {
        return fdUiParseLocalDate(value);
    }

    private formatDate(value: Date | null | undefined): string | null {
        if (value === null || value === undefined) {
            return null;
        }

        const date = fdUiParseLocalDate(value);
        if (date === null) {
            return null;
        }

        return fdUiFormatDateInputValue(date);
    }
}
