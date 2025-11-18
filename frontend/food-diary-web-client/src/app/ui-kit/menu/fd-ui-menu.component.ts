import { CommonModule } from '@angular/common';
import { Component, ViewChild } from '@angular/core';
import { MatMenuModule, MatMenu } from '@angular/material/menu';

@Component({
    selector: 'fd-ui-menu',
    standalone: true,
    imports: [CommonModule, MatMenuModule],
    templateUrl: './fd-ui-menu.component.html',
    styleUrls: ['./fd-ui-menu.component.scss'],
    exportAs: 'fdUiMenu',
})
export class FdUiMenuComponent {
    @ViewChild(MatMenu, { static: true }) private readonly matMenuRef!: MatMenu;

    public get matMenu(): MatMenu {
        return this.matMenuRef;
    }
}
