import { ChangeDetectionStrategy, Component, ViewEncapsulation, input, model, output } from '@angular/core';
import { MatTabsModule } from '@angular/material/tabs';
import { TranslateModule } from '@ngx-translate/core';

export interface FdUiTab {
    value: string;
    label?: string;
    labelKey?: string;
}

export type FdUiTabsAppearance = 'default' | 'wrap-compact';

@Component({
    selector: 'fd-ui-tabs',
    standalone: true,
    imports: [MatTabsModule, TranslateModule],
    templateUrl: './fd-ui-tabs.component.html',
    styleUrls: ['./fd-ui-tabs.component.scss'],
    changeDetection: ChangeDetectionStrategy.OnPush,
    encapsulation: ViewEncapsulation.None,
})
export class FdUiTabsComponent {
    public readonly tabs = input<FdUiTab[]>([]);
    public readonly selectedValue = model.required<string>();
    public readonly selectedValueChange = output<string>();
    public readonly appearance = input<FdUiTabsAppearance>('default');

    protected get appearanceClass(): string {
        return `fd-ui-tabs--appearance-${this.appearance()}`;
    }

    protected get selectedIndex(): number {
        const index = this.tabs().findIndex(tab => tab.value === this.selectedValue());
        return index >= 0 ? index : 0;
    }

    protected handleIndexChange(index: number): void {
        const tab = this.tabs()[index];
        if (!tab) {
            return;
        }
        this.selectedValue.set(tab.value);
        this.selectedValueChange.emit(tab.value);
    }
}
