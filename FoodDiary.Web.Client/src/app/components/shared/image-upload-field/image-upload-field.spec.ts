import { type ComponentFixture, TestBed } from '@angular/core/testing';
import { TranslateService } from '@ngx-translate/core';
import { firstValueFrom, of, Subject, throwError } from 'rxjs';
import { afterEach, beforeEach, describe, expect, it, type MockInstance, vi } from 'vitest';

import { waitForAsyncTasksAsync } from '../../../../testing/async-testing';
import { provideTranslateTesting } from '../../../../testing/translate-testing.module';
import { FrontendLoggerService } from '../../../services/frontend-logger.service';
import { ImageUploadFacade } from '../../../shared/lib/image-upload.facade';
import { type ImageSelection, imageSelection } from '../../../shared/models/image-upload.data';
import { PrivacyConsentFacade } from '../../../shared/privacy/privacy-consent.facade';
import { ImageUploadFieldComponent } from './image-upload-field';

type ImageUploadFieldTestContext = {
    component: ImageUploadFieldComponent;
    fixture: ComponentFixture<ImageUploadFieldComponent>;
    imageUploadService: {
        deleteAsset: ReturnType<typeof vi.fn>;
        upload: ReturnType<typeof vi.fn>;
    };
    logger: {
        warn: ReturnType<typeof vi.fn>;
    };
    privacyConsent: {
        ensureHealthConsentAsync: ReturnType<typeof vi.fn<() => Promise<boolean>>>;
        failed: ReturnType<typeof vi.fn<() => boolean>>;
        needsReload: ReturnType<typeof vi.fn<() => boolean>>;
    };
    translateService: TranslateService;
};

const TINY_MAX_SIZE_MB = 0.000001;
const CROP_SHIFTED_X = 20;
const CROP_SURFACE_SIZE = { width: 400, height: 300 };
const PREVIEW_URL = 'blob:image-upload-preview';
const DECODE_URL = 'blob:image-upload-decode';
const RESIZE_LIMIT = 1200;
const LARGE_IMAGE_SIZE = { width: 2400, height: 1200 };
const RESIZED_IMAGE_SIZE = { width: 1200, height: 600 };

beforeEach(() => {
    vi.spyOn(URL, 'createObjectURL').mockReturnValue(PREVIEW_URL);
    vi.spyOn(URL, 'revokeObjectURL').mockImplementation(() => {});
});

afterEach(() => {
    vi.restoreAllMocks();
    vi.unstubAllGlobals();
});

async function setupImageUploadFieldAsync(): Promise<ImageUploadFieldTestContext> {
    const imageUploadService = {
        upload: vi.fn().mockReturnValue(of(imageSelection('https://cdn.example.com/image.jpg', 'asset-1'))),
        deleteAsset: vi.fn().mockReturnValue(of(void 0)),
    };
    const logger = { warn: vi.fn() };
    const privacyConsent = {
        ensureHealthConsentAsync: vi.fn<() => Promise<boolean>>().mockResolvedValue(true),
        failed: vi.fn<() => boolean>().mockReturnValue(false),
        needsReload: vi.fn<() => boolean>().mockReturnValue(false),
    };

    await TestBed.configureTestingModule({
        imports: [ImageUploadFieldComponent],
        providers: [
            provideTranslateTesting(),
            { provide: ImageUploadFacade, useValue: imageUploadService },
            { provide: FrontendLoggerService, useValue: logger },
            { provide: PrivacyConsentFacade, useValue: privacyConsent },
        ],
    }).compileComponents();

    const fixture = TestBed.createComponent(ImageUploadFieldComponent);
    const component = fixture.componentInstance;
    const translateService = TestBed.inject(TranslateService);
    return { component, fixture, imageUploadService, logger, privacyConsent, translateService };
}

function requireElement<T extends Element>(host: ParentNode, selector: string, elementType: new () => T): T {
    const element = host.querySelector(selector);
    if (!(element instanceof elementType)) {
        throw new Error(`Missing element ${selector}`);
    }
    return element;
}

function selectFiles(host: HTMLElement, ...files: File[]): void {
    const input = requireElement(host, 'input[type="file"]', HTMLInputElement);
    Object.defineProperty(input, 'files', { configurable: true, value: files });
    input.dispatchEvent(new Event('change', { bubbles: true }));
}

function dropFiles(host: HTMLElement, ...files: File[]): void {
    const dropzone = requireElement(host, '.image-upload-field__dropzone', HTMLElement);
    // jsdom has no native drag data store, so provide only the files visible to the drop event.
    const event = new Event('drop', { bubbles: true, cancelable: true });
    Object.defineProperty(event, 'dataTransfer', { value: { files } });
    dropzone.dispatchEvent(event);
}

describe('ImageUploadFieldComponent control value', () => {
    it('writes value and disabled state', async () => {
        const { component, fixture } = await setupImageUploadFieldAsync();
        fixture.detectChanges();

        component.value.set(imageSelection('https://example.com/image.jpg', 'asset-1'));
        fixture.componentRef.setInput('disabled', true);
        fixture.detectChanges();

        expect(component['selection']()).toEqual(imageSelection('https://example.com/image.jpg', 'asset-1'));
        expect(component.disabled()).toBe(true);
    });

    it('clears the rendered preview when the external value becomes null without deleting an asset or emitting a user change', async () => {
        const { component, fixture, imageUploadService } = await setupImageUploadFieldAsync();
        const changed = vi.fn<(selection: ImageSelection | null) => void>();
        component.imageChanged.subscribe(changed);
        component.value.subscribe(changed);
        fixture.componentRef.setInput('deleteOnClear', true);
        const saved = imageSelection('https://example.test/reset-photo.png', 'reset-asset');
        fixture.componentRef.setInput('value', saved);
        fixture.detectChanges();
        const host = fixture.nativeElement as HTMLElement;
        expect(host.querySelector('img.image-upload-field__preview')?.getAttribute('src')).toBe(saved.url);

        fixture.componentRef.setInput('value', null);
        fixture.detectChanges();

        expect(host.querySelector('img.image-upload-field__preview')).toBeNull();
        expect(component.value()).toBeNull();
        expect(component.touched()).toBe(false);
        expect(changed).not.toHaveBeenCalled();
        expect(imageUploadService.deleteAsset).not.toHaveBeenCalled();
    });
});

describe('ImageUploadFieldComponent stored initial selection', () => {
    it.each([
        imageSelection('https://example.test/stored-photo.png', null),
        imageSelection(null, 'stored-asset'),
        imageSelection(null, null),
    ])('restores a $kind selection without uploading', async restored => {
        const { component, fixture, imageUploadService } = await setupImageUploadFieldAsync();
        const published = vi.fn<(selection: ImageSelection | null) => void>();
        component.imageChanged.subscribe(published);
        fixture.componentRef.setInput('initialSelection', restored);
        fixture.detectChanges();
        const host = fixture.nativeElement as HTMLElement;

        if (restored.kind === 'empty') {
            expect(published).not.toHaveBeenCalled();
        } else {
            expect(published).toHaveBeenCalledExactlyOnceWith(restored);
        }
        expect(host.querySelector('img.image-upload-field__preview')?.getAttribute('src') ?? null).toBe(restored.url);
        expect(imageUploadService.upload).not.toHaveBeenCalled();
    });

    it('restores the configured fallback silently when the parent clears its explicit value', async () => {
        const { component, fixture } = await setupImageUploadFieldAsync();
        const changed = vi.fn<(selection: ImageSelection | null) => void>();
        component.imageChanged.subscribe(changed);
        const restored = imageSelection('https://example.test/restored-photo.png', null);
        fixture.componentRef.setInput('initialSelection', restored);
        fixture.detectChanges();
        expect(changed).toHaveBeenCalledExactlyOnceWith(restored);
        changed.mockClear();

        fixture.componentRef.setInput('value', imageSelection(null, null));
        fixture.detectChanges();
        const host = fixture.nativeElement as HTMLElement;
        expect(host.querySelector('img.image-upload-field__preview')).toBeNull();

        fixture.componentRef.setInput('value', null);
        fixture.detectChanges();
        expect(host.querySelector('img.image-upload-field__preview')?.getAttribute('src')).toBe(restored.url);
        expect(changed).not.toHaveBeenCalled();
    });
});

describe('ImageUploadFieldComponent upload', () => {
    it('rejects non-image files before upload', async () => {
        const { component, fixture, imageUploadService, translateService } = await setupImageUploadFieldAsync();
        vi.spyOn(translateService, 'instant').mockReturnValue('Only images');
        fixture.detectChanges();

        selectFiles(fixture.nativeElement as HTMLElement, new File(['text'], 'notes.txt', { type: 'text/plain' }));
        await fixture.whenStable();

        expect(component['error']()).toBe('Only images');
        expect(imageUploadService.upload).not.toHaveBeenCalled();
    });

    it('uploads valid image files and emits selection', async () => {
        const { component, fixture, imageUploadService } = await setupImageUploadFieldAsync();
        const changeSpy = vi.fn();
        component['imageChanged'].subscribe(changeSpy);
        fixture.detectChanges();

        selectFiles(fixture.nativeElement as HTMLElement, new File(['image'], 'photo.png', { type: 'image/png' }));
        await fixture.whenStable();

        expect(imageUploadService.upload).toHaveBeenCalledOnce();
        expect(component['selection']()).toEqual(imageSelection('https://cdn.example.com/image.jpg', 'asset-1'));
        expect(changeSpy).toHaveBeenCalledWith(imageSelection('https://cdn.example.com/image.jpg', 'asset-1'));
    });

    it('rejects oversized files before requesting upload URL', async () => {
        const { component, fixture, imageUploadService, translateService } = await setupImageUploadFieldAsync();
        vi.spyOn(translateService, 'instant').mockReturnValue('Too large');
        fixture.componentRef.setInput('maxSizeMb', TINY_MAX_SIZE_MB);
        fixture.detectChanges();

        selectFiles(fixture.nativeElement as HTMLElement, new File(['image-data'], 'photo.png', { type: 'image/png' }));
        await fixture.whenStable();

        expect(component['error']()).toBe('Too large');
        expect(imageUploadService.upload).not.toHaveBeenCalled();
    });

    it('shows upload error when presigned upload fails', async () => {
        const { component, fixture, imageUploadService, translateService } = await setupImageUploadFieldAsync();
        vi.spyOn(translateService, 'instant').mockReturnValue('Upload failed');
        imageUploadService.upload.mockReturnValueOnce(throwError(() => new Error('upload failed')));
        fixture.detectChanges();

        selectFiles(fixture.nativeElement as HTMLElement, new File(['image'], 'photo.png', { type: 'image/png' }));
        await fixture.whenStable();

        expect(component['error']()).toBe('Upload failed');
        expect(component['isUploading']()).toBe(false);
    });
});

describe('ImageUploadFieldComponent configured file types', () => {
    beforeEach(mockNativeDialogMethods);

    it.each(['file picker', 'drop'] as const)('rejects an image outside the configured crop formats from %s', async origin => {
        const { fixture, component, imageUploadService } = await setupImageUploadFieldAsync();
        const previous = imageSelection('https://example.test/saved.png', 'saved-avatar');
        const preparing = vi.fn<(url: string) => void>();
        component.imagePreparationStarted.subscribe(preparing);
        component.value.set(previous);
        fixture.componentRef.setInput('acceptedTypes', 'image/png,image/jpeg,image/webp');
        fixture.componentRef.setInput('cropEnabled', true);
        fixture.detectChanges();
        const host = fixture.nativeElement as HTMLElement;
        const file = new File(['GIF89a'], 'animated.gif', { type: 'image/gif' });

        const choose = origin === 'file picker' ? selectFiles : dropFiles;
        choose(host, file);
        fixture.detectChanges();

        expect(host.querySelector('[role="alert"]')?.textContent.trim()).toBe('IMAGE_UPLOAD_FIELD.ERRORS.ONLY_IMAGES');
        expect(host.querySelector('dialog')).toBeNull();
        expect(component.value()).toEqual(previous);
        expect(preparing).not.toHaveBeenCalled();
        expect(imageUploadService.upload).not.toHaveBeenCalled();
    });
});

