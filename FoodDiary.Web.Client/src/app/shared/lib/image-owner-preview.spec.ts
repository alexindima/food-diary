import { PLATFORM_ID, signal } from '@angular/core';
import { TestBed } from '@angular/core/testing';
import { Subject, throwError } from 'rxjs';
import { describe, expect, it, vi } from 'vitest';

import { SessionEventsService } from '../auth/session-events.service';
import { type ImagePreviewSource, injectImagePreviews } from './image-owner-preview';
import { ImageUploadFacade } from './image-upload.facade';

const CDN = 'https://cdn.example.com/recipe-photo.webp';
const DATA_URL = 'data:image/webp;base64,UklGRg==';

describe('recipe owner photo previews', () => {
    it('loads protected photos once per view and clears them when the session changes', () => {
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
