import { DOCUMENT } from '@angular/common';
import {
    afterRenderEffect,
    ChangeDetectionStrategy,
    Component,
    computed,
    DestroyRef,
    effect,
    type ElementRef,
    inject,
    Injector,
    input,
    linkedSignal,
    model,
    output,
    signal,
    viewChild,
} from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import type { FormValueControl } from '@angular/forms/signals';
import { TranslatePipe, TranslateService } from '@ngx-translate/core';
import { FdUiHintDirective } from 'fd-ui-kit';
import { FdUiButtonComponent } from 'fd-ui-kit/button/fd-ui-button';
import { finalize, firstValueFrom, Subject, takeUntil } from 'rxjs';

import { FrontendLoggerService } from '../../../services/frontend-logger.service';
import { injectImagePreviews } from '../../../shared/lib/image-owner-preview';
import { ImageUploadFacade } from '../../../shared/lib/image-upload.facade';
import type { ImageSelection } from '../../../shared/models/image-upload.data';
import { imageSelection } from '../../../shared/models/image-upload.data';
import { PrivacyConsentFacade } from '../../../shared/privacy/privacy-consent.facade';
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

type CropPreparation = {
    file: File;
    revision: number;
};

@Component({
    selector: 'fd-image-upload-field',
    host: {
        '(dragover)': 'onDragOver($event)',
        '(dragleave)': 'onDragLeave($event)',
    },
    imports: [FdUiButtonComponent, TranslatePipe, FdUiHintDirective],
    templateUrl: './image-upload-field.html',
    styleUrls: ['./image-upload-field.scss'],
    changeDetection: ChangeDetectionStrategy.OnPush,
})
export class ImageUploadFieldComponent implements FormValueControl<ImageSelection | null> {
    private readonly injector = inject(Injector);
    private readonly imageUploadFacade = inject(ImageUploadFacade);
    private readonly translateService = inject(TranslateService);
    private readonly logger = inject(FrontendLoggerService);
    private readonly document = inject(DOCUMENT);
    private readonly destroyRef = inject(DestroyRef);

    public readonly label = input<string>('Image');
    public readonly description = input<string>();
    public readonly requireHealthConsent = input(false);
    public readonly multiple = input(false);
    public readonly keepPrivate = input(false);
    public readonly maxFiles = input(1);
    public readonly selectText = input<string>();
    public readonly dropText = input<string>();
    public readonly imagesAdded = output<ImageSelection[]>();
    public readonly batchUploading = output<boolean>();
    public readonly showRecommendation = input(true);
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
    public readonly clearRequest = input(0);
    public readonly initialSelection = input<ImageSelection | null>(null);
    public readonly value = model<ImageSelection | null>(null);
    public readonly touched = model(false);
    public readonly disabled = input(false);
    public readonly fileSelectionLocked = input<() => boolean>((): boolean => false);
    public readonly appearance = input<'default' | 'compact' | 'preview' | 'step' | 'tile' | 'hidden'>('default');

    public readonly imageChanged = output<ImageSelection | null>();
    public readonly imagePreparationStarted = output<string>();
    public readonly imagePreparationFailed = output();
    public readonly hasErrorChange = output<boolean>();

    private readonly fileInputRef = viewChild<ElementRef<HTMLInputElement>>('fileInput');
    private readonly cropSurfaceRef = viewChild<ElementRef<HTMLDivElement>>('cropSurface');
    private readonly cropDialogRef = viewChild<ElementRef<HTMLDialogElement>>('cropDialog');
    protected readonly errorId = createImageUploadId('image-upload-error');
    protected readonly cropTitleId = createImageUploadId('image-upload-crop-title');
    protected readonly cropSubtitleId = createImageUploadId('image-upload-crop-subtitle');

