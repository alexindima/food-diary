import { CommonModule } from '@angular/common';
import {
    ChangeDetectionStrategy,
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

export interface FdUiDateRangeValue {
    start: Date | null;
    end: Date | null;
}

type FdUiDateInputValue = Date | string | number | FdUiDateRangeValue | null;

@Component({
    selector: 'fd-ui-date-input',
    standalone: true,
    imports: [
        CommonModule,
        ReactiveFormsModule,
        MatFormFieldModule,
        MatInputModule,
        MatDatepickerModule,
        MatNativeDateModule,
    ],
    templateUrl: './fd-ui-date-input.component.html',
    styleUrls: ['./fd-ui-date-input.component.scss'],
    changeDetection: ChangeDetectionStrategy.OnPush,
    encapsulation: ViewEncapsulation.None,
    providers: [
        {
            provide: NG_VALUE_ACCESSOR,
            useExisting: forwardRef((): typeof FdUiDateInputComponent => FdUiDateInputComponent),
            multi: true,
        },
    ],
})
export class FdUiDateInputComponent implements ControlValueAccessor {
    @Input() public label?: string;
    @Input() public placeholder?: string;
    @Input() public startPlaceholder?: string;
    @Input() public endPlaceholder?: string;
    @Input() public mode: 'single' | 'range' = 'single';
    @Input() public min?: Date;
    @Input() public max?: Date;
    @Input() public floatLabel: 'auto' | 'always' = 'auto';

    protected readonly singleControl = new FormControl<Date | null>(null);
    protected readonly rangeGroup = new FormGroup({
        start: new FormControl<Date | null>(null),
        end: new FormControl<Date | null>(null),
    });
    protected isDisabled = false;

    private readonly destroyRef = inject(DestroyRef);
    private onChange: (value: FdUiDateInputValue) => void = () => undefined;
    private onTouched: () => void = () => undefined;

    public constructor() {
        this.singleControl.valueChanges
            .pipe(takeUntilDestroyed(this.destroyRef))
            .subscribe(value => {
                if (this.mode === 'single') {
                    this.onChange(value ?? null);
                }
            });

        this.rangeGroup.valueChanges
            .pipe(takeUntilDestroyed(this.destroyRef))
            .subscribe(value => {
                if (this.mode === 'range') {
                    this.onChange({
                        start: value.start ?? null,
                        end: value.end ?? null,
                    });
                }
            });
    }

    public writeValue(value: FdUiDateInputValue): void {
        if (this.mode === 'range') {
            const coerced = this.toRangeValue(value);
            this.rangeGroup.setValue(coerced, { emitEvent: false });
        } else {
            const coerced = this.toDateValue(value);
            this.singleControl.setValue(coerced, { emitEvent: false });
        }
    }

    public registerOnChange(fn: (value: FdUiDateInputValue) => void): void {
        this.onChange = fn;
    }

    public registerOnTouched(fn: () => void): void {
        this.onTouched = fn;
    }

    public setDisabledState(isDisabled: boolean): void {
        this.isDisabled = isDisabled;
        if (isDisabled) {
            this.singleControl.disable({ emitEvent: false });
            this.rangeGroup.disable({ emitEvent: false });
        } else {
            this.singleControl.enable({ emitEvent: false });
            this.rangeGroup.enable({ emitEvent: false });
        }
    }

    protected handleBlur(): void {
        this.onTouched();
    }

    protected get resolvedStartPlaceholder(): string {
        return this.startPlaceholder ?? this.placeholder ?? '';
    }

    protected get resolvedEndPlaceholder(): string {
        return this.endPlaceholder ?? this.placeholder ?? '';
    }

    protected get resolvedPlaceholder(): string {
        return this.placeholder ?? '';
    }

    private toDateValue(value: FdUiDateInputValue): Date | null {
        if (!value) {
            return null;
        }

        if (value instanceof Date) {
            return value;
        }

        if (this.isRangeValue(value)) {
            return value.start ? this.toDateValue(value.start) : null;
        }

        const date = new Date(value);
        return Number.isNaN(date.getTime()) ? null : date;
    }

    private toRangeValue(value: FdUiDateInputValue): FdUiDateRangeValue {
        if (this.isRangeValue(value)) {
            return {
                start: this.toDateValue(value.start ?? null),
                end: this.toDateValue(value.end ?? null),
            };
        }

        const date = this.toDateValue(value);
        return {
            start: date,
            end: null,
        };
    }

    private isRangeValue(value: unknown): value is FdUiDateRangeValue {
        if (!value || typeof value !== 'object') {
            return false;
        }
        const maybeRange = value as Record<string, unknown>;
        return maybeRange.hasOwnProperty('start') && maybeRange.hasOwnProperty('end');
    }
}
