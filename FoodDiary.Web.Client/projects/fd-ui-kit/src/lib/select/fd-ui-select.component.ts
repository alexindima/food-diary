import { CdkConnectedOverlay, CdkOverlayOrigin } from '@angular/cdk/overlay';
import { CommonModule } from '@angular/common';
import { ChangeDetectionStrategy, Component, computed, type ElementRef, input, signal, viewChild } from '@angular/core';
import { type ControlValueAccessor, NG_VALUE_ACCESSOR } from '@angular/forms';

import { FdUiIconComponent } from '../icon/fd-ui-icon.component';
import type { FdUiFieldSize } from '../types/field-size.type';

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
    providers: [
        {
            provide: NG_VALUE_ACCESSOR,
            useExisting: FdUiSelectComponent,
            multi: true,
        },
    ],
})
export class FdUiSelectComponent<T = unknown> implements ControlValueAccessor {
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

    protected readonly internalValue = signal<T | null>(null);
    protected readonly disabled = signal(false);
    protected readonly isFocused = signal(false);
    protected readonly isOpen = signal(false);
    protected readonly activeIndex = signal(-1);
    protected readonly overlayMinWidth = signal(0);

    private onChange: (value: T | null) => void = () => undefined;
    private onTouched: () => void = () => undefined;

    protected readonly sizeClass = computed(() => `fd-ui-select--size-${this.size()}`);
    protected readonly hostClass = computed(
        () =>
            `fd-ui-select ${this.sizeClass()}${this.error() ? ' fd-ui-select--has-error' : ''}${this.shouldFloatLabel() ? ' fd-ui-select--floating' : ''}`,
    );
    protected readonly selectedIndex = computed(() => this.options().findIndex(option => this.isEqual(option.value, this.internalValue())));

    protected readonly shouldFloatLabel = computed(() => this.isFocused() || this.selectedIndex() >= 0);

    protected readonly selectedLabel = computed(() => {
        const selectedIndex = this.selectedIndex();
        if (selectedIndex < 0) {
            return this.isFocused() ? (this.placeholder() ?? '') : '';
        }

        return this.options()[selectedIndex]?.label ?? '';
    });

    protected readonly hasValue = computed(() => this.selectedIndex() >= 0);

    protected readonly activeOptionId = computed(() => {
        const activeIndex = this.activeIndex();
        if (!this.isOpen() || activeIndex < 0 || activeIndex >= this.options().length) {
            return null;
        }

        return this.getOptionId(activeIndex);
    });

    public writeValue(value: T | null): void {
        this.internalValue.set(value);
    }

    public registerOnChange(fn: (value: T | null) => void): void {
        this.onChange = fn;
    }

    public registerOnTouched(fn: () => void): void {
        this.onTouched = fn;
    }

    public setDisabledState(isDisabled: boolean): void {
        this.disabled.set(isDisabled);
    }

    protected onOptionSelect(option: FdUiSelectOption<T>): void {
        if (this.disabled()) {
            return;
        }

        this.internalValue.set(option.value);
        this.onChange(this.internalValue());
        this.onTouched();
        this.closeMenu();
    }

    protected onFocus(): void {
        this.isFocused.set(true);
    }

    protected onBlur(): void {
        if (!this.isOpen()) {
            this.isFocused.set(false);
            this.onTouched();
        }
    }

    protected openMenu(event?: Event): void {
        event?.preventDefault();

        if (this.disabled() || this.isOpen()) {
            return;
        }

        this.overlayMinWidth.set(this.controlWrapRef()?.nativeElement.getBoundingClientRect().width ?? 0);
        this.isOpen.set(true);
        this.isFocused.set(true);
        this.activeIndex.set(Math.max(this.selectedIndex(), 0));
    }

    protected closeMenu(): void {
        this.isOpen.set(false);
        this.isFocused.set(false);
    }

    protected toggleMenu(event?: Event): void {
        if (this.isOpen()) {
            event?.preventDefault();
            this.closeMenu();
            return;
        }

        this.openMenu(event);
    }

    protected onControlWrapClick(event: MouseEvent): void {
        if (this.disabled()) {
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
                if (this.isOpen()) {
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
                this.activeIndex.update(activeIndex => (activeIndex + 1 + options.length) % options.length);
                break;
            case 'ArrowUp':
                event.preventDefault();
                this.activeIndex.update(activeIndex => (activeIndex - 1 + options.length) % options.length);
                break;
            case 'Home':
                event.preventDefault();
                this.activeIndex.set(0);
                break;
            case 'End':
                event.preventDefault();
                this.activeIndex.set(options.length - 1);
                break;
            case 'Enter':
            case ' ':
                event.preventDefault();
                if (this.activeIndex() >= 0) {
                    this.onOptionSelect(options[this.activeIndex()]);
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
