import { CdkTrapFocus } from '@angular/cdk/a11y';
import { CdkConnectedOverlay, CdkOverlayOrigin } from '@angular/cdk/overlay';
import { CommonModule, DOCUMENT } from '@angular/common';
import {
    afterNextRender,
    ChangeDetectionStrategy,
    Component,
    computed,
    effect,
    ElementRef,
    inject,
    Injector,
    input,
    linkedSignal,
    LOCALE_ID,
    model,
    output,
    signal,
} from '@angular/core';
import { toSignal } from '@angular/core/rxjs-interop';
import type { FormValueControl, ValidationError } from '@angular/forms/signals';
import { TranslateService } from '@ngx-translate/core';
import { map, of } from 'rxjs';

import { FdUiCalendarComponent } from '../calendar/fd-ui-calendar';
import { fdUiFormatDateInputValue, fdUiParseLocalDate, fdUiStartOfLocalDay } from '../date/fd-ui-date.utils';
import { fdUiLocalizedDateHint, fdUiParseLocalizedDate } from '../date/fd-ui-localized-date.utils';
import { injectFdUiControlFocus } from '../form/fd-ui-control-focus';
import { injectFdUiFieldError } from '../form-error/fd-ui-signal-field-error';
import { FdUiIconComponent } from '../icon/fd-ui-icon';
import { fdUiIsComposing } from '../interaction/fd-ui-text-enter.directive';
import type { FdUiFieldSize } from '../types/field-size.type';

let uniqueId = 0;

@Component({
    selector: 'fd-ui-date-input',
    imports: [CommonModule, CdkOverlayOrigin, CdkConnectedOverlay, CdkTrapFocus, FdUiCalendarComponent, FdUiIconComponent],
    templateUrl: './fd-ui-date-input.html',
    styleUrls: ['./fd-ui-date-input.scss'],
    changeDetection: ChangeDetectionStrategy.OnPush,
    host: { '(keydown)': 'onOverlayKeydown($event)' },
})
export class FdUiDateInputComponent implements FormValueControl<string | Date | null> {
    // eslint-disable-next-line local/prefer-protected-template-members -- Public Signal Forms focus contract.
    public readonly focus = injectFdUiControlFocus();
    private readonly host = inject<ElementRef<HTMLElement>>(ElementRef);
    private readonly locale = inject(LOCALE_ID);
    private readonly document = inject(DOCUMENT);
    private readonly injector = inject(Injector);
    private pickerTrigger: HTMLButtonElement | null = null;

    private readonly translateService = inject(TranslateService, { optional: true });
    protected readonly language = toSignal(this.translateService?.onLangChange.pipe(map(event => event.lang)) ?? of(this.locale), {
        initialValue: this.translateService?.getCurrentLang() ?? this.locale,
    });
    public readonly allowManualInput = input(false);
    public readonly allowYearSelection = input(false);
    public readonly manualInputFormat = input<'native' | 'localized'>('native');
    protected readonly isLocalizedInput = computed(() => this.allowManualInput() && this.manualInputFormat() === 'localized');
    public readonly id = input(`fd-ui-date-input-${uniqueId++}`);
    public readonly label = input<string>();
    public readonly clearAriaLabel = input<string>();
    public readonly pickerAriaLabel = input<string>();
    public readonly placeholder = input<string>();
    public readonly todayLabel = input<string>();
    public readonly error = input<string | null>();
    public readonly errors = input<readonly ValidationError.WithOptionalFieldTree[]>([]);
    public readonly invalidDateLabel = input<string>();
    private readonly invalidManualDate = signal(false);
    protected readonly fieldError = computed(() => {
        if (this.invalidManualDate()) {
            return this.isFocused() ? null : (this.invalidDateLabel() ?? this.resolvedError());
        }
        return this.resolvedError();
    });
    public readonly required = input(false);
    public readonly showRequiredIndicator = input(true);
    public readonly size = input<FdUiFieldSize>('md');
    public readonly min = input<string | Date>();
    public readonly max = input<string | Date>();
    public readonly latestDate = input<string | Date>();
    public readonly earliestDate = input<string | Date>();
    public readonly value = model<string | Date | null>(null);
    public readonly touched = model(false);
    protected readonly resolvedError = injectFdUiFieldError(this.error, this.errors, this.touched);
    public readonly touch = output();
    public readonly manualInputInvalidChange = output<boolean>();
    public readonly disabled = input(false);

