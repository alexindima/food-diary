import { CommonModule } from '@angular/common';
import {
    ChangeDetectionStrategy,
    ChangeDetectorRef,
    Component,
    forwardRef,
    Input,
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
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatInputModule } from '@angular/material/input';
import { MatDatepickerModule } from '@angular/material/datepicker';
import { MatNativeDateModule } from '@angular/material/core';
import { FdUiFieldSize } from '../types/field-size.type';

export interface FdUiDateRangeValue {
    start: Date | null;
    end: Date | null;
}

@Component({
    selector: 'fd-ui-date-range-input',
    standalone: true,
    imports: [
        CommonModule,
        ReactiveFormsModule,
        MatFormFieldModule,
        MatInputModule,
        MatDatepickerModule,
        MatNativeDateModule,
    ],
    templateUrl: './fd-ui-date-range-input.component.html',
    styleUrls: ['./fd-ui-date-range-input.component.scss'],
    changeDetection: ChangeDetectionStrategy.OnPush,
    encapsulation: ViewEncapsulation.None,
    providers: [
        {
            provide: NG_VALUE_ACCESSOR,
            useExisting: forwardRef((): typeof FdUiDateRangeInputComponent => FdUiDateRangeInputComponent),
            multi: true,
        },
    ],
})
export class FdUiDateRangeInputComponent implements ControlValueAccessor {
    @Input() public label?: string;
    @Input() public startPlaceholder = '';
    @Input() public endPlaceholder = '';
    @Input() public min?: Date;
    @Input() public max?: Date;
    @Input() public floatLabel: 'auto' | 'always' = 'auto';
    @Input() public size: FdUiFieldSize = 'md';
    @Input() public hideSubscript = false;

    protected readonly rangeGroup = new FormGroup({
        start: new FormControl<Date | null>(null),
        end: new FormControl<Date | null>(null),
    });
    protected isDisabled = false;

    private readonly destroyRef = inject(DestroyRef);
    private onChange: (value: FdUiDateRangeValue) => void = () => undefined;
    private onTouched: () => void = () => undefined;

    public constructor(private readonly cdr: ChangeDetectorRef) {
        this.rangeGroup.valueChanges
            .pipe(takeUntilDestroyed(this.destroyRef))
            .subscribe(value => {
                this.onChange({
                    start: value.start ?? null,
                    end: value.end ?? null,
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

    protected get sizeClass(): string {
        return `fd-ui-date-range-input--size-${this.size}`;
    }

    private toRangeValue(value: FdUiDateRangeValue | null): FdUiDateRangeValue {
        if (!value) {
            return { start: null, end: null };
        }

        return {
            start: this.toDateValue(value.start),
            end: this.toDateValue(value.end),
        };
    }

    private toDateValue(value: Date | string | number | null | undefined): Date | null {
        if (!value) {
            return null;
        }

        if (value instanceof Date) {
            return value;
        }

        const date = new Date(value);
        return Number.isNaN(date.getTime()) ? null : date;
    }
}
