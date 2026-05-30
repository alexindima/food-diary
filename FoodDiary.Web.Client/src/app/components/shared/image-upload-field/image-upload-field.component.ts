import { DOCUMENT, NgOptimizedImage } from '@angular/common';
import {
    ChangeDetectionStrategy,
    ChangeDetectorRef,
    Component,
    computed,
    effect,
    type ElementRef,
    inject,
    input,
    output,
    viewChild,
} from '@angular/core';
import { type ControlValueAccessor, NG_VALUE_ACCESSOR } from '@angular/forms';
import { TranslatePipe, TranslateService } from '@ngx-translate/core';
import { FdUiHintDirective } from 'fd-ui-kit';
import { FdUiButtonComponent } from 'fd-ui-kit/button/fd-ui-button.component';
import { finalize, map, switchMap } from 'rxjs';

import { FrontendLoggerService } from '../../../services/frontend-logger.service';
import { ImageUploadService } from '../../../shared/api/image-upload.service';
import type { ImageSelection } from '../../../shared/models/image-upload.data';
import {
    calculateImageResizeDimensions,
    canResizeImageType,
    createImageUploadId,
    getMaxImageUploadBytes,
} from './image-upload-field.utils';
import {
    calculateContainedImageBounds,
    createCroppedCanvas,
    createInitialCropSelection,
    type CropInteractionMode,
    type CropRect,
    moveCropSelection,
    resizeCanvasToMax,
    resizeCropSelection,
} from './image-upload-field-crop.utils';

const DEFAULT_MAX_SIZE_MB = 20;
const DEFAULT_CROP_SIZE = 512;
const DEFAULT_CROP_MAX_SIZE = 1024;
const DEFAULT_RESIZE_QUALITY = 0.86;
const MIN_CROP_SELECTION_SIZE = 48;
const CROP_KEYBOARD_FAST_STEP = 10;
const CROP_KEYBOARD_STEP = 1;

type CropInteraction = {
    mode: CropInteractionMode;
    pointerId: number;
    startX: number;
    startY: number;
    startRect: CropRect;
};

@Component({
    selector: 'fd-image-upload-field',
    host: {
        '(dragover)': 'onDragOver($event)',
        '(dragleave)': 'onDragLeave($event)',
    },
    imports: [NgOptimizedImage, FdUiButtonComponent, TranslatePipe, FdUiHintDirective],
    templateUrl: './image-upload-field.component.html',
    styleUrls: ['./image-upload-field.component.scss'],
    changeDetection: ChangeDetectionStrategy.OnPush,
    providers: [
        {
            provide: NG_VALUE_ACCESSOR,
            useExisting: ImageUploadFieldComponent,
            multi: true,
        },
    ],
})
export class ImageUploadFieldComponent implements ControlValueAccessor {
    private readonly cdr = inject(ChangeDetectorRef);
    private readonly imageUploadService = inject(ImageUploadService);
    private readonly translateService = inject(TranslateService);
    private readonly logger = inject(FrontendLoggerService);
    private readonly document = inject(DOCUMENT);

    public readonly label = input<string>('Image');
    public readonly description = input<string>();
    public readonly recommendedSize = input<string>('2160 x 1080');
    public readonly maxSizeMb = input<number>(DEFAULT_MAX_SIZE_MB);
    public readonly acceptedTypes = input<string>('image/jpeg,image/png,image/webp,image/gif');
    public readonly cropEnabled = input<boolean>(false);
    public readonly cropSize = input<number | null>(DEFAULT_CROP_SIZE);
    public readonly cropMaxSize = input<number>(DEFAULT_CROP_MAX_SIZE);
    public readonly cropAspectRatio = input<number | null>(1);
    public readonly resizeMaxDimension = input<number | null>(null);
    public readonly resizeQuality = input<number>(DEFAULT_RESIZE_QUALITY);
    public readonly deleteOnClear = input<boolean>(false);
    public readonly initialSelection = input<ImageSelection | null>(null);
    public readonly appearance = input<'default' | 'compact' | 'preview' | 'step' | 'hidden'>('default');