    protected readonly internalValue = computed(() => fdUiParseLocalDate(this.value()));
    protected readonly isOpen = signal(false);
    protected readonly displayMonth = linkedSignal({
        source: this.value,
        computation: () => this.internalValue() ?? new Date(),
    });
    protected readonly isFocused = signal(false);
    protected readonly pickerAccessibleName = computed(() => {
        const language = this.language();
        const explicitLabel = [this.pickerAriaLabel(), this.label()].find(label => label !== undefined && label.trim().length > 0);
        return (
            explicitLabel?.trim() ??
            this.translateService?.instant('CALENDAR.CHOOSE_DATE') ??
            new Intl.DateTimeFormat(language, { month: 'long', year: 'numeric' }).format(this.displayMonth())
        );
    });

    protected readonly sizeClass = computed(() => `fd-ui-date-input--size-${this.size()}`);
    protected readonly hasError = computed(() => {
        if (this.invalidManualDate()) {
            return !this.isFocused();
        }
        const error = this.fieldError();

        return typeof error === 'string' && error.trim().length > 0;
    });
    protected readonly shouldFloatLabel = computed(
        () => this.allowManualInput() || this.isFocused() || this.isOpen() || this.internalValue() !== null,
    );
    protected readonly hostClass = computed(
        () =>
            `fd-ui-date-input ${this.sizeClass()}${this.hasError() ? ' fd-ui-date-input--has-error' : ''}${this.shouldFloatLabel() ? ' fd-ui-date-input--floating' : ''}`,
    );
    protected readonly shouldShowPlaceholder = computed(() => (this.isFocused() || this.isOpen()) && this.internalValue() === null);
    protected readonly placeholderAttribute = computed(() =>
        this.isLocalizedInput()
            ? (this.placeholder() ?? fdUiLocalizedDateHint(this.language()))
            : this.shouldShowPlaceholder()
              ? (this.placeholder() ?? null)
              : null,
    );
    protected readonly displayValue = computed(() => {
        const value = this.internalValue();
        if (value === null) {
            return '';
        }

        const todayLabel = this.todayLabel();
        if (todayLabel !== undefined && this.isToday(value)) {
            return todayLabel;
        }

        return new Intl.DateTimeFormat(this.language(), {
            day: '2-digit',
            month: '2-digit',
            year: 'numeric',
        }).format(value);
    });

    public constructor() {
        effect(() => {
            if (this.disabled()) {
                this.closeDatePicker();
            }
        });
    }

    public reset(): void {
        this.setManualInputInvalid(false);
        this.displayMonth.set(this.internalValue() ?? new Date());
        this.pickerTrigger = null;
        this.isOpen.set(false);
        this.isFocused.set(false);
        this.touched.set(false);
        const control = this.host.nativeElement.querySelector<HTMLInputElement>('input');
        if (control !== null) {
            control.value = this.manualValue();
        }
    }

    protected readonly manualValue = computed(() => {
        const value = this.internalValue();
        if (value === null) {
            return '';
        }
        return this.isLocalizedInput()
            ? new Intl.DateTimeFormat(this.language(), { day: '2-digit', month: '2-digit', year: 'numeric' }).format(value)
            : fdUiFormatDateInputValue(value);
    });
    protected onManualInput(target: HTMLInputElement): void {
        const parsed = this.parseManualDate(target.value);
        const min = this.minDate();
        const max = this.maxDate();
        if (parsed !== null && target.validity.valid && (min === null || parsed >= min) && (max === null || parsed <= max)) {
            this.onDateSelect(parsed);
        } else {
            const invalid = target.value !== '' || target.validity.badInput;
            this.setManualInputInvalid(invalid);
            if (!invalid) {
                this.value.set(null);
            }
        }
        this.touched.set(true);
        this.touch.emit();
    }

