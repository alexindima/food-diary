import { CommonModule } from '@angular/common';
import { ChangeDetectionStrategy, Component, computed, input, model } from '@angular/core';

export interface FdUiSegmentedToggleOption {
    label: string;
    value: string;
}

export type FdUiSegmentedToggleAppearance = 'default' | 'soft';
export type FdUiSegmentedToggleSize = 'sm' | 'md';

@Component({
    selector: 'fd-ui-segmented-toggle',
    standalone: true,
    imports: [CommonModule],
    templateUrl: './fd-ui-segmented-toggle.component.html',
    styleUrls: ['./fd-ui-segmented-toggle.component.scss'],
    changeDetection: ChangeDetectionStrategy.OnPush,
    host: {
        '[class.fd-ui-segmented-toggle-host--full-width]': 'fullWidth()',
    },
})
export class FdUiSegmentedToggleComponent {
    public readonly options = input<FdUiSegmentedToggleOption[]>([]);
    public readonly selectedValue = model.required<string>();
    public readonly ariaLabel = input<string | null>(null);
    public readonly appearance = input<FdUiSegmentedToggleAppearance>('default');
    public readonly size = input<FdUiSegmentedToggleSize>('md');
    public readonly fullWidth = input(false);
    public readonly shrinkItems = input(false);

    protected readonly containerClass = computed(() => {
        const classes = [
            'fd-ui-segmented-toggle',
            `fd-ui-segmented-toggle--appearance-${this.appearance()}`,
            `fd-ui-segmented-toggle--size-${this.size()}`,
        ];
        if (this.fullWidth()) {
            classes.push('fd-ui-segmented-toggle--full-width');
        }
        if (this.shrinkItems()) {
            classes.push('fd-ui-segmented-toggle--shrink-items');
        }
        return classes.join(' ');
    });

    protected select(value: string): void {
        if (value === this.selectedValue()) {
            return;
        }

        this.selectedValue.set(value);
    }
}
