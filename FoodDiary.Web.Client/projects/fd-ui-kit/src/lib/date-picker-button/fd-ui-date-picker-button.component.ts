import { CdkConnectedOverlay, CdkOverlayOrigin } from '@angular/cdk/overlay';
import { CommonModule } from '@angular/common';
import { booleanAttribute, ChangeDetectionStrategy, Component, input, model, signal } from '@angular/core';

import { FdUiButtonComponent } from '../button/fd-ui-button.component';
import { FdUiCalendarComponent } from '../calendar/fd-ui-calendar.component';
import { FdUiHintDirective } from '../hint/fd-ui-hint.directive';

@Component({
    selector: 'fd-ui-date-picker-button',
    standalone: true,
    imports: [CommonModule, CdkOverlayOrigin, CdkConnectedOverlay, FdUiHintDirective, FdUiButtonComponent, FdUiCalendarComponent],
    templateUrl: './fd-ui-date-picker-button.component.html',
    styleUrl: './fd-ui-date-picker-button.component.scss',
    changeDetection: ChangeDetectionStrategy.OnPush,
})
export class FdUiDatePickerButtonComponent {
    public readonly value = model<Date | null>(null);
    public readonly min = input<Date | null>(null);
    public readonly max = input<Date | null>(null);
    public readonly disabled = input(false, { transform: booleanAttribute });
    public readonly ariaLabel = input<string>('');
    public readonly hint = input<string | null>(null);
    public readonly icon = input('calendar_today');

    protected readonly isOpen = signal(false);
    protected readonly displayMonth = signal(this.value() ?? new Date());

    public open(): void {
        if (this.disabled()) {
            return;
        }

        this.displayMonth.set(this.value() ?? new Date());
        this.isOpen.set(true);
    }

    protected close(): void {
        this.isOpen.set(false);
    }

    protected onCalendarSelect(value: Date | null): void {
        if (value === null) {
            return;
        }

        this.value.set(value);
        this.close();
    }

    protected onDisplayMonthChange(value: Date | null): void {
        if (value === null) {
            return;
        }

        this.displayMonth.set(value);
    }

    protected onOverlayKeydown(event: KeyboardEvent): void {
        if (event.key === 'Escape') {
            event.preventDefault();
            this.close();
        }
    }
}