describe('ImageUploadFieldComponent resizing selected photos', () => {
    it.each([
        ['image/jpeg', 'photo.jpg'],
        ['image/png', 'photo.png'],
    ] as const)('fits a large %s to the configured dimensions and preserves its format and name', async (type, name) => {
        const { fixture, component, imageUploadService } = await setupImageUploadFieldAsync();
        const { host, image, revokeObjectUrl } = prepareNativeResize(fixture);
        const drawImage = vi.fn<CanvasRenderingContext2D['drawImage']>();
        const fillRect = vi.fn<CanvasRenderingContext2D['fillRect']>();
        vi.spyOn(HTMLCanvasElement.prototype, 'getContext').mockReturnValue({ drawImage, fillRect } as unknown as CanvasRenderingContext2D);
        const resized = new Blob(['resized bytes'], { type });
        const toBlob = vi.spyOn(HTMLCanvasElement.prototype, 'toBlob').mockImplementation(callback => {
            callback(resized);
        });
        const file = new File(['source bytes'], name, { type, lastModified: 100 });

        selectFiles(host, file);
        expect(image.src).toBe(DECODE_URL);
        expect(imageUploadService.upload).not.toHaveBeenCalled();
        image.dispatchEvent(new Event('load'));

        await vi.waitFor(() => {
            expect(imageUploadService.upload).toHaveBeenCalledExactlyOnceWith(
                expect.objectContaining({ name, type, size: resized.size, lastModified: file.lastModified }),
            );
        });
        const { width, height } = RESIZED_IMAGE_SIZE;
        expect(drawImage).toHaveBeenCalledExactlyOnceWith(image, 0, 0, width, height);
        expect(toBlob).toHaveBeenCalledExactlyOnceWith(expect.any(Function), type, component.resizeQuality());
        if (type === 'image/jpeg') {
            expect(fillRect).toHaveBeenCalledExactlyOnceWith(0, 0, width, height);
        } else {
            expect(fillRect).not.toHaveBeenCalled();
        }
        expect(revokeObjectUrl.mock.calls).toEqual([[DECODE_URL], [PREVIEW_URL]]);

        fixture.destroy();

        expect(revokeObjectUrl).toHaveBeenCalledTimes(2);
    });

    it.each(['decode', 'export'] as const)('uploads the original photo after a %s failure', async stage => {
        const { fixture, imageUploadService, logger } = await setupImageUploadFieldAsync();
        const { host, image, revokeObjectUrl } = prepareNativeResize(fixture);
        vi.spyOn(HTMLCanvasElement.prototype, 'getContext').mockReturnValue({ drawImage: vi.fn() } as unknown as CanvasRenderingContext2D);
        vi.spyOn(HTMLCanvasElement.prototype, 'toBlob').mockImplementation(callback => {
            callback(null);
        });
        const file = new File(['original bytes'], 'photo.png', { type: 'image/png' });

        selectFiles(host, file);
        image.dispatchEvent(new Event(stage === 'decode' ? 'error' : 'load'));

        await vi.waitFor(() => {
            expect(imageUploadService.upload).toHaveBeenCalledExactlyOnceWith(file);
        });
        expect(logger.warn).toHaveBeenCalledWith('Failed to resize image before upload', expect.any(Error));
        expect(revokeObjectUrl).toHaveBeenCalledWith(DECODE_URL);
    });

    it('keeps the original animated GIF without decoding it for resizing', async () => {
        const { fixture, imageUploadService } = await setupImageUploadFieldAsync();
        const { host, createObjectUrl } = prepareNativeResize(fixture);
        const file = new File(['GIF89a'], 'photo.gif', { type: 'image/gif' });

        selectFiles(host, file);

        await vi.waitFor(() => {
            expect(imageUploadService.upload).toHaveBeenCalledExactlyOnceWith(file);
        });
        expect(createObjectUrl).toHaveBeenCalledExactlyOnceWith(file);
    });
});

describe('ImageUploadFieldComponent preparation ownership and limits', () => {
    it('stops before decoding when its owner removes the field as a batch begins', async () => {
        const { fixture, component, imageUploadService } = await setupImageUploadFieldAsync();
        fixture.componentRef.setInput('multiple', true);
        fixture.componentRef.setInput('maxFiles', 2);
        const { host, createObjectUrl } = prepareNativeResize(fixture, 'batch');
        const added = vi.fn<(photos: ImageSelection[]) => void>();
        const busy = vi.fn<(pending: boolean) => void>();
        component.imagesAdded.subscribe(added);
        component.batchUploading.subscribe(pending => {
            busy(pending);
            if (pending) {
                fixture.destroy();
            }
        });

        const first = new File(['first'], 'first.png', { type: 'image/png' });
        const second = new File(['second'], 'second.png', { type: 'image/png' });
        selectFiles(host, first, second);

        expect(busy).toHaveBeenCalledExactlyOnceWith(true);
        expect(createObjectUrl).not.toHaveBeenCalled();
        expect(imageUploadService.upload).not.toHaveBeenCalled();
        expect(added).not.toHaveBeenCalled();
    });

    it('keeps the saved photo when the resized replacement exceeds the upload limit', async () => {
        const { fixture, component, imageUploadService } = await setupImageUploadFieldAsync();
        const saved = imageSelection('https://example.test/saved-photo.png', 'saved-photo');
        const failed = vi.fn();
        component.value.set(saved);
        component.imagePreparationFailed.subscribe(failed);
        fixture.componentRef.setInput('maxSizeMb', 1);
        const { host, image, revokeObjectUrl } = prepareNativeResize(fixture);
        const bytesPerMegabyte = 1_048_576;
        const resized = new Blob([new Uint8Array(bytesPerMegabyte + 1)], { type: 'image/png' });
        vi.spyOn(HTMLCanvasElement.prototype, 'getContext').mockReturnValue({ drawImage: vi.fn() } as unknown as CanvasRenderingContext2D);
        vi.spyOn(HTMLCanvasElement.prototype, 'toBlob').mockImplementation(callback => {
            callback(resized);
        });

        selectFiles(host, new File(['compressed source'], 'replacement.png', { type: 'image/png' }));
        expect(image.src).toBe(DECODE_URL);
        image.dispatchEvent(new Event('load'));

        await vi.waitFor(() => {
            fixture.detectChanges();
            expect(host.querySelector('[role="alert"]')?.textContent.trim()).toBe('IMAGE_UPLOAD_FIELD.ERRORS.FILE_TOO_LARGE');
        });
        expect(requireElement(host, 'img.image-upload-field__preview', HTMLImageElement).src).toBe(saved.url);
        expect(component.value()).toEqual(saved);
        expect(failed).toHaveBeenCalledOnce();
        expect(revokeObjectUrl.mock.calls).toEqual([[DECODE_URL], [PREVIEW_URL]]);
        expect(imageUploadService.upload).not.toHaveBeenCalled();
    });
});

function prepareNativeResize(
    fixture: ComponentFixture<ImageUploadFieldComponent>,
    mode: 'single' | 'batch' = 'single',
): {
    host: HTMLElement;
    image: HTMLImageElement;
    createObjectUrl: MockInstance<typeof URL.createObjectURL>;
    revokeObjectUrl: MockInstance<typeof URL.revokeObjectURL>;
} {
    const image = document.createElement('img');
    Object.defineProperties(image, {
        naturalWidth: { value: LARGE_IMAGE_SIZE.width },
        naturalHeight: { value: LARGE_IMAGE_SIZE.height },
    });
    vi.stubGlobal('Image', function createDecodedImage(): HTMLImageElement {
        return image;
    });
    const createObjectUrl = vi.spyOn(URL, 'createObjectURL');
    if (mode === 'single') {
        createObjectUrl.mockReturnValueOnce(PREVIEW_URL);
    }
    createObjectUrl.mockReturnValueOnce(DECODE_URL);
    const revokeObjectUrl = vi.spyOn(URL, 'revokeObjectURL');
    fixture.componentRef.setInput('resizeMaxDimension', RESIZE_LIMIT);
    fixture.detectChanges();
    return { host: fixture.nativeElement as HTMLElement, image, createObjectUrl, revokeObjectUrl };
}

describe('ImageUploadFieldComponent disabled actions', () => {
    it('disables native replacement and deletion while preserving external clear requests', async () => {
        const { component, fixture } = await setupImageUploadFieldAsync();
        const saved = imageSelection('https://example.test/saved.png', 'saved');
        component.value.set(saved);
        fixture.componentRef.setInput('disabled', true);
        fixture.detectChanges();
        const buttons = Array.from(
            (fixture.nativeElement as HTMLElement).querySelectorAll<HTMLButtonElement>('.image-upload-field__control button'),
        );
        expect(buttons).toHaveLength(2);
        expect(buttons.every(button => button.disabled)).toBe(true);
        buttons[1].click();
        component['onDeleteClick'](new Event('click'));
        expect(component.value()).toEqual(saved);
        fixture.componentRef.setInput('clearRequest', 1);
        fixture.detectChanges();
        expect(component.value()).toEqual(imageSelection(null, null));
    });
});

describe('ImageUploadFieldComponent chooser lifecycle', () => {
    it('lets the owner reopen the native picker only when the upload field is available', async () => {
        const { fixture, component, imageUploadService } = await setupImageUploadFieldAsync();
        const pending = new Subject<ImageSelection>();
        imageUploadService.upload.mockReturnValueOnce(pending);
        const previous = imageSelection('https://example.test/saved.png', 'saved');
        component.value.set(previous);
        fixture.detectChanges();
        const host = fixture.nativeElement as HTMLElement;
        const picker = requireElement(host, 'input[type="file"]', HTMLInputElement);
        const openPicker = vi.spyOn(picker, 'click').mockImplementation(() => {});

        component.openFilePicker();
        expect(openPicker).toHaveBeenCalledOnce();
        selectFiles(host, new File(['replacement'], 'replacement.png', { type: 'image/png' }));
        await vi.waitFor(() => {
            fixture.detectChanges();
            expect(host.querySelector('[role="status"]')?.textContent).toContain('IMAGE_UPLOAD_FIELD.UPLOADING');
        });
        component.openFilePicker();
        expect(openPicker).toHaveBeenCalledOnce();
        expect(component.value()).toEqual(previous);

        const replacement = imageSelection('https://example.test/new.png', 'new');
        pending.next(replacement);
        pending.complete();
        fixture.componentRef.setInput('disabled', true);
        fixture.detectChanges();
        component.openFilePicker();
        expect(openPicker).toHaveBeenCalledOnce();

        fixture.componentRef.setInput('disabled', false);
        fixture.detectChanges();
        component.openFilePicker();

        expect(openPicker).toHaveBeenCalledTimes(2);
        expect(component.value()).toEqual(replacement);
        expect(host.querySelector('img.image-upload-field__preview')?.getAttribute('src')).toBe(replacement.url);
    });
});

