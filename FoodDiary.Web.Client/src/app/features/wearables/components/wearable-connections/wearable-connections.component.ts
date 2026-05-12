import { CommonModule } from '@angular/common';
import { ChangeDetectionStrategy, Component, computed, DestroyRef, inject, signal } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { TranslatePipe } from '@ngx-translate/core';
import { FdUiButtonComponent } from 'fd-ui-kit/button/fd-ui-button.component';

import { WearableService } from '../../api/wearable.service';
import type { WearableConnection, WearableProvider } from '../../models/wearable.data';

type ProviderConfig = {
    id: WearableProvider;
    name: string;
    icon: string;
};

type ProviderRow = {
    connection: WearableConnection | undefined;
} & ProviderConfig;

@Component({
    selector: 'fd-wearable-connections',
    standalone: true,
    imports: [CommonModule, TranslatePipe, FdUiButtonComponent],
    templateUrl: './wearable-connections.component.html',
    styleUrls: ['./wearable-connections.component.scss'],
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
    public readonly providerRows = computed<ProviderRow[]>(() => {
        const activeConnections = new Map(
            this.connections()
                .filter(connection => connection.isActive)
                .map(connection => [connection.provider, connection]),
        );

        return this.providers.map(provider => ({
            ...provider,
            connection: activeConnections.get(provider.id),
        }));
    });

    public constructor() {
        this.loadConnections();
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
