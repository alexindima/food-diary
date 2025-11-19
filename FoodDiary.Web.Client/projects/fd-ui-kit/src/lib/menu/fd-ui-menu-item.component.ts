
import { Component, input, output } from '@angular/core';
import { MatMenuModule } from '@angular/material/menu';
import { RouterModule } from '@angular/router';

@Component({
    selector: 'fd-ui-menu-item',
    standalone: true,
    imports: [MatMenuModule, RouterModule],
    template: `
        <button
            mat-menu-item
            [type]="type()"
            [routerLink]="routerLink()"
            [queryParams]="queryParams()"
            [fragment]="fragment()"
            [disabled]="disabled()"
            (click)="onClick($event)"
        >
            <ng-content />
        </button>
    `,
})
export class FdUiMenuItemComponent {
    public readonly type = input<'button' | 'submit' | 'reset'>('button');
    public readonly routerLink = input<string | any[] | null>();
    public readonly queryParams = input<Record<string, unknown>>();
    public readonly fragment = input<string>();
    public readonly disabled = input(false);
    public readonly itemClick = output<Event>();

    public onClick(event: Event): void {
        this.itemClick.emit(event);
    }
}
