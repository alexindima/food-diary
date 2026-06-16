import { ChangeDetectionStrategy, Component, effect, input, output, untracked } from '@angular/core';
import { type FieldTree, FormField } from '@angular/forms/signals';
import { FdUiDateRangeInputComponent } from 'fd-ui-kit/date-range-input/fd-ui-date-range-input';
import { type FdUiTab, FdUiTabsComponent } from 'fd-ui-kit/tabs/fd-ui-tabs';

type DateRangeValue = { start: Date | null; end: Date | null } | null;

@Component({
    selector: 'fd-period-filter',
    imports: [FdUiTabsComponent, FdUiDateRangeInputComponent, FormField],
    templateUrl: './period-filter.html',
    styleUrls: ['./period-filter.scss'],
    changeDetection: ChangeDetectionStrategy.OnPush,
})
export class PeriodFilterComponent {
    public readonly tabs = input.required<FdUiTab[]>();
    public readonly selectedValue = input.required<string>();
    public readonly rangeField = input.required<FieldTree<DateRangeValue>>();
    public readonly displayRange = input<{ start: Date; end: Date } | null>(null);
    public readonly startLabel = input<string>();
    public readonly endLabel = input<string>();
    public readonly startPlaceholder = input<string>();
    public readonly endPlaceholder = input<string>();

    public readonly rangeChange = output<string>();

    public constructor() {
        effect(() => {
            const field = this.rangeField();
            const value = this.selectedValue();
            const range = this.displayRange();

            if (value === 'custom') {
                return;
            }

            if (range !== null) {
                const currentRange = untracked(() => field().value());
                if (areDateRangesEqual(currentRange, range)) {
                    return;
                }

                field().value.set({ start: range.start, end: range.end });
            }
        });
    }

    protected onRangeChange(value: unknown): void {
        if (typeof value === 'string') {
            this.rangeChange.emit(value);
        }
    }
}

function areDateRangesEqual(currentRange: DateRangeValue, nextRange: { start: Date; end: Date }): boolean {
    return currentRange !== null && areDatesEqual(currentRange.start, nextRange.start) && areDatesEqual(currentRange.end, nextRange.end);
}

function areDatesEqual(currentDate: Date | null, nextDate: Date): boolean {
    return currentDate !== null && currentDate.getTime() === nextDate.getTime();
}
