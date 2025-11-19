
import {
  ChangeDetectionStrategy,
  Component,
  input, model,
  output
} from '@angular/core';
import { MatTabsModule } from '@angular/material/tabs';
import { TranslateModule } from '@ngx-translate/core';

export interface FdUiTab {
    value: string;
    label?: string;
    labelKey?: string;
}

@Component({
    selector: 'fd-ui-tabs',
    standalone: true,
    imports: [MatTabsModule, TranslateModule],
    templateUrl: './fd-ui-tabs.component.html',
    styleUrls: ['./fd-ui-tabs.component.scss'],
    changeDetection: ChangeDetectionStrategy.OnPush,
})
export class FdUiTabsComponent {
    public readonly tabs = input<FdUiTab[]>([]);
    public readonly selectedValue = model.required<string>();
    public readonly selectedValueChange = output<string>();

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
