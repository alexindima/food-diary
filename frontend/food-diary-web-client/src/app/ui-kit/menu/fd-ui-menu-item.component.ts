import { CommonModule } from '@angular/common';
import { Component, EventEmitter, Input, Output } from '@angular/core';
import { MatMenuModule } from '@angular/material/menu';
import { RouterModule } from '@angular/router';

@Component({
    selector: 'fd-ui-menu-item',
    standalone: true,
    imports: [CommonModule, MatMenuModule, RouterModule],
    template: `
        <button
            mat-menu-item
            [type]="type"
            [routerLink]="routerLink"
            [queryParams]="queryParams"
            [fragment]="fragment"
            [disabled]="disabled"
            (click)="onClick($event)"
        >
            <ng-content></ng-content>
        </button>
    `,
})
export class FdUiMenuItemComponent {
    @Input() public type: 'button' | 'submit' | 'reset' = 'button';
    @Input() public routerLink?: string | any[] | null;
    @Input() public queryParams?: Record<string, unknown>;
    @Input() public fragment?: string;
    @Input() public disabled = false;
    @Output() public itemClick = new EventEmitter<Event>();

    public onClick(event: Event): void {
        this.itemClick.emit(event);
    }
}
