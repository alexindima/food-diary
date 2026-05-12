import { CommonModule } from '@angular/common';
import { ChangeDetectionStrategy, Component, computed, type ElementRef, input, model, viewChildren } from '@angular/core';

import { FdUiHintDirective } from '../hint/fd-ui-hint.directive';

export type FdUiEmojiPickerValue = string | number;

const NOT_FOCUSABLE_TAB_INDEX = -1;
const FOCUSABLE_TAB_INDEX = 0;
const NO_ENABLED_OPTION_INDEX = -1;
const FIRST_OPTION_INDEX = 0;
const NEXT_OPTION_OFFSET = 1;
const PREVIOUS_OPTION_OFFSET = -1;

export type FdUiEmojiPickerOption<T extends FdUiEmojiPickerValue = FdUiEmojiPickerValue> = {
    value: T;
    emoji: string;
    label?: string;
    description?: string;
    ariaLabel?: string;
    hint?: string;
    disabled?: boolean;
};

export type FdUiEmojiPickerSize = 'sm' | 'md';

@Component({
    selector: 'fd-ui-emoji-picker',
    standalone: true,
    imports: [CommonModule, FdUiHintDirective],
    templateUrl: './fd-ui-emoji-picker.component.html',
    styleUrls: ['./fd-ui-emoji-picker.component.scss'],
    changeDetection: ChangeDetectionStrategy.OnPush,
})
export class FdUiEmojiPickerComponent {
    public readonly options = input<FdUiEmojiPickerOption[]>([]);
    public readonly selectedValue = model<FdUiEmojiPickerValue | null>(null);
    public readonly ariaLabel = input<string | null>(null);
    public readonly size = input<FdUiEmojiPickerSize>('md');
    public readonly fullWidth = input(false);
    public readonly showLabels = input(false);
    public readonly showDescriptions = input(false);

    private readonly optionButtons = viewChildren<ElementRef<HTMLButtonElement>>('optionButton');

    protected readonly containerClass = computed(() => {
        const classes = ['fd-ui-emoji-picker', `fd-ui-emoji-picker--size-${this.size()}`];
        if (this.fullWidth()) {
            classes.push('fd-ui-emoji-picker--full-width');
        }
        if (this.showLabels()) {
            classes.push('fd-ui-emoji-picker--with-labels');
        }
        if (this.showDescriptions()) {
            classes.push('fd-ui-emoji-picker--with-descriptions');
        }
        return classes.join(' ');
    });

    private readonly firstEnabledValue = computed<FdUiEmojiPickerValue | null>(() => {
        const option = this.options().find(item => item.disabled !== true);
        return option?.value ?? null;
    });

    protected isSelected(value: FdUiEmojiPickerValue): boolean {
        return this.selectedValue() === value;
    }

    protected getTabIndex(value: FdUiEmojiPickerValue): number {
        const selectedValue = this.selectedValue();
        if (selectedValue !== null) {
            return selectedValue === value ? FOCUSABLE_TAB_INDEX : NOT_FOCUSABLE_TAB_INDEX;
        }

        return this.firstEnabledValue() === value ? FOCUSABLE_TAB_INDEX : NOT_FOCUSABLE_TAB_INDEX;
    }

    protected select(option: FdUiEmojiPickerOption): void {
        if (option.disabled === true || option.value === this.selectedValue()) {
            return;
        }

        this.selectedValue.set(option.value);
    }

    protected handleKeydown(index: number, event: KeyboardEvent): void {
        switch (event.key) {
            case 'ArrowRight':
            case 'ArrowDown':
                event.preventDefault();
                this.focusRelative(index, NEXT_OPTION_OFFSET);
                break;
            case 'ArrowLeft':
            case 'ArrowUp':
                event.preventDefault();
                this.focusRelative(index, PREVIOUS_OPTION_OFFSET);
                break;
            case 'Home':
                event.preventDefault();
                this.focusIndex(this.findEnabledIndex(FIRST_OPTION_INDEX, NEXT_OPTION_OFFSET));
                break;
            case 'End':
                event.preventDefault();
                this.focusIndex(this.findEnabledIndex(this.options().length + PREVIOUS_OPTION_OFFSET, PREVIOUS_OPTION_OFFSET));
                break;
            case ' ':
            case 'Enter': {
                event.preventDefault();
                const option = this.options().at(index);
                if (option !== undefined) {
                    this.select(option);
                }
                break;
            }
            default:
                break;
        }
    }

    protected getOptionAriaLabel(option: FdUiEmojiPickerOption): string {
        return option.ariaLabel ?? option.label ?? String(option.value);
    }

    private focusRelative(startIndex: number, direction: 1 | -1): void {
        const nextIndex = this.findEnabledIndex(startIndex + direction, direction);
        this.focusIndex(nextIndex);
    }

    private findEnabledIndex(startIndex: number, direction: 1 | -1): number {
        const options = this.options();
        if (options.length === 0) {
            return NO_ENABLED_OPTION_INDEX;
        }

        let index = startIndex;
        while (index >= FIRST_OPTION_INDEX && index < options.length) {
            const option = options.at(index);
            if (option !== undefined && option.disabled !== true) {
                return index;
            }
            index += direction;
        }

        return NO_ENABLED_OPTION_INDEX;
    }

    private focusIndex(index: number): void {
        if (index < FIRST_OPTION_INDEX) {
            return;
        }

        const option = this.options().at(index);
        if (option === undefined || option.disabled === true) {
            return;
        }

        this.select(option);
        this.optionButtons()[index]?.nativeElement.focus();
    }
}
