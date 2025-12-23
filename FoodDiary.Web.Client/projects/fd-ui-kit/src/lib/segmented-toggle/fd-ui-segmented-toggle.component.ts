import { CommonModule } from '@angular/common';
import { ChangeDetectionStrategy, Component, input, model } from '@angular/core';

export interface FdUiSegmentedToggleOption {
    label: string;
    value: string;
}

@Component({
    selector: 'fd-ui-segmented-toggle',
    standalone: true,
    imports: [CommonModule],
    templateUrl: './fd-ui-segmented-toggle.component.html',
    styleUrls: ['./fd-ui-segmented-toggle.component.scss'],
    changeDetection: ChangeDetectionStrategy.OnPush,
})
export class FdUiSegmentedToggleComponent {
    public readonly options = input<FdUiSegmentedToggleOption[]>([]);
    public readonly selectedValue = model.required<string>();
    public readonly ariaLabel = input<string | null>(null);

    protected select(value: string): void {
        if (value === this.selectedValue()) {
            return;
        }

        this.selectedValue.set(value);
    }
}
