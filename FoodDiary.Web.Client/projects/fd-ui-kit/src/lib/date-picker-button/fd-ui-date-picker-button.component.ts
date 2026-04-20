import { ChangeDetectionStrategy, Component, input, output, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { CdkConnectedOverlay, CdkOverlayOrigin } from '@angular/cdk/overlay';
import { FdUiHintDirective } from '../hint/fd-ui-hint.directive';
import { FdUiButtonComponent } from '../button/fd-ui-button.component';
import { FdUiCalendarComponent } from '../calendar/fd-ui-calendar.component';

@Component({
    selector: 'fd-ui-date-picker-button',
    standalone: true,
    imports: [CommonModule, CdkOverlayOrigin, CdkConnectedOverlay, FdUiHintDirective, FdUiButtonComponent, FdUiCalendarComponent],
    templateUrl: './fd-ui-date-picker-button.component.html',
    styleUrl: './fd-ui-date-picker-button.component.scss',
    changeDetection: ChangeDetectionStrategy.OnPush,
})
export class FdUiDatePickerButtonComponent {
    public readonly value = input<Date | null>(null);
    public readonly min = input<Date | null>(null);
    public readonly max = input<Date | null>(null);
    public readonly ariaLabel = input<string>('');
    public readonly hint = input<string | null>(null);
    public readonly icon = input('calendar_today');

    public readonly valueChange = output<Date>();
    protected readonly isOpen = signal(false);
    protected readonly displayMonth = signal(this.value() ?? new Date());

    public open(): void {
        this.displayMonth.set(this.value() ?? new Date());
        this.isOpen.set(true);
    }

    protected close(): void {
        this.isOpen.set(false);
    }

    protected onCalendarSelect(value: Date): void {
        this.valueChange.emit(value);
        this.close();
    }

    protected onDisplayMonthChange(value: Date): void {
        this.displayMonth.set(value);
    }

    protected onOverlayKeydown(event: KeyboardEvent): void {
        if (event.key === 'Escape') {
            event.preventDefault();
            this.close();
        }
    }
}
