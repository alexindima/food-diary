import { CommonModule } from '@angular/common';
import {
    ChangeDetectionStrategy,
    ChangeDetectorRef,
    Component,
    forwardRef,
    input,
    ViewEncapsulation,
} from '@angular/core';
import {
    ControlValueAccessor,
    FormControl,
    FormGroup,
    NG_VALUE_ACCESSOR,
    ReactiveFormsModule,
} from '@angular/forms';
import { DestroyRef, inject } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { FdUiFieldSize } from '../types/field-size.type';
import { FdUiPlainDateInputComponent } from '../plain-date-input/fd-ui-plain-date-input.component';

export interface FdUiPlainDateRangeValue {
    start: Date | null;
    end: Date | null;
}

@Component({
    selector: 'fd-ui-plain-date-range-input',
    standalone: true,
    imports: [CommonModule, ReactiveFormsModule, FdUiPlainDateInputComponent],
    templateUrl: './fd-ui-plain-date-range-input.component.html',
    styleUrls: ['./fd-ui-plain-date-range-input.component.scss'],
    changeDetection: ChangeDetectionStrategy.OnPush,
    encapsulation: ViewEncapsulation.None,
    providers: [
        {
            provide: NG_VALUE_ACCESSOR,
            useExisting: forwardRef((): typeof FdUiPlainDateRangeInputComponent => FdUiPlainDateRangeInputComponent),
            multi: true,
        },
    ],
})
export class FdUiPlainDateRangeInputComponent implements ControlValueAccessor {
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

    private onChange: (value: FdUiPlainDateRangeValue) => void = () => undefined;
    private onTouched: () => void = () => undefined;

    public constructor() {
        this.rangeGroup.valueChanges
            .pipe(takeUntilDestroyed(this.destroyRef))
            .subscribe(value => {
                this.onChange({
                    start: this.toDateValue(value.start),
                    end: this.toDateValue(value.end),
                });
            });
    }

    public writeValue(value: FdUiPlainDateRangeValue | null): void {
        const coerced = this.toRangeValue(value);
        this.rangeGroup.setValue(coerced, { emitEvent: false });
        this.cdr.markForCheck();
    }

    public registerOnChange(fn: (value: FdUiPlainDateRangeValue) => void): void {
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

    private toRangeValue(value: FdUiPlainDateRangeValue | null): { start: string | null; end: string | null } {
        if (!value) {
            return { start: null, end: null };
        }

        return {
            start: this.formatDate(value.start),
            end: this.formatDate(value.end),
        };
    }

    private toDateValue(value: string | Date | null | undefined): Date | null {
        if (!value) {
            return null;
        }

        if (value instanceof Date) {
            return value;
        }

        const date = new Date(value);
        return Number.isNaN(date.getTime()) ? null : date;
    }

    private formatDate(value: Date | null | undefined): string | null {
        if (!value) {
            return null;
        }

        const date = value instanceof Date ? value : new Date(value);
        if (Number.isNaN(date.getTime())) {
            return null;
        }

        const year = date.getFullYear();
        const month = String(date.getMonth() + 1).padStart(2, '0');
        const day = String(date.getDate()).padStart(2, '0');
        return `${year}-${month}-${day}`;
    }
}
