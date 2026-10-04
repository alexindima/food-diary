import { ChangeDetectionStrategy, Component, inject } from '@angular/core';
import { TranslatePipe } from '@ngx-translate/core';
import { FdUiDialogComponent } from 'fd-ui-kit/dialog/fd-ui-dialog';
import { FdUiDialogRef } from 'fd-ui-kit/dialog/fd-ui-dialog-ref';

import type { FoodRecognitionJob } from '../../../shared/models/food-recognition.data';
import { FoodRecognitionHistoryComponent } from './food-recognition-history';

@Component({
    selector: 'fd-food-recognition-history-dialog',
    templateUrl: './food-recognition-history-dialog.html',
    changeDetection: ChangeDetectionStrategy.OnPush,
    imports: [TranslatePipe, FdUiDialogComponent, FoodRecognitionHistoryComponent],
})
export class FoodRecognitionHistoryDialogComponent {
    protected readonly dialogRef = inject(FdUiDialogRef<FoodRecognitionHistoryDialogComponent, FoodRecognitionJob>);
}
