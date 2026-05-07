import {
    ChangeDetectionStrategy,
    ChangeDetectorRef,
    Component,
    computed,
    effect,
    type ElementRef,
    forwardRef,
    inject,
    input,
    output,
    viewChild,
    ViewEncapsulation,
} from '@angular/core';
import { type ControlValueAccessor, NG_VALUE_ACCESSOR } from '@angular/forms';
import { TranslatePipe, TranslateService } from '@ngx-translate/core';
import type Cropper from 'cropperjs';
import { FdUiHintDirective } from 'fd-ui-kit';
import { FdUiButtonComponent } from 'fd-ui-kit/button/fd-ui-button.component';
import { finalize, map, switchMap } from 'rxjs/operators';

import { FrontendLoggerService } from '../../../services/frontend-logger.service';
import { ImageUploadService } from '../../../shared/api/image-upload.service';
import { type ImageSelection } from '../../../shared/models/image-upload.data';

@Component({
    selector: 'fd-image-upload-field',
    standalone: true,
    host: {
        '(dragover)': 'onDragOver($event)',
        '(dragleave)': 'onDragLeave($event)',
    },
    imports: [FdUiButtonComponent, TranslatePipe, FdUiHintDirective],
    templateUrl: './image-upload-field.component.html',
    styleUrls: ['./image-upload-field.component.scss'],
    changeDetection: ChangeDetectionStrategy.OnPush,
    encapsulation: ViewEncapsulation.None,
    providers: [
        {
            provide: NG_VALUE_ACCESSOR,
            useExisting: forwardRef(() => ImageUploadFieldComponent),
            multi: true,
        },
    ],
})
export class ImageUploadFieldComponent implements ControlValueAccessor {
    private readonly cdr = inject(ChangeDetectorRef);
    private readonly imageUploadService = inject(ImageUploadService);
    private readonly translateService = inject(TranslateService);
    private readonly logger = inject(FrontendLoggerService);

    public readonly label = input<string>('Image');
    public readonly description = input<string>();
    public readonly recommendedSize = input<string>('2160 x 1080');
    public readonly maxSizeMb = input<number>(20);
    public readonly acceptedTypes = input<string>('image/jpeg,image/png,image/webp,image/gif');
    public readonly cropEnabled = input<boolean>(false);
    public readonly cropSize = input<number | null>(512);
    public readonly cropMaxSize = input<number>(1024);
    public readonly cropAspectRatio = input<number | null>(1);
    public readonly resizeMaxDimension = input<number | null>(null);
    public readonly resizeQuality = input<number>(0.86);
    public readonly deleteOnClear = input<boolean>(false);
    public readonly initialSelection = input<ImageSelection | null>(null);
    public readonly appearance = input<'default' | 'compact' | 'preview' | 'step' | 'hidden'>('default');

    public readonly imageChanged = output<ImageSelection | null>();

    private readonly fileInputRef = viewChild<ElementRef<HTMLInputElement>>('fileInput');
    protected readonly errorId = ImageUploadFieldComponent.createId('image-upload-error');
    protected readonly cropTitleId = ImageUploadFieldComponent.createId('image-upload-crop-title');
    protected readonly cropSubtitleId = ImageUploadFieldComponent.createId('image-upload-crop-subtitle');

    public selection: ImageSelection = { url: null, assetId: null };
    public isDragging = false;
    public isUploading = false;
    public error: string | null = null;
    public disabled = false;
    public isCropping = false;

    private readonly initialSelectionEffect = effect(() => {
        const initial = this.initialSelection();
        if (initial?.url || initial?.assetId) {
            this.selection = {
                url: initial.url ?? null,
                assetId: initial.assetId ?? null,
            };
            this.imageChanged.emit(this.selection);
            this.cdr.markForCheck();
        }
    });
    public cropPreviewUrl: string | null = null;
    private cropper: Cropper | null = null;
    private originalFile: File | null = null;

    private onChange: (value: ImageSelection | null) => void = () => {};
    private onTouched: () => void = () => {};

    protected readonly appearanceClass = computed(() => `image-upload-field--appearance-${this.appearance()}`);

    public writeValue(value: ImageSelection | null): void {
        this.selection = value ?? { url: null, assetId: null };
        this.cdr.markForCheck();
    }

    public registerOnChange(fn: (value: ImageSelection | null) => void): void {
        this.onChange = fn;
    }

    public registerOnTouched(fn: () => void): void {
        this.onTouched = fn;
    }

    public setDisabledState(isDisabled: boolean): void {
        this.disabled = isDisabled;
        this.cdr.markForCheck();
    }

    public onFileSelected(event: Event): void {
        const target = event.target as HTMLInputElement;
        const file = target.files?.[0];
        if (file) {
            this.handleIncomingFile(file);
        }
        target.value = '';
    }

    public onDrop(event: DragEvent): void {
        event.preventDefault();
        event.stopPropagation();
        if (this.disabled || this.isUploading) {
            return;
        }
        this.isDragging = false;
        const file = event.dataTransfer?.files[0];
        if (file) {
            this.handleIncomingFile(file);
        }
    }

