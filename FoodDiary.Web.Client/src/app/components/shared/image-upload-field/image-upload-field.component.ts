
import {
    ChangeDetectionStrategy,
    ChangeDetectorRef,
    Component,
    HostListener,
    ViewEncapsulation,
    forwardRef,
    inject,
    input,
    output,
} from '@angular/core';
import { ControlValueAccessor, NG_VALUE_ACCESSOR } from '@angular/forms';
import { finalize, map, switchMap } from 'rxjs/operators';
import { FdUiButtonComponent } from 'fd-ui-kit/button/fd-ui-button.component';
import { ImageUploadService } from '../../../services/image-upload.service';
import { ImageSelection } from '../../../types/image-upload.data';
import Cropper from 'cropperjs';

@Component({
    selector: 'fd-image-upload-field',
    standalone: true,
    imports: [FdUiButtonComponent],
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

    public readonly label = input<string>('Image');
    public readonly description = input<string>();
    public readonly recommendedSize = input<string>('2160 x 1080');
    public readonly maxSizeMb = input<number>(20);
    public readonly acceptedTypes = input<string>('image/jpeg,image/png,image/webp,image/gif');
    public readonly cropEnabled = input<boolean>(false);
    public readonly cropSize = input<number>(512);
    public readonly cropAspectRatio = input<number>(1);
    public readonly deleteOnClear = input<boolean>(false);

    public readonly imageChanged = output<ImageSelection | null>();

    public selection: ImageSelection = { url: null, assetId: null };
    public isDragging = false;
    public isUploading = false;
    public error: string | null = null;
    public disabled = false;
    public isCropping = false;
    public cropPreviewUrl: string | null = null;
    private cropper: Cropper | null = null;
    private originalFile: File | null = null;

    private onChange: (value: ImageSelection | null) => void = () => {};
    private onTouched: () => void = () => {};

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
        const file = event.dataTransfer?.files?.[0];
        if (file) {
            this.handleIncomingFile(file);
        }
    }

    @HostListener('dragover', ['$event'])
    public onDragOver(event: DragEvent): void {
        event.preventDefault();
        if (this.disabled || this.isUploading) {
            return;
        }
        this.isDragging = true;
    }

    @HostListener('dragleave', ['$event'])
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
                error: err => console.warn('Failed to delete orphan image asset', err),
            });
        }
    }

    public onCropperImageLoaded(img: HTMLImageElement): void {
        this.destroyCropper();
        this.cropper = new Cropper(img, {
            aspectRatio: this.cropAspectRatio(),
            viewMode: 1,
            background: false,
            autoCropArea: 1,
            movable: true,
            scalable: false,
            zoomable: true,
            rotatable: false,
        });
    }

    public cancelCrop(): void {
        this.isCropping = false;
        this.destroyCropper();
        this.clearCropState();
        this.cdr.markForCheck();
    }

    public confirmCrop(): void {
        if (!this.cropper) {
            return;
        }

        const canvas = this.cropper.getCroppedCanvas({
            width: this.cropSize(),
            height: this.cropSize(),
            fillColor: '#fff',
        });

        canvas.toBlob(blob => {
            if (!blob) {
                this.error = 'Image processing failed. Please try again.';
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

    private handleIncomingFile(file: File): void {
        if (this.disabled || this.isUploading) {
            return;
        }

        this.error = null;
        if (!file.type.startsWith('image/')) {
            this.error = 'Only image files are allowed.';
            this.cdr.markForCheck();
            return;
        }

        const maxBytes = this.maxSizeMb() * 1024 * 1024;
        if (!this.cropEnabled() && file.size > maxBytes) {
            this.error = `File size exceeds ${this.maxSizeMb()} MB.`;
            this.cdr.markForCheck();
            return;
        }

        if (this.cropEnabled()) {
            this.startCropping(file);
        } else {
            this.uploadFile(file);
        }
    }

    private startCropping(file: File): void {
        this.originalFile = file;
        const reader = new FileReader();
        reader.onload = () => {
            this.cropPreviewUrl = typeof reader.result === 'string' ? reader.result : null;
            this.isCropping = !!this.cropPreviewUrl;
            this.cdr.markForCheck();
        };
        reader.onerror = () => {
            this.error = 'Could not read the image. Please try another file.';
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
                    this.imageUploadService.uploadToPresignedUrl(presign.uploadUrl, file).pipe(
                        map(() => ({ url: presign.fileUrl, assetId: presign.assetId })),
                    ),
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
                    this.error = 'Image upload failed. Please try again.';
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

    private clearCropState(): void {
        if (this.cropPreviewUrl) {
            this.cropPreviewUrl = null;
        }
        this.originalFile = null;
    }
}
