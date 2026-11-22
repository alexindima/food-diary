import { TestBed } from '@angular/core/testing';
import { By } from '@angular/platform-browser';
import { provideRouter } from '@angular/router';
import { TranslateService } from '@ngx-translate/core';
import { FdUiDialogService } from 'fd-ui-kit/dialog/fd-ui-dialog.service';
import { Subject } from 'rxjs';
import { describe, expect, it, onTestFinished, vi } from 'vitest';

import { provideTranslateTesting } from '../../../../testing/translate-testing.module';
import { FrontendLoggerService } from '../../../services/frontend-logger.service';
import { ImageUploadFacade } from '../../../shared/lib/image-upload.facade';
import { type ImageSelection, imageSelection } from '../../../shared/models/image-upload.data';
import { ImageUploadFieldComponent } from '../image-upload-field/image-upload-field';
import { ImageGalleryEditorComponent } from './image-gallery-editor';

function getPhotoMenuButton(label: string): HTMLButtonElement | undefined {
    return Array.from(document.querySelectorAll<HTMLButtonElement>('button[role="menuitem"]')).find(
        button => button.textContent.trim() === label && button.closest('[hidden]') === null,
    );
}

function getRemoveMenuButton(): HTMLButtonElement | undefined {
    return getPhotoMenuButton('IMAGE_UPLOAD_FIELD.REMOVE');
}

describe('ImageGalleryEditorComponent upload errors', () => {
    it('shows an invalid file and fills the last gallery slot after choosing one valid photo', async () => {
        const uploadResult = new Subject<ImageSelection>();
        const upload = vi.fn().mockReturnValue(uploadResult);
        TestBed.configureTestingModule({
            imports: [ImageGalleryEditorComponent],
            providers: [
                provideTranslateTesting(),
                provideRouter([]),
                { provide: ImageUploadFacade, useValue: { upload } },
                { provide: FrontendLoggerService, useValue: { warn: vi.fn() } },
            ],
        });
        vi.spyOn(TestBed.inject(TranslateService), 'instant').mockReturnValue('Only images');
        const fixture = TestBed.createComponent(ImageGalleryEditorComponent);
        const saved = ['front', 'back', 'label', 'side'].map(name => imageSelection(`https://example.com/${name}.png`, `photo-${name}`));
        fixture.componentRef.setInput('photos', saved);
        fixture.detectChanges();
        const uploaderElement = fixture.debugElement.query(By.directive(ImageUploadFieldComponent));
        const uploaderHost = uploaderElement.nativeElement as HTMLElement;
        expect(uploaderHost.classList.contains('recognition-photos__uploader-hidden')).toBe(true);
        const input = uploaderHost.querySelector('input[type=file]') as HTMLInputElement;
        Object.defineProperty(input, 'files', { configurable: true, value: [new File(['text'], 'notes.txt', { type: 'text/plain' })] });

        input.dispatchEvent(new Event('change'));
        await fixture.whenStable();
        fixture.detectChanges();

        expect(uploaderHost.classList.contains('recognition-photos__uploader-hidden')).toBe(false);
        expect(uploaderHost.querySelector('[role=alert]')?.textContent).toBe('Only images');
        expect(uploaderHost.querySelector('.image-upload-field__dropzone')?.getAttribute('aria-hidden')).toBe('true');
        expect(fixture.componentInstance.photos()).toEqual(saved);
        expect(upload).not.toHaveBeenCalled();

        const decoded = document.createElement('img');
        const thumbnailSize = 320;
        Object.defineProperties(decoded, { naturalWidth: { value: thumbnailSize }, naturalHeight: { value: thumbnailSize } });
        vi.stubGlobal('Image', function createDecodedImage(): HTMLImageElement {
            return decoded;
        });
        const createUrl = vi.spyOn(URL, 'createObjectURL').mockReturnValue('blob:gallery-thumbnail');
        const revokeUrl = vi.spyOn(URL, 'revokeObjectURL').mockImplementation(() => {});
        onTestFinished(() => {
            createUrl.mockRestore();
            revokeUrl.mockRestore();
            vi.unstubAllGlobals();
        });

        const valid = new File(['thumbnail bytes'], 'thumbnail.png', { type: 'image/png' });
        const added = imageSelection('https://example.com/thumbnail.png', 'new-photo');
        const busy = vi.fn();
        fixture.componentInstance.uploading.subscribe(busy);
        Object.defineProperty(input, 'files', { configurable: true, value: [valid] });
        input.dispatchEvent(new Event('change', { bubbles: true }));
        decoded.dispatchEvent(new Event('load'));

        await vi.waitFor(() => {
            expect(upload).toHaveBeenCalledExactlyOnceWith(valid);
            expect(uploadResult.observed).toBe(true);
        });
        fixture.detectChanges();
        expect(busy.mock.calls).toEqual([[true]]);
        expect(fixture.componentInstance.photos()).toEqual(saved);
        expect(uploaderHost.querySelector('[role=alert]')).toBeNull();
        expect(uploaderHost.classList.contains('recognition-photos__uploader-hidden')).toBe(true);

        uploadResult.next(added);
        await fixture.whenStable();
        fixture.detectChanges();

        expect(busy.mock.calls).toEqual([[true], [false]]);
        const fullGallery = [...saved, added];
        expect(fixture.componentInstance.photos()).toEqual(fullGallery);
        const host = fixture.nativeElement as HTMLElement;
        const previews = host.querySelectorAll<HTMLImageElement>('.recognition-photos__preview img');
        expect(Array.from(previews, image => image.getAttribute('src'))).toEqual(fullGallery.map(photo => photo.url));
        expect(host.querySelector('input[type="file"]')).toBeNull();
    });
});

