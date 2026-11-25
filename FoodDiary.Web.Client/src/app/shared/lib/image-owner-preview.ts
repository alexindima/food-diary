import { isPlatformBrowser } from '@angular/common';
import {
    computed,
    DestroyRef,
    effect,
    inject,
    Injector,
    PLATFORM_ID,
    type ResourceRef,
    type Signal,
    signal,
    untracked,
} from '@angular/core';
import { rxResource, takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { catchError, map, merge, of, take } from 'rxjs';

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
    const previews = signal(new Map<string, ResourceRef<string | null>>());
    const assets = computed(() => (browser && !sessionEnded() ? ownerAssets(sources()) : new Set<string>()));

    const clear = (): void => {
        previews().forEach(preview => {
            preview.destroy();
        });
        previews.set(new Map());
    };

    effect(() => {
        const ids = assets();
        untracked(() => {
            const previous = previews();
            const next = new Map<string, ResourceRef<string | null>>();
            for (const [id, preview] of previous) {
                if (!ids.has(id)) {
                    preview.destroy();
                }
            }
            for (const id of ids) {
                const preview =
                    previous.get(id) ??
                    rxResource({
                        injector,
                        defaultValue: null,
                        stream: () =>
                            injector
                                .get(ImageUploadFacade)
                                .getOwnerPreview(entityId<'image-asset'>(id))
                                .pipe(
                                    map(url => (url.length > 0 ? url : null)),
                                    catchError(() => of(null)),
                                ),
                    });
                next.set(id, preview);
            }
            previews.set(next);
        });
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
                    const preview = previews().get(id);
                    return preview?.hasValue() === true ? preview.value() : null;
                }
                const url = source.url;
                return typeof url === 'string' && url.trim().length > 0 ? url : null;
            }),
        ),
        loading: computed(() => [...assets()].some(id => previews().get(id)?.isLoading() ?? true)),
    };
}

function ownerAssets(sources: readonly ImagePreviewSource[]): Set<string> {
    const ids = sources.map(ownerPreviewAssetId).filter((id): id is string => id !== null);
    return new Set(ids);
}
