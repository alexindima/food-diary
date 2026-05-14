import { computed, DestroyRef, inject, Injectable, signal } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import type { Observable } from 'rxjs';

import { WearableService } from '../api/wearable.service';
import type { WearableAuthUrl, WearableConnection } from '../models/wearable.data';
import { buildWearableProviderRows } from './wearable.mapper';

@Injectable({ providedIn: 'root' })
export class WearableConnectionsFacade {
    private readonly wearableService = inject(WearableService);
    private readonly destroyRef = inject(DestroyRef);

    public readonly connections = signal<WearableConnection[]>([]);
    public readonly providerRows = computed(() => buildWearableProviderRows(this.connections()));

    public initialize(): void {
        this.loadConnections();
    }

    public getAuthUrl(providerId: string, state: string): Observable<WearableAuthUrl> {
        return this.wearableService.getAuthUrl(providerId, state);
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