    protected readonly selection = signal<ImageSelection>(imageSelection(null, null));
    private readonly imagePreviews = injectImagePreviews(computed(() => [{ ...this.selection(), requiresOwner: this.keepPrivate() }]));
    protected readonly previewUrl = computed(() => this.imagePreviews.urls()[0] ?? null);
    protected readonly hasSelection = computed(
        () => this.previewUrl() !== null || (this.keepPrivate() && this.selection().assetId !== null),
    );
    protected readonly isUploading = signal(false);
    private readonly isInputBlocked = computed(() => this.disabled() || this.isUploading() || this.fileSelectionLocked()());
    protected readonly isDragging = linkedSignal({
        source: this.isInputBlocked,
        computation: () => false,
    });
    protected readonly error = signal<string | null>(null);
    protected readonly isCropping = signal(false);
    protected readonly isProcessingCrop = signal(false);
    protected readonly cropPreviewUrl = signal<string | null>(null);
    protected readonly cropImageBounds = signal<CropRect | null>(null);
    protected readonly cropSelection = signal<CropRect | null>(null);
    private readonly editableCropGeometry = computed(() => {
        const bounds = this.cropImageBounds();
        const selection = this.cropSelection();
        return bounds !== null && selection !== null && !this.isProcessingCrop() ? { bounds, selection } : null;
    });
    protected readonly canConfirmCrop = computed(() => this.editableCropGeometry() !== null);
    private cropPreparation: CropPreparation | null = null;
    private cropImageElement: HTMLImageElement | null = null;
    private cropInteraction: CropInteraction | null = null;
    private pendingPreviewUrl: string | null = null;
    private handledClearRequest = 0;
    private uploadRevision = 0;
    private readonly uploadCancelled = new Subject<void>();
    private batchActive = false;

    protected readonly appearanceClass = computed(() => `image-upload-field--appearance-${this.appearance()}`);

    public constructor() {
        effect(() => {
            this.hasErrorChange.emit(this.error() !== null);
        });
        this.destroyRef.onDestroy(() => {
            this.cancelUpload();
            this.uploadCancelled.complete();
            this.cropDialogRef()?.nativeElement.close();
            this.clearPreparationPreview();
        });
        afterRenderEffect(() => {
            const dialog = this.cropDialogRef()?.nativeElement;
            if (dialog !== undefined && !dialog.open && this.isCropping()) {
                dialog.showModal();
            }
        });
        this.observeCropSurface();
        effect(() => {
            const value = this.value();
            if (value !== null) {
                this.selection.set(value);
                return;
            }

            const initial = this.initialSelection();
            if (this.hasInitialSelection(initial)) {
                const selection = imageSelection(initial.url ?? null, initial.assetId ?? null);
                this.selection.set(selection);
                this.imageChanged.emit(selection);
            }
        });
        effect(() => {
            const request = this.clearRequest();
            if (request === this.handledClearRequest) {
                return;
            }

            this.handledClearRequest = request;
            this.clearImage();
        });
    }

    private hasInitialSelection(initial: ImageSelection | null): initial is ImageSelection {
        return initial !== null && (initial.url !== null || initial.assetId !== null);
    }

    protected onFileSelected(fileInput: HTMLInputElement): void {
        void this.handleFilesAsync(fileInput.files);
        fileInput.value = '';
    }

    protected onDrop(event: DragEvent): void {
        event.preventDefault();
        event.stopPropagation();
        if (this.isInputBlocked()) {
            return;
        }
        this.isDragging.set(false);
        void this.handleFilesAsync(event.dataTransfer?.files);
    }

    private async handleFilesAsync(fileList: FileList | null | undefined): Promise<void> {
        if (this.isInputBlocked() || fileList?.length === undefined || fileList.length === 0) {
            return;
        }
        const files = Array.from(fileList);
        if (this.requireHealthConsent()) {
            const revision = ++this.uploadRevision;
            const privacy = this.injector.get(PrivacyConsentFacade);
            const allowed = await privacy.ensureHealthConsentAsync(true);
            if (!this.isCurrentUpload(revision) || this.isInputBlocked()) {
                return;
            }
            if (!allowed) {
                this.reportUploadConsentFailure(privacy);
                return;
            }
        }

        this.startSelectedFiles(files);
    }

