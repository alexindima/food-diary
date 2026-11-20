import { isPlatformBrowser } from '@angular/common';
import { computed, DestroyRef, effect, inject, Injector, PLATFORM_ID, type Signal, signal, type WritableSignal } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { catchError, map, merge, of, type Subscription, take } from 'rxjs';

import { SessionEventsService } from '../auth/session-events.service';
import { entityId } from '../models/semantics/entity-id';
import { ImageUploadFacade } from './image-upload.facade';

export type ImagePreviewSource = {
    url?: string | null;
    assetId?: string | null;
    requiresOwner?: boolean;
};

export function ownerPreviewAssetId(source: ImagePreviewSource): string | null {
    const id = source.assetId?.trim() ?? '';
    return source.requiresOwner === true && id.length > 0 ? id : null;
}

/** Keep authorized preview bytes in the current view and clear them when access is lost. */
export function injectImagePreviews(sources: Signal<readonly ImagePreviewSource[]>): {
    urls: Signal<ReadonlyArray<string | null>>;
    loading: Signal<boolean>;
} {
    const browser = isPlatformBrowser(inject(PLATFORM_ID));
    const injector = inject(Injector);
    const events = inject(SessionEventsService);
    const destroy = inject(DestroyRef);
    const sessionEnded = signal(false);
    const requests = new Map<string, Subscription>();
    const checked = signal(new Map<string, string | null>());
    const assets = computed(() => (browser && !sessionEnded() ? ownerAssets(sources()) : new Set<string>()));

    const clear = (): void => {
        requests.forEach(request => {
            request.unsubscribe();
        });
        requests.clear();
        checked.set(new Map());
    };

    effect(() => {
        refreshPreviews(assets(), requests, checked, injector);
    });
    merge(events.authenticated$, events.sessionEnded$, events.localSessionInvalidating$)
        .pipe(take(1), takeUntilDestroyed(destroy))
        .subscribe(() => {
            sessionEnded.set(true);
            clear();
        });
    destroy.onDestroy(clear);

    return {
        urls: computed(() =>
            sources().map(source => {
                const id = ownerPreviewAssetId(source);
                if (id !== null) {
                    return checked().get(id) ?? null;
                }
                const url = source.url;
                return typeof url === 'string' && url.trim().length > 0 ? url : null;
            }),
        ),
        loading: computed(() => [...assets()].some(id => !checked().has(id))),
    };
}

function ownerAssets(sources: readonly ImagePreviewSource[]): Set<string> {
    const ids = sources.map(ownerPreviewAssetId).filter((id): id is string => id !== null);
    return new Set(ids);
}

function refreshPreviews(
    assets: ReadonlySet<string>,
    requests: Map<string, Subscription>,
    checked: WritableSignal<Map<string, string | null>>,
    injector: Injector,
): void {
    for (const [id, request] of requests) {
        if (!assets.has(id)) {
            request.unsubscribe();
            requests.delete(id);
            checked.update(previous => {
                const current = new Map(previous);
                current.delete(id);
                return current;
            });
        }
    }

    for (const id of assets) {
        if (requests.has(id)) {
            continue;
        }
        const request = injector
            .get(ImageUploadFacade)
            .getOwnerPreview(entityId<'image-asset'>(id))
            .pipe(
                map(url => (url.length > 0 ? url : null)),
                catchError(() => of(null)),
            )
            .subscribe(url => {
                checked.update(previous => new Map(previous).set(id, url));
            });
        requests.set(id, request);
    }
}
