import { ChangeDetectionStrategy, Component, input, output } from '@angular/core';
import { TranslatePipe } from '@ngx-translate/core';
import { FdUiButtonComponent } from 'fd-ui-kit';
import { SidebarActionItem } from './sidebar.models';

@Component({
    selector: 'fd-sidebar-action-links',
    standalone: true,
    imports: [TranslatePipe, FdUiButtonComponent],
    template: `
        @for (item of items(); track item.id) {
            <fd-ui-button
                type="button"
                [class]="getButtonClass(item)"
                [variant]="item.variant ?? 'secondary'"
                [fill]="item.fill ?? 'outline'"
                [icon]="item.icon"
                (click)="actionSelected.emit(item.action)"
            >
                {{ item.labelKey | translate }}
                @if (item.badge) {
                    <span class="sidebar-action-links__badge">{{ item.badge }}</span>
                }
            </fd-ui-button>
        }
    `,
    styles: [
        `
            :host {
                display: contents;
            }

            .sidebar-action-links__badge {
                margin-left: auto;
                min-width: 22px;
                height: 22px;
                padding: 0 7px;
                border-radius: 999px;
                display: inline-flex;
                align-items: center;
                justify-content: center;
                background: var(--fd-color-danger);
                color: var(--fd-color-white);
                font-size: var(--fd-text-helper-size, 0.75rem);
                font-weight: var(--fd-text-stat-label-weight, 600);
                line-height: 1;
            }
        `,
    ],
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