    public readonly imageChanged = output<ImageSelection | null>();

    private readonly fileInputRef = viewChild<ElementRef<HTMLInputElement>>('fileInput');
    private readonly cropSurfaceRef = viewChild<ElementRef<HTMLDivElement>>('cropSurface');
    protected readonly errorId = createImageUploadId('image-upload-error');
    protected readonly cropTitleId = createImageUploadId('image-upload-crop-title');
    protected readonly cropSubtitleId = createImageUploadId('image-upload-crop-subtitle');

    protected selection: ImageSelection = { url: null, assetId: null };
    protected isDragging = false;
    protected isUploading = false;
    protected error: string | null = null;
    protected disabled = false;
    protected isCropping = false;

    protected cropPreviewUrl: string | null = null;
    protected cropImageBounds: CropRect | null = null;
    protected cropSelection: CropRect | null = null;
    private originalFile: File | null = null;
    private cropImageElement: HTMLImageElement | null = null;
    private cropInteraction: CropInteraction | null = null;

    private onChange: (value: ImageSelection | null) => void = () => {};
    private onTouched: () => void = () => {};

    protected readonly appearanceClass = computed(() => `image-upload-field--appearance-${this.appearance()}`);

    public constructor() {
        effect(() => {
            const initial = this.initialSelection();
            if (this.hasInitialSelection(initial)) {
                this.selection = {
                    url: initial.url ?? null,
                    assetId: initial.assetId ?? null,
                };
                this.imageChanged.emit(this.selection);
                this.cdr.markForCheck();
            }
        });
    }

    private hasInitialSelection(initial: ImageSelection | null): initial is ImageSelection {
        return initial !== null && (initial.url !== null || initial.assetId !== null);
    }

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

    protected onFileSelected(event: Event): void {
        if (!(event.target instanceof HTMLInputElement)) {
            return;
        }

        const target = event.target;
        const file = target.files?.[0];
        if (file !== undefined) {
            this.handleIncomingFile(file);
        }
        target.value = '';
    }

    protected onDrop(event: DragEvent): void {
        event.preventDefault();
        event.stopPropagation();
        if (this.disabled || this.isUploading) {
            return;
        }
        this.isDragging = false;
        const file = event.dataTransfer?.files[0];
        if (file !== undefined) {
            this.handleIncomingFile(file);
        }
    }

    protected onDragOver(event: DragEvent): void {
        event.preventDefault();
        if (this.disabled || this.isUploading) {
            return;
        }
        this.isDragging = true;
    }

    protected onDragLeave(event: DragEvent): void {
        event.preventDefault();
        this.isDragging = false;
    }

    protected clearImage(): void {
        const assetId = this.selection.assetId;
        this.selection = { url: null, assetId: null };
        this.error = null;
        this.onChange(this.selection);
        this.onTouched();
        this.imageChanged.emit(this.selection);
        this.isCropping = false;
        this.clearCropState();
        this.cdr.markForCheck();

        if (this.deleteOnClear() && assetId !== null) {
            this.imageUploadService.deleteAsset(assetId).subscribe({
                error: (err: unknown) => {
                    this.logger.warn('Failed to delete orphan image asset', err);
                },
            });
        }
    }

    protected onCropImageLoaded(img: HTMLImageElement): void {
        this.cropImageElement = img;
        this.initializeCropSelection(img);
    }

    protected onCropPointerDown(event: PointerEvent, mode: CropInteractionMode): void {
        if (this.cropSelection === null || this.cropImageBounds === null) {
            return;
        }

        event.preventDefault();
        event.stopPropagation();
        const surface = this.cropSurfaceRef()?.nativeElement;
        surface?.setPointerCapture(event.pointerId);
        this.cropInteraction = {
            mode,
            pointerId: event.pointerId,
            startX: event.clientX,
            startY: event.clientY,
            startRect: { ...this.cropSelection },
        };
    }

