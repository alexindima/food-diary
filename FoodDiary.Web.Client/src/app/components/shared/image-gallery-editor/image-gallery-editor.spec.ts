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
        fixture.componentRef.setInput('editor', true);
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
    it.each([0, 1])('restores focus to photo %s instead of the disappearing menu item', index => {
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
        fixture.componentRef.setInput('editor', true);
        fixture.componentRef.setInput('photos', [
            imageSelection('https://example.com/one.png', 'photo-1'),
            imageSelection('https://example.com/two.png', 'photo-2'),
        ]);
        fixture.detectChanges();
        const host = fixture.nativeElement as HTMLElement;
        const trigger = host.querySelectorAll('.recognition-photos__preview button')[index];

        fixture.componentInstance['preview'](index);

        expect(open).toHaveBeenCalledWith(expect.anything(), expect.objectContaining({ restoreFocus: trigger }));
    });
});
