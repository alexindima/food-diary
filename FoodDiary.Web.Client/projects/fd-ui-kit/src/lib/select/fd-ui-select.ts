import { CdkConnectedOverlay, CdkOverlayOrigin } from '@angular/cdk/overlay';
import { CommonModule } from '@angular/common';
import {
    afterNextRender,
    afterRenderEffect,
    ChangeDetectionStrategy,
    Component,
    computed,
    effect,
    type ElementRef,
    inject,
    Injector,
    input,
    model,
    output,
    signal,
    untracked,
    viewChild,
} from '@angular/core';
import type { FormValueControl, ValidationError } from '@angular/forms/signals';

import { injectFdUiControlFocus } from '../form/fd-ui-control-focus';
import { injectFdUiFieldError } from '../form-error/fd-ui-signal-field-error';
import { FdUiIconComponent } from '../icon/fd-ui-icon';
import type { FdUiFieldSize } from '../types/field-size.type';

const NO_ACTIVE_OPTION_INDEX = -1;
const FIRST_OPTION_INDEX = 0;
const NEXT_OPTION_OFFSET = 1;
const PREVIOUS_OPTION_OFFSET = -1;

let uniqueId = 0;

export type FdUiSelectOption<T = unknown> = {
    value: T;
    label: string;
    hint?: string;
};

@Component({
    selector: 'fd-ui-select',
    imports: [CommonModule, FdUiIconComponent, CdkOverlayOrigin, CdkConnectedOverlay],
    templateUrl: './fd-ui-select.html',
    styleUrls: ['./fd-ui-select.scss'],
    changeDetection: ChangeDetectionStrategy.OnPush,
})
export class FdUiSelectComponent<T = unknown> implements FormValueControl<T | null> {
    private readonly injector = inject(Injector);
    protected readonly isEqual = Object.is;
    private readonly controlWrapRef = viewChild.required<ElementRef<HTMLDivElement>>('controlWrap');
    protected readonly listboxRef = viewChild<ElementRef<HTMLDivElement>>('listbox');
    private renderedOptions: ReadonlyArray<FdUiSelectOption<T>> = [];

    public readonly id = input(`fd-ui-select-${uniqueId++}`);
    public readonly appearance = input<'field' | 'text'>('field');
    public readonly label = input<string>();
    public readonly ariaLabel = input<string>();
    public readonly placeholder = input<string>();
    public readonly error = input<string | null>();
    public readonly errors = input<readonly ValidationError.WithOptionalFieldTree[]>([]);
    public readonly required = input(false);
    public readonly options = input<Array<FdUiSelectOption<T>>>([]);
    public readonly size = input<FdUiFieldSize>('md');
    public readonly fillColor = input<string | null>(null);
    public readonly value = model<T | null>(null);
    public readonly optionSelected = output<FdUiSelectOption<T>>();
    public readonly touched = model(false);
    protected readonly resolvedError = injectFdUiFieldError(this.error, this.errors, this.touched);
    public readonly touch = output();
    public readonly disabled = input(false);

    protected readonly internalValue = computed(() => this.value());
    protected readonly isFocused = signal(false);
    protected readonly isOpen = signal(false);
    protected readonly activeIndex = signal(NO_ACTIVE_OPTION_INDEX);
    protected readonly overlayMinWidth = signal(0);

    // eslint-disable-next-line local/prefer-protected-template-members -- Public Signal Forms focus contract.
    public readonly focus = injectFdUiControlFocus();

    public constructor() {
        afterRenderEffect({
            read: () => {
                this.renderedOptions = this.options();
            },
        });
        effect(() => {
            const options = this.options();
            if (this.isOpen()) {
                const index = untracked(this.activeIndex);
                this.activeIndex.set(
                    options.length === 0 ? NO_ACTIVE_OPTION_INDEX : Math.min(Math.max(index, FIRST_OPTION_INDEX), options.length - 1),
                );
            }
        });
    }

    protected readonly sizeClass = computed(() => `fd-ui-select--size-${this.size()}`);
    protected readonly hasError = computed(() => {
        const error = this.resolvedError();

        return typeof error === 'string' && error.trim().length > 0;
    });
    protected readonly hostClass = computed(
        () =>
            `fd-ui-select fd-ui-select--appearance-${this.appearance()} ${this.sizeClass()}${this.hasError() ? ' fd-ui-select--has-error' : ''}${this.shouldFloatLabel() ? ' fd-ui-select--floating' : ''}`,
    );
    private readonly selection = computed(() => {
        const options = this.options();
        const index = options.findIndex(option => this.isEqual(option.value, this.internalValue()));
        return { index, label: index === NO_ACTIVE_OPTION_INDEX ? null : options[index].label };
    });