    protected onCropPointerMove(event: PointerEvent): void {
        const interaction = this.cropInteraction;
        const bounds = this.cropImageBounds;
        if (bounds === null || event.pointerId !== interaction?.pointerId) {
            return;
        }

        event.preventDefault();
        const dx = event.clientX - interaction.startX;
        const dy = event.clientY - interaction.startY;
        this.cropSelection =
            interaction.mode === 'move'
                ? moveCropSelection(interaction.startRect, bounds, dx, dy)
                : resizeCropSelection({
                      mode: interaction.mode,
                      rect: interaction.startRect,
                      bounds,
                      dx,
                      dy,
                      aspectRatio: this.cropAspectRatio(),
                      minSize: MIN_CROP_SELECTION_SIZE,
                  });
        this.cdr.markForCheck();
    }

    protected onCropPointerUp(event: PointerEvent): void {
        if (event.pointerId !== this.cropInteraction?.pointerId) {
            return;
        }

        this.cropSurfaceRef()?.nativeElement.releasePointerCapture(event.pointerId);
        this.cropInteraction = null;
    }

    protected onCropKeydown(event: KeyboardEvent): void {
        if (this.cropSelection === null || this.cropImageBounds === null) {
            return;
        }

        const step = event.shiftKey ? CROP_KEYBOARD_FAST_STEP : CROP_KEYBOARD_STEP;
        const deltas: Partial<Record<string, { dx: number; dy: number }>> = {
            ArrowDown: { dx: 0, dy: step },
            ArrowLeft: { dx: -step, dy: 0 },
            ArrowRight: { dx: step, dy: 0 },
            ArrowUp: { dx: 0, dy: -step },
        };
        const delta = deltas[event.key];
        if (delta === undefined) {
            return;
        }

        event.preventDefault();
        this.cropSelection = moveCropSelection(this.cropSelection, this.cropImageBounds, delta.dx, delta.dy);
        this.cdr.markForCheck();
    }

    protected cancelCrop(): void {
        this.isCropping = false;
        this.clearCropState();
        this.cdr.markForCheck();
    }

    protected confirmCrop(): void {
        this.confirmCropInternal();
    }

    public openFilePicker(): void {
        this.fileInputRef()?.nativeElement.click();
    }

    protected onZoneClick(fileInput: HTMLInputElement): void {
        if (this.disabled || this.isUploading || this.selection.url !== null) {
            return;
        }
        fileInput.click();
    }

    protected onDeleteClick(event: Event): void {
        event.stopPropagation();
        this.clearImage();
    }

    protected handleIncomingFile(file: File): void {
        void this.handleIncomingFileAsync(file);
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

        if (this.cropEnabled()) {
            this.startCropping(file);
        } else {
            const uploadFile = await this.resizeFileIfNeededAsync(file);
            this.uploadFile(uploadFile);
        }
    }

    private async resizeFileIfNeededAsync(file: File): Promise<File> {
        const maxDimension = this.resizeMaxDimension();
        if (maxDimension === null || maxDimension <= 0 || !this.canResizeFile(file)) {
            return file;
        }

        try {
            const image = await this.loadImageAsync(file);
            const dimensions = calculateImageResizeDimensions(image.naturalWidth, image.naturalHeight, maxDimension);
            if (dimensions === null) {
                return file;
            }

            const canvas = this.document.createElement('canvas');
            canvas.width = dimensions.width;
            canvas.height = dimensions.height;
            const ctx = canvas.getContext('2d');
            if (ctx === null) {
                return file;
            }

            if (file.type === 'image/jpeg') {
                ctx.fillStyle = '#fff';
                ctx.fillRect(0, 0, dimensions.width, dimensions.height);
            }

            ctx.drawImage(image, 0, 0, dimensions.width, dimensions.height);
            const blob = await this.canvasToBlobAsync(canvas, file.type, this.resizeQuality());
            return new File([blob], file.name, { type: file.type, lastModified: file.lastModified });
        } catch (error) {
            this.logger.warn('Failed to resize image before upload', error);
            return file;
        }
    }

