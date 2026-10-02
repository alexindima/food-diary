import { TestBed } from '@angular/core/testing';
import { By } from '@angular/platform-browser';
import { provideRouter } from '@angular/router';
import { TranslateService } from '@ngx-translate/core';
import { FdUiDialogService } from 'fd-ui-kit/dialog/fd-ui-dialog.service';
import { describe, expect, it, vi } from 'vitest';

import { provideTranslateTesting } from '../../../../testing/translate-testing.module';
import { FrontendLoggerService } from '../../../services/frontend-logger.service';
import { ImageUploadFacade } from '../../../shared/lib/image-upload.facade';
import { ImageUploadFieldComponent } from '../image-upload-field/image-upload-field';
import { ImageGalleryEditorComponent } from './image-gallery-editor';

describe('ImageGalleryEditorComponent upload errors', () => {
    it('exposes validation errors beside existing photos while keeping duplicate upload controls hidden', async () => {
        const upload = vi.fn();
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
        const photo = { assetId: 'existing-photo', url: 'https://example.com/photo.png' };
        fixture.componentRef.setInput('editor', true);
        fixture.componentRef.setInput('photos', [photo]);
        fixture.detectChanges();
        const uploaderElement = fixture.debugElement.query(By.directive(ImageUploadFieldComponent));
        const uploaderHost = uploaderElement.nativeElement as HTMLElement;
        expect(uploaderHost.classList.contains('recognition-photos__uploader-hidden')).toBe(true);
        const input = uploaderHost.querySelector('input[type=file]') as HTMLInputElement;
        Object.defineProperty(input, 'files', { value: [new File(['text'], 'notes.txt', { type: 'text/plain' })] });

        input.dispatchEvent(new Event('change'));
        await fixture.whenStable();
        fixture.detectChanges();

        expect(uploaderHost.classList.contains('recognition-photos__uploader-hidden')).toBe(false);
        expect(uploaderHost.querySelector('[role=alert]')?.textContent).toBe('Only images');
        expect(uploaderHost.querySelector('.image-upload-field__dropzone')?.getAttribute('aria-hidden')).toBe('true');
        expect(fixture.componentInstance.photos()).toEqual([photo]);
        expect(upload).not.toHaveBeenCalled();
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
            { assetId: 'photo-1', url: 'https://example.com/one.png' },
            { assetId: 'photo-2', url: 'https://example.com/two.png' },
        ]);
        fixture.detectChanges();
        const host = fixture.nativeElement as HTMLElement;
        const trigger = host.querySelectorAll('.recognition-photos__preview button')[index];

        fixture.componentInstance['preview'](index);

        expect(open).toHaveBeenCalledWith(expect.anything(), expect.objectContaining({ restoreFocus: trigger }));
    });
});