    protected readonly shouldFloatLabel = computed(() => {
        const label = this.label();

        return label !== undefined && label.trim().length > 0 && (this.isFocused() || this.selection().index >= 0);
    });

    protected readonly selectedLabel = computed(() => {
        const label = this.selection().label;
        return label ?? (this.isFocused() ? (this.placeholder() ?? '') : '');
    });

    protected readonly hasValue = computed(() => this.selection().index >= 0);

    protected readonly activeOptionId = computed(() => {
        const activeIndex = this.activeIndex();
        if (!this.isOpen() || activeIndex < FIRST_OPTION_INDEX || activeIndex >= this.options().length) {
            return null;
        }

        return this.getOptionId(activeIndex);
    });

    protected onOptionSelect(option: FdUiSelectOption<T>): void {
        if (this.disabled()) {
            return;
        }

        if (!this.isEqual(this.value(), option.value)) {
            this.value.set(option.value);
            this.optionSelected.emit(option);
        }
        this.touched.set(true);
        this.touch.emit();
        this.closeMenu();
        this.focus();
    }

    protected onFocus(): void {
        this.isFocused.set(true);
    }

    protected onBlur(): void {
        if (!this.isOpen()) {
            this.isFocused.set(false);
            this.touched.set(true);
            this.touch.emit();
        }
    }

    protected openMenu(event?: Event): void {
        event?.preventDefault();

        if (this.disabled() || this.isOpen()) {
            return;
        }

        this.overlayMinWidth.set(this.controlWrapRef().nativeElement.getBoundingClientRect().width);
        this.isOpen.set(true);
        this.isFocused.set(true);
        this.activeIndex.set(this.options().length === 0 ? NO_ACTIVE_OPTION_INDEX : Math.max(this.selection().index, FIRST_OPTION_INDEX));
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

    protected onControlWrapClick(event: MouseEvent, control: HTMLButtonElement): void {
        if (this.disabled() || event.composedPath().includes(control)) {
            return;
        }

        control.focus();
        this.toggleMenu(event);
    }

    protected onControlKeydown(event: KeyboardEvent): void {
        switch (event.key) {
            case 'ArrowDown':
            case 'ArrowUp':
            case 'Enter':
            case ' ': {
                this.openMenu(event);
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

    protected onListboxKeydown(event: KeyboardEvent): void {
        if (event.key === 'Tab') {
            this.closeMenu();
            this.focus();
            return;
        }

        const options = this.renderedOptions;
        if (options.length === 0 && event.key !== 'Escape') {
            return;
        }

        const handled = this.handleListboxNavigationKey(event, options);
        if (handled) {
            event.preventDefault();
        }
    }

    private handleListboxNavigationKey(event: KeyboardEvent, options: ReadonlyArray<FdUiSelectOption<T>>): boolean {
        const actions: Partial<Record<string, () => void>> = {
            ArrowDown: () => {
                this.activeIndex.update(activeIndex => (activeIndex + NEXT_OPTION_OFFSET + options.length) % options.length);
            },
            ArrowUp: () => {
                this.activeIndex.update(activeIndex => (activeIndex + PREVIOUS_OPTION_OFFSET + options.length) % options.length);
            },
            Home: () => {
                this.activeIndex.set(FIRST_OPTION_INDEX);
            },
            End: () => {
                this.activeIndex.set(options.length + PREVIOUS_OPTION_OFFSET);
            },
            Escape: () => {
                this.closeMenu();
                this.focus();
            },
        };

        if (event.key === 'Enter' || event.key === ' ') {
            this.selectActiveOption(options);
            return true;
        }

        const action = actions[event.key];
        if (action === undefined) {
            return false;
        }

        action();
        if (this.isOpen()) {
            this.scrollActiveOptionIntoView();
        }
        return true;
    }

    private selectActiveOption(options: ReadonlyArray<FdUiSelectOption<T>>): void {
        const index = this.activeIndex();
        const option = index === NO_ACTIVE_OPTION_INDEX ? undefined : options.at(index);
        if (option !== undefined && this.options().some(current => this.isEqual(option.value, current.value))) {
            this.onOptionSelect(option);
        }
    }

    protected onMenuAttached(): void {
        afterNextRender(
            () => {
                this.listboxRef()?.nativeElement.focus();
                this.scrollActiveOptionIntoView();
            },
            { injector: this.injector },
        );
    }

    protected getOptionId(index: number): string {
        return `${this.id()}-option-${index}`;
    }

    private scrollActiveOptionIntoView(): void {
        this.listboxRef()?.nativeElement.children.item(this.activeIndex())?.scrollIntoView({ block: 'nearest', inline: 'nearest' });
    }
}
