import { ChangeDetectionStrategy, Component, input, output, viewChild } from '@angular/core';
import { MatNativeDateModule } from '@angular/material/core';
import { MatDatepicker, MatDatepickerInputEvent, MatDatepickerModule } from '@angular/material/datepicker';
import { MatInputModule } from '@angular/material/input';
import { FdUiHintDirective } from '../hint/fd-ui-hint.directive';
import { FdUiButtonComponent } from '../button/fd-ui-button.component';

@Component({
    selector: 'fd-ui-date-picker-button',
    standalone: true,
    imports: [MatDatepickerModule, MatNativeDateModule, MatInputModule, FdUiHintDirective, FdUiButtonComponent],
    templateUrl: './fd-ui-date-picker-button.component.html',
    styleUrl: './fd-ui-date-picker-button.component.scss',
    changeDetection: ChangeDetectionStrategy.OnPush,
})
export class FdUiDatePickerButtonComponent {
    public readonly value = input<Date | null>(null);
    public readonly ariaLabel = input<string>('');
    public readonly hint = input<string | null>(null);
    public readonly icon = input('calendar_today');

    public readonly valueChange = output<Date>();

    private readonly picker = viewChild.required(MatDatepicker<Date>);

    public open(): void {
        this.picker().open();
    }

    public onDateChange(event: MatDatepickerInputEvent<Date>): void {
        if (event.value) {
            this.valueChange.emit(event.value);
        }
    }
}
