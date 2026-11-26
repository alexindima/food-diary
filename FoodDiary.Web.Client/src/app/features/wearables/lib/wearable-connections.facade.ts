import { computed, DestroyRef, inject, Injectable, linkedSignal, signal } from '@angular/core';
import { rxResource, takeUntilDestroyed } from '@angular/core/rxjs-interop';
import type { Observable } from 'rxjs';

import { WearableService } from '../api/wearable.service';
import type { WearableAuthUrl, WearableConnection } from '../models/wearable.data';
import { buildWearableProviderRows } from './wearable.mapper';

@Injectable()
export class WearableConnectionsFacade {
    private readonly wearableService = inject(WearableService);
    private readonly destroyRef = inject(DestroyRef);

    private readonly connectionsRevision = signal(0);
    private readonly connectionsResource = rxResource({
        params: this.connectionsRevision,
        stream: () => this.wearableService.getConnections(),
    });
    private readonly connections = linkedSignal({
        source: this.connectionsResource.snapshot,
        computation: (snapshot, previous): WearableConnection[] =>
            snapshot.status === 'resolved' ? (snapshot.value ?? []) : (previous?.value ?? []),
    });

    public readonly providerRows = computed(() => buildWearableProviderRows(this.connections()));

    public getAuthUrl(providerId: string, state: string): Observable<WearableAuthUrl> {
        return this.wearableService.getAuthUrl(providerId, state);
    }

    public disconnect(providerId: string): void {
        this.wearableService
            .disconnect(providerId)
            .pipe(takeUntilDestroyed(this.destroyRef))
            .subscribe(() => {
                this.connectionsRevision.update(revision => revision + 1);
            });
    }
}
