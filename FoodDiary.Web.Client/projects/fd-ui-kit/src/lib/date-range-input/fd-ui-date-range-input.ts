import { CommonModule } from '@angular/common';
import { ChangeDetectionStrategy, Component, computed, input, model, output } from '@angular/core';
import type { FormValueControl } from '@angular/forms/signals';

import { fdUiFormatDateInputValue, fdUiParseLocalDate } from '../date/fd-ui-date.utils';
import { FdUiDateInputComponent } from '../date-input/fd-ui-date-input';
import { injectFdUiControlFocus } from '../form/fd-ui-control-focus';
import type { FdUiFieldSize } from '../types/field-size.type';

export type FdUiDateRangeValue = {
    start: Date | null;
    end: Date | null;
};

@Component({
    selector: 'fd-ui-date-range-input',
    imports: [CommonModule, FdUiDateInputComponent],
    templateUrl: './fd-ui-date-range-input.html',
    styleUrls: ['./fd-ui-date-range-input.scss'],
    changeDetection: ChangeDetectionStrategy.OnPush,
})
export class FdUiDateRangeInputComponent implements FormValueControl<FdUiDateRangeValue | null> {
    // eslint-disable-next-line local/prefer-protected-template-members -- Public Signal Forms focus contract.
    public readonly focus = injectFdUiControlFocus();

    public readonly startPlaceholder = input<string>();
    public readonly endPlaceholder = input<string>();
    public readonly startLabel = input<string>();
    public readonly endLabel = input<string>();
    public readonly size = input<FdUiFieldSize>('md');
    public readonly disabled = input(false);
    public readonly controlDisabled = input(false);
    public readonly value = model<FdUiDateRangeValue | null>(null);
    public readonly touched = model(false);
    public readonly touch = output();

    protected readonly rangeModel = computed(() => this.toRangeValue(this.value()));
    protected readonly endpointsDisabled = computed(() => this.disabled() || this.controlDisabled());

    protected onEndpointChange(endpoint: 'start' | 'end', value: string | Date | null): void {
        if (this.endpointsDisabled()) {
            return;
        }

        const range = this.rangeModel();
        this.value.set({
            start: this.toDateValue(endpoint === 'start' ? value : range.start),
            end: this.toDateValue(endpoint === 'end' ? value : range.end),
        });
    }

    protected touchRange(): void {
        this.touched.set(true);
        this.touch.emit();
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
