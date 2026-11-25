import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { PLATFORM_ID, signal, type WritableSignal } from '@angular/core';
import { TestBed } from '@angular/core/testing';
import { Subject, throwError } from 'rxjs';
import { describe, expect, it, vi } from 'vitest';

import { environment } from '../../../environments/environment';
import { waitForAsyncTasksAsync } from '../../../testing/async-testing';
import type { OwnedImageContentHttpResponse } from '../api/sdk/generated/model/owned-image-content-http-response';
import { SessionEventsService } from '../auth/session-events.service';
import { type ImagePreviewSource, injectImagePreviews } from './image-owner-preview';
import { ImageUploadFacade } from './image-upload.facade';

const CDN = 'https://cdn.example.com/recipe-photo.webp';
const DATA_URL = 'data:image/webp;base64,UklGRg==';
const OLD_OWNER_ASSET = '00000000-0000-4000-8000-000000000001';
const CURRENT_OWNER_ASSET = '00000000-0000-4000-8000-000000000002';

describe('recipe owner photo previews', () => {
    it('loads protected photos once per view and clears them when the session changes', async () => {
        const remote = new Subject<string>();
        const getOwnerPreview = vi.fn(() => remote.asObservable());
        TestBed.configureTestingModule({
            providers: [
                { provide: PLATFORM_ID, useValue: 'browser' },
                SessionEventsService,
                { provide: ImageUploadFacade, useValue: { getOwnerPreview } },
            ],
        });
        const photo = { url: CDN, assetId: 'photo', requiresOwner: true };
        const sources = signal<ImagePreviewSource[]>([photo, photo]);
        const previews = TestBed.runInInjectionContext(() => injectImagePreviews(sources));

        expect(previews.urls()).toEqual([null, null]);
        TestBed.tick();
        expect(getOwnerPreview).toHaveBeenCalledExactlyOnceWith('photo');
        remote.next(DATA_URL);
        await waitForAsyncTasksAsync();
        expect(previews.urls()).toEqual([DATA_URL, DATA_URL]);

        TestBed.inject(SessionEventsService).notifyLocalSessionInvalidating();
        expect(previews.urls()).toEqual([null, null]);
        expect(remote.observed).toBe(false);
    });

    it('keeps an unavailable protected photo off the CDN while confirmed public photos still use it', () => {
        const getOwnerPreview = vi.fn(() => throwError(() => new Error('Photo unavailable')));
        TestBed.configureTestingModule({
            providers: [
                { provide: PLATFORM_ID, useValue: 'browser' },
                SessionEventsService,
                { provide: ImageUploadFacade, useValue: { getOwnerPreview } },
            ],
        });
        const previews = TestBed.runInInjectionContext(() =>
            injectImagePreviews(
                signal([
                    { url: CDN, assetId: 'private', requiresOwner: true },
                    { url: CDN, assetId: 'public' },
                ]),
            ),
        );

        TestBed.tick();
        expect(getOwnerPreview).toHaveBeenCalledExactlyOnceWith('private');
        expect(previews.loading()).toBe(false);
        expect(previews.urls()).toEqual([null, CDN]);
    });
});

describe('owner preview transport response', () => {
    it('settles an empty preview response without using the protected CDN fallback', async () => {
        const { previews, http } = setupOwnerPreviews([protectedPhoto(CURRENT_OWNER_ASSET), { url: CDN }]);
        TestBed.tick();
        expect(previews.loading()).toBe(true);
        const absent: OwnedImageContentHttpResponse = { dataUrl: '' };
        http.expectOne({ method: 'GET', url: previewUrl(CURRENT_OWNER_ASSET) }).flush(absent);
        await waitForAsyncTasksAsync();

        expect(previews.loading()).toBe(false);
        expect(previews.urls()).toEqual([null, CDN]);
        http.verify();
    });

    it('cancels a removed asset and discards the new private image when the session ends', async () => {
        const { previews, sources, http } = setupOwnerPreviews([protectedPhoto(OLD_OWNER_ASSET)]);
        TestBed.tick();
        const previous = http.expectOne({ method: 'GET', url: previewUrl(OLD_OWNER_ASSET) });
        expect(previous.cancelled).toBe(false);

        sources.set([protectedPhoto(CURRENT_OWNER_ASSET)]);
        TestBed.tick();
        expect(previous.cancelled).toBe(true);
        expect(previews.urls()).toEqual([null]);
        const current: OwnedImageContentHttpResponse = { dataUrl: DATA_URL };
        http.expectOne({ method: 'GET', url: previewUrl(CURRENT_OWNER_ASSET) }).flush(current);
        await waitForAsyncTasksAsync();
        expect(previews.urls()).toEqual([DATA_URL]);

        TestBed.inject(SessionEventsService).notifySessionEnded();
        TestBed.tick();
        expect(previews.urls()).toEqual([null]);
        expect(previews.loading()).toBe(false);
        http.verify();
    });
});

function setupOwnerPreviews(initial: ImagePreviewSource[]): {
    sources: WritableSignal<ImagePreviewSource[]>;
    previews: ReturnType<typeof injectImagePreviews>;
    http: HttpTestingController;
} {
    TestBed.configureTestingModule({
        providers: [{ provide: PLATFORM_ID, useValue: 'browser' }, provideHttpClient(), provideHttpClientTesting()],
    });
    const sources = signal(initial);
    const previews = TestBed.runInInjectionContext(() => injectImagePreviews(sources));
    return { sources, previews, http: TestBed.inject(HttpTestingController) };
}

function protectedPhoto(assetId: string): ImagePreviewSource {
    return { url: CDN, assetId, requiresOwner: true };
}

function previewUrl(assetId: string): string {
    return `${environment.apiUrls.images}/${assetId}/content`;
}