    public onDragOver(event: DragEvent): void {
        event.preventDefault();
        if (this.disabled || this.isUploading) {
            return;
        }
        this.isDragging = true;
    }

    public onDragLeave(event: DragEvent): void {
        event.preventDefault();
        this.isDragging = false;
    }

    public clearImage(): void {
        const assetId = this.selection.assetId;
        this.selection = { url: null, assetId: null };
        this.error = null;
        this.onChange(this.selection);
        this.onTouched();
        this.imageChanged.emit(this.selection);
        this.isCropping = false;
        this.destroyCropper();
        this.clearCropState();
        this.cdr.markForCheck();

        if (this.deleteOnClear() && assetId) {
            this.imageUploadService.deleteAsset(assetId).subscribe({
                error: err => {
                    this.logger.warn('Failed to delete orphan image asset', err);
                },
            });
        }
    }

    public async onCropperImageLoadedAsync(img: HTMLImageElement): Promise<void> {
        this.destroyCropper();
        const { default: CropperClass } = await import('cropperjs');
        this.cropper = new CropperClass(img, {});

        const selection = this.cropper.getCropperSelection();
        const aspectRatio = this.cropAspectRatio();
        if (selection && aspectRatio) {
            selection.aspectRatio = aspectRatio;
            selection.initialAspectRatio = aspectRatio;
        }
    }

    public cancelCrop(): void {
        this.isCropping = false;
        this.destroyCropper();
        this.clearCropState();
        this.cdr.markForCheck();
    }

    public confirmCrop(): void {
        void this.confirmCropAsync();
    }

    public openFilePicker(): void {
        this.fileInputRef()?.nativeElement.click();
    }

    protected previewAlt(): string {
        return this.translateService.instant('IMAGE_PREVIEW.ALT', { name: this.label() });
    }

    public onZoneClick(fileInput: HTMLInputElement): void {
        if (this.disabled || this.isUploading || this.selection.url) {
            return;
        }
        fileInput.click();
    }

    public onDeleteClick(event: Event): void {
        event.stopPropagation();
        this.clearImage();
    }

    public handleIncomingFile(file: File): void {
        void this.handleIncomingFileAsync(file);
    }

    private static createId(prefix: string): string {
        const cryptoLike = (globalThis as { crypto?: { randomUUID?: () => string } }).crypto;
        return `${prefix}-${cryptoLike?.randomUUID?.() ?? Math.random().toString(36).slice(2)}`;
    }

    private async handleIncomingFileAsync(file: File): Promise<void> {
        if (this.disabled || this.isUploading) {
            return;
        }

        this.error = null;
        if (!file.type.startsWith('image/')) {
            this.error = this.translateService.instant('IMAGE_UPLOAD_FIELD.ERRORS.ONLY_IMAGES');
            this.cdr.markForCheck();
            return;
        }

        const maxBytes = this.maxSizeMb() * 1024 * 1024;

        if (this.cropEnabled()) {
            this.startCropping(file);
        } else {
            const uploadFile = await this.resizeFileIfNeededAsync(file);
            if (uploadFile.size > maxBytes) {
                this.error = this.translateService.instant('IMAGE_UPLOAD_FIELD.ERRORS.FILE_TOO_LARGE', {
                    maxSizeMb: this.maxSizeMb(),
                });
                this.cdr.markForCheck();
                return;
            }

            this.uploadFile(uploadFile);
        }
    }

    private async resizeFileIfNeededAsync(file: File): Promise<File> {
        const maxDimension = this.resizeMaxDimension();
        if (!maxDimension || maxDimension <= 0 || !this.canResizeFile(file)) {
            return file;
        }

        try {
            const image = await this.loadImageAsync(file);
            const largestSide = Math.max(image.naturalWidth, image.naturalHeight);
            if (largestSide <= maxDimension) {
                return file;
            }

            const scale = maxDimension / largestSide;
            const targetWidth = Math.max(1, Math.round(image.naturalWidth * scale));
            const targetHeight = Math.max(1, Math.round(image.naturalHeight * scale));
            const canvas = document.createElement('canvas');
            canvas.width = targetWidth;
            canvas.height = targetHeight;
            const ctx = canvas.getContext('2d');
            if (!ctx) {
                return file;
            }

            if (file.type === 'image/jpeg') {
                ctx.fillStyle = '#fff';
                ctx.fillRect(0, 0, targetWidth, targetHeight);
            }

            ctx.drawImage(image, 0, 0, targetWidth, targetHeight);
            const blob = await this.canvasToBlobAsync(canvas, file.type, this.resizeQuality());
            return new File([blob], file.name, { type: file.type, lastModified: file.lastModified });
        } catch (err) {
            this.logger.warn('Failed to resize image before upload', err);
            return file;
        }
    }

