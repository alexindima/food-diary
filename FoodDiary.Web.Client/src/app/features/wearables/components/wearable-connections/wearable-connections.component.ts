import { CommonModule } from '@angular/common';
import { ChangeDetectionStrategy, Component, DestroyRef, inject, signal } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { TranslatePipe } from '@ngx-translate/core';
import { FdUiButtonComponent } from 'fd-ui-kit/button/fd-ui-button.component';

import { WearableService } from '../../api/wearable.service';
import { type WearableConnection } from '../../models/wearable.data';

interface ProviderConfig {
    id: string;
    name: string;
    icon: string;
}

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