    protected clearValue(button: HTMLButtonElement | null = null): void {
        if (this.disabled()) {
            return;
        }
        this.setManualInputInvalid(false);
        this.value.set(null);
        this.touched.set(true);
        this.touch.emit();
        this.closeDatePicker();
        if (button !== null && this.document.activeElement === button && !this.disabled()) {
            this.focus();
        }
    }

    protected openDatePicker(trigger: HTMLButtonElement | null = null): void {
        if (this.disabled()) {
            return;
        }

        if (!this.isOpen()) {
            this.pickerTrigger = trigger;
        }
        this.displayMonth.set(this.internalValue() ?? new Date());
        this.isOpen.set(true);
        this.isFocused.set(true);
    }

    protected closeDatePicker(): void {
        if (!this.isOpen()) {
            return;
        }

        const activeElement = this.document.activeElement;
        const restoreFocus = this.document.getElementById(`${this.id()}-dialog`)?.contains(activeElement) === true;
        const trigger = this.pickerTrigger;
        this.pickerTrigger = null;
        this.isOpen.set(false);
        this.isFocused.set(false);
        this.touched.set(true);
        this.touch.emit();
        if (restoreFocus && !this.disabled() && this.document.activeElement === activeElement) {
            if (trigger?.isConnected === true && !trigger.disabled) {
                trigger.focus();
            } else {
                this.focus();
            }
        }
    }

    protected focusDatePicker(): void {
        afterNextRender(
            () => {
                if (this.isOpen()) {
                    this.document
                        .getElementById(`${this.id()}-dialog`)
                        ?.querySelector<HTMLButtonElement>('[role="gridcell"][tabindex="0"]:not([disabled])')
                        ?.focus();
                }
            },
            { injector: this.injector },
        );
    }

    protected onDateSelect(value: Date | null): void {
        if (value === null) {
            return;
        }

        const normalized = this.stripTime(value);
        this.setManualInputInvalid(false);
        this.displayMonth.set(normalized);
        const isoDate = this.formatIsoDate(normalized);
        this.value.set(isoDate);
        this.closeDatePicker();
    }

    protected onDisplayMonthChange(value: Date | null): void {
        if (value === null) {
            return;
        }

        this.displayMonth.set(value);
    }

    protected onFocusIn(): void {
        this.isFocused.set(true);
    }

    protected onFocusOut(event: FocusEvent): void {
        const nextTarget = event.relatedTarget;
        if (nextTarget instanceof Node && this.host.nativeElement.contains(nextTarget)) {
            return;
        }

        if (this.isOpen()) {
            return;
        }

        this.isFocused.set(false);
        this.touched.set(true);
        this.touch.emit();
    }

    protected onInputKeydown(event: KeyboardEvent): void {
        if (fdUiIsComposing(event)) {
            return;
        }
        switch (event.key) {
            case 'ArrowDown':
            case 'Enter':
            case ' ': {
                if (this.allowManualInput() && event.key !== 'ArrowDown') {
                    return;
                }
                event.preventDefault();
                this.openDatePicker();
                break;
            }
            case 'Escape': {
                this.onOverlayKeydown(event);
                break;
            }
        }
    }

    protected onOverlayKeydown(event: KeyboardEvent): void {
        if (event.key === 'Escape' && this.isOpen()) {
            event.preventDefault();
            event.stopPropagation();
            this.closeDatePicker();
        }
    }

    protected readonly minDate = computed(() => fdUiParseLocalDate(this.earliestDate() ?? this.min()));
    protected readonly maxDate = computed(() => fdUiParseLocalDate(this.latestDate() ?? this.max()));

    private stripTime(date: Date): Date {
        return fdUiStartOfLocalDay(date);
    }

    private parseManualDate(value: string): Date | null {
        return this.isLocalizedInput() ? fdUiParseLocalizedDate(value, this.language()) : fdUiParseLocalDate(value);
    }

    private isToday(date: Date): boolean {
        const today = new Date();
        return date.getFullYear() === today.getFullYear() && date.getMonth() === today.getMonth() && date.getDate() === today.getDate();
    }

    private formatIsoDate(date: Date): string {
        return fdUiFormatDateInputValue(date);
    }

    private setManualInputInvalid(invalid: boolean): void {
        this.invalidManualDate.set(invalid);
        this.manualInputInvalidChange.emit(invalid);
    }
}
