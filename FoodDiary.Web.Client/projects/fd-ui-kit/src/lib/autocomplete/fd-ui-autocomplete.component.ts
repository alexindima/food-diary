import { CdkConnectedOverlay, CdkOverlayOrigin } from '@angular/cdk/overlay';
import { CommonModule } from '@angular/common';
import { ChangeDetectionStrategy, Component, computed, type ElementRef, input, output, signal, viewChild } from '@angular/core';
import { type ControlValueAccessor, NG_VALUE_ACCESSOR } from '@angular/forms';

import { FdUiIconComponent } from '../icon/fd-ui-icon.component';
import type { FdUiFieldSize } from '../types/field-size.type';
import { FdUiAutocompleteMenuComponent } from './fd-ui-autocomplete-menu.component';

const NO_ACTIVE_OPTION_INDEX = -1;
const FIRST_OPTION_INDEX = 0;
const NEXT_OPTION_OFFSET = 1;
const PREVIOUS_OPTION_OFFSET = -1;
const EMPTY_STATE_MIN_QUERY_LENGTH = 2;

let uniqueId = 0;

export interface FdUiAutocompleteOption<T = unknown> {
    id?: string | number;
    value: T;
    label: string;
    hint?: string | null;
    badge?: string | null;
    data?: unknown;
}

@Component({
    selector: 'fd-ui-autocomplete',
    standalone: true,
    imports: [CommonModule, CdkOverlayOrigin, CdkConnectedOverlay, FdUiIconComponent, FdUiAutocompleteMenuComponent],
    templateUrl: './fd-ui-autocomplete.component.html',
    styleUrls: ['./fd-ui-autocomplete.component.scss'],
    changeDetection: ChangeDetectionStrategy.OnPush,
    providers: [
        {
            provide: NG_VALUE_ACCESSOR,
            useExisting: FdUiAutocompleteComponent,
            multi: true,
        },
    ],
})
export class FdUiAutocompleteComponent<T = unknown> implements ControlValueAccessor {
    protected readonly controlRef = viewChild<ElementRef<HTMLInputElement>>('control');
    protected readonly controlWrapRef = viewChild<ElementRef<HTMLDivElement>>('controlWrap');
    protected readonly listboxRef = viewChild<ElementRef<HTMLDivElement>>('listbox');
    protected readonly isEqual = Object.is;

    public readonly id = input(`fd-ui-autocomplete-${uniqueId++}`);
    public readonly label = input<string>();
    public readonly placeholder = input<string>();
    public readonly error = input<string | null>();
    public readonly required = input(false);
    public readonly options = input<Array<FdUiAutocompleteOption<T>>>([]);
    public readonly loading = input(false);
    public readonly emptyText = input<string>();
    public readonly showEmptyState = input(true);
    public readonly size = input<FdUiFieldSize>('md');
    public readonly fillColor = input<string | null>(null);
    public readonly displayWith = input<(value: T | null) => string>();

    public readonly queryChange = output<string>();
    public readonly optionSelected = output<FdUiAutocompleteOption<T>>();

    protected readonly internalValue = signal<T | null>(null);
    protected readonly queryText = signal('');
    protected readonly disabled = signal(false);
    protected readonly isFocused = signal(false);
    protected readonly isOpen = signal(false);
    protected readonly activeIndex = signal(NO_ACTIVE_OPTION_INDEX);
    protected readonly overlayMinWidth = signal(0);

    private onChange: (value: T | string | null) => void = () => undefined;
    private onTouched: () => void = () => undefined;

    protected readonly sizeClass = computed(() => `fd-ui-autocomplete--size-${this.size()}`);
    protected readonly shouldFloatLabel = computed(() => this.isFocused() || this.queryText().trim().length > 0);
    protected readonly hostClass = computed(
        () =>
            `fd-ui-autocomplete ${this.sizeClass()}${this.error() !== null ? ' fd-ui-autocomplete--has-error' : ''}${this.shouldFloatLabel() ? ' fd-ui-autocomplete--floating' : ''}`,
    );
    protected readonly shouldShowPlaceholder = computed(() => this.isFocused() && this.queryText().trim().length === 0);
    protected readonly placeholderAttribute = computed(() => (this.shouldShowPlaceholder() ? (this.placeholder() ?? null) : null));
    protected readonly activeOptionId = computed(() => {
        const activeIndex = this.activeIndex();
        if (!this.isOpen() || activeIndex < 0 || activeIndex >= this.options().length) {
            return null;
        }

        return this.getOptionId(activeIndex);
    });