describe('ImageGalleryEditorComponent preview focus', () => {
    it.each([
        { index: 0, initialIndex: 0 },
        { index: 2, initialIndex: 1 },
    ])(
        'opens photo $index at collage position $initialIndex and targets its original button for focus',
        async ({ index, initialIndex }) => {
            const open = vi.fn();
            TestBed.configureTestingModule({
                imports: [ImageGalleryEditorComponent],
                providers: [
                    provideTranslateTesting(),
                    provideRouter([]),
                    { provide: FdUiDialogService, useValue: { open } },
                    { provide: ImageUploadFacade, useValue: { upload: vi.fn() } },
                    { provide: FrontendLoggerService, useValue: { warn: vi.fn() } },
                ],
            });
            const fixture = TestBed.createComponent(ImageGalleryEditorComponent);
            const first = imageSelection('https://example.com/one.png', 'photo-1');
            const assetOnly = imageSelection(null, 'legacy-photo');
            const last = imageSelection('https://example.com/two.png', 'photo-2');
            const photos = [first, assetOnly, last];
            fixture.componentRef.setInput('photos', photos);
            fixture.componentRef.setInput('cover', first);
            fixture.componentRef.setInput('disabled', true);
            fixture.detectChanges();
            const host = fixture.nativeElement as HTMLElement;
            const trigger = host.querySelectorAll<HTMLButtonElement>('.recognition-photos__preview button')[index];
            expect(trigger).toBeInstanceOf(HTMLButtonElement);
            trigger.click();

            const viewLabel = 'PRODUCT_AI_DIALOG.VIEW_PHOTO';
            await vi.waitFor(() => {
                fixture.detectChanges();
                expect(getPhotoMenuButton(viewLabel)?.disabled).toBe(false);
            });
            getPhotoMenuButton(viewLabel)?.click();

            expect(open).toHaveBeenCalledExactlyOnceWith(
                expect.anything(),
                expect.objectContaining({
                    restoreFocus: trigger,
                    data: { collageImages: [{ url: first.url }, { url: last.url }], initialIndex },
                }),
            );
            expect(fixture.componentInstance.cover()).toEqual(first);
            expect(fixture.componentInstance.photos()).toEqual(photos);
        },
    );
});

