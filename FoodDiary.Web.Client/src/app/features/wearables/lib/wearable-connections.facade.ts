import { computed, DestroyRef, inject, Injectable, linkedSignal, signal } from '@angular/core';
import { rxResource, takeUntilDestroyed } from '@angular/core/rxjs-interop';
import type { Observable } from 'rxjs';

import { WearableService } from '../api/wearable.service';
import type { WearableAuthUrl } from '../models/wearable.data';
import { buildWearableProviderRows } from './wearable.mapper';
import type { WearableProviderRow } from './wearable.types';

@Injectable()
export class WearableConnectionsFacade {
    private readonly wearableService = inject(WearableService);
    private readonly destroyRef = inject(DestroyRef);

    private readonly connectionsRevision = signal(0);
    private readonly connectionsResource = rxResource({
        params: this.connectionsRevision,
        stream: () => this.wearableService.getConnections(),
    });
    public readonly providerRows = linkedSignal({
        source: this.connectionsResource.snapshot,
        computation: (snapshot, previous): WearableProviderRow[] =>
            snapshot.status === 'resolved' && snapshot.value !== undefined
                ? buildWearableProviderRows(snapshot.value)
                : (previous?.value ?? []),
    }).asReadonly();
    public readonly isLoading = this.connectionsResource.isLoading;
    public readonly hasLoadError = computed(() => this.connectionsResource.error() !== undefined);

    public retryLoad(): void {
        this.connectionsResource.reload();
    }

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