    protected readonly hasSelectedValue = computed(() => this.internalValue() !== null && this.internalValue() !== undefined);

    protected readonly shouldOpenOverlay = computed(
        () =>
            this.isOpen() &&
            ((this.loading() && this.showEmptyState()) ||
                this.options().length > 0 ||
                (this.showEmptyState() &&
                    this.queryText().trim().length >= EMPTY_STATE_MIN_QUERY_LENGTH &&
                    this.emptyText() !== undefined)),
    );

    public writeValue(value: T | null): void {
        this.internalValue.set(value);
        this.queryText.set(this.getDisplayText(value));
    }

    public registerOnChange(fn: (value: T | string | null) => void): void {
        this.onChange = fn;
    }

    public registerOnTouched(fn: () => void): void {
        this.onTouched = fn;
    }

    public setDisabledState(isDisabled: boolean): void {
        this.disabled.set(isDisabled);
    }

    protected onInput(value: string): void {
        if (this.disabled()) {
            return;
        }

        this.queryText.set(value);
        this.internalValue.set(null);
        this.onChange(value);
        this.queryChange.emit(value);
        this.openMenu();
    }

    protected onFocus(): void {
        this.isFocused.set(true);
        this.openMenu();
    }

    protected onBlur(): void {
        if (!this.isOpen()) {
            this.isFocused.set(false);
            this.onTouched();
        }
    }

    protected clearValue(event: MouseEvent): void {
        event.preventDefault();
        event.stopPropagation();

        if (this.disabled()) {
            return;
        }

        this.internalValue.set(null);
        this.queryText.set('');
        this.onChange('');
        this.queryChange.emit('');
        this.closeMenu();
        this.controlRef()?.nativeElement.focus();
    }

    protected selectOption(option: FdUiAutocompleteOption<T>): void {
        if (this.disabled()) {
            return;
        }

        this.internalValue.set(option.value);
        this.queryText.set(option.label);
        this.onChange(option.value);
        this.onTouched();
        this.optionSelected.emit(option);
        this.closeMenu();
        this.controlRef()?.nativeElement.focus();
    }

    protected onControlKeydown(event: KeyboardEvent): void {
        switch (event.key) {
            case 'ArrowDown':
                event.preventDefault();
                this.openMenu();
                this.moveActive(NEXT_OPTION_OFFSET);
                break;
            case 'ArrowUp':
                event.preventDefault();
                this.openMenu();
                this.moveActive(PREVIOUS_OPTION_OFFSET);
                break;
            case 'Enter':
                if (this.isOpen() && this.activeIndex() >= FIRST_OPTION_INDEX) {
                    event.preventDefault();
                    this.selectOption(this.options()[this.activeIndex()]);
                }
                break;
            case 'Escape':
                if (this.isOpen()) {
                    event.preventDefault();
                    this.closeMenu();
                }
                break;
        }
    }

    protected closeMenu(): void {
        this.isOpen.set(false);
        this.activeIndex.set(NO_ACTIVE_OPTION_INDEX);
        this.isFocused.set(false);
    }

    protected onMenuAttached(): void {
        queueMicrotask(() => this.listboxRef()?.nativeElement.scrollTo({ top: 0 }));
    }

    protected getOptionId(index: number): string {
        return `${this.id()}-option-${index}`;
    }

    protected getOptionTrack(option: FdUiAutocompleteOption<T>, index: number): string | number {
        return option.id ?? `${String(option.badge ?? '')}:${option.label}:${index}`;
    }

    private openMenu(): void {
        if (this.disabled()) {
            return;
        }

        this.overlayMinWidth.set(this.controlWrapRef()?.nativeElement.getBoundingClientRect().width ?? 0);
        this.isOpen.set(true);
        this.activeIndex.set(this.options().length > 0 ? FIRST_OPTION_INDEX : NO_ACTIVE_OPTION_INDEX);
    }

    private moveActive(delta: number): void {
        const options = this.options();
        if (options.length === 0) {
            this.activeIndex.set(NO_ACTIVE_OPTION_INDEX);
            return;
        }

        this.activeIndex.update(activeIndex => (activeIndex + delta + options.length) % options.length);
    }

    private getDisplayText(value: T | null): string {
        if (value === null || value === undefined) {
            return '';
        }

        const option = this.options().find(item => this.isEqual(item.value, value));
        if (option !== undefined) {
            return option.label;
        }

        return this.displayWith()?.(value) ?? String(value);
    }
}
