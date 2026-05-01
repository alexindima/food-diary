import { ChangeDetectionStrategy, Component, input, output } from '@angular/core';
import { TranslatePipe } from '@ngx-translate/core';
import { FdUiButtonComponent } from 'fd-ui-kit';

import { SidebarActionItem } from './sidebar.models';

@Component({
    selector: 'fd-sidebar-action-links',
    standalone: true,
    imports: [TranslatePipe, FdUiButtonComponent],
    templateUrl: './sidebar-action-links.component.html',
    styleUrls: ['./sidebar-action-links.component.scss'],
    changeDetection: ChangeDetectionStrategy.OnPush,
})
export class SidebarActionLinksComponent {
    public readonly items = input.required<SidebarActionItem[]>();
    public readonly buttonClass = input('');
    public readonly actionSelected = output<SidebarActionItem['action']>();

    protected getButtonClass(item: SidebarActionItem): string {
        const baseClass = this.buttonClass();

        if (!item.className) {
            return baseClass;
        }

        return `${baseClass} ${item.className}`.trim();
    }
}
