import { CommonModule } from '@angular/common';
import {
    ChangeDetectionStrategy,
    Component,
    EventEmitter,
    Input,
    Output,
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
    imports: [CommonModule, MatTabsModule, TranslateModule],
    templateUrl: './fd-ui-tabs.component.html',
    styleUrls: ['./fd-ui-tabs.component.scss'],
    changeDetection: ChangeDetectionStrategy.OnPush,
})
export class FdUiTabsComponent {
    @Input() public tabs: FdUiTab[] = [];
    @Input() public selectedValue?: string;
    @Output() public selectedValueChange = new EventEmitter<string>();

    protected get selectedIndex(): number {
        const index = this.tabs.findIndex(tab => tab.value === this.selectedValue);
        return index >= 0 ? index : 0;
    }

    protected handleIndexChange(index: number): void {
        const tab = this.tabs[index];
        if (!tab) {
            return;
        }
        this.selectedValue = tab.value;
        this.selectedValueChange.emit(tab.value);
    }
}