    private canResizeFile(file: File): boolean {
        return canResizeImageType(file.type);
    }

    private async loadImageAsync(file: File): Promise<HTMLImageElement> {
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

    private async canvasToBlobAsync(canvas: HTMLCanvasElement, type: string, quality: number): Promise<Blob> {
        return new Promise((resolve, reject) => {
            canvas.toBlob(
                blob => {
                    if (blob !== null) {
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
            this.isCropping = this.cropPreviewUrl !== null;
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
        if (file.size > getMaxImageUploadBytes(this.maxSizeMb())) {
            this.error = this.translateService.instant('IMAGE_UPLOAD_FIELD.ERRORS.FILE_TOO_LARGE', {
                maxSizeMb: this.maxSizeMb(),
            });
            this.cdr.markForCheck();
            return;
        }

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

    private confirmCropInternal(): void {
        if (this.cropImageElement === null || this.cropSelection === null || this.cropImageBounds === null) {
            return;
        }

        const fixedSize = this.cropSize();
        const canvas = createCroppedCanvas({
            ownerDocument: this.document,
            image: this.cropImageElement,
            selection: this.cropSelection,
            bounds: this.cropImageBounds,
            fixedSize,
            fillBackground: this.originalFile?.type === 'image/jpeg',
        });
        if (canvas === null) {
            this.error = this.translateService.instant('IMAGE_UPLOAD_FIELD.ERRORS.PROCESSING_FAILED');
            this.cdr.markForCheck();
            return;
        }

        const resizedCanvas = this.resizeCropCanvasIfNeeded(canvas, fixedSize);
        if (resizedCanvas === null) {
            this.error = this.translateService.instant('IMAGE_UPLOAD_FIELD.ERRORS.PROCESSING_FAILED');
            this.cdr.markForCheck();
            return;
        }

        resizedCanvas.toBlob((blob: Blob | null) => {
            if (blob === null) {
                this.error = this.translateService.instant('IMAGE_UPLOAD_FIELD.ERRORS.PROCESSING_FAILED');
                this.cdr.markForCheck();
                return;
            }

            const fileName = this.originalFile?.name ?? 'avatar.png';
            const croppedFile = new File([blob], fileName, { type: this.originalFile?.type ?? 'image/png' });
            this.isCropping = false;
            this.clearCropState();
            this.uploadFile(croppedFile);
        }, this.originalFile?.type ?? 'image/png');
    }

    private initializeCropSelection(img: HTMLImageElement): void {
        const surface = this.cropSurfaceRef()?.nativeElement;
        if (surface === undefined || img.naturalWidth <= 0 || img.naturalHeight <= 0) {
            return;
        }

        const surfaceRect = surface.getBoundingClientRect();
        const bounds = calculateContainedImageBounds({
            surfaceWidth: surfaceRect.width,
            surfaceHeight: surfaceRect.height,
            imageWidth: img.naturalWidth,
            imageHeight: img.naturalHeight,
        });
        if (bounds === null) {
            return;
        }

        this.cropImageBounds = bounds;
        this.cropSelection = createInitialCropSelection(bounds, this.cropAspectRatio());
        this.cdr.markForCheck();
    }

    private resizeCropCanvasIfNeeded(canvas: HTMLCanvasElement, fixedSize: number | null): HTMLCanvasElement | null {
        const maxSize = this.cropMaxSize();
        if (fixedSize !== null && fixedSize > 0) {
            return canvas;
        }

        if (maxSize <= 0 || (canvas.width <= maxSize && canvas.height <= maxSize)) {
            return canvas;
        }

        return resizeCanvasToMax({ ownerDocument: this.document, canvas, maxSize });
    }

    private clearCropState(): void {
        if (this.cropPreviewUrl !== null) {
            this.cropPreviewUrl = null;
        }
        this.originalFile = null;
        this.cropImageElement = null;
        this.cropImageBounds = null;
        this.cropSelection = null;
        this.cropInteraction = null;
    }
}