describe('ImageUploadFieldComponent upload cancellation', () => {
    it('cancels a cleared upload and accepts only a later upload', async () => {
        const { component, fixture, imageUploadService } = await setupImageUploadFieldAsync();
        const pending = new Subject<ReturnType<typeof imageSelection>>();
        imageUploadService.upload.mockReturnValueOnce(pending);
        fixture.detectChanges();
        selectFiles(fixture.nativeElement as HTMLElement, new File(['a'], 'old.png', { type: 'image/png' }));
        await fixture.whenStable();
        expect(pending.observed).toBe(true);
        fixture.componentRef.setInput('clearRequest', 1);
        fixture.detectChanges();
        expect(pending.observed).toBe(false);
        pending.next(imageSelection('https://example.test/late.png', 'late'));
        expect(component.value()).toEqual(imageSelection(null, null));
        expect(component['isUploading']()).toBe(false);
        selectFiles(fixture.nativeElement as HTMLElement, new File(['b'], 'new.png', { type: 'image/png' }));
        await fixture.whenStable();
        expect(component.value()).toEqual(imageSelection('https://cdn.example.com/image.jpg', 'asset-1'));
    });

    it('cancels a pending upload when the owner is destroyed', async () => {
        const { component, fixture, imageUploadService } = await setupImageUploadFieldAsync();
        const pending = new Subject<ReturnType<typeof imageSelection>>();
        imageUploadService.upload.mockReturnValue(pending);
        fixture.detectChanges();
        selectFiles(fixture.nativeElement as HTMLElement, new File(['a'], 'photo.png', { type: 'image/png' }));
        await fixture.whenStable();
        expect(pending.observed).toBe(true);
        fixture.destroy();
        expect(pending.observed).toBe(false);
        pending.next(imageSelection('https://example.test/late.png', 'late'));
        expect(component.value()).toBeNull();
    });
});

describe('ImageUploadFieldComponent clearing', () => {
    it('clears selection when the external clear request changes', async () => {
        const { component, fixture, imageUploadService } = await setupImageUploadFieldAsync();
        fixture.componentRef.setInput('deleteOnClear', true);
        component.value.set(imageSelection('https://example.com/image.jpg', 'asset-1'));
        fixture.detectChanges();

        fixture.componentRef.setInput('clearRequest', 1);
        fixture.detectChanges();

        expect(component.value()).toEqual(imageSelection(null, null));
        expect(imageUploadService.deleteAsset).toHaveBeenCalledWith('asset-1');
    });

    it('removes the rendered preview and deletes its asset when configured', async () => {
        const { component, fixture, imageUploadService } = await setupImageUploadFieldAsync();
        fixture.componentRef.setInput('deleteOnClear', true);
        component.value.set(imageSelection('https://example.com/image.jpg', 'asset-1'));
        fixture.detectChanges();
        const host = fixture.nativeElement as HTMLElement;
        const removal = requireElement(host, 'button[aria-label="IMAGE_UPLOAD_FIELD.REMOVE"]', HTMLButtonElement);

        removal.click();
        fixture.detectChanges();

        expect(host.querySelector('img.image-upload-field__preview')).toBeNull();
        expect(component.value()).toEqual(imageSelection(null, null));
        expect(component.touched()).toBe(true);
        expect(imageUploadService.deleteAsset).toHaveBeenCalledWith('asset-1');
    });

    it('logs delete failures without restoring cleared selection', async () => {
        const { component, fixture, imageUploadService, logger } = await setupImageUploadFieldAsync();
        imageUploadService.deleteAsset.mockReturnValueOnce(throwError(() => new Error('delete failed')));
        fixture.componentRef.setInput('deleteOnClear', true);
        component.value.set(imageSelection('https://example.com/image.jpg', 'asset-1'));
        fixture.detectChanges();

        component['clearImage']();

        expect(component['selection']()).toEqual(imageSelection(null, null));
        expect(logger.warn).toHaveBeenCalledWith('Failed to delete orphan image asset', expect.any(Error));
    });
});

describe('ImageUploadFieldComponent pending preparation', () => {
    it.each(['clear', 'destroy'])('does not upload an image resized after %s', async action => {
        const { component, fixture, imageUploadService } = await setupImageUploadFieldAsync();
        const file = new File(['a'], 'photo.png', { type: 'image/png' });
        let finishResize: (result: File) => void = () => {};
        const preparation = new Promise<File>(resolve => {
            finishResize = resolve;
        });
        const resizeOwner = component as unknown as { resizeFileIfNeededAsync: (file: File) => Promise<File> };
        vi.spyOn(resizeOwner, 'resizeFileIfNeededAsync').mockReturnValue(preparation);
        fixture.detectChanges();
        const run = component['handleIncomingFileAsync'](file);
        if (action === 'clear') {
            fixture.componentRef.setInput('clearRequest', 1);
            fixture.detectChanges();
        } else {
            fixture.destroy();
        }
        finishResize(file);
        await run;
        expect(imageUploadService.upload).not.toHaveBeenCalled();
    });
});

describe('ImageUploadFieldComponent native preparation ownership', () => {
    it('discards an earlier decode that settles after the replacement photo has uploaded', async () => {
        const { fixture, component, imageUploadService } = await setupImageUploadFieldAsync();
        const { host, image: earlier, createObjectUrl, revokeObjectUrl } = prepareNativeResize(fixture);
        const later = document.createElement('img');
        Object.defineProperties(later, {
            naturalWidth: { value: RESIZED_IMAGE_SIZE.width },
            naturalHeight: { value: RESIZED_IMAGE_SIZE.height },
        });
        const decodedImages = [earlier, later];
        vi.stubGlobal('Image', function nextDecodedImage(): HTMLImageElement {
            const image = decodedImages.shift();
            if (image === undefined) {
                throw new Error('Unexpected image decode');
            }
            return image;
        });
        const laterPreview = 'blob:replacement-preview';
        const laterDecode = 'blob:replacement-decode';
        createObjectUrl.mockReturnValueOnce(laterPreview).mockReturnValueOnce(laterDecode);
        vi.spyOn(HTMLCanvasElement.prototype, 'getContext').mockReturnValue(null);
        const changed = vi.fn<(selection: ImageSelection | null) => void>();
        component.imageChanged.subscribe(changed);
        const earlierFile = new File(['first'], 'first.png', { type: 'image/png' });
        const replacement = new File(['second'], 'replacement.png', { type: 'image/png' });

        selectFiles(host, earlierFile);
        selectFiles(host, replacement);
        expect(earlier.src).toBe(DECODE_URL);
        expect(later.src).toBe(laterDecode);
        expect(revokeObjectUrl).toHaveBeenCalledWith(PREVIEW_URL);
        later.dispatchEvent(new Event('load'));
        await vi.waitFor(() => {
            expect(changed).toHaveBeenCalledOnce();
        });
        earlier.dispatchEvent(new Event('load'));
        await waitForAsyncTasksAsync();

        expect(imageUploadService.upload).toHaveBeenCalledExactlyOnceWith(replacement);
        expect(changed).toHaveBeenCalledExactlyOnceWith(component.value());
        expect(revokeObjectUrl).toHaveBeenCalledWith(DECODE_URL);
        expect(revokeObjectUrl).toHaveBeenCalledWith(laterDecode);
    });

    it('cancels a whole pending batch before any presigned request when cleared during decode', async () => {
        const { fixture, component, imageUploadService } = await setupImageUploadFieldAsync();
        fixture.componentRef.setInput('multiple', true);
        const files = [new File(['first'], 'first.png', { type: 'image/png' }), new File(['second'], 'second.png', { type: 'image/png' })];
        fixture.componentRef.setInput('maxFiles', files.length);
        const { host, image } = prepareNativeResize(fixture, 'batch');
        const busy = vi.fn<(pending: boolean) => void>();
        const added = vi.fn<(photos: ImageSelection[]) => void>();
        component.batchUploading.subscribe(busy);
        component.imagesAdded.subscribe(added);
        vi.spyOn(HTMLCanvasElement.prototype, 'getContext').mockReturnValue(null);

        dropFiles(host, ...files);
        expect(image.src).toBe(DECODE_URL);
        expect(busy.mock.calls).toEqual([[true]]);
        expect(imageUploadService.upload).not.toHaveBeenCalled();
        fixture.componentRef.setInput('clearRequest', 1);
        fixture.detectChanges();
        image.dispatchEvent(new Event('load'));
        await waitForAsyncTasksAsync();

        expect(busy.mock.calls).toEqual([[true], [false]]);
        expect(imageUploadService.upload).not.toHaveBeenCalled();
        expect(added).not.toHaveBeenCalled();
    });
});

describe('ImageUploadFieldComponent disabled preparation', () => {
    it('settles disabled photo preparation and accepts a later selection', async () => {
        const { fixture, component, imageUploadService } = await setupImageUploadFieldAsync();
        const { host, image, revokeObjectUrl } = prepareNativeResize(fixture);
        vi.spyOn(HTMLCanvasElement.prototype, 'getContext').mockReturnValue(null);
        const saved = imageSelection('https://example.test/saved.png', 'saved-photo');
        component.value.set(saved);
        fixture.detectChanges();
        const first = new File(['first photo'], 'first.png', { type: 'image/png' });
        const preparationStarted = vi.fn();
        const preparationFailed = vi.fn();
        component.imagePreparationStarted.subscribe(preparationStarted);
        component.imagePreparationFailed.subscribe(preparationFailed);

        selectFiles(host, first);
        expect(image.src).toBe(DECODE_URL);
        expect(preparationStarted).toHaveBeenCalledExactlyOnceWith(PREVIEW_URL);
        fixture.componentRef.setInput('disabled', true);
        fixture.detectChanges();
        expect(requireElement(host, 'input[type="file"]', HTMLInputElement).disabled).toBe(true);
        image.dispatchEvent(new Event('load'));
        await vi.waitFor(() => {
            expect(preparationFailed).toHaveBeenCalledOnce();
        });

        expect(imageUploadService.upload).not.toHaveBeenCalled();
        expect(component.value()).toEqual(saved);
        expect(revokeObjectUrl).toHaveBeenCalledWith(DECODE_URL);
        expect(revokeObjectUrl).toHaveBeenCalledWith(PREVIEW_URL);

        fixture.componentRef.setInput('disabled', false);
        fixture.detectChanges();
        const replacement = new File(['replacement photo'], 'replacement.png', { type: 'image/png' });
        selectFiles(host, replacement);
        image.dispatchEvent(new Event('load'));
        await vi.waitFor(() => {
            expect(imageUploadService.upload).toHaveBeenCalledExactlyOnceWith(replacement);
        });
    });
});

describe('ImageUploadFieldComponent interactions', () => {
    it('removes hidden upload controls from the accessibility tree and tab order', async () => {
        const { fixture } = await setupImageUploadFieldAsync();
        fixture.componentRef.setInput('appearance', 'hidden');
        fixture.detectChanges();

        const host = fixture.nativeElement as HTMLElement;
        const dropzone = host.querySelector('.image-upload-field__dropzone') as HTMLElement;

        expect(dropzone.getAttribute('role')).toBeNull();
        expect(dropzone.getAttribute('tabindex')).toBe('-1');
        expect(dropzone.getAttribute('aria-hidden')).toBe('true');
    });

    it.each([
        { action: 'pointer', key: null },
        { action: 'Enter', key: 'Enter' },
        { action: 'Space', key: ' ' },
    ])('opens the empty file picker by $action and blocks activation when disabled or filled', async ({ key }) => {
        const { component, fixture } = await setupImageUploadFieldAsync();
        fixture.detectChanges();
        const host = fixture.nativeElement as HTMLElement;
        const dropzone = requireElement(host, '.image-upload-field__dropzone', HTMLElement);
        const fileInput = requireElement(host, 'input[type="file"]', HTMLInputElement);
        const activated = vi.fn();
        fileInput.addEventListener('click', activated);
        const activate = (): void => {
            if (key === null) {
                dropzone.click();
            } else {
                dropzone.dispatchEvent(new KeyboardEvent('keydown', { key, bubbles: true, cancelable: true }));
            }
        };

        activate();
        expect(activated).toHaveBeenCalledOnce();
        fixture.componentRef.setInput('disabled', true);
        fixture.detectChanges();
        expect(dropzone.getAttribute('aria-disabled')).toBe('true');
        activate();
        expect(activated).toHaveBeenCalledOnce();

        fixture.componentRef.setInput('disabled', false);
        component.value.set(imageSelection('https://example.com/image.jpg', 'asset-1'));
        fixture.detectChanges();
        activate();
        expect(activated).toHaveBeenCalledOnce();
    });
});

