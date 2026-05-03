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
    viewChild,
    ViewEncapsulation,
} from '@angular/core';
import { type ControlValueAccessor, NG_VALUE_ACCESSOR } from '@angular/forms';

import { FdUiIconComponent } from '../icon/fd-ui-icon.component';
import { type FdUiFieldSize } from '../types/field-size.type';

let uniqueId = 0;

export interface FdUiSelectOption<T = unknown> {
    value: T;
    label: string;
    hint?: string;
}

@Component({
    selector: 'fd-ui-select',
    standalone: true,
    imports: [CommonModule, FdUiIconComponent, CdkOverlayOrigin, CdkConnectedOverlay],
    templateUrl: './fd-ui-select.component.html',
    styleUrls: ['./fd-ui-select.component.scss'],
    changeDetection: ChangeDetectionStrategy.OnPush,
    encapsulation: ViewEncapsulation.None,
    providers: [
        {
            provide: NG_VALUE_ACCESSOR,
            useExisting: forwardRef((): typeof FdUiSelectComponent => FdUiSelectComponent),
            multi: true,
        },
    ],
})
export class FdUiSelectComponent<T = unknown> implements ControlValueAccessor {
    private readonly cdr = inject(ChangeDetectorRef);

    protected readonly isEqual = Object.is;
    protected readonly controlRef = viewChild<ElementRef<HTMLButtonElement>>('control');
    protected readonly controlWrapRef = viewChild<ElementRef<HTMLDivElement>>('controlWrap');
    protected readonly listboxRef = viewChild<ElementRef<HTMLDivElement>>('listbox');

    public readonly id = input(`fd-ui-select-${uniqueId++}`);
    public readonly label = input<string>();
    public readonly placeholder = input<string>();
    public readonly error = input<string | null>();
    public readonly required = input(false);
    public readonly options = input<FdUiSelectOption<T>[]>([]);
    public readonly size = input<FdUiFieldSize>('md');
    public readonly fillColor = input<string | null>(null);

    protected internalValue: T | null = null;
    protected disabled = false;
    protected isFocused = false;
    protected isOpen = false;
    protected activeIndex = -1;
    protected overlayMinWidth = 0;

    private onChange: (value: T | null) => void = () => undefined;
    private onTouched: () => void = () => undefined;

    protected get sizeClass(): string {
        return `fd-ui-select--size-${this.size()}`;
    }

    protected get selectedIndex(): number {
        return this.options().findIndex(option => this.isEqual(option.value, this.internalValue));
    }

    protected get shouldFloatLabel(): boolean {
        return this.isFocused || this.selectedIndex >= 0;
    }

    protected get selectedLabel(): string {
        if (this.selectedIndex < 0) {
            return this.isFocused ? (this.placeholder() ?? '') : '';
        }

        return this.options()[this.selectedIndex]?.label ?? '';
    }

    protected get hasValue(): boolean {
        return this.selectedIndex >= 0;
    }

    protected get activeOptionId(): string | null {
        if (!this.isOpen || this.activeIndex < 0 || this.activeIndex >= this.options().length) {
            return null;
        }

        return this.getOptionId(this.activeIndex);
    }

    public writeValue(value: T | null): void {
        this.internalValue = value;
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

    protected onOptionSelect(option: FdUiSelectOption<T>): void {
        if (this.disabled) {
            return;
        }

        this.internalValue = option.value;
        this.onChange(this.internalValue);
        this.onTouched();
        this.closeMenu();
    }

    protected onFocus(): void {
        this.isFocused = true;
    }

    protected onBlur(): void {
        if (!this.isOpen) {
            this.isFocused = false;
            this.onTouched();
        }
    }

    protected openMenu(event?: Event): void {
        event?.preventDefault();

        if (this.disabled || this.isOpen) {
            return;
        }

        this.overlayMinWidth = this.controlWrapRef()?.nativeElement.getBoundingClientRect().width ?? 0;
        this.isOpen = true;
        this.isFocused = true;
        this.activeIndex = Math.max(this.selectedIndex, 0);
    }

    protected closeMenu(): void {
        this.isOpen = false;
        this.isFocused = false;
    }

    protected toggleMenu(event?: Event): void {
        if (this.isOpen) {
            event?.preventDefault();
            this.closeMenu();
            return;
        }

        this.openMenu(event);
    }

    protected onControlWrapClick(event: MouseEvent): void {
        if (this.disabled) {
            return;
        }

        const target = event.target as HTMLElement | null;
        if (target?.closest('.fd-ui-select__control')) {
            return;
        }

        this.controlRef()?.nativeElement.focus();
        this.toggleMenu(event);
    }

    protected onControlKeydown(event: KeyboardEvent): void {
        switch (event.key) {
            case 'ArrowDown':
            case 'ArrowUp':
            case 'Enter':
            case ' ':
                this.openMenu(event);
                break;
            case 'Escape':
                if (this.isOpen) {
                    event.preventDefault();
                    this.closeMenu();
                }
                break;
        }
    }

    protected onListboxKeydown(event: KeyboardEvent): void {
        const options = this.options();
        if (!options.length) {
            return;
        }

        switch (event.key) {
            case 'ArrowDown':
                event.preventDefault();
                this.activeIndex = (this.activeIndex + 1 + options.length) % options.length;
                break;
            case 'ArrowUp':
                event.preventDefault();
                this.activeIndex = (this.activeIndex - 1 + options.length) % options.length;
                break;
            case 'Home':
                event.preventDefault();
                this.activeIndex = 0;
                break;
            case 'End':
                event.preventDefault();
                this.activeIndex = options.length - 1;
                break;
            case 'Enter':
            case ' ':
                event.preventDefault();
                if (this.activeIndex >= 0) {
                    this.onOptionSelect(options[this.activeIndex]);
                }
                break;
            case 'Escape':
                event.preventDefault();
                this.closeMenu();
                this.controlRef()?.nativeElement.focus();
                break;
        }
    }

    protected onMenuAttached(): void {
        queueMicrotask(() => {
            this.listboxRef()?.nativeElement.focus();
        });
    }

    protected getOptionId(index: number): string {
        return `${this.id()}-option-${index}`;
    }
}
