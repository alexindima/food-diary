import { CommonModule } from '@angular/common';
import { ChangeDetectionStrategy, ChangeDetectorRef, Component, input } from '@angular/core';
import { DestroyRef, inject } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { type ControlValueAccessor, FormControl, FormGroup, NG_VALUE_ACCESSOR, ReactiveFormsModule } from '@angular/forms';

import { fdUiFormatDateInputValue, fdUiParseLocalDate } from '../date/fd-ui-date.utils';
import { FdUiDateInputComponent } from '../date-input/fd-ui-date-input.component';
import type { FdUiFieldSize } from '../types/field-size.type';

export type FdUiDateRangeValue = {
    start: Date | null;
    end: Date | null;
};

@Component({
    selector: 'fd-ui-date-range-input',
    standalone: true,
    imports: [CommonModule, ReactiveFormsModule, FdUiDateInputComponent],
    templateUrl: './fd-ui-date-range-input.component.html',
    styleUrls: ['./fd-ui-date-range-input.component.scss'],
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
    private readonly destroyRef = inject(DestroyRef);

    public readonly startPlaceholder = input<string>();
    public readonly endPlaceholder = input<string>();
    public readonly startLabel = input<string>();
    public readonly endLabel = input<string>();
    public readonly size = input<FdUiFieldSize>('md');

    protected readonly rangeGroup = new FormGroup({
        start: new FormControl<string | null>(null),
        end: new FormControl<string | null>(null),
    });
    protected isDisabled = false;

    private onChange: (value: FdUiDateRangeValue) => void = () => undefined;
    private onTouched: () => void = () => undefined;

    public constructor() {
        this.rangeGroup.valueChanges.pipe(takeUntilDestroyed(this.destroyRef)).subscribe(value => {
            this.onChange({
                start: this.toDateValue(value.start),
                end: this.toDateValue(value.end),
            });
        });
    }

    public writeValue(value: FdUiDateRangeValue | null): void {
        const coerced = this.toRangeValue(value);
        this.rangeGroup.setValue(coerced, { emitEvent: false });
        this.cdr.markForCheck();
    }

    public registerOnChange(fn: (value: FdUiDateRangeValue) => void): void {
        this.onChange = fn;
    }

    public registerOnTouched(fn: () => void): void {
        this.onTouched = fn;
    }

    public setDisabledState(isDisabled: boolean): void {
        this.isDisabled = isDisabled;
        if (isDisabled) {
            this.rangeGroup.disable({ emitEvent: false });
        } else {
            this.rangeGroup.enable({ emitEvent: false });
        }
        this.cdr.markForCheck();
    }

    protected handleBlur(): void {
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
