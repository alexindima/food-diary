import { ChangeDetectionStrategy, Component, computed, input, model } from '@angular/core';
import { FdUiHintDirective } from '../hint/fd-ui-hint.directive';

export interface FdUiChipSelectOption {
    value: string;
    label: string;
    disabled?: boolean;
    ariaLabel?: string | null;
    hint?: string | null;
}

export type FdUiChipSelectSize = 'sm' | 'md';

@Component({
    selector: 'fd-ui-chip-select',
    standalone: true,
    imports: [FdUiHintDirective],
    templateUrl: './fd-ui-chip-select.component.html',
    styleUrls: ['./fd-ui-chip-select.component.scss'],
    changeDetection: ChangeDetectionStrategy.OnPush,
})
export class FdUiChipSelectComponent {
    public readonly options = input<FdUiChipSelectOption[]>([]);
    public readonly selectedValues = model<string[]>([]);
    public readonly ariaLabel = input<string | null>(null);
    public readonly size = input<FdUiChipSelectSize>('md');

    protected readonly classes = computed(() => ['fd-ui-chip-select', `fd-ui-chip-select--size-${this.size()}`].join(' '));

    protected isSelected(value: string): boolean {
        return this.selectedValues().includes(value);
    }

    protected toggle(value: string): void {
        const current = this.selectedValues();
        if (current.includes(value)) {
            this.selectedValues.set(current.filter(item => item !== value));
            return;
        }

        this.selectedValues.set([...current, value]);
    }
}
