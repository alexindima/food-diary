import { CdkConnectedOverlay, CdkOverlayOrigin } from '@angular/cdk/overlay';
import { CommonModule, DOCUMENT } from '@angular/common';
import {
    afterNextRender,
    booleanAttribute,
    ChangeDetectionStrategy,
    Component,
    type ElementRef,
    inject,
    Injector,
    input,
    model,
    signal,
    viewChild,
} from '@angular/core';

import { FdUiButtonComponent } from '../button/fd-ui-button';
import { FdUiCalendarComponent } from '../calendar/fd-ui-calendar';
import { FdUiHintDirective } from '../hint/fd-ui-hint.directive';

@Component({
    selector: 'fd-ui-date-picker-button',
    imports: [CommonModule, CdkOverlayOrigin, CdkConnectedOverlay, FdUiHintDirective, FdUiButtonComponent, FdUiCalendarComponent],
    templateUrl: './fd-ui-date-picker-button.html',
    styleUrl: './fd-ui-date-picker-button.scss',
    changeDetection: ChangeDetectionStrategy.OnPush,
})
export class FdUiDatePickerButtonComponent {
    private readonly document = inject(DOCUMENT);
    private readonly injector = inject(Injector);
    private readonly trigger = viewChild.required<FdUiButtonComponent>('trigger');
    private readonly panel = viewChild<ElementRef<HTMLElement>>('panel');
    private focusOrigin: HTMLElement | null = null;
    public readonly value = model<Date | null>(null);
    public readonly locale = input<string | null>(null);
    public readonly min = input<Date | null>(null);
    public readonly max = input<Date | null>(null);
    public readonly disabled = input(false, { transform: booleanAttribute });
    public readonly ariaLabel = input<string>('');
    public readonly hint = input<string | null>(null);
    public readonly icon = input('calendar_today');

    protected readonly isOpen = signal(false);
    protected readonly displayMonth = signal(this.value() ?? new Date());

    protected open(): void {
        if (this.disabled() || this.isOpen()) {
            return;
        }

        const active = this.document.activeElement;
        this.focusOrigin = active instanceof HTMLElement && active !== this.document.body ? active : null;
        this.displayMonth.set(this.value() ?? new Date());
        this.isOpen.set(true);
    }

    protected close(): void {
        if (!this.isOpen()) {
            return;
        }
        const restoreFocus = this.panel()?.nativeElement.contains(this.document.activeElement) === true;
        const focusOrigin = this.focusOrigin;
        this.focusOrigin = null;
        this.isOpen.set(false);
        if (restoreFocus && !this.disabled()) {
            if (focusOrigin?.isConnected === true) {
                focusOrigin.focus();
            } else {
                this.trigger().focus();
            }
        }
    }

    protected focusCalendar(): void {
        afterNextRender(
            () => {
                if (this.isOpen()) {
                    this.panel()?.nativeElement.querySelector<HTMLElement>('[role="gridcell"][tabindex="0"]:not(:disabled)')?.focus();
                }
            },
            { injector: this.injector },
        );
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
        if (event.key === 'Escape' && this.isOpen()) {
            event.preventDefault();
            event.stopPropagation();
            this.close();
        }
    }
}
