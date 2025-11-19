
import { Component, viewChild } from '@angular/core';
import { MatMenuModule, MatMenu } from '@angular/material/menu';

@Component({
    selector: 'fd-ui-menu',
    standalone: true,
    imports: [MatMenuModule],
    templateUrl: './fd-ui-menu.component.html',
    styleUrls: ['./fd-ui-menu.component.scss'],
    exportAs: 'fdUiMenu',
})
export class FdUiMenuComponent {
    private readonly matMenuRef = viewChild.required(MatMenu);

    public get matMenu(): MatMenu {
        return this.matMenuRef();
    }
}