describe('ImageUploadFieldComponent drag, drop and crop controls', () => {
    it('clears the drop highlight after leaving or dropping a link and keeps the saved photo', async () => {
        const { component, fixture, imageUploadService } = await setupImageUploadFieldAsync();
        const saved = imageSelection('https://example.com/saved.jpg', 'saved-photo');
        const changed = vi.fn();
        component.imageChanged.subscribe(changed);
        component.value.set(saved);
        fixture.detectChanges();
        const zone = requireElement(fixture.nativeElement as HTMLElement, '.image-upload-field__dropzone', HTMLElement);
        const draggingClass = 'image-upload-field__dropzone--dragging';
        const dragOver = new Event('dragover', { bubbles: true, cancelable: true });

        zone.dispatchEvent(dragOver);
        fixture.detectChanges();
        expect(dragOver.defaultPrevented).toBe(true);
        expect(zone.classList.contains(draggingClass)).toBe(true);
        zone.dispatchEvent(new Event('dragleave', { bubbles: true, cancelable: true }));
        fixture.detectChanges();
        expect(zone.classList.contains(draggingClass)).toBe(false);

        zone.dispatchEvent(new Event('dragover', { bubbles: true, cancelable: true }));
        const drop = new Event('drop', { bubbles: true, cancelable: true });
        Object.defineProperty(drop, 'dataTransfer', { value: { files: [], types: ['text/uri-list'] } });
        zone.dispatchEvent(drop);
        fixture.detectChanges();

        expect(drop.defaultPrevented).toBe(true);
        expect(zone.classList.contains(draggingClass)).toBe(false);
        expect(zone.getAttribute('aria-busy')).toBe('false');
        expect(requireElement(zone, '.image-upload-field__preview', HTMLImageElement).src).toBe(saved.url);
        expect(imageUploadService.upload).not.toHaveBeenCalled();
        expect(changed).not.toHaveBeenCalled();
    });

    it('ignores drop and dragover while disabled', async () => {
        const { component, fixture, imageUploadService } = await setupImageUploadFieldAsync();
        const preventDefault = vi.fn();
        const stopPropagation = vi.fn();
        fixture.detectChanges();

        fixture.componentRef.setInput('disabled', true);
        fixture.detectChanges();
        component['onDragOver']({ preventDefault } as unknown as DragEvent);
        component['onDrop']({
            preventDefault,
            stopPropagation,
            dataTransfer: { files: [new File(['image'], 'photo.png', { type: 'image/png' })] },
        } as unknown as DragEvent);

        expect(component['isDragging']()).toBe(false);
        expect(imageUploadService.upload).not.toHaveBeenCalled();
        expect(preventDefault).toHaveBeenCalled();
        expect(stopPropagation).toHaveBeenCalled();
    });
});

describe('ImageUploadFieldComponent preview keyboard controls', () => {
    it.each(['REPLACE', 'REMOVE'])('leaves Space activation to the native %s button in a filled upload field', async action => {
        const { component, fixture } = await setupImageUploadFieldAsync();
        component.value.set(imageSelection('https://example.test/saved.jpg', 'saved-photo'));
        fixture.detectChanges();
        const host = fixture.nativeElement as HTMLElement;
        const control = requireElement(host, `button[aria-label="IMAGE_UPLOAD_FIELD.${action}"]`, HTMLButtonElement);
        const fileInput = requireElement(host, 'input[type="file"]', HTMLInputElement);
        const chooser = vi.fn();
        fileInput.addEventListener('click', chooser);
        expect(control.disabled).toBe(false);

        const space = new KeyboardEvent('keydown', { key: ' ', bubbles: true, cancelable: true });
        control.dispatchEvent(space);

        expect(space.defaultPrevented).toBe(false);
        expect(chooser).not.toHaveBeenCalled();
    });
});

describe('ImageUploadFieldComponent interrupted drag', () => {
    it('clears the highlighted drop target when the owner disables and re-enables it', async () => {
        const { fixture, imageUploadService } = await setupImageUploadFieldAsync();
        fixture.detectChanges();
        const host = fixture.nativeElement as HTMLElement;
        const zone = requireElement(host, '.image-upload-field__dropzone', HTMLElement);
        const draggingClass = 'image-upload-field__dropzone--dragging';

        zone.dispatchEvent(new Event('dragover', { bubbles: true, cancelable: true }));
        fixture.detectChanges();
        expect(zone.classList.contains(draggingClass)).toBe(true);

        fixture.componentRef.setInput('disabled', true);
        fixture.detectChanges();
        expect(zone.getAttribute('aria-disabled')).toBe('true');
        expect(zone.classList.contains(draggingClass)).toBe(false);

        fixture.componentRef.setInput('disabled', false);
        fixture.detectChanges();
        expect(zone.getAttribute('aria-disabled')).toBe('false');
        expect(zone.classList.contains(draggingClass)).toBe(false);
        dropFiles(host, new File(['photo'], 'next.png', { type: 'image/png' }));
        await vi.waitFor(() => {
            fixture.detectChanges();
            expect(imageUploadService.upload).toHaveBeenCalledOnce();
            expect(zone.querySelector('.image-upload-field__preview')).not.toBeNull();
        });
    });
});

describe('ImageUploadFieldComponent keyboard crop controls', () => {
    it('moves crop selection with keyboard arrows and ignores unrelated keys', async () => {
        const { component, fixture } = await setupImageUploadFieldAsync();
        const preventDefault = vi.fn();
        component['cropImageBounds'].set({ x: 0, y: 0, width: 100, height: 100 });
        component['cropSelection'].set({ x: 10, y: 10, width: 40, height: 40 });
        fixture.detectChanges();

        component['onCropKeydown']({ key: 'ArrowRight', shiftKey: true, preventDefault } as unknown as KeyboardEvent);
        expect(component['cropSelection']()?.x).toBe(CROP_SHIFTED_X);

        component['onCropKeydown']({ key: 'Enter', shiftKey: false, preventDefault } as unknown as KeyboardEvent);
        expect(component['cropSelection']()?.x).toBe(CROP_SHIFTED_X);
    });
});

describe('ImageUploadFieldComponent multiple uploads', () => {
    const files = [new File(['a'], 'one.png', { type: 'image/png' }), new File(['b'], 'two.png', { type: 'image/png' })];
    it.each(['clear', 'destroy'])('cancels the active batch on %s without starting the next file', async action => {
        const { component, fixture, imageUploadService } = await setupImageUploadFieldAsync();
        const pending = new Subject<ReturnType<typeof imageSelection>>();
        imageUploadService.upload.mockReturnValue(pending);
        const added = vi.fn();
        const busy = vi.fn();
        component.imagesAdded.subscribe(added);
        component.batchUploading.subscribe(busy);
        fixture.detectChanges();
        const run = component['uploadBatchAsync'](files);
        await vi.waitFor(() => {
            expect(pending.observed).toBe(true);
        });
        if (action === 'clear') {
            fixture.componentRef.setInput('clearRequest', 1);
            fixture.detectChanges();
        } else {
            fixture.destroy();
        }
        await run;
        expect(pending.observed).toBe(false);
        expect(imageUploadService.upload).toHaveBeenCalledOnce();
        expect(added).not.toHaveBeenCalled();
        if (action === 'clear') {
            expect(busy.mock.calls).toEqual([[true], [false]]);
            expect(component['isUploading']()).toBe(false);
        }
    });
    it('uploads a dropped batch in order and keeps busy until completion', async () => {
        const { component, fixture, imageUploadService } = await setupImageUploadFieldAsync();
        fixture.componentRef.setInput('multiple', true);
        fixture.componentRef.setInput('maxFiles', 2);
        const added = vi.fn();
        const busy = vi.fn();
        component.imagesAdded.subscribe(added);
        component.batchUploading.subscribe(busy);
        component['onDrop']({ preventDefault: vi.fn(), stopPropagation: vi.fn(), dataTransfer: { files } } as unknown as DragEvent);
        await vi.waitFor(() => {
            expect(added).toHaveBeenCalledOnce();
        });
        expect(imageUploadService.upload).toHaveBeenNthCalledWith(1, files[0]);
        expect(imageUploadService.upload).toHaveBeenNthCalledWith(2, files[1]);
        expect(added.mock.calls[0][0]).toHaveLength(2);
        expect(busy.mock.calls).toEqual([[true], [false]]);
    });
    it('rejects an oversized batch without uploading or silently discarding files', async () => {
        const { component, fixture, imageUploadService } = await setupImageUploadFieldAsync();
        fixture.componentRef.setInput('multiple', true);
        component['onDrop']({ preventDefault: vi.fn(), stopPropagation: vi.fn(), dataTransfer: { files } } as unknown as DragEvent);
        expect(imageUploadService.upload).not.toHaveBeenCalled();
        expect(component['error']()).toBeTruthy();
    });
    it('preserves successful uploads when a later upload fails and releases busy state', async () => {
        const { component, fixture, imageUploadService } = await setupImageUploadFieldAsync();
        fixture.componentRef.setInput('multiple', true);
        fixture.componentRef.setInput('maxFiles', 2);
        imageUploadService.upload
            .mockReturnValueOnce(of(imageSelection('/one.png', 'one')))
            .mockReturnValueOnce(throwError(() => new Error('failed')));
        const added = vi.fn();
        component.imagesAdded.subscribe(added);
        component['onDrop']({ preventDefault: vi.fn(), stopPropagation: vi.fn(), dataTransfer: { files } } as unknown as DragEvent);
        await vi.waitFor(() => {
            expect(added).toHaveBeenCalledWith([imageSelection('/one.png', 'one')]);
        });
        expect(component['error']()).toBeTruthy();
        expect(component['isUploading']()).toBe(false);
    });
});

describe('ImageUploadFieldComponent partial batches and retry', () => {
    it.each([
        ['format', new File(['x'], 'unsupported.gif', { type: 'image/gif' }), 'IMAGE_UPLOAD_FIELD.ERRORS.ONLY_IMAGES'],
        ['size', new File(['too large'], 'oversized.png', { type: 'image/png' }), 'IMAGE_UPLOAD_FIELD.ERRORS.FILE_TOO_LARGE'],
    ] as const)('keeps the completed photo when a later batch file fails %s validation', async (_failure, rejected, key) => {
        const { fixture, component, imageUploadService } = await setupImageUploadFieldAsync();
        const added = vi.fn<(photos: ImageSelection[]) => void>();
        const busy = vi.fn<(pending: boolean) => void>();
        const hasError = vi.fn<(error: boolean) => void>();
        component.imagesAdded.subscribe(added);
        component.batchUploading.subscribe(busy);
        component.hasErrorChange.subscribe(hasError);
        const first = new File(['a'], 'first.png', { type: 'image/png' });
        const next = new File(['b'], 'next.png', { type: 'image/png' });
        const chosen = [first, rejected, next];
        fixture.componentRef.setInput('multiple', true);
        fixture.componentRef.setInput('maxFiles', chosen.length);
        fixture.componentRef.setInput('acceptedTypes', 'image/png');
        fixture.componentRef.setInput('maxSizeMb', TINY_MAX_SIZE_MB);
        fixture.detectChanges();
        const host = fixture.nativeElement as HTMLElement;
        const firstResult = imageSelection('https://example.test/first.png', 'first');
        const retryResult = imageSelection('https://example.test/next.png', 'next');
        imageUploadService.upload.mockReturnValueOnce(of(firstResult)).mockReturnValueOnce(of(retryResult));

        dropFiles(host, ...chosen);
        await vi.waitFor(() => {
            fixture.detectChanges();
            expect(host.querySelector('[role="alert"]')?.textContent.trim()).toBe(key);
        });

        expect(imageUploadService.upload).toHaveBeenCalledExactlyOnceWith(first);
        expect(added).toHaveBeenCalledExactlyOnceWith([firstResult]);
        expect(busy.mock.calls).toEqual([[true], [false]]);
        expect(hasError).toHaveBeenLastCalledWith(true);

        dropFiles(host, next);
        await vi.waitFor(() => {
            fixture.detectChanges();
            expect(added).toHaveBeenLastCalledWith([retryResult]);
        });

        expect(imageUploadService.upload.mock.calls).toEqual([[first], [next]]);
        expect(host.querySelector('[role="alert"]')).toBeNull();
        expect(busy.mock.calls).toEqual([[true], [false], [true], [false]]);
        expect(hasError).toHaveBeenLastCalledWith(false);
    });
});