    private startSelectedFiles(files: File[]): void {
        if (!this.multiple() || this.cropEnabled()) {
            void this.handleIncomingFileAsync(files[0]);
            return;
        }
        if (files.length > this.maxFiles()) {
            this.error.set(this.translateService.instant('IMAGE_UPLOAD_FIELD.ERRORS.TOO_MANY_FILES', { count: this.maxFiles() }));
            return;
        }
        void this.uploadBatchAsync(files);
    }

    private reportUploadConsentFailure(privacy: PrivacyConsentFacade): void {
        if (privacy.failed()) {
            this.error.set(this.translateService.instant(privacy.needsReload() ? 'PRIVACY_CHOICES.RELOAD' : 'PRIVACY_CHOICES.ERROR'));
        }
    }

    private async uploadBatchAsync(files: File[]): Promise<void> {
        const revision = ++this.uploadRevision;
        this.error.set(null);
        this.isUploading.set(true);
        this.batchActive = true;
        this.batchUploading.emit(true);
        const uploaded: ImageSelection[] = [];
        try {
            for (const file of files) {
                if (!this.isCurrentUpload(revision)) {
                    break;
                }
                if (!this.validateUploadType(file) || !this.validateUploadSize(file)) {
                    break;
                }
                const resized = await this.resizeFileIfNeededAsync(file);
                if (!this.isCurrentUpload(revision)) {
                    break;
                }
                const selection = await firstValueFrom(
                    this.uploadSelection(resized).pipe(takeUntil(this.uploadCancelled), takeUntilDestroyed(this.destroyRef)),
                    { defaultValue: null },
                );
                if (selection === null || !this.isCurrentUpload(revision)) {
                    break;
                }
                uploaded.push(selection);
            }
        } catch {
            if (this.isCurrentUpload(revision)) {
                this.error.set(this.translateService.instant('IMAGE_UPLOAD_FIELD.ERRORS.UPLOAD_FAILED'));
            }
        } finally {
            this.completeBatch(uploaded, revision);
        }
    }

    private completeBatch(uploaded: ImageSelection[], revision: number): void {
        if (this.isCurrentUpload(revision)) {
            this.batchActive = false;
            this.isUploading.set(false);
            this.imagesAdded.emit(uploaded);
            this.batchUploading.emit(false);
        }
    }

    private validateUploadType(file: File): boolean {
        if (!this.acceptedTypes().split(',').includes(file.type)) {
            this.error.set(this.translateService.instant('IMAGE_UPLOAD_FIELD.ERRORS.ONLY_IMAGES'));
            return false;
        }
        return true;
    }

    private validateUploadSize(file: File): boolean {
        if (file.size > getMaxImageUploadBytes(this.maxSizeMb())) {
            this.error.set(this.translateService.instant('IMAGE_UPLOAD_FIELD.ERRORS.FILE_TOO_LARGE', { maxSizeMb: this.maxSizeMb() }));
            return false;
        }
        return true;
    }

    protected onDragOver(event: DragEvent): void {
        event.preventDefault();
        if (this.isInputBlocked()) {
            return;
        }
        this.isDragging.set(true);
    }

    protected onDragLeave(event: DragEvent): void {
        event.preventDefault();
        this.isDragging.set(false);
    }

    protected clearImage(): void {
        this.cancelUpload();
        const assetId = this.selection().assetId;
        const selection: ImageSelection = imageSelection(null, null);
        this.selection.set(selection);
        this.error.set(null);
        this.value.set(selection);
        this.touched.set(true);
        this.imageChanged.emit(selection);
        this.isCropping.set(false);
        this.clearCropState();
        this.clearPreparationPreview();

        if (assetId !== null && this.deleteOnClear()) {
            this.imageUploadFacade.deleteAsset(assetId).subscribe({
                error: (err: unknown) => {
                    this.logger.warn('Failed to delete orphan image asset', err);
                },
            });
        }
    }

