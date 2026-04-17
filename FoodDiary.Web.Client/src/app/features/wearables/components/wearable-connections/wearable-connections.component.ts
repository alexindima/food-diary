import { ChangeDetectionStrategy, Component, DestroyRef, inject, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { TranslatePipe } from '@ngx-translate/core';
import { FdUiButtonComponent } from 'fd-ui-kit/button/fd-ui-button.component';
import { WearableService } from '../../api/wearable.service';
import { WearableConnection } from '../../models/wearable.data';

interface ProviderConfig {
    id: string;
    name: string;
    icon: string;
}

@Component({
    selector: 'fd-wearable-connections',
    standalone: true,
    imports: [CommonModule, TranslatePipe, FdUiButtonComponent],
    template: `
        <div class="connections">
            <h3 class="title">{{ 'WEARABLES.CONNECTIONS_TITLE' | translate }}</h3>
            @for (provider of providers; track provider.id) {
                <div class="provider-row">
                    <span class="provider-icon">{{ provider.icon }}</span>
                    <div class="provider-info">
                        <span class="provider-name">{{ provider.name }}</span>
                        @if (getConnection(provider.id); as conn) {
                            <span class="provider-status connected">
                                {{ 'WEARABLES.CONNECTED' | translate }}
                            </span>
                        } @else {
                            <span class="provider-status">{{ 'WEARABLES.NOT_CONNECTED' | translate }}</span>
                        }
                    </div>
                    @if (getConnection(provider.id); as conn) {
                        <fd-ui-button variant="text" (click)="disconnect(provider.id)">
                            {{ 'WEARABLES.DISCONNECT' | translate }}
                        </fd-ui-button>
                    } @else {
                        <fd-ui-button variant="flat" (click)="connect(provider.id)">
                            {{ 'WEARABLES.CONNECT' | translate }}
                        </fd-ui-button>
                    }
                </div>
            }
        </div>
    `,
    styles: [
        `
            .connections {
                padding: 16px;
            }

            .title {
                font-size: 16px;
                font-weight: 600;
                margin: 0 0 16px;
            }

            .provider-row {
                display: flex;
                align-items: center;
                gap: 12px;
                padding: 12px 0;
                border-bottom: 1px solid var(--fd-divider, rgba(0, 0, 0, 0.08));

                &:last-child {
                    border-bottom: none;
                }
            }

            .provider-icon {
                font-size: 28px;
                width: 40px;
                text-align: center;
            }

            .provider-info {
                flex: 1;
                display: flex;
                flex-direction: column;
                gap: 2px;
            }

            .provider-name {
                font-size: 14px;
                font-weight: 600;
            }

            .provider-status {
                font-size: 12px;
                color: var(--fd-text-secondary, var(--fd-color-neutral-600));
            }

            .provider-status.connected {
                color: var(--fd-success, #4caf50);
            }
        `,
    ],
    changeDetection: ChangeDetectionStrategy.OnPush,
})
export class WearableConnectionsComponent {
    private readonly wearableService = inject(WearableService);
    private readonly destroyRef = inject(DestroyRef);

    public readonly connections = signal<WearableConnection[]>([]);

    public readonly providers: ProviderConfig[] = [
        { id: 'Fitbit', name: 'Fitbit', icon: '\u231A' },
        { id: 'GoogleFit', name: 'Google Fit', icon: '\uD83D\uDFE2' },
        { id: 'Garmin', name: 'Garmin', icon: '\u2328\uFE0F' },
        { id: 'AppleHealth', name: 'Apple Health', icon: '\uD83C\uDF4F' },
    ];

    public constructor() {
        this.loadConnections();
    }

    public getConnection(providerId: string): WearableConnection | undefined {
        return this.connections().find(c => c.provider === providerId && c.isActive);
    }

    public connect(providerId: string): void {
        const state = crypto.randomUUID();
        this.wearableService
            .getAuthUrl(providerId, state)
            .pipe(takeUntilDestroyed(this.destroyRef))
            .subscribe(result => {
                window.location.href = result.authorizationUrl;
            });
    }

    public disconnect(providerId: string): void {
        this.wearableService
            .disconnect(providerId)
            .pipe(takeUntilDestroyed(this.destroyRef))
            .subscribe(() => {
                this.loadConnections();
            });
    }

    private loadConnections(): void {
        this.wearableService
            .getConnections()
            .pipe(takeUntilDestroyed(this.destroyRef))
            .subscribe(connections => {
                this.connections.set(connections);
            });
    }
}