describe('ImageUploadFieldComponent batch response after clearing', () => {
    it('discards an already queued upload error when the owner clears the batch', async () => {
        const { fixture, component, imageUploadService } = await setupImageUploadFieldAsync();
        const pending = new Subject<ImageSelection>();
        const saved = imageSelection('https://example.test/new.png', 'new');
        const added = vi.fn<(photos: ImageSelection[]) => void>();
        const busy = vi.fn<(pending: boolean) => void>();
        component.imagesAdded.subscribe(added);
        component.batchUploading.subscribe(busy);
        imageUploadService.upload.mockReturnValueOnce(pending).mockReturnValueOnce(of(saved));
        fixture.componentRef.setInput('multiple', true);
        fixture.componentRef.setInput('maxFiles', 2);
        fixture.detectChanges();
        const host = fixture.nativeElement as HTMLElement;
        const old = new File(['old'], 'old.png', { type: 'image/png' });
        const ignored = new File(['ignored'], 'ignored.png', { type: 'image/png' });
        const next = new File(['new'], 'new.png', { type: 'image/png' });

        dropFiles(host, old, ignored);
        await vi.waitFor(() => {
            expect(pending.observed).toBe(true);
        });

        pending.error(new Error('upload failed'));
        fixture.componentRef.setInput('clearRequest', 1);
        fixture.detectChanges();
        await fixture.whenStable();

        expect(host.querySelector('[role="alert"]')).toBeNull();
        expect(host.querySelector('[role="status"]')).toBeNull();
        expect(added).not.toHaveBeenCalled();
        expect(imageUploadService.upload).toHaveBeenCalledExactlyOnceWith(old);

        dropFiles(host, next);
        await vi.waitFor(() => {
            expect(added).toHaveBeenCalledExactlyOnceWith([saved]);
        });
        expect(imageUploadService.upload.mock.calls).toEqual([[old], [next]]);
        expect(busy.mock.calls).toEqual([[true], [false], [true], [false]]);
    });
});

describe('ImageUploadFieldComponent health consent', () => {
    it('waits for consent before sending a selected attachment', async () => {
        const { fixture, imageUploadService, privacyConsent } = await setupImageUploadFieldAsync();
        const consent = new Subject<boolean>();
        privacyConsent.ensureHealthConsentAsync.mockReturnValueOnce(firstValueFrom(consent));
        fixture.componentRef.setInput('requireHealthConsent', true);
        fixture.detectChanges();

        const photo = new File(['image'], 'attachment.png', { type: 'image/png' });
        selectFiles(fixture.nativeElement as HTMLElement, photo);

        expect(privacyConsent.ensureHealthConsentAsync).toHaveBeenCalledExactlyOnceWith(true);
        expect(imageUploadService.upload).not.toHaveBeenCalled();
        consent.next(true);
        await vi.waitFor(() => {
            expect(imageUploadService.upload).toHaveBeenCalledExactlyOnceWith(photo);
        });
    });

    it.each([
        { failed: false, needsReload: false, feedback: null },
        { failed: true, needsReload: false, feedback: 'PRIVACY_CHOICES.ERROR' },
        { failed: true, needsReload: true, feedback: 'PRIVACY_CHOICES.RELOAD' },
    ])('keeps the attachment local after declined consent and shows $feedback', async ({ failed, needsReload, feedback }) => {
        const { fixture, imageUploadService, privacyConsent, translateService } = await setupImageUploadFieldAsync();
        vi.spyOn(translateService, 'instant').mockImplementation(key => String(key));
        privacyConsent.ensureHealthConsentAsync.mockResolvedValueOnce(false);
        privacyConsent.failed.mockReturnValue(failed);
        privacyConsent.needsReload.mockReturnValue(needsReload);
        fixture.componentRef.setInput('requireHealthConsent', true);
        fixture.detectChanges();
        const host = fixture.nativeElement as HTMLElement;

        selectFiles(host, new File(['image'], 'attachment.png', { type: 'image/png' }));
        await waitForAsyncTasksAsync();
        await fixture.whenStable();
        fixture.detectChanges();

        expect(imageUploadService.upload).not.toHaveBeenCalled();
        expect(host.querySelector('[role="alert"]')?.textContent.trim() ?? null).toBe(feedback);
    });

    it.each(['before approval', 'while approval is being applied'])('keeps a cleared attachment local %s', async clearTiming => {
        const { fixture, imageUploadService, privacyConsent } = await setupImageUploadFieldAsync();
        const consent = new Subject<boolean>();
        const approval = firstValueFrom(consent);
        privacyConsent.ensureHealthConsentAsync.mockReturnValueOnce(approval);
        fixture.componentRef.setInput('requireHealthConsent', true);
        fixture.detectChanges();
        const host = fixture.nativeElement as HTMLElement;

        selectFiles(host, new File(['old'], 'old.png', { type: 'image/png' }));
        if (clearTiming === 'while approval is being applied') {
            consent.next(true);
            await approval;
        }
        fixture.componentRef.setInput('clearRequest', 1);
        fixture.detectChanges();
        if (clearTiming === 'before approval') {
            consent.next(true);
        }
        await waitForAsyncTasksAsync();
        await fixture.whenStable();
        expect(imageUploadService.upload).not.toHaveBeenCalled();

        const replacement = new File(['new'], 'new.png', { type: 'image/png' });
        selectFiles(host, replacement);
        await vi.waitFor(() => {
            expect(imageUploadService.upload).toHaveBeenCalledExactlyOnceWith(replacement);
        });
    });
});

function mockNativeDialogMethods(): () => void {
    const dialogPrototype = HTMLDialogElement.prototype;
    const showModal = Object.getOwnPropertyDescriptor(dialogPrototype, 'showModal');
    const close = Object.getOwnPropertyDescriptor(dialogPrototype, 'close');

    // jsdom exposes the dialog element without the modal methods.
    Object.defineProperties(dialogPrototype, {
        showModal: {
            configurable: true,
            value(this: HTMLDialogElement) {
                this.open = true;
            },
        },
        close: {
            configurable: true,
            value(this: HTMLDialogElement) {
                this.open = false;
            },
        },
    });

    return () => {
        for (const [method, descriptor] of [
            ['showModal', showModal],
            ['close', close],
        ] as const) {
            if (descriptor === undefined) {
                Reflect.deleteProperty(dialogPrototype, method);
            } else {
                Object.defineProperty(dialogPrototype, method, descriptor);
            }
        }
    };
}

type CropperDom = {
    dialog: HTMLDialogElement;
    apply: HTMLButtonElement;
    surface: HTMLDivElement;
    image: HTMLImageElement;
};

async function mountCropperAsync(
    fixture: ComponentFixture<ImageUploadFieldComponent>,
    fileName: string,
    type = 'image/png',
): Promise<CropperDom> {
    fixture.componentRef.setInput('cropEnabled', true);
    fixture.detectChanges();
    const host = fixture.nativeElement as HTMLElement;
    selectFiles(host, new File(['source image'], fileName, { type }));
    await vi.waitFor(() => {
        fixture.detectChanges();
        expect(host.querySelector('dialog[open]')).not.toBeNull();
    });

    const dialog = requireElement(host, 'dialog', HTMLDialogElement);
    const apply = requireElement(dialog, '.image-upload-field__cropper-actions fd-ui-button:last-child button', HTMLButtonElement);
    expect(apply.disabled).toBe(true);
    const surface = requireElement(dialog, '.image-upload-field__cropper-body', HTMLDivElement);
    vi.spyOn(surface, 'getBoundingClientRect').mockReturnValue(new DOMRect(0, 0, CROP_SURFACE_SIZE.width, CROP_SURFACE_SIZE.height));
    const image = requireElement(surface, 'img', HTMLImageElement);
    return { dialog, apply, surface, image };
}

async function openCropperAsync(
    fixture: ComponentFixture<ImageUploadFieldComponent>,
    fileName: string,
    type = 'image/png',
): Promise<CropperDom> {
    const cropper = await mountCropperAsync(fixture, fileName, type);
    Object.defineProperties(cropper.image, { naturalWidth: { value: 800 }, naturalHeight: { value: 600 } });
    cropper.image.dispatchEvent(new Event('load'));
    fixture.detectChanges();
    expect(cropper.apply.disabled).toBe(false);
    return cropper;
}

describe('ImageUploadFieldComponent rendered cropper', () => {
    beforeEach(mockNativeDialogMethods);
    it('uploads the crop selected through the rendered drag and resize controls', async () => {
        const { component, fixture, imageUploadService } = await setupImageUploadFieldAsync();
        const changed = vi.fn();
        component.imageChanged.subscribe(changed);
        const { dialog, apply, surface, image } = await openCropperAsync(fixture, 'photo.png');
        const capture = vi.fn();
        const release = vi.fn();
        Object.defineProperties(surface, { setPointerCapture: { value: capture }, releasePointerCapture: { value: release } });
        const drawImage = vi.fn();
        vi.spyOn(HTMLCanvasElement.prototype, 'getContext').mockReturnValue({ drawImage } as unknown as CanvasRenderingContext2D);
        const cropped = new Blob(['cropped'], { type: 'image/png' });
        vi.spyOn(HTMLCanvasElement.prototype, 'toBlob').mockImplementation(callback => {
            callback(cropped);
        });

        const movePointer = 7;
        const resizePointer = 8;
        const otherFinger = 9;
        const selection = requireElement(surface, '[role="group"]', HTMLElement);
        selection.dispatchEvent(new PointerEvent('pointerdown', { bubbles: true, pointerId: movePointer, clientX: 100, clientY: 80 }));
        surface.dispatchEvent(new PointerEvent('pointermove', { pointerId: otherFinger, clientX: 140, clientY: 80 }));
        surface.dispatchEvent(new PointerEvent('pointerup', { pointerId: otherFinger }));
        fixture.detectChanges();
        expect(selection.style.left).toBe('50px');
        expect(release).not.toHaveBeenCalled();
        surface.dispatchEvent(new PointerEvent('pointermove', { pointerId: movePointer, clientX: 120, clientY: 80 }));
        surface.dispatchEvent(new PointerEvent('pointerup', { pointerId: movePointer }));
        fixture.detectChanges();
        expect(selection.style.left).toBe('70px');

        const handle = requireElement(selection, '.image-upload-field__crop-handle--se', HTMLElement);
        handle.dispatchEvent(new PointerEvent('pointerdown', { bubbles: true, pointerId: resizePointer, clientX: 370, clientY: 300 }));
        surface.dispatchEvent(new PointerEvent('pointermove', { pointerId: resizePointer, clientX: 320, clientY: 250 }));
        surface.dispatchEvent(new PointerEvent('pointercancel', { pointerId: resizePointer }));
        fixture.detectChanges();
        expect(selection.style.width).toBe('250px');
        expect(selection.style.height).toBe('250px');
        expect(capture.mock.calls).toEqual([[movePointer], [resizePointer]]);
        expect(release.mock.calls).toEqual([[movePointer], [resizePointer]]);
        expect(imageUploadService.upload).not.toHaveBeenCalled();

        apply.click();
        fixture.detectChanges();

        const sourceCrop = { left: 140, size: 500 };
        const avatarSize = 512;
        expect(drawImage).toHaveBeenCalledExactlyOnceWith(
            image,
            sourceCrop.left,
            0,
            sourceCrop.size,
            sourceCrop.size,
            0,
            0,
            avatarSize,
            avatarSize,
        );
        expect(imageUploadService.upload).toHaveBeenCalledExactlyOnceWith(
            expect.objectContaining({ name: 'photo.png', type: 'image/png', size: cropped.size }),
        );
        expect(dialog.open).toBe(false);
        expect(component.value()).toEqual(imageSelection('https://cdn.example.com/image.jpg', 'asset-1'));
        expect(changed).toHaveBeenCalledExactlyOnceWith(component.value());
    });
});