    protected onCropImageLoaded(img: HTMLImageElement, surface: HTMLDivElement): void {
        if (!this.isCurrentCropImage(img)) {
            return;
        }
        const { naturalWidth: imageWidth, naturalHeight: imageHeight } = img;
        if (imageWidth <= 0 || imageHeight <= 0) {
            this.failCrop('READ_FAILED');
            return;
        }
        this.cropImageElement = img;
        this.updateCropGeometry(surface, imageWidth, imageHeight);
    }

    protected onCropImageFailed(img: HTMLImageElement): void {
        if (this.isCurrentCropImage(img)) {
            this.failCrop('READ_FAILED');
        }
    }

    private isCurrentCropImage(img: HTMLImageElement): boolean {
        return this.isCropping() && img.getAttribute('src') === this.cropPreviewUrl();
    }

    protected onCropPointerDown(event: PointerEvent, mode: CropInteractionMode): void {
        const geometry = this.editableCropGeometry();
        if (geometry === null) {
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
            startRect: { ...geometry.selection },
        };
    }

    protected onCropPointerMove(event: PointerEvent): void {
        const interaction = this.cropInteraction;
        const bounds = this.cropImageBounds();
        if (bounds === null || event.pointerId !== interaction?.pointerId) {
            return;
        }

        event.preventDefault();
        const dx = event.clientX - interaction.startX;
        const dy = event.clientY - interaction.startY;
        this.cropSelection.set(
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
                  }),
        );
    }

    protected onCropPointerUp(event: PointerEvent): void {
        if (event.pointerId !== this.cropInteraction?.pointerId) {
            return;
        }

        this.cropSurfaceRef()?.nativeElement.releasePointerCapture(event.pointerId);
        this.cropInteraction = null;
    }

    protected onCropKeydown(event: KeyboardEvent): void {
        const geometry = this.editableCropGeometry();
        if (geometry === null) {
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
        this.cropSelection.set(moveCropSelection(geometry.selection, geometry.bounds, delta.dx, delta.dy));
    }

    protected cancelCrop(): void {
        this.cropDialogRef()?.nativeElement.close();
        this.isCropping.set(false);
        this.clearCropState();
        this.imagePreparationFailed.emit();
        this.clearPreparationPreview();
    }

    protected confirmCrop(): void {
        const geometry = this.editableCropGeometry();
        const preparation = this.cropPreparation;
        const image = this.cropImageElement;
        if (preparation === null || geometry === null || image === null) {
            return;
        }

        this.isProcessingCrop.set(true);
        this.endCropInteraction(this.cropSurfaceRef()?.nativeElement);
        try {
            const canvas = this.createCropUploadCanvas(geometry.selection, geometry.bounds, image, preparation.file.type === 'image/jpeg');
            if (canvas === null) {
                this.failCrop('PROCESSING_FAILED');
                return;
            }
            canvas.toBlob(blob => {
                this.completeCropUpload(blob, preparation);
            }, preparation.file.type);
        } catch {
            this.failCrop('PROCESSING_FAILED');
        }
    }

    public openFilePicker(): void {
        if (!this.isInputBlocked()) {
            this.fileInputRef()?.nativeElement.click();
        }
    }

    protected onZoneClick(fileInput: HTMLInputElement): void {
        if (this.isInputBlocked() || this.hasSelection()) {
            return;
        }
        fileInput.click();
    }

    protected onZoneKeydown(event: Event, fileInput: HTMLInputElement, preventDefault = false): void {
        if (event.target !== event.currentTarget) {
            return;
        }

        this.onZoneClick(fileInput);
        if (preventDefault) {
            event.preventDefault();
        }
    }

    protected onDeleteClick(event: Event): void {
        event.stopPropagation();
        if (this.isInputBlocked()) {
            return;
        }
        this.clearImage();
    }

    private async handleIncomingFileAsync(file: File): Promise<void> {
        this.error.set(null);
        if (!this.validateUploadType(file)) {
            return;
        }

        const revision = ++this.uploadRevision;
        this.emitPreparationPreview(file);

        if (this.cropEnabled()) {
            this.startCropping({ file, revision });
        } else {
            const uploadFile = await this.resizeFileIfNeededAsync(file);
            this.uploadFile(uploadFile, revision);
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

    private startCropping(preparation: CropPreparation): void {
        this.clearCropState();
        this.cropPreparation = preparation;
        const reader = new FileReader();
        const finishRead = (): void => {
            if (!this.isCurrentUpload(preparation.revision) || this.cropPreparation !== preparation) {
                return;
            }
            if (this.disabled()) {
                this.cancelCrop();
                return;
            }
            const previewUrl = reader.result;
            if (typeof previewUrl !== 'string') {
                this.failCrop('READ_FAILED');
                return;
            }
            this.cropPreviewUrl.set(previewUrl);
            this.isCropping.set(true);
        };
        reader.onload = finishRead;
        reader.onerror = finishRead;
        reader.readAsDataURL(preparation.file);
    }

    private uploadFile(file: File, revision: number): void {
        if (!this.isCurrentUpload(revision)) {
            return;
        }
        if (this.disabled()) {
            this.clearPreparationPreview();
            this.imagePreparationFailed.emit();
            return;
        }

        this.error.set(null);
        if (!this.validateUploadSize(file)) {
            this.imagePreparationFailed.emit();
            this.clearPreparationPreview();
            return;
        }

        this.isUploading.set(true);

        this.uploadSelection(file)
            .pipe(
                takeUntil(this.uploadCancelled),
                takeUntilDestroyed(this.destroyRef),
                finalize(() => {
                    if (this.isCurrentUpload(revision)) {
                        this.isUploading.set(false);
                    }
                }),
            )
            .subscribe({
                next: this.applyUploadedSelection.bind(this),
                error: () => {
                    this.error.set(this.translateService.instant('IMAGE_UPLOAD_FIELD.ERRORS.UPLOAD_FAILED'));
                    this.imagePreparationFailed.emit();
                    this.clearPreparationPreview();
                },
            });
    }

    private applyUploadedSelection(selection: ImageSelection): void {
        this.clearPreparationPreview();
        this.selection.set(selection);
        this.value.set(selection);
        this.touched.set(true);
        this.imageChanged.emit(selection);
    }

    private uploadSelection(file: File): ReturnType<ImageUploadFacade['upload']> {
        return this.keepPrivate() ? this.imageUploadFacade.upload(file, true) : this.imageUploadFacade.upload(file);
    }

    private isCurrentUpload(revision: number): boolean {
        return !this.destroyRef.destroyed && revision === this.uploadRevision;
    }

    private cancelUpload(): void {
        this.uploadRevision += 1;
        this.uploadCancelled.next();
        this.isUploading.set(false);
        if (this.batchActive) {
            this.batchActive = false;
            if (!this.destroyRef.destroyed) {
                this.batchUploading.emit(false);
            }
        }
    }

    private emitPreparationPreview(file: File): void {
        this.clearPreparationPreview();
        this.pendingPreviewUrl = URL.createObjectURL(file);
        this.imagePreparationStarted.emit(this.pendingPreviewUrl);
    }

    private clearPreparationPreview(): void {
        if (this.pendingPreviewUrl === null) {
            return;
        }

        URL.revokeObjectURL(this.pendingPreviewUrl);
        this.pendingPreviewUrl = null;
    }

    private createCropUploadCanvas(
        selection: CropRect,
        bounds: CropRect,
        image: HTMLImageElement,
        fillBackground: boolean,
    ): HTMLCanvasElement | null {
        const fixedSize = this.cropSize();
        const canvas = createCroppedCanvas({
            ownerDocument: this.document,
            image,
            selection,
            bounds,
            fixedSize,
            fillBackground,
        });
        return canvas === null ? null : this.resizeCropCanvasIfNeeded(canvas, fixedSize);
    }

    private completeCropUpload(blob: Blob | null, preparation: CropPreparation): void {
        if (!this.isCurrentUpload(preparation.revision) || !this.isCropping() || this.cropPreparation !== preparation) {
            return;
        }
        if (this.disabled()) {
            this.cancelCrop();
            return;
        }
        if (blob === null) {
            this.failCrop('PROCESSING_FAILED');
            return;
        }
        const croppedFile = new File([blob], preparation.file.name, { type: preparation.file.type });
        const dialog = this.cropDialogRef()?.nativeElement;
        this.isCropping.set(false);
        this.clearCropState();
        dialog?.close();
        this.uploadFile(croppedFile, preparation.revision);
    }

    private failCrop(errorKey: 'READ_FAILED' | 'PROCESSING_FAILED'): void {
        this.cancelCrop();
        this.error.set(this.translateService.instant(`IMAGE_UPLOAD_FIELD.ERRORS.${errorKey}`));
    }

    private observeCropSurface(): void {
        afterRenderEffect({
            read: onCleanup => {
                const surface = this.cropSurfaceRef()?.nativeElement;
                const observerConstructor = this.document.defaultView?.ResizeObserver;
                if (surface === undefined || observerConstructor === undefined) {
                    return;
                }

                const observer = new observerConstructor(() => {
                    const image = this.cropImageElement;
                    if (image !== null && this.isCurrentCropImage(image) && this.cropSurfaceRef()?.nativeElement === surface) {
                        this.updateCropGeometry(surface, image.naturalWidth, image.naturalHeight);
                    }
                });
                observer.observe(surface);
                onCleanup(() => {
                    observer.disconnect();
                });
            },
        });
    }

    private updateCropGeometry(surface: HTMLDivElement, imageWidth: number, imageHeight: number): void {
        const surfaceRect = surface.getBoundingClientRect();
        const bounds = calculateContainedImageBounds({
            surfaceWidth: surfaceRect.width,
            surfaceHeight: surfaceRect.height,
            imageWidth,
            imageHeight,
        });
        if (bounds === null) {
            return;
        }

        const previous = this.cropImageBounds();
        if (previous?.x === bounds.x && previous.y === bounds.y && previous.width === bounds.width && previous.height === bounds.height) {
            return;
        }

        const selection = this.cropSelection();
        this.cropImageBounds.set(bounds);
        if (previous === null || selection === null) {
            this.cropSelection.set(createInitialCropSelection(bounds, this.cropAspectRatio()));
            return;
        }

        this.endCropInteraction(surface);
        const scaleX = bounds.width / previous.width;
        const scaleY = bounds.height / previous.height;
        this.cropSelection.set({
            x: bounds.x + (selection.x - previous.x) * scaleX,
            y: bounds.y + (selection.y - previous.y) * scaleY,
            width: selection.width * scaleX,
            height: selection.height * scaleY,
        });
    }

    private endCropInteraction(surface: HTMLDivElement | undefined): void {
        const interaction = this.cropInteraction;
        this.cropInteraction = null;
        if (interaction !== null && surface?.hasPointerCapture(interaction.pointerId) === true) {
            surface.releasePointerCapture(interaction.pointerId);
        }
    }

    private resizeCropCanvasIfNeeded(canvas: HTMLCanvasElement, fixedSize: number | null): HTMLCanvasElement | null {
        if (fixedSize !== null && fixedSize > 0) {
            return canvas;
        }

        return resizeCanvasToMax({ ownerDocument: this.document, canvas, maxSize: this.cropMaxSize() });
    }

    private clearCropState(): void {
        this.isProcessingCrop.set(false);
        if (this.cropPreviewUrl() !== null) {
            this.cropPreviewUrl.set(null);
        }
        this.cropPreparation = null;
        this.cropImageElement = null;
        this.cropImageBounds.set(null);
        this.cropSelection.set(null);
        this.cropInteraction = null;
    }
}
