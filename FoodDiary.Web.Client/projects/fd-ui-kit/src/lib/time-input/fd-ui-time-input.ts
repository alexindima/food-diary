import { CdkConnectedOverlay, CdkOverlayOrigin } from '@angular/cdk/overlay';
import { CommonModule, DOCUMENT } from '@angular/common';
import {
    ChangeDetectionStrategy,
    Component,
    computed,
    effect,
    ElementRef,
    inject,
    input,
    model,
    signal,
    viewChildren,
} from '@angular/core';
import type { FormValueControl } from '@angular/forms/signals';

import { FdUiIconComponent } from '../icon/fd-ui-icon';
import type { FdUiFieldSize } from '../types/field-size.type';

const MIN_TIME_VALUE = 0;
const MAX_HOURS_VALUE = 23;
const MAX_MINUTES_VALUE = 59;
const TIME_MATCH_HOURS_INDEX = 1;
const TIME_MATCH_MINUTES_INDEX = 2;
const PADDED_TIME_PART_LENGTH = 2;
const MINUTES_STEP = 5;

let uniqueId = 0;

@Component({
    selector: 'fd-ui-time-input',
    imports: [CommonModule, CdkOverlayOrigin, CdkConnectedOverlay, FdUiIconComponent],
    templateUrl: './fd-ui-time-input.html',
    styleUrls: ['./fd-ui-time-input.scss'],
    changeDetection: ChangeDetectionStrategy.OnPush,
})
export class FdUiTimeInputComponent implements FormValueControl<string | null> {
    public readonly id = input(`fd-ui-time-input-${uniqueId++}`);
    public readonly label = input<string>();
    public readonly pickerAriaLabel = input<string>();
    public readonly placeholder = input<string>();
    public readonly hoursLabel = input('Hours');
    public readonly minutesLabel = input('Minutes');
    public readonly error = input<string | null>();
    public readonly required = input(false);
    public readonly size = input<FdUiFieldSize>('md');
    public readonly value = model<string | null>(null);
    public readonly touched = model(false);
    public readonly disabled = input(false);

    protected readonly internalValue = signal('');
    protected readonly isFocused = signal(false);
    protected readonly isOpen = signal(false);
    protected readonly hours = Array.from({ length: MAX_HOURS_VALUE + 1 }, (_, hour) => hour);
    protected readonly minutes = Array.from({ length: (MAX_MINUTES_VALUE + 1) / MINUTES_STEP }, (_, index) => index * MINUTES_STEP);
    protected readonly hourOptionElements = viewChildren<ElementRef<HTMLButtonElement>>('hourOption');
    protected readonly minuteOptionElements = viewChildren<ElementRef<HTMLButtonElement>>('minuteOption');

    private readonly host = inject<ElementRef<HTMLElement>>(ElementRef);
    private readonly document = inject(DOCUMENT);

    public constructor() {
        effect(() => {
            this.internalValue.set(this.value() ?? '');
        });

        effect(() => {
            if (this.disabled()) {
                this.closeTimePicker();
            }
        });

        effect(() => {
            if (!this.isOpen()) {
                return;
            }

            this.centerOption(this.hourOptionElements()[this.selectedHour()]);
            const minuteIndex = Math.round(this.selectedMinute() / MINUTES_STEP);
            this.centerOption(this.minuteOptionElements()[minuteIndex]);
        });
    }

    protected readonly sizeClass = computed(() => `fd-ui-time-input--size-${this.size()}`);
    protected readonly hasError = computed(() => {
        const error = this.error();

        return error !== null && error !== undefined && error.trim().length > 0;
    });
    protected readonly shouldFloatLabel = computed(() => this.isFocused() || this.internalValue().trim().length > 0);
    protected readonly hostClass = computed(
        () =>
            `fd-ui-time-input ${this.sizeClass()}${this.hasError() ? ' fd-ui-time-input--has-error' : ''}${this.shouldFloatLabel() ? ' fd-ui-time-input--floating' : ''}`,
    );
    protected readonly shouldShowPlaceholder = computed(() => this.isFocused() && this.internalValue().trim().length === 0);
    protected readonly placeholderAttribute = computed(() => (this.shouldShowPlaceholder() ? (this.placeholder() ?? 'HH:mm') : null));
    protected readonly selectedHour = computed(() => this.parseTime(this.internalValue())?.hours ?? 0);
    protected readonly selectedMinute = computed(() => this.parseTime(this.internalValue())?.minutes ?? 0);