describe('ImageUploadFieldComponent responsive cropper', () => {
    beforeEach(mockNativeDialogMethods);

    it('preserves the crop source pixels across a resize during a drag', async () => {
        const { notifyResize, observe, disconnect } = mockCropResizeObserver();
        const { fixture, imageUploadService } = await setupImageUploadFieldAsync();
        const { apply, surface, image } = await openCropperAsync(fixture, 'photo.png');
        const release = vi.fn();
        Object.defineProperties(surface, {
            setPointerCapture: { value: vi.fn() },
            hasPointerCapture: { value: vi.fn().mockReturnValue(true) },
            releasePointerCapture: { value: release },
        });
        const drawImage = vi.fn();
        vi.spyOn(HTMLCanvasElement.prototype, 'getContext').mockReturnValue({ drawImage } as unknown as CanvasRenderingContext2D);
        vi.spyOn(HTMLCanvasElement.prototype, 'toBlob').mockImplementation(callback => {
            callback(new Blob(['cropped'], { type: 'image/png' }));
        });

        const pointerId = 7;
        const selection = requireElement(surface, '[role="group"]', HTMLElement);
        selection.dispatchEvent(new PointerEvent('pointerdown', { bubbles: true, pointerId, clientX: 100, clientY: 80 }));
        surface.dispatchEvent(new PointerEvent('pointermove', { pointerId, clientX: 130, clientY: 80 }));
        fixture.detectChanges();
        expect(selection.style.left).toBe('80px');
        expect(observe).toHaveBeenCalledExactlyOnceWith(surface);

        const resizedSurface = { width: 200, height: 300 };
        vi.spyOn(surface, 'getBoundingClientRect').mockReturnValue(new DOMRect(0, 0, resizedSurface.width, resizedSurface.height));
        notifyResize();
        surface.dispatchEvent(new PointerEvent('pointermove', { pointerId, clientX: 140, clientY: 85 }));
        fixture.detectChanges();

        expect(selection.style.left).toBe('40px');
        expect(selection.style.top).toBe('75px');
        expect(selection.style.width).toBe('150px');
        expect(release).toHaveBeenCalledExactlyOnceWith(pointerId);

        apply.click();
        fixture.detectChanges();
        const sourceCrop = { left: 160, size: 600 };
        const avatarSize = 512;
        expect(drawImage).toHaveBeenCalledExactlyOnceWith(
            image,
            sourceCrop.left,
            0,
            sourceCrop.size,
            sourceCrop.size,
            0,
            0,
            avatarSize,
            avatarSize,
        );
        expect(imageUploadService.upload).toHaveBeenCalledOnce();
        expect(disconnect).toHaveBeenCalledOnce();
    });

    it.each(['cancel', 'destroy'] as const)('stops watching the crop surface after %s', async action => {
        const { observe, disconnect } = mockCropResizeObserver();
        const { fixture, imageUploadService } = await setupImageUploadFieldAsync();
        const { dialog, surface } = await openCropperAsync(fixture, 'photo.png');
        expect(observe).toHaveBeenCalledExactlyOnceWith(surface);
        if (action === 'cancel') {
            dialog.dispatchEvent(new Event('cancel', { cancelable: true }));
            fixture.detectChanges();
            expect(dialog.open).toBe(false);
        } else {
            fixture.destroy();
        }
        expect(disconnect).toHaveBeenCalledOnce();
        expect(imageUploadService.upload).not.toHaveBeenCalled();
    });
});

describe('ImageUploadFieldComponent crop surface observations', () => {
    beforeEach(mockNativeDialogMethods);

    it('waits for a decoded image and preserves a finished drag through an ordinary resize', async () => {
        const { notifyResize, observe, disconnect } = mockCropResizeObserver();
        const { fixture, imageUploadService } = await setupImageUploadFieldAsync();
        const cold = await mountCropperAsync(fixture, 'slow-photo.png');
        expect(observe).toHaveBeenNthCalledWith(1, cold.surface);
        notifyResize();
        fixture.detectChanges();
        expect(cold.apply.disabled).toBe(true);
        expect(cold.surface.querySelector('[role="group"]')).toBeNull();

        cold.dialog.dispatchEvent(new Event('cancel', { cancelable: true }));
        fixture.detectChanges();
        expect(cold.dialog.open).toBe(false);
        expect(disconnect).toHaveBeenCalledOnce();
        expect(imageUploadService.upload).not.toHaveBeenCalled();

        const { apply, surface, image } = await openCropperAsync(fixture, 'next-photo.png');
        expect(observe).toHaveBeenNthCalledWith(2, surface);
        const selection = requireElement(surface, '[role="group"]', HTMLElement);
        const initialGeometry = selection.style.cssText;
        expect(initialGeometry).toContain('width: 300px');
        notifyResize();
        fixture.detectChanges();
        expect(selection.style.cssText).toBe(initialGeometry);
        expect(apply.disabled).toBe(false);

        const release = vi.fn();
        Object.defineProperties(surface, { setPointerCapture: { value: vi.fn() }, releasePointerCapture: { value: release } });
        const pointerId = 7;
        selection.dispatchEvent(new PointerEvent('pointerdown', { bubbles: true, pointerId, clientX: 100, clientY: 80 }));
        surface.dispatchEvent(new PointerEvent('pointermove', { pointerId, clientX: 130, clientY: 80 }));
        surface.dispatchEvent(new PointerEvent('pointerup', { pointerId }));
        fixture.detectChanges();
        expect(selection.style.left).toBe('80px');

        const resizedSurface = { width: 200, height: 300 };
        vi.spyOn(surface, 'getBoundingClientRect').mockReturnValue(new DOMRect(0, 0, resizedSurface.width, resizedSurface.height));
        notifyResize();
        fixture.detectChanges();
        expect(selection.style.left).toBe('40px');
        expect(selection.style.top).toBe('75px');
        expect(selection.style.width).toBe('150px');
        expect(release).toHaveBeenCalledExactlyOnceWith(pointerId);

        const drawImage = vi.fn();
        vi.spyOn(HTMLCanvasElement.prototype, 'getContext').mockReturnValue({ drawImage } as unknown as CanvasRenderingContext2D);
        vi.spyOn(HTMLCanvasElement.prototype, 'toBlob').mockImplementation(callback => {
            callback(new Blob(['cropped'], { type: 'image/png' }));
        });
        apply.click();
        fixture.detectChanges();
        const sourceCrop = { left: 160, size: 600 };
        const avatarSize = 512;
        expect(drawImage).toHaveBeenCalledExactlyOnceWith(
            image,
            sourceCrop.left,
            0,
            sourceCrop.size,
            sourceCrop.size,
            0,
            0,
            avatarSize,
            avatarSize,
        );
        expect(imageUploadService.upload).toHaveBeenCalledExactlyOnceWith(expect.objectContaining({ name: 'next-photo.png' }));
    });
});

function mockCropResizeObserver(): { notifyResize: () => void; observe: ReturnType<typeof vi.fn>; disconnect: ReturnType<typeof vi.fn> } {
    let notifyResize = (): void => {};
    const observe = vi.fn();
    const disconnect = vi.fn();
    vi.stubGlobal(
        'ResizeObserver',
        class implements ResizeObserver {
            public readonly observe = observe;
            public readonly unobserve = vi.fn();
            public readonly disconnect = disconnect;

            public constructor(callback: ResizeObserverCallback) {
                notifyResize = (): void => {
                    callback([], this);
                };
            }
        },
    );
    return {
        notifyResize: (): void => {
            notifyResize();
        },
        observe,
        disconnect,
    };
}

describe('ImageUploadFieldComponent rendered cropper dismissal', () => {
    beforeEach(mockNativeDialogMethods);
    it('closes the dialog on native cancel and discards a late canvas result', async () => {
        const { component, fixture, imageUploadService } = await setupImageUploadFieldAsync();
        const previous = imageSelection('https://example.test/avatar.png', 'existing');
        component.value.set(previous);
        const revokePreview = vi.spyOn(URL, 'revokeObjectURL');
        const { dialog, apply } = await openCropperAsync(fixture, 'replacement.png');
        vi.spyOn(HTMLCanvasElement.prototype, 'getContext').mockReturnValue({ drawImage: vi.fn() } as unknown as CanvasRenderingContext2D);
        const toBlob = vi.spyOn(HTMLCanvasElement.prototype, 'toBlob').mockImplementation(() => {});

        apply.click();
        expect(toBlob).toHaveBeenCalledOnce();
        const cancel = new Event('cancel', { cancelable: true });
        dialog.dispatchEvent(cancel);
        fixture.detectChanges();
        toBlob.mock.calls[0][0](new Blob(['finished late'], { type: 'image/png' }));

        expect(cancel.defaultPrevented).toBe(true);
        expect(dialog.open).toBe(false);
        expect(component.value()).toEqual(previous);
        expect(imageUploadService.upload).not.toHaveBeenCalled();
        expect(revokePreview).toHaveBeenCalledWith(PREVIEW_URL);
    });

    it('cancels the pending crop and releases its preview when the field becomes disabled', async () => {
        const { component, fixture, imageUploadService } = await setupImageUploadFieldAsync();
        const previous = imageSelection('https://example.test/avatar.png', 'existing');
        component.value.set(previous);
        const changed = vi.fn();
        const preparationFailed = vi.fn();
        const revokePreview = vi.spyOn(URL, 'revokeObjectURL');
        component.imageChanged.subscribe(changed);
        component.imagePreparationFailed.subscribe(preparationFailed);
        const { dialog, apply } = await openCropperAsync(fixture, 'replacement.png');
        vi.spyOn(HTMLCanvasElement.prototype, 'getContext').mockReturnValue({ drawImage: vi.fn() } as unknown as CanvasRenderingContext2D);
        const toBlob = vi.spyOn(HTMLCanvasElement.prototype, 'toBlob').mockImplementation(() => {});
        apply.click();
        expect(toBlob).toHaveBeenCalledOnce();

        fixture.componentRef.setInput('disabled', true);
        fixture.detectChanges();
        toBlob.mock.calls[0][0](new Blob(['finished after disabling'], { type: 'image/png' }));
        fixture.detectChanges();

        expect(dialog.open).toBe(false);
        expect(component.value()).toEqual(previous);
        expect(changed).not.toHaveBeenCalled();
        expect(imageUploadService.upload).not.toHaveBeenCalled();
        expect(preparationFailed).toHaveBeenCalledOnce();
        expect(revokePreview).toHaveBeenCalledExactlyOnceWith(PREVIEW_URL);
    });
});

