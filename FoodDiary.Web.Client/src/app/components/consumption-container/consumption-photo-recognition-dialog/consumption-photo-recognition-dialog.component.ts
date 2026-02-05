import { ChangeDetectionStrategy, Component, inject, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { TranslatePipe } from '@ngx-translate/core';
import { FormsModule } from '@angular/forms';
import { FdUiDialogComponent } from 'fd-ui-kit/dialog/fd-ui-dialog.component';
import { FdUiDialogFooterDirective } from 'fd-ui-kit/dialog/fd-ui-dialog-footer.directive';
import { FdUiButtonComponent } from 'fd-ui-kit/button/fd-ui-button.component';
import { FdUiLoaderComponent } from 'fd-ui-kit/loader/fd-ui-loader.component';
import { FdUiInputComponent } from 'fd-ui-kit/input/fd-ui-input.component';
import { ImageUploadFieldComponent } from '../../shared/image-upload-field/image-upload-field.component';
import { ImageSelection } from '../../../types/image-upload.data';
import { AiFoodService } from '../../../services/ai-food.service';
import { FoodVisionItem } from '../../../types/ai.data';
import { FdUiDialogRef } from 'fd-ui-kit/material';
import { catchError, of } from 'rxjs';

@Component({
    selector: 'fd-consumption-photo-recognition-dialog',
    standalone: true,
    templateUrl: './consumption-photo-recognition-dialog.component.html',
    styleUrls: ['./consumption-photo-recognition-dialog.component.scss'],
    changeDetection: ChangeDetectionStrategy.OnPush,
    imports: [
        CommonModule,
        FormsModule,
        TranslatePipe,
        FdUiDialogComponent,
        FdUiDialogFooterDirective,
        FdUiButtonComponent,
        FdUiLoaderComponent,
        FdUiInputComponent,
        ImageUploadFieldComponent,
    ],
})
export class ConsumptionPhotoRecognitionDialogComponent {
    private readonly aiFoodService = inject(AiFoodService);
    private readonly dialogRef = inject(
        FdUiDialogRef<ConsumptionPhotoRecognitionDialogComponent, FoodVisionItem[] | null>,
        { optional: true },
    );

    public readonly isLoading = signal(false);
    public readonly errorKey = signal<string | null>(null);
    public readonly results = signal<FoodVisionItem[]>([]);
    public readonly selection = signal<ImageSelection | null>(null);

    public getDisplayName(item: FoodVisionItem): string {
        return item.nameLocal?.trim() || item.nameEn;
    }

    public onImageChanged(selection: ImageSelection | null): void {
        this.selection.set(selection);
        this.errorKey.set(null);
        this.results.set([]);

        if (!selection?.assetId) {
            return;
        }

        this.isLoading.set(true);
        this.aiFoodService
            .analyzeFoodImage({ imageAssetId: selection.assetId })
            .pipe(
                catchError(err => {
                    if (err?.status === 403) {
                        this.errorKey.set('CONSUMPTION_MANAGE.PHOTO_AI_DIALOG.ERROR_PREMIUM');
                    } else {
                        this.errorKey.set('CONSUMPTION_MANAGE.PHOTO_AI_DIALOG.ERROR_GENERIC');
                    }
                    return of(null);
                }),
            )
            .subscribe(response => {
                this.isLoading.set(false);
                if (!response) {
                    return;
                }
                this.results.set(response.items ?? []);
            });
    }

    public close(): void {
        this.dialogRef?.close(null);
    }
}
