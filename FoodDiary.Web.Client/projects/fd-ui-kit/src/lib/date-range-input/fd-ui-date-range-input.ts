import { CommonModule } from '@angular/common';
import { ChangeDetectionStrategy, ChangeDetectorRef, Component, effect, inject, input, model, signal } from '@angular/core';
import { disabled as disabledRule, form, FormField, type FormValueControl } from '@angular/forms/signals';

import { fdUiFormatDateInputValue, fdUiParseLocalDate } from '../date/fd-ui-date.utils';
import { FdUiDateInputComponent } from '../date-input/fd-ui-date-input';
import type { FdUiFieldSize } from '../types/field-size.type';

export type FdUiDateRangeValue = {
    start: Date | null;
    end: Date | null;
};

@Component({
    selector: 'fd-ui-date-range-input',
    imports: [CommonModule, FormField, FdUiDateInputComponent],
    templateUrl: './fd-ui-date-range-input.html',
    styleUrls: ['./fd-ui-date-range-input.scss'],
    changeDetection: ChangeDetectionStrategy.OnPush,
})
export class FdUiDateRangeInputComponent implements FormValueControl<FdUiDateRangeValue | null> {
    private readonly cdr = inject(ChangeDetectorRef);

    public readonly startPlaceholder = input<string>();
    public readonly endPlaceholder = input<string>();
    public readonly startLabel = input<string>();
    public readonly endLabel = input<string>();
    public readonly size = input<FdUiFieldSize>('md');
    public readonly disabled = input(false);
    public readonly controlDisabled = input(false);
    public readonly value = model<FdUiDateRangeValue | null>(null);
    public readonly touched = model(false);

    protected readonly rangeModel = signal<{ start: string | null; end: string | null }>({
        start: null,
        end: null,
    });
    protected readonly rangeForm = form(this.rangeModel, path => {
        disabledRule(path.start, { when: () => this.disabled() || this.controlDisabled() });
        disabledRule(path.end, { when: () => this.disabled() || this.controlDisabled() });
    });
    private skippedModelUpdate: string | null = null;

    public constructor() {
        effect(() => {
            const coerced = this.toRangeValue(this.value());
            this.skippedModelUpdate = JSON.stringify(coerced);
            this.rangeModel.set(coerced);
            this.cdr.markForCheck();
        });

        effect(() => {
            const value = this.rangeModel();
            const serialized = JSON.stringify(value);
            if (serialized === this.skippedModelUpdate) {
                this.skippedModelUpdate = null;
                return;
            }

            this.value.set({
                start: this.toDateValue(value.start),
                end: this.toDateValue(value.end),
            });
        });
    }

    protected touchRange(): void {
        this.touched.set(true);
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