    protected onInput(value: string): void {
        if (this.disabled()) {
            return;
        }

        if (value.length === 0) {
            this.internalValue.set('');
            this.value.set(null);
            return;
        }

        const parsed = this.parseTime(value);
        if (parsed === null) {
            this.internalValue.set(value);
            return;
        }

        this.internalValue.set(`${this.padNumber(parsed.hours)}:${this.padNumber(parsed.minutes)}`);
        this.value.set(this.internalValue());
    }

    protected onBlur(): void {
        this.isFocused.set(false);
        const internalValue = this.internalValue();
        if (internalValue.length > 0) {
            const parsed = this.parseTime(internalValue);
            if (parsed !== null) {
                this.internalValue.set(`${this.padNumber(parsed.hours)}:${this.padNumber(parsed.minutes)}`);
                this.value.set(this.internalValue());
            }
        }
        this.touched.set(true);
    }

    protected onFocus(): void {
        this.isFocused.set(true);
    }

    protected openTimePicker(): void {
        if (this.disabled()) {
            return;
        }

        this.isOpen.set(true);
        this.isFocused.set(true);
    }

    protected closeTimePicker(): void {
        if (!this.isOpen()) {
            return;
        }

        this.isOpen.set(false);
        this.isFocused.set(false);
        this.touched.set(true);
    }

    protected selectHour(hours: number): void {
        this.setTime(hours, this.selectedMinute());
    }

    protected selectMinute(minutes: number): void {
        this.setTime(this.selectedHour(), minutes);
        this.closeTimePicker();
    }

    protected formatPart(value: number): string {
        return this.padNumber(value);
    }

    protected onInputKeydown(event: KeyboardEvent): void {
        if (event.key === 'Enter' || event.key === ' ' || event.key === 'ArrowDown') {
            event.preventDefault();
            this.openTimePicker();
        } else if (event.key === 'Escape') {
            this.closeTimePicker();
        }
    }

    protected onOverlayKeydown(event: KeyboardEvent): void {
        if (event.key === 'Escape') {
            event.preventDefault();
            this.closeTimePicker();
        }
    }

    protected onFocusOut(): void {
        const active = this.document.activeElement;
        if (active !== null && this.host.nativeElement.contains(active)) {
            return;
        }
        if (this.isOpen()) {
            return;
        }
        this.isFocused.set(false);
        this.touched.set(true);
    }

    private parseTime(value: string): { hours: number; minutes: number } | null {
        const match = /^(\d{1,2}):?(\d{2})$/.exec(value);
        if (match === null) {
            return null;
        }

        const hours = Number(match[TIME_MATCH_HOURS_INDEX]);
        const minutes = Number(match[TIME_MATCH_MINUTES_INDEX]);
        if (Number.isNaN(hours) || Number.isNaN(minutes)) {
            return null;
        }

        if (hours < MIN_TIME_VALUE || hours > MAX_HOURS_VALUE || minutes < MIN_TIME_VALUE || minutes > MAX_MINUTES_VALUE) {
            return null;
        }

        return { hours, minutes };
    }

    private padNumber(value: number): string {
        return value.toString().padStart(PADDED_TIME_PART_LENGTH, '0');
    }

    private setTime(hours: number, minutes: number): void {
        const formatted = `${this.padNumber(hours)}:${this.padNumber(minutes)}`;
        this.internalValue.set(formatted);
        this.value.set(formatted);
    }

    private centerOption(option: ElementRef<HTMLButtonElement> | undefined): void {
        const element = option?.nativeElement;
        const container = element?.parentElement;
        if (element === undefined || container === null || container === undefined) {
            return;
        }

        container.scrollTop = element.offsetTop - (container.clientHeight - element.clientHeight) / 2;
    }
}
