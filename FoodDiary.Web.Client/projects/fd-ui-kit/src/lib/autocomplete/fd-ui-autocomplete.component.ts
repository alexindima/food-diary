import { CdkConnectedOverlay, CdkOverlayOrigin } from '@angular/cdk/overlay';
import { CommonModule } from '@angular/common';
import {
    ChangeDetectionStrategy,
    ChangeDetectorRef,
    Component,
    type ElementRef,
    forwardRef,
    inject,
    input,
    output,
    viewChild,
} from '@angular/core';
import { type ControlValueAccessor, NG_VALUE_ACCESSOR } from '@angular/forms';

import { FdUiIconComponent } from '../icon/fd-ui-icon.component';
import { FdUiLoaderComponent } from '../loader/fd-ui-loader.component';
import { type FdUiFieldSize } from '../types/field-size.type';

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
    imports: [CommonModule, CdkOverlayOrigin, CdkConnectedOverlay, FdUiIconComponent, FdUiLoaderComponent],
    templateUrl: './fd-ui-autocomplete.component.html',
    styleUrls: ['./fd-ui-autocomplete.component.scss'],
    changeDetection: ChangeDetectionStrategy.OnPush,
    providers: [
        {
            provide: NG_VALUE_ACCESSOR,
            useExisting: forwardRef((): typeof FdUiAutocompleteComponent => FdUiAutocompleteComponent),
            multi: true,
        },
    ],
})
export class FdUiAutocompleteComponent<T = unknown> implements ControlValueAccessor {
    private readonly cdr = inject(ChangeDetectorRef);

    protected readonly controlRef = viewChild<ElementRef<HTMLInputElement>>('control');
    protected readonly controlWrapRef = viewChild<ElementRef<HTMLDivElement>>('controlWrap');
    protected readonly listboxRef = viewChild<ElementRef<HTMLDivElement>>('listbox');
    protected readonly isEqual = Object.is;

    public readonly id = input(`fd-ui-autocomplete-${uniqueId++}`);
    public readonly label = input<string>();
    public readonly placeholder = input<string>();
    public readonly error = input<string | null>();
    public readonly required = input(false);
    public readonly options = input<FdUiAutocompleteOption<T>[]>([]);
    public readonly loading = input(false);
    public readonly emptyText = input<string>();
    public readonly showEmptyState = input(true);
    public readonly size = input<FdUiFieldSize>('md');
    public readonly fillColor = input<string | null>(null);
    public readonly displayWith = input<(value: T | null) => string>();

    public readonly queryChange = output<string>();
    public readonly optionSelected = output<FdUiAutocompleteOption<T>>();

    protected internalValue: T | null = null;
    protected queryText = '';
    protected disabled = false;
    protected isFocused = false;
    protected isOpen = false;
    protected activeIndex = -1;
    protected overlayMinWidth = 0;

    private onChange: (value: T | null) => void = () => undefined;
    private onTouched: () => void = () => undefined;

    protected get sizeClass(): string {
        return `fd-ui-autocomplete--size-${this.size()}`;
    }

    protected get shouldFloatLabel(): boolean {
        return this.isFocused || this.queryText.trim().length > 0;
    }

    protected get shouldShowPlaceholder(): boolean {
        return this.isFocused && this.queryText.trim().length === 0;
    }

    protected get activeOptionId(): string | null {
        if (!this.isOpen || this.activeIndex < 0 || this.activeIndex >= this.options().length) {
            return null;
        }

        return this.getOptionId(this.activeIndex);
    }

    protected get hasSelectedValue(): boolean {
        return this.internalValue !== null && this.internalValue !== undefined;
    }

    protected get shouldOpenOverlay(): boolean {
        return (
            this.isOpen &&
            ((this.loading() && this.showEmptyState()) ||
                this.options().length > 0 ||
                (this.showEmptyState() && this.queryText.trim().length >= 2 && !!this.emptyText()))
        );
    }

    public writeValue(value: T | null): void {
        this.internalValue = value;
        this.queryText = this.getDisplayText(value);
        this.cdr.markForCheck();
    }

    public registerOnChange(fn: (value: T | null) => void): void {
        this.onChange = fn;
    }

    public registerOnTouched(fn: () => void): void {
        this.onTouched = fn;
    }

    public setDisabledState(isDisabled: boolean): void {
        this.disabled = isDisabled;
        this.cdr.markForCheck();
    }

    protected onInput(value: string): void {
        if (this.disabled) {
            return;
        }

        this.queryText = value;
        this.internalValue = null;
        this.onChange(value as T);
        this.queryChange.emit(value);
        this.openMenu();
    }

    protected onFocus(): void {
        this.isFocused = true;
        this.openMenu();
    }

    protected onBlur(): void {
        if (!this.isOpen) {
            this.isFocused = false;
            this.onTouched();
        }
    }

    protected clearValue(event: MouseEvent): void {
        event.preventDefault();
        event.stopPropagation();

        if (this.disabled) {
            return;
        }

        this.internalValue = null;
        this.queryText = '';
        this.onChange('' as T);
        this.queryChange.emit('');
        this.closeMenu();
        this.controlRef()?.nativeElement.focus();
    }

    protected selectOption(option: FdUiAutocompleteOption<T>): void {
        if (this.disabled) {
            return;
        }

        this.internalValue = option.value;
        this.queryText = option.label;
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
                this.moveActive(1);
                break;
            case 'ArrowUp':
                event.preventDefault();
                this.openMenu();
                this.moveActive(-1);
                break;
            case 'Enter':
                if (this.isOpen && this.activeIndex >= 0) {
                    event.preventDefault();
                    this.selectOption(this.options()[this.activeIndex]);
                }
                break;
            case 'Escape':
                if (this.isOpen) {
                    event.preventDefault();
                    this.closeMenu();
                }
                break;
        }
    }

    protected closeMenu(): void {
        this.isOpen = false;
        this.activeIndex = -1;
        this.isFocused = false;
        this.cdr.markForCheck();
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
        if (this.disabled) {
            return;
        }

        this.overlayMinWidth = this.controlWrapRef()?.nativeElement.getBoundingClientRect().width ?? 0;
        this.isOpen = true;
        this.activeIndex = this.options().length > 0 ? 0 : -1;
        this.cdr.markForCheck();
    }

    private moveActive(delta: number): void {
        const options = this.options();
        if (!options.length) {
            this.activeIndex = -1;
            return;
        }

        this.activeIndex = (this.activeIndex + delta + options.length) % options.length;
    }

    private getDisplayText(value: T | null): string {
        if (value === null || value === undefined) {
            return '';
        }

        const option = this.options().find(item => this.isEqual(item.value, value));
        if (option) {
            return option.label;
        }

        return this.displayWith()?.(value) ?? String(value);
    }
}
