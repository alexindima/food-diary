import { CommonModule } from '@angular/common';
import {
    ChangeDetectionStrategy,
    Component,
    DestroyRef,
    ElementRef,
    forwardRef,
    inject,
    input,
    ViewEncapsulation,
} from '@angular/core';
import { ControlValueAccessor, FormControl, NG_VALUE_ACCESSOR, ReactiveFormsModule } from '@angular/forms';
import { MatDatepicker, MatDatepickerModule } from '@angular/material/datepicker';
import { MatNativeDateModule } from '@angular/material/core';
import { MatIconModule } from '@angular/material/icon';
import { MatInputModule } from '@angular/material/input';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { FdUiFieldSize } from '../types/field-size.type';

let uniqueId = 0;

@Component({
    selector: 'fd-ui-plain-date-input',
    standalone: true,
    imports: [
        CommonModule,
        ReactiveFormsModule,
        MatDatepickerModule,
        MatNativeDateModule,
        MatInputModule,
        MatIconModule,
    ],
    templateUrl: './fd-ui-plain-date-input.component.html',
    styleUrls: ['./fd-ui-plain-date-input.component.scss'],
    changeDetection: ChangeDetectionStrategy.OnPush,
    encapsulation: ViewEncapsulation.None,
    providers: [
        {
            provide: NG_VALUE_ACCESSOR,
            useExisting: forwardRef((): typeof FdUiPlainDateInputComponent => FdUiPlainDateInputComponent),
            multi: true,
        },
    ],
})
export class FdUiPlainDateInputComponent implements ControlValueAccessor {
    public readonly id = input(`fd-ui-plain-date-input-${uniqueId++}`);
    public readonly label = input<string>();
    public readonly placeholder = input<string>();
    public readonly error = input<string | null>();
    public readonly required = input(false);
    public readonly size = input<FdUiFieldSize>('md');

    protected readonly dateControl = new FormControl<Date | null>(null);
    protected disabled = false;
    protected isFocused = false;

    private readonly destroyRef = inject(DestroyRef);
    private readonly host = inject(ElementRef<HTMLElement>);

    private onChange: (value: string | null) => void = () => undefined;
    private onTouched: () => void = () => undefined;

    public constructor() {
        this.dateControl.valueChanges
            .pipe(takeUntilDestroyed(this.destroyRef))
            .subscribe(date => this.emitValue(date));
    }

    protected get sizeClass(): string {
        return `fd-ui-plain-date-input--size-${this.size()}`;
    }

    protected get shouldFloatLabel(): boolean {
        return this.isFocused || !!this.dateControl.value;
    }

    protected get shouldShowPlaceholder(): boolean {
        return this.isFocused && !this.dateControl.value;
    }

    public writeValue(value: string | Date | null): void {
        if (!value) {
            this.dateControl.setValue(null, { emitEvent: false });
            return;
        }

        const parsed = typeof value === 'string' ? this.parseDateString(value) : value;
        if (!parsed) {
            this.dateControl.setValue(null, { emitEvent: false });
            return;
        }

        this.dateControl.setValue(parsed, { emitEvent: false });
    }

    public registerOnChange(fn: (value: string | null) => void): void {
        this.onChange = fn;
    }

    public registerOnTouched(fn: () => void): void {
        this.onTouched = fn;
    }

    public setDisabledState(isDisabled: boolean): void {
        this.disabled = isDisabled;
        if (isDisabled) {
            this.dateControl.disable({ emitEvent: false });
        } else {
            this.dateControl.enable({ emitEvent: false });
        }
    }

    protected openDatePicker(picker: MatDatepicker<Date>): void {
        if (this.disabled) {
            return;
        }
        picker.open();
    }

    protected onFocusIn(): void {
        this.isFocused = true;
    }

    protected onFocusOut(): void {
        const active = document.activeElement;
        if (active && this.host.nativeElement.contains(active)) {
            return;
        }
        this.isFocused = false;
        this.onTouched();
    }

    private emitValue(date: Date | null): void {
        if (!date) {
            this.onChange(null);
            return;
        }

        const formatted = [
            date.getFullYear(),
            this.padNumber(date.getMonth() + 1),
            this.padNumber(date.getDate()),
        ].join('-');
        this.onChange(formatted);
    }

    private parseDateString(value: string): Date | null {
        const match = value.match(/^(\d{4})-(\d{2})-(\d{2})/);
        if (!match) {
            const date = new Date(value);
            return Number.isNaN(date.getTime()) ? null : date;
        }
        const year = Number(match[1]);
        const month = Number(match[2]);
        const day = Number(match[3]);
        return new Date(year, month - 1, day);
    }

    private padNumber(value: number): string {
        return value.toString().padStart(2, '0');
    }
}