describe('ImageUploadFieldComponent crop selection during export', () => {
    beforeEach(mockNativeDialogMethods);

    it('keeps the confirmed crop fixed while encoding and still lets the user cancel', async () => {
        const { fixture, imageUploadService } = await setupImageUploadFieldAsync();
        const { dialog, apply, surface } = await openCropperAsync(fixture, 'pressed-crop.png');
        const selection = requireElement(surface, '[role="group"]', HTMLElement);
        const cancel = requireElement(dialog, '.image-upload-field__cropper-actions fd-ui-button:first-child button', HTMLButtonElement);
        const moveRight = (): void => {
            selection.dispatchEvent(new KeyboardEvent('keydown', { key: 'ArrowRight', bubbles: true, cancelable: true }));
            fixture.detectChanges();
        };
        selection.focus();
        const initialLeft = selection.style.left;
        moveRight();
        const confirmedLeft = selection.style.left;
        expect(confirmedLeft).not.toBe(initialLeft);

        vi.spyOn(HTMLCanvasElement.prototype, 'getContext').mockReturnValue({ drawImage: vi.fn() } as unknown as CanvasRenderingContext2D);
        const toBlob = vi.spyOn(HTMLCanvasElement.prototype, 'toBlob').mockImplementation(() => {});
        Object.defineProperties(surface, { setPointerCapture: { value: vi.fn() }, releasePointerCapture: { value: vi.fn() } });
        apply.click();
        fixture.detectChanges();
        expect(toBlob).toHaveBeenCalledOnce();
        expect(apply.disabled).toBe(true);
        expect(cancel.disabled).toBe(false);

        selection.focus();
        moveRight();
        const afterKeyboard = selection.style.left;
        const pointerId = 7;
        selection.dispatchEvent(new PointerEvent('pointerdown', { bubbles: true, pointerId, clientX: 100, clientY: 80 }));
        surface.dispatchEvent(new PointerEvent('pointermove', { pointerId, clientX: 120, clientY: 80 }));
        surface.dispatchEvent(new PointerEvent('pointerup', { pointerId }));
        fixture.detectChanges();
        const afterPointer = selection.style.left;

        cancel.click();
        fixture.detectChanges();
        toBlob.mock.calls[0][0](new Blob(['cancelled crop'], { type: 'image/png' }));

        expect(dialog.open).toBe(false);
        expect(imageUploadService.upload).not.toHaveBeenCalled();
        expect({ afterKeyboard, afterPointer }).toEqual({ afterKeyboard: confirmedLeft, afterPointer: confirmedLeft });
    });
});

describe('ImageUploadFieldComponent dimensionless crop preview', () => {
    beforeEach(mockNativeDialogMethods);

    it('rejects a loaded image without an intrinsic width and preserves the saved image', async () => {
        const { component, fixture, imageUploadService } = await setupImageUploadFieldAsync();
        fixture.componentRef.setInput('cropEnabled', true);
        fixture.componentRef.setInput('acceptedTypes', 'image/svg+xml');
        const saved = imageSelection('https://example.test/avatar.png', 'saved-avatar');
        component.value.set(saved);
        fixture.detectChanges();
        const host = fixture.nativeElement as HTMLElement;
        const svg = '<svg xmlns="http://www.w3.org/2000/svg" width="0" height="128"></svg>';
        selectFiles(host, new File([svg], 'empty-width.svg', { type: 'image/svg+xml' }));
        const preview = await vi.waitFor(() => {
            fixture.detectChanges();
            return requireElement(host, 'dialog[open] img', HTMLImageElement);
        });
        Object.defineProperties(preview, { naturalWidth: { value: 0 }, naturalHeight: { value: 128 } });

        preview.dispatchEvent(new Event('load'));
        fixture.detectChanges();

        expect(host.querySelector('dialog')).toBeNull();
        expect(host.querySelector('[role="alert"]')?.textContent.trim()).toBe('IMAGE_UPLOAD_FIELD.ERRORS.READ_FAILED');
        expect(component.value()).toEqual(saved);
        expect(imageUploadService.upload).not.toHaveBeenCalled();
    });
});

describe('ImageUploadFieldComponent reading crop files', () => {
    beforeEach(mockNativeDialogMethods);

    it('reports a native file read error and leaves the saved image in place', async () => {
        const { component, fixture, imageUploadService } = await setupImageUploadFieldAsync();
        const { host, readers } = captureCropReads(fixture);
        const saved = imageSelection('https://example.test/saved.png', 'saved-avatar');
        const failed = vi.fn();
        const revokePreview = vi.spyOn(URL, 'revokeObjectURL');
        component.value.set(saved);
        component.imagePreparationFailed.subscribe(failed);
        fixture.detectChanges();

        selectFiles(host, new File(['source photo'], 'photo.png', { type: 'image/png' }));
        expect(readers).toHaveLength(1);
        readers.at(0)?.dispatchEvent(new ProgressEvent('error'));
        fixture.detectChanges();

        expect(host.querySelector('[role="alert"]')?.textContent.trim()).toBe('IMAGE_UPLOAD_FIELD.ERRORS.READ_FAILED');
        expect(host.querySelector('dialog')).toBeNull();
        expect(component.value()).toEqual(saved);
        expect(failed).toHaveBeenCalledOnce();
        expect(revokePreview).toHaveBeenCalledWith(PREVIEW_URL);
        expect(imageUploadService.upload).not.toHaveBeenCalled();
    });

    it.each(['load', 'error'] as const)('keeps the newer crop open after a stale read emits %s', async event => {
        const { fixture, imageUploadService } = await setupImageUploadFieldAsync();
        const { host, readers } = captureCropReads(fixture);
        selectFiles(host, new File(['first photo'], 'first.png', { type: 'image/png' }));
        selectFiles(host, new File(['second photo'], 'second.png', { type: 'image/png' }));
        const [first, second] = readers;
        expect(readers).toHaveLength(2);

        completeCropRead(second, 'c2Vjb25k');
        await vi.waitFor(() => {
            fixture.detectChanges();
            expect(host.querySelector('dialog[open] img')?.getAttribute('src')).toBe('data:image/png;base64,c2Vjb25k');
        });
        if (event === 'load') {
            completeCropRead(first, 'Zmlyc3Q=');
        } else {
            first.dispatchEvent(new ProgressEvent('error'));
        }
        fixture.detectChanges();

        expect(host.querySelector('dialog[open] img')?.getAttribute('src')).toBe('data:image/png;base64,c2Vjb25k');
        expect(host.querySelector('[role="alert"]')).toBeNull();
        expect(imageUploadService.upload).not.toHaveBeenCalled();
    });
});

describe('ImageUploadFieldComponent crop preparation after disabling', () => {
    beforeEach(mockNativeDialogMethods);

    it('discards the pending crop read if the owner disables the field before it completes', async () => {
        const { component, fixture, imageUploadService } = await setupImageUploadFieldAsync();
        const { host, readers } = captureCropReads(fixture);
        const saved = imageSelection('https://example.test/saved.png', 'saved-avatar');
        const failed = vi.fn<() => void>();
        const revokePreview = vi.spyOn(URL, 'revokeObjectURL');
        component.value.set(saved);
        component.imagePreparationFailed.subscribe(failed);
        fixture.detectChanges();

        selectFiles(host, new File(['old'], 'old.png', { type: 'image/png' }));
        expect(readers).toHaveLength(1);
        fixture.componentRef.setInput('disabled', true);
        fixture.detectChanges();
        completeCropRead(readers[0], 'b2xk');
        await fixture.whenStable();
        fixture.detectChanges();

        expect(host.querySelector('dialog[open]')).toBeNull();
        expect(host.querySelector('[role="alert"]')).toBeNull();
        expect(component.value()).toEqual(saved);
        expect(revokePreview).toHaveBeenCalledWith(PREVIEW_URL);
        expect(failed).toHaveBeenCalledOnce();
        expect(imageUploadService.upload).not.toHaveBeenCalled();

        fixture.componentRef.setInput('disabled', false);
        fixture.detectChanges();
        selectFiles(host, new File(['new'], 'new.png', { type: 'image/png' }));
        expect(readers).toHaveLength(2);
        completeCropRead(readers[1], 'bmV3');
        await vi.waitFor(() => {
            fixture.detectChanges();
            expect(host.querySelector('dialog[open] img')?.getAttribute('src')).toBe('data:image/png;base64,bmV3');
        });
    });
});

describe('ImageUploadFieldComponent replaced crop preview', () => {
    beforeEach(mockNativeDialogMethods);

    it('keeps crop controls unavailable when only the replaced image has decoded', async () => {
        const { fixture, imageUploadService } = await setupImageUploadFieldAsync();
        const { host, readers } = captureCropReads(fixture);
        selectFiles(host, new File(['old'], 'old.png', { type: 'image/png' }));
        completeCropRead(readers[0], 'b2xk');
        await vi.waitFor(() => {
            fixture.detectChanges();
            expect(host.querySelector('dialog[open] img')).not.toBeNull();
        });
        const oldImage = requireElement(host, 'dialog[open] img', HTMLImageElement);
        const surface = requireElement(host, '.image-upload-field__cropper-body', HTMLDivElement);
        Object.defineProperties(oldImage, {
            naturalWidth: { value: RESIZED_IMAGE_SIZE.width },
            naturalHeight: { value: RESIZED_IMAGE_SIZE.height },
        });
        vi.spyOn(surface, 'getBoundingClientRect').mockReturnValue(new DOMRect(0, 0, CROP_SURFACE_SIZE.width, CROP_SURFACE_SIZE.height));

        selectFiles(host, new File(['new'], 'new.png', { type: 'image/png' }));
        expect(readers).toHaveLength(2);
        oldImage.dispatchEvent(new Event('load'));
        completeCropRead(readers[1], 'bmV3');
        fixture.detectChanges();

        const dialog = requireElement(host, 'dialog[open]', HTMLDialogElement);
        expect(dialog.querySelector('img')?.getAttribute('src')).toBe('data:image/png;base64,bmV3');
        expect(dialog.querySelector('.image-upload-field__crop-selection')).toBeNull();
        expect(
            requireElement(dialog, '.image-upload-field__cropper-actions fd-ui-button:last-child button', HTMLButtonElement).disabled,
        ).toBe(true);
        expect(host.querySelector('[role="alert"]')).toBeNull();
        expect(imageUploadService.upload).not.toHaveBeenCalled();
    });
});

describe('ImageUploadFieldComponent crop decode after cancel', () => {
    beforeEach(mockNativeDialogMethods);

    it('opens a fresh picker while decoding and only enables the current crop', async () => {
        const { component, fixture, imageUploadService } = await setupImageUploadFieldAsync();
        const { host, readers } = captureCropReads(fixture);
        const saved = imageSelection('https://example.test/saved.png', 'saved-photo');
        component.value.set(saved);
        fixture.detectChanges();

        selectFiles(host, new File(['first photo'], 'first.png', { type: 'image/png' }));
        completeCropRead(readers[0], 'Zmlyc3Q=');
        const first = await vi.waitFor(() => {
            fixture.detectChanges();
            return requireElement(host, 'dialog[open]', HTMLDialogElement);
        });
        const firstImage = requireElement(first, 'img', HTMLImageElement);
        expect(first.querySelector('.image-upload-field__crop-selection')).toBeNull();
        first.dispatchEvent(new Event('cancel', { cancelable: true }));
        firstImage.dispatchEvent(new Event('load'));
        fixture.detectChanges();

        expect(first.open).toBe(false);
        expect(host.querySelector('dialog')).toBeNull();
        expect(host.querySelector('[role="alert"]')).toBeNull();

        selectFiles(host, new File(['second photo'], 'second.png', { type: 'image/png' }));
        completeCropRead(readers[1], 'c2Vjb25k');
        fixture.detectChanges();
        const current = requireElement(host, 'dialog[open]', HTMLDialogElement);
        const apply = requireElement(current, '.image-upload-field__cropper-actions fd-ui-button:last-child button', HTMLButtonElement);
        expect(apply.disabled).toBe(true);

        const body = requireElement(current, '.image-upload-field__cropper-body', HTMLDivElement);
        vi.spyOn(body, 'getBoundingClientRect').mockReturnValue(new DOMRect(0, 0, CROP_SURFACE_SIZE.width, CROP_SURFACE_SIZE.height));
        const image = requireElement(body, 'img', HTMLImageElement);
        Object.defineProperties(image, {
            naturalWidth: { value: RESIZED_IMAGE_SIZE.width },
            naturalHeight: { value: RESIZED_IMAGE_SIZE.height },
        });
        image.dispatchEvent(new Event('load'));
        fixture.detectChanges();

        expect(current.open).toBe(true);
        expect(current.querySelector('.image-upload-field__crop-selection')).not.toBeNull();
        expect(apply.disabled).toBe(false);
        expect(component.value()).toEqual(saved);
        expect(imageUploadService.upload).not.toHaveBeenCalled();
    });
});

function completeCropRead(reader: FileReader, content: string): void {
    Object.defineProperty(reader, 'result', { value: `data:image/png;base64,${content}` });
    reader.dispatchEvent(new ProgressEvent('load'));
}

