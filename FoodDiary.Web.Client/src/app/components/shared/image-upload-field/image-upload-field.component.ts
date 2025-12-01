import { CommonModule } from '@angular/common';
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

@Component({
    selector: 'fd-image-upload-field',
    standalone: true,
    imports: [CommonModule, FdUiButtonComponent],
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

    public readonly imageChanged = output<ImageSelection | null>();

    public selection: ImageSelection = { url: null, assetId: null };
    public isDragging = false;
    public isUploading = false;
    public error: string | null = null;
    public disabled = false;

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
            this.uploadFile(file);
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
            this.uploadFile(file);
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
        this.selection = { url: null, assetId: null };
        this.error = null;
        this.onChange(this.selection);
        this.onTouched();
        this.imageChanged.emit(this.selection);
        this.cdr.markForCheck();
    }

    private uploadFile(file: File): void {
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
        if (file.size > maxBytes) {
            this.error = `File size exceeds ${this.maxSizeMb()} MB.`;
            this.cdr.markForCheck();
            return;
        }

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
}
