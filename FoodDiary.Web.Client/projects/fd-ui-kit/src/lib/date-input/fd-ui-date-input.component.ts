import { CommonModule } from '@angular/common';
import {
  ChangeDetectionStrategy,
  ChangeDetectorRef,
  Component,
  forwardRef,
  ViewEncapsulation,
  input
} from '@angular/core';
import {
    ControlValueAccessor,
    FormControl,
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
    private readonly cdr = inject(ChangeDetectorRef);

    public readonly label = input<string>();
    public readonly placeholder = input('');
    public readonly min = input<Date>();
    public readonly max = input<Date>();
    public readonly floatLabel = input<'auto' | 'always'>('auto');
    public readonly size = input<FdUiFieldSize>('md');
    public readonly hideSubscript = input(false);

    protected readonly dateControl = new FormControl<Date | null>(null);
    protected isDisabled = false;

    private readonly destroyRef = inject(DestroyRef);
    private onChange: (value: Date | null) => void = () => undefined;
    private onTouched: () => void = () => undefined;

    public constructor() {
        this.dateControl.valueChanges
            .pipe(takeUntilDestroyed(this.destroyRef))
            .subscribe(value => {
                this.onChange(value ?? null);
            });
    }

    public writeValue(value: Date | string | number | null): void {
        const coerced = this.toDateValue(value);
        this.dateControl.setValue(coerced, { emitEvent: false });
        this.cdr.markForCheck();
    }

    public registerOnChange(fn: (value: Date | null) => void): void {
        this.onChange = fn;
    }

    public registerOnTouched(fn: () => void): void {
        this.onTouched = fn;
    }

    public setDisabledState(isDisabled: boolean): void {
        this.isDisabled = isDisabled;
        if (isDisabled) {
            this.dateControl.disable({ emitEvent: false });
        } else {
            this.dateControl.enable({ emitEvent: false });
        }
        this.cdr.markForCheck();
    }

    protected handleBlur(): void {
        this.onTouched();
    }

    protected get sizeClass(): string {
        return `fd-ui-date-input--size-${this.size()}`;
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