function captureCropReads(fixture: ComponentFixture<ImageUploadFieldComponent>): { host: HTMLElement; readers: FileReader[] } {
    const readers: FileReader[] = [];
    vi.spyOn(FileReader.prototype, 'readAsDataURL').mockImplementation(function (this: FileReader): void {
        readers.push(this);
    });
    fixture.componentRef.setInput('cropEnabled', true);
    fixture.detectChanges();
    return { host: fixture.nativeElement as HTMLElement, readers };
}

describe('ImageUploadFieldComponent crop failures', () => {
    const cropUrl = 'data:image/png;base64,bm90LWFuLWltYWdl';

    it('closes a failed decode, preserves the saved selection and releases the preparation preview', async () => {
        const { component, fixture, imageUploadService, translateService } = await setupImageUploadFieldAsync();
        const previousSelection = imageSelection('https://example.com/saved-avatar.png', 'saved-avatar');
        const failed = vi.fn();
        const revokePreview = vi.spyOn(URL, 'revokeObjectURL');
        vi.spyOn(translateService, 'instant').mockReturnValue('Could not read image');
        component.imagePreparationFailed.subscribe(failed);
        component.value.set(previousSelection);
        fixture.detectChanges();
        component['emitPreparationPreview'](new File(['broken'], 'broken.png', { type: 'image/png' }));
        component['cropPreviewUrl'].set(cropUrl);
        component['isCropping'].set(true);
        const image = document.createElement('img');
        image.setAttribute('src', cropUrl);

        component['onCropImageFailed'](image);
        fixture.detectChanges();

        expect(component.value()).toEqual(previousSelection);
        expect(component['selection']()).toEqual(previousSelection);
        expect(component['isCropping']()).toBe(false);
        expect(component['cropPreviewUrl']()).toBeNull();
        expect((fixture.nativeElement as HTMLElement).querySelector('[role="alert"]')?.textContent).toContain('Could not read image');
        expect(failed).toHaveBeenCalledOnce();
        expect(revokePreview).toHaveBeenCalledWith(PREVIEW_URL);
        expect(imageUploadService.upload).not.toHaveBeenCalled();
    });

    it('ignores a delayed decode error from a replaced crop image', async () => {
        const { component } = await setupImageUploadFieldAsync();
        component['cropPreviewUrl'].set(cropUrl);
        component['isCropping'].set(true);
        const oldImage = document.createElement('img');
        oldImage.setAttribute('src', 'data:image/png;base64,old');

        component['onCropImageFailed'](oldImage);

        expect(component['isCropping']()).toBe(true);
        expect(component['cropPreviewUrl']()).toBe(cropUrl);
        expect(component['error']()).toBeNull();
    });

    it('prevents confirmation until a decoded image and crop bounds are ready', async () => {
        const { component, imageUploadService } = await setupImageUploadFieldAsync();
        component['isCropping'].set(true);

        component['confirmCrop']();

        expect(component['canConfirmCrop']()).toBe(false);
        expect(component['isProcessingCrop']()).toBe(false);
        expect(imageUploadService.upload).not.toHaveBeenCalled();
    });
});

describe('ImageUploadFieldComponent crop preview without layout', () => {
    beforeEach(mockNativeDialogMethods);

    it('keeps Apply disabled if the decoded image has no visible area to crop', async () => {
        const { fixture, component, imageUploadService } = await setupImageUploadFieldAsync();
        const { host, readers } = captureCropReads(fixture);
        const saved = imageSelection('https://example.test/saved.png', 'saved-photo');
        component.value.set(saved);
        fixture.detectChanges();
        selectFiles(host, new File(['source photo'], 'source.png', { type: 'image/png' }));
        expect(readers).toHaveLength(1);
        const [reader] = readers;
        Object.defineProperty(reader, 'result', { value: 'data:image/png;base64,c291cmNl' });
        reader.dispatchEvent(new ProgressEvent('load'));
        await vi.waitFor(() => {
            fixture.detectChanges();
            expect(host.querySelector('dialog[open]')).not.toBeNull();
        });

        const dialog = requireElement(host, 'dialog[open]', HTMLDialogElement);
        const surface = requireElement(dialog, '.image-upload-field__cropper-body', HTMLDivElement);
        const image = requireElement(surface, 'img', HTMLImageElement);
        Object.defineProperties(image, {
            naturalWidth: { value: RESIZED_IMAGE_SIZE.width },
            naturalHeight: { value: RESIZED_IMAGE_SIZE.height },
        });
        vi.spyOn(surface, 'getBoundingClientRect').mockReturnValue(new DOMRect());
        image.dispatchEvent(new Event('load'));
        fixture.detectChanges();

        const apply = requireElement(dialog, '.image-upload-field__cropper-actions fd-ui-button:last-child button', HTMLButtonElement);
        expect(apply.disabled).toBe(true);
        expect(dialog.querySelector('.image-upload-field__crop-selection')).toBeNull();
        apply.click();
        dialog.dispatchEvent(new Event('cancel', { cancelable: true }));
        fixture.detectChanges();

        expect(dialog.open).toBe(false);
        expect(component.value()).toEqual(saved);
        expect(imageUploadService.upload).not.toHaveBeenCalled();
    });
});

describe('ImageUploadFieldComponent native crop export', () => {
    beforeEach(mockNativeDialogMethods);

    it.each(['context', 'resize context', 'blob', 'encoding exception'] as const)(
        'shows a crop error and retains the saved image after a %s failure',
        async failure => {
            const { component, fixture, imageUploadService } = await setupImageUploadFieldAsync();
            const saved = imageSelection('https://example.test/saved-avatar.png', 'saved-avatar');
            const failed = vi.fn();
            component.value.set(saved);
            component.imagePreparationFailed.subscribe(failed);
            const ctx = failure === 'context' ? null : ({ drawImage: vi.fn() } as unknown as CanvasRenderingContext2D);
            const getContext = vi.spyOn(HTMLCanvasElement.prototype, 'getContext').mockReturnValue(ctx);
            if (failure === 'resize context') {
                const cropLimit = 256;
                fixture.componentRef.setInput('cropSize', null);
                fixture.componentRef.setInput('cropMaxSize', cropLimit);
                getContext.mockReturnValueOnce(ctx).mockReturnValueOnce(null);
            }
            const encode = vi.spyOn(HTMLCanvasElement.prototype, 'toBlob').mockImplementation(callback => {
                if (failure === 'encoding exception') {
                    throw new DOMException('Canvas export failed', 'SecurityError');
                }
                callback(null);
            });
            const revokePreview = vi.spyOn(URL, 'revokeObjectURL');
            const { dialog, apply } = await openCropperAsync(fixture, 'replacement.png');

            apply.click();
            fixture.detectChanges();

            if (failure === 'blob' || failure === 'encoding exception') {
                expect(encode).toHaveBeenCalledOnce();
            }
            const host = fixture.nativeElement as HTMLElement;
            expect(dialog.open).toBe(false);
            expect(host.querySelector('[role="alert"]')?.textContent.trim()).toBe('IMAGE_UPLOAD_FIELD.ERRORS.PROCESSING_FAILED');
            expect(host.querySelector('img.image-upload-field__preview')?.getAttribute('src')).toBe(saved.url);
            expect(component.value()).toEqual(saved);
            expect(failed).toHaveBeenCalledOnce();
            expect(revokePreview).toHaveBeenCalledWith(PREVIEW_URL);
            expect(imageUploadService.upload).not.toHaveBeenCalled();
        },
    );
});

describe('ImageUploadFieldComponent crop retry', () => {
    beforeEach(mockNativeDialogMethods);

    it('retries a capped crop after the smaller canvas could not allocate its context', async () => {
        const { component, fixture, imageUploadService } = await setupImageUploadFieldAsync();
        const saved = imageSelection('https://example.test/saved-avatar.png', 'saved-avatar');
        const cropLimit = 256;
        const sourceSize = 600;
        component.value.set(saved);
        fixture.componentRef.setInput('cropSize', null);
        fixture.componentRef.setInput('cropMaxSize', cropLimit);
        const ctx = { drawImage: vi.fn(), fillRect: vi.fn() } as unknown as CanvasRenderingContext2D;
        const canvases: number[] = [];
        let failDownscale = true;
        vi.spyOn(HTMLCanvasElement.prototype, 'getContext').mockImplementation(function (this: HTMLCanvasElement) {
            canvases.push(this.width);
            return failDownscale && this.width === cropLimit ? null : ctx;
        });
        const uploaded = new Blob(['cropped PNG'], { type: 'image/png' });
        const encoded = vi.spyOn(HTMLCanvasElement.prototype, 'toBlob').mockImplementation(callback => {
            callback(uploaded);
        });
        const host = fixture.nativeElement as HTMLElement;
        const first = await openCropperAsync(fixture, 'first.png');
        first.apply.click();
        fixture.detectChanges();

        expect(canvases).toEqual([sourceSize, cropLimit]);
        expect(host.querySelector('[role="alert"]')?.textContent.trim()).toBe('IMAGE_UPLOAD_FIELD.ERRORS.PROCESSING_FAILED');
        expect(first.dialog.open).toBe(false);
        expect(component.value()).toEqual(saved);
        expect(encoded).not.toHaveBeenCalled();
        expect(imageUploadService.upload).not.toHaveBeenCalled();

        failDownscale = false;
        const second = await openCropperAsync(fixture, 'second.png');
        second.apply.click();
        fixture.detectChanges();

        expect(canvases).toEqual([sourceSize, cropLimit, sourceSize, cropLimit]);
        expect(host.querySelector('[role="alert"]')).toBeNull();
        expect(second.dialog.open).toBe(false);
        expect(imageUploadService.upload).toHaveBeenCalledExactlyOnceWith(
            expect.objectContaining({ name: 'second.png', size: uploaded.size, type: 'image/png' }),
        );
        expect(component.value()).toEqual(imageSelection('https://cdn.example.com/image.jpg', 'asset-1'));
    });
});

describe('ImageUploadFieldComponent JPEG crop dimensions', () => {
    beforeEach(mockNativeDialogMethods);

    it.each([
        { maximum: 256, expectedSize: 256 },
        { maximum: 1024, expectedSize: 600 },
    ])('uses the JPEG crop dimensions subject to the $maximum pixel cap', async ({ maximum, expectedSize }) => {
        const { fixture, imageUploadService } = await setupImageUploadFieldAsync();
        fixture.componentRef.setInput('cropSize', null);
        fixture.componentRef.setInput('cropMaxSize', maximum);
        const drawImage = vi.fn<CanvasRenderingContext2D['drawImage']>();
        const fillRect = vi.fn<CanvasRenderingContext2D['fillRect']>();
        vi.spyOn(HTMLCanvasElement.prototype, 'getContext').mockReturnValue({ drawImage, fillRect } as unknown as CanvasRenderingContext2D);
        const blob = new Blob(['cropped photo'], { type: 'image/jpeg' });
        const exported = vi.fn<(width: number, height: number, type: string | undefined) => void>();
        vi.spyOn(HTMLCanvasElement.prototype, 'toBlob').mockImplementation(function (this: HTMLCanvasElement, callback, type) {
            exported(this.width, this.height, type);
            callback(blob);
        });
        const { dialog, apply } = await openCropperAsync(fixture, 'portrait.jpg', 'image/jpeg');

        apply.click();
        fixture.detectChanges();

        const sourceSize = 600;
        expect(fillRect).toHaveBeenCalledWith(0, 0, sourceSize, sourceSize);
        expect(exported).toHaveBeenCalledExactlyOnceWith(expectedSize, expectedSize, 'image/jpeg');
        expect(imageUploadService.upload).toHaveBeenCalledExactlyOnceWith(
            expect.objectContaining({ name: 'portrait.jpg', size: blob.size, type: 'image/jpeg' }),
        );
        expect(dialog.open).toBe(false);
    });
});