describe('Saved photo without a preview URL', () => {
    it('retains the locked cover and removes its asset only when editing becomes available', async () => {
        const open = vi.fn();
        TestBed.configureTestingModule({
            imports: [ImageGalleryEditorComponent],
            providers: [
                provideTranslateTesting(),
                provideRouter([]),
                { provide: FdUiDialogService, useValue: { open } },
                { provide: ImageUploadFacade, useValue: { upload: vi.fn() } },
                { provide: FrontendLoggerService, useValue: { warn: vi.fn() } },
            ],
        });
        const fixture = TestBed.createComponent(ImageGalleryEditorComponent);
        const cover = imageSelection(null, 'saved-cover');
        const photo = imageSelection('/gallery/photo.jpg', 'saved-photo');
        fixture.componentRef.setInput('photos', [cover, photo]);
        fixture.componentRef.setInput('cover', cover);
        fixture.componentRef.setInput('disabled', true);
        fixture.detectChanges();

        const host = fixture.nativeElement as HTMLElement;
        const trigger = host.querySelector<HTMLButtonElement>('.recognition-photos__preview button');
        expect(trigger).not.toBeNull();
        trigger?.click();
        await vi.waitFor(() => {
            fixture.detectChanges();
            expect(getRemoveMenuButton()?.disabled).toBe(true);
        });
        expect(getPhotoMenuButton('PRODUCT_AI_DIALOG.VIEW_PHOTO')).toBeUndefined();
        getRemoveMenuButton()?.click();
        expect(fixture.componentInstance.cover()).toEqual(cover);
        expect(fixture.componentInstance.photos()).toEqual([cover, photo]);
        expect(open).not.toHaveBeenCalled();

        fixture.componentRef.setInput('disabled', false);
        fixture.detectChanges();
        expect(getRemoveMenuButton()?.disabled).toBe(false);
        getRemoveMenuButton()?.click();
        fixture.detectChanges();

        expect(fixture.componentInstance.cover()).toBeNull();
        expect(fixture.componentInstance.photos()).toEqual([photo]);
        expect(host.querySelector('.recognition-photos__preview img')?.getAttribute('src')).toBe(photo.url);
        expect(open).not.toHaveBeenCalled();
    });
});

describe('Saved photos without asset IDs', () => {
    it('removes only the chosen legacy photo and keeps the other photo selected as cover', async () => {
        TestBed.configureTestingModule({
            imports: [ImageGalleryEditorComponent],
            providers: [
                provideTranslateTesting(),
                provideRouter([]),
                { provide: ImageUploadFacade, useValue: { upload: vi.fn() } },
                { provide: FrontendLoggerService, useValue: { warn: vi.fn() } },
            ],
        });
        const fixture = TestBed.createComponent(ImageGalleryEditorComponent);
        const cover = imageSelection('/gallery/cover.jpg', null);
        const otherPhoto = imageSelection('/gallery/other.jpg', null);
        fixture.componentRef.setInput('photos', [cover, otherPhoto]);
        fixture.componentRef.setInput('cover', cover);
        fixture.detectChanges();

        const host = fixture.nativeElement as HTMLElement;
        const previewUrls = (): Array<string | null> =>
            Array.from(host.querySelectorAll('.recognition-photos__preview img'), image => image.getAttribute('src'));
        const removePhotoAsync = async (index: number): Promise<void> => {
            const trigger = Array.from(host.querySelectorAll<HTMLButtonElement>('.recognition-photos__preview button')).at(index);
            expect(trigger).toBeDefined();
            trigger?.click();
            await vi.waitFor(() => {
                fixture.detectChanges();
                expect(getRemoveMenuButton()?.disabled).toBe(false);
            });
            getRemoveMenuButton()?.click();
            await vi.waitFor(() => {
                fixture.detectChanges();
                expect(getRemoveMenuButton()).toBeUndefined();
            });
        };

        expect(previewUrls()).toEqual([cover.url, otherPhoto.url]);
        await removePhotoAsync(1);

        expect(previewUrls()).toEqual([cover.url]);
        expect(fixture.componentInstance.photos()).toEqual([cover]);
        expect(fixture.componentInstance.cover()).toEqual(cover);

        await removePhotoAsync(0);

        expect(previewUrls()).toEqual([]);
        expect(fixture.componentInstance.photos()).toEqual([]);
        expect(fixture.componentInstance.cover()).toBeNull();
    });
});