    private canResizeFile(file: File): boolean {
        return ['image/jpeg', 'image/png', 'image/webp'].includes(file.type);
    }

    private loadImageAsync(file: File): Promise<HTMLImageElement> {
        return new Promise((resolve, reject) => {
            const url = URL.createObjectURL(file);
            const image = new Image();
            image.onload = (): void => {
                URL.revokeObjectURL(url);
                resolve(image);
            };
            image.onerror = (): void => {
                URL.revokeObjectURL(url);
                reject(new Error('Image load failed'));
            };
            image.src = url;
        });
    }

    private canvasToBlobAsync(canvas: HTMLCanvasElement, type: string, quality: number): Promise<Blob> {
        return new Promise((resolve, reject) => {
            canvas.toBlob(
                blob => {
                    if (blob) {
                        resolve(blob);
                    } else {
                        reject(new Error('Canvas export failed'));
                    }
                },
                type,
                quality,
            );
        });
    }

    private startCropping(file: File): void {
        this.originalFile = file;
        const reader = new FileReader();
        reader.onload = (): void => {
            this.cropPreviewUrl = typeof reader.result === 'string' ? reader.result : null;
            this.isCropping = !!this.cropPreviewUrl;
            this.cdr.markForCheck();
        };
        reader.onerror = (): void => {
            this.error = this.translateService.instant('IMAGE_UPLOAD_FIELD.ERRORS.READ_FAILED');
            this.cdr.markForCheck();
        };
        reader.readAsDataURL(file);
    }

    private uploadFile(file: File): void {
        if (this.disabled || this.isUploading) {
            return;
        }

        this.error = null;

        this.isUploading = true;
        this.cdr.markForCheck();

        this.imageUploadService
            .requestUploadUrl(file)
            .pipe(
                switchMap(presign =>
                    this.imageUploadService
                        .uploadToPresignedUrl(presign.uploadUrl, file)
                        .pipe(map(() => ({ url: presign.fileUrl, assetId: presign.assetId }))),
                ),
                finalize(() => {
                    this.isUploading = false;
                    this.cdr.markForCheck();
                }),
            )
            .subscribe({
                next: selection => {
                    this.selection = selection;
                    this.onChange(selection);
                    this.onTouched();
                    this.imageChanged.emit(selection);
                    this.cdr.markForCheck();
                },
                error: () => {
                    this.error = this.translateService.instant('IMAGE_UPLOAD_FIELD.ERRORS.UPLOAD_FAILED');
                    this.cdr.markForCheck();
                },
            });
    }

    private destroyCropper(): void {
        if (this.cropper) {
            this.cropper.destroy();
            this.cropper = null;
        }
    }

    private async confirmCropAsync(): Promise<void> {
        if (!this.cropper) {
            return;
        }

        const selection = this.cropper.getCropperSelection();
        if (!selection) {
            this.error = this.translateService.instant('IMAGE_UPLOAD_FIELD.ERRORS.PROCESSING_FAILED');
            this.cdr.markForCheck();
            return;
        }

        const fixedSize = this.cropSize();
        let canvas = await selection.$toCanvas(
            fixedSize
                ? {
                      width: fixedSize,
                      height: fixedSize,
                  }
                : undefined,
        );

        if (!fixedSize) {
            const maxSize = this.cropMaxSize();
            if (maxSize > 0 && (canvas.width > maxSize || canvas.height > maxSize)) {
                const scale = Math.min(maxSize / canvas.width, maxSize / canvas.height);
                const targetWidth = Math.max(1, Math.round(canvas.width * scale));
                const targetHeight = Math.max(1, Math.round(canvas.height * scale));
                const resized = document.createElement('canvas');
                resized.width = targetWidth;
                resized.height = targetHeight;
                const ctx = resized.getContext('2d');
                if (!ctx) {
                    this.error = this.translateService.instant('IMAGE_UPLOAD_FIELD.ERRORS.PROCESSING_FAILED');
                    this.cdr.markForCheck();
                    return;
                }
                ctx.fillStyle = 'var(--fd-color-white)';
                ctx.fillRect(0, 0, targetWidth, targetHeight);
                ctx.drawImage(canvas, 0, 0, targetWidth, targetHeight);
                canvas = resized;
            }
        }

        canvas.toBlob((blob: Blob | null) => {
            if (!blob) {
                this.error = this.translateService.instant('IMAGE_UPLOAD_FIELD.ERRORS.PROCESSING_FAILED');
                this.cdr.markForCheck();
                return;
            }

            const fileName = this.originalFile?.name ?? 'avatar.png';
            const croppedFile = new File([blob], fileName, { type: this.originalFile?.type || 'image/png' });
            this.isCropping = false;
            this.destroyCropper();
            this.clearCropState();
            this.uploadFile(croppedFile);
        }, this.originalFile?.type || 'image/png');
    }

    private clearCropState(): void {
        if (this.cropPreviewUrl) {
            this.cropPreviewUrl = null;
        }
        this.originalFile = null;
    }
}
