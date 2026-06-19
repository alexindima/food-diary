import { CdkConnectedOverlay, CdkOverlayOrigin } from '@angular/cdk/overlay';
import { CommonModule } from '@angular/common';
import {
    ChangeDetectionStrategy,
    Component,
    computed,
    effect,
    type ElementRef,
    input,
    model,
    output,
    signal,
    viewChild,
} from '@angular/core';
import type { FormValueControl } from '@angular/forms/signals';

import { FdUiIconComponent } from '../icon/fd-ui-icon';
import type { FdUiFieldSize } from '../types/field-size.type';
import type { FdUiAutocompleteOption } from './fd-ui-autocomplete.types';
import { FdUiAutocompleteMenuComponent } from './fd-ui-autocomplete-menu';

export type { FdUiAutocompleteOption } from './fd-ui-autocomplete.types';

const NO_ACTIVE_OPTION_INDEX = -1;
const FIRST_OPTION_INDEX = 0;
const NEXT_OPTION_OFFSET = 1;
const PREVIOUS_OPTION_OFFSET = -1;
const EMPTY_STATE_MIN_QUERY_LENGTH = 2;

let uniqueId = 0;

@Component({
    selector: 'fd-ui-autocomplete',
    imports: [CommonModule, CdkOverlayOrigin, CdkConnectedOverlay, FdUiIconComponent, FdUiAutocompleteMenuComponent],
    templateUrl: './fd-ui-autocomplete.html',
    styleUrls: ['./fd-ui-autocomplete.scss'],
    changeDetection: ChangeDetectionStrategy.OnPush,
})
export class FdUiAutocompleteComponent<T = unknown> implements FormValueControl<T | string | null> {
    protected readonly controlRef = viewChild<ElementRef<HTMLInputElement>>('control');
    protected readonly controlWrapRef = viewChild<ElementRef<HTMLDivElement>>('controlWrap');
    protected readonly listboxRef = viewChild<ElementRef<HTMLDivElement>>('listbox');
    protected readonly isEqual = Object.is;

    public readonly id = input(`fd-ui-autocomplete-${uniqueId++}`);
    public readonly label = input<string>();
    public readonly placeholder = input<string>();
    public readonly error = input<string | null>();
    public readonly required = input(false);
    public readonly maximumLength = input<number>();
    public readonly options = input<Array<FdUiAutocompleteOption<T>>>([]);
    public readonly loading = input(false);
    public readonly emptyText = input<string>();
    public readonly showEmptyState = input(true);
    public readonly size = input<FdUiFieldSize>('md');
    public readonly fillColor = input<string | null>(null);
    public readonly displayWith = input<(value: T | null) => string>();
    public readonly value = model<T | string | null>(null);
    public readonly touched = model(false);
    public readonly disabled = input(false);

    public readonly queryChange = output<string>();
    public readonly optionSelected = output<FdUiAutocompleteOption<T>>();

    protected readonly internalValue = signal<T | null>(null);
    protected readonly queryText = signal('');
    protected readonly isFocused = signal(false);
    protected readonly isOpen = signal(false);
    protected readonly activeIndex = signal(NO_ACTIVE_OPTION_INDEX);
    protected readonly overlayMinWidth = signal(0);

    public constructor() {
        effect(() => {
            const value = this.value();
            if (typeof value === 'string') {
                this.internalValue.set(null);
                this.queryText.set(value);
                return;
            }

            this.internalValue.set(value);
            this.queryText.set(this.getDisplayText(value));
        });
    }

    protected readonly sizeClass = computed(() => `fd-ui-autocomplete--size-${this.size()}`);
    protected readonly hasError = computed(() => {
        const error = this.error();

        return error !== null && error !== undefined && error.trim().length > 0;
    });
    protected readonly shouldFloatLabel = computed(() => this.isFocused() || this.queryText().trim().length > 0);
    protected readonly hostClass = computed(
        () =>
            `fd-ui-autocomplete ${this.sizeClass()}${this.hasError() ? ' fd-ui-autocomplete--has-error' : ''}${this.shouldFloatLabel() ? ' fd-ui-autocomplete--floating' : ''}`,
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

    protected onInput(value: string): void {
        if (this.disabled()) {
            return;
        }

        this.queryText.set(value);
        this.internalValue.set(null);
        this.value.set(value);
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
            this.touched.set(true);
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
        this.value.set('');
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
        this.value.set(option.value);
        this.touched.set(true);
        this.optionSelected.emit(option);
        this.closeMenu();
        this.controlRef()?.nativeElement.focus();
    }

    protected onControlKeydown(event: KeyboardEvent): void {
        switch (event.key) {
            case 'ArrowDown': {
                event.preventDefault();
                this.openMenu();
                this.moveActive(NEXT_OPTION_OFFSET);
                break;
            }
            case 'ArrowUp': {
                event.preventDefault();
                this.openMenu();
                this.moveActive(PREVIOUS_OPTION_OFFSET);
                break;
            }
            case 'Enter': {
                if (this.isOpen() && this.activeIndex() >= FIRST_OPTION_INDEX) {
                    event.preventDefault();
                    this.selectOption(this.options()[this.activeIndex()]);
                }
                break;
            }
            case 'Escape': {
                if (this.isOpen()) {
                    event.preventDefault();
                    this.closeMenu();
                }
                break;
            }
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
