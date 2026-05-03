import { CommonModule } from '@angular/common';
import { ChangeDetectionStrategy, Component, computed, type ElementRef, input, model, viewChildren } from '@angular/core';

import { FdUiHintDirective } from '../hint/fd-ui-hint.directive';

export type FdUiEmojiPickerValue = string | number;

export interface FdUiEmojiPickerOption<T extends FdUiEmojiPickerValue = FdUiEmojiPickerValue> {
    value: T;
    emoji: string;
    label?: string;
    description?: string;
    ariaLabel?: string;
    hint?: string;
    disabled?: boolean;
}

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
        const option = this.options().find(item => !item.disabled);
        return option?.value ?? null;
    });

    protected isSelected(value: FdUiEmojiPickerValue): boolean {
        return this.selectedValue() === value;
    }

    protected getTabIndex(value: FdUiEmojiPickerValue): number {
        const selectedValue = this.selectedValue();
        if (selectedValue !== null) {
            return selectedValue === value ? 0 : -1;
        }

        return this.firstEnabledValue() === value ? 0 : -1;
    }

    protected select(option: FdUiEmojiPickerOption): void {
        if (option.disabled || option.value === this.selectedValue()) {
            return;
        }

        this.selectedValue.set(option.value);
    }

    protected handleKeydown(index: number, event: KeyboardEvent): void {
        switch (event.key) {
            case 'ArrowRight':
            case 'ArrowDown':
                event.preventDefault();
                this.focusRelative(index, 1);
                break;
            case 'ArrowLeft':
            case 'ArrowUp':
                event.preventDefault();
                this.focusRelative(index, -1);
                break;
            case 'Home':
                event.preventDefault();
                this.focusIndex(this.findEnabledIndex(0, 1));
                break;
            case 'End':
                event.preventDefault();
                this.focusIndex(this.findEnabledIndex(this.options().length - 1, -1));
                break;
            case ' ':
            case 'Enter': {
                event.preventDefault();
                const option = this.options().at(index);
                if (option) {
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
        if (!options.length) {
            return -1;
        }

        let index = startIndex;
        while (index >= 0 && index < options.length) {
            const option = options.at(index);
            if (option && !option.disabled) {
                return index;
            }
            index += direction;
        }

        return -1;
    }

    private focusIndex(index: number): void {
        if (index < 0) {
            return;
        }

        const option = this.options().at(index);
        if (!option || option.disabled) {
            return;
        }

        this.select(option);
        this.optionButtons()[index]?.nativeElement.focus();
    }
}
