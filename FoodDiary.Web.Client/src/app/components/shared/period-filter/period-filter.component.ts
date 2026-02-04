import { ChangeDetectionStrategy, Component, effect, input, output } from '@angular/core';
import { FdUiTabsComponent, FdUiTab } from 'fd-ui-kit/tabs/fd-ui-tabs.component';
import { FdUiPlainDateRangeInputComponent } from 'fd-ui-kit/plain-date-range-input/fd-ui-plain-date-range-input.component';
import { FormControl, ReactiveFormsModule } from '@angular/forms';

type DateRangeValue = { start: Date | null; end: Date | null } | null;

@Component({
    selector: 'fd-period-filter',
    standalone: true,
    imports: [FdUiTabsComponent, FdUiPlainDateRangeInputComponent, ReactiveFormsModule],
    templateUrl: './period-filter.component.html',
    styleUrls: ['./period-filter.component.scss'],
    changeDetection: ChangeDetectionStrategy.OnPush,
})
export class PeriodFilterComponent {
    public readonly tabs = input.required<FdUiTab[]>();
    public readonly selectedValue = input.required<string>();
    public readonly rangeControl = input.required<FormControl<DateRangeValue>>();
    public readonly displayRange = input<{ start: Date; end: Date } | null>(null);
    public readonly startLabel = input<string>();
    public readonly endLabel = input<string>();
    public readonly startPlaceholder = input<string>();
    public readonly endPlaceholder = input<string>();

    public readonly rangeChange = output<string>();

    public constructor() {
        effect(() => {
            const control = this.rangeControl();
            const value = this.selectedValue();
            const range = this.displayRange();

            if (!control) {
                return;
            }

            if (value === 'custom') {
                if (control.disabled) {
                    control.enable({ emitEvent: false });
                }
                return;
            }

            if (range?.start && range?.end) {
                control.setValue({ start: range.start, end: range.end }, { emitEvent: false });
            }

            if (control.enabled) {
                control.disable({ emitEvent: false });
            }
        });
    }

    public onRangeChange(value: unknown): void {
        if (typeof value === 'string') {
            this.rangeChange.emit(value);
        }
    }
}
