import { ChangeDetectionStrategy, Component, ViewEncapsulation, input, output } from '@angular/core';
import { RouterModule } from '@angular/router';
import { TranslatePipe } from '@ngx-translate/core';
import { FdUiIconComponent } from 'fd-ui-kit';
import { SidebarRouteItem } from './sidebar.models';

@Component({
    selector: 'fd-sidebar-route-links',
    standalone: true,
    imports: [RouterModule, TranslatePipe, FdUiIconComponent],
    template: `
        @for (item of items(); track item.id) {
            <a
                [routerLink]="item.route"
                routerLinkActive="is-active"
                [routerLinkActiveOptions]="{ exact: item.exact ?? false }"
                [class]="linkClass()"
                [class.is-pending]="pendingRoute() === item.route"
                (click)="itemSelected.emit(item)"
            >
                <fd-ui-icon [name]="item.icon"></fd-ui-icon>
                <span>{{ item.labelKey | translate }}</span>
            </a>
        }
    `,
    styles: [
        `
            :host {
                display: contents;
            }

            .sidebar__link,
            .sidebar-mobile__sheet-link {
                appearance: none;
                border: none;
                width: 100%;
                background: transparent;
                text-align: left;
                cursor: pointer;
                color: var(--fd-color-text-muted, var(--fd-color-slate-600));
                text-decoration: none;
                transition:
                    background 0.2s ease,
                    color 0.2s ease;
            }

            .sidebar__link {
                display: grid;
                grid-template-columns: 24px 1fr;
                gap: 10px;
                align-items: center;
                padding: 10px 12px;
                border-radius: var(--fd-radius-menu-item, 12px);
                font-weight: 600;
            }

            .sidebar__link fd-ui-icon {
                font-size: 20px;
                color: var(--fd-color-primary-600);
            }

            .sidebar__link:hover {
                background: color-mix(in srgb, var(--fd-color-primary-600) 8%, transparent);
                color: var(--fd-color-text-strong, var(--fd-color-slate-900));
            }

            .sidebar__link.is-active {
                background: linear-gradient(135deg, var(--fd-gradient-brand-start), var(--fd-gradient-brand-end));
                color: var(--fd-color-on-brand, var(--fd-color-white));
                box-shadow: 0 10px 26px color-mix(in srgb, var(--fd-color-primary-600) 28%, transparent);
            }

            .sidebar__link.is-pending:not(.is-active) {
                background: color-mix(in srgb, var(--fd-color-primary-600) 12%, transparent);
                color: var(--fd-color-text-strong, var(--fd-color-slate-900));
                box-shadow: inset 0 0 0 1px color-mix(in srgb, var(--fd-color-primary-600) 12%, transparent);
            }

            .sidebar__link.is-active fd-ui-icon {
                color: var(--fd-color-on-brand, var(--fd-color-white));
            }

            .sidebar__link.is-pending:not(.is-active) fd-ui-icon {
                color: var(--fd-color-primary-700, var(--fd-color-primary-600));
            }

            .sidebar__link.sidebar__link--secondary {
                padding: 8px 10px;
                font-weight: 600;
            }

            .sidebar__link.sidebar__link--secondary fd-ui-icon {
                font-size: 18px;
            }

            .sidebar-mobile__sheet-link {
                display: flex;
                align-items: center;
                gap: 10px;
                padding: 10px 12px;
                border-radius: var(--fd-radius-menu-item, 12px);
                background: var(--fd-color-surface-muted, var(--fd-color-slate-50));
                color: var(--fd-color-text-strong, var(--fd-color-slate-900));
                border: 1px solid var(--fd-color-border, color-mix(in srgb, var(--fd-color-slate-900) 6%, transparent));
                font-weight: 600;
            }

            .sidebar-mobile__sheet-link.is-pending:not(.is-active) {
                background: color-mix(in srgb, var(--fd-color-primary-600) 10%, var(--fd-color-surface-muted, var(--fd-color-slate-50)));
                border-color: color-mix(in srgb, var(--fd-color-primary-600) 22%, transparent);
                color: var(--fd-color-text-strong, var(--fd-color-slate-900));
            }
        `,
    ],
    encapsulation: ViewEncapsulation.None,
    changeDetection: ChangeDetectionStrategy.OnPush,
})
export class SidebarRouteLinksComponent {
    public readonly items = input.required<SidebarRouteItem[]>();
    public readonly linkClass = input.required<string>();
    public readonly pendingRoute = input<string | null>(null);
    public readonly itemSelected = output<SidebarRouteItem>();
}
